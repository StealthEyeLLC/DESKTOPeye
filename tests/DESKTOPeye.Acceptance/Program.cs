using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Acceptance;

internal static class Program
{
    const string ShellEyeTaskName="shelleye-kernel-dev";
    static readonly string[] TaskNames=[
        "StealthEye-DESKTOPeye-Build001-Session",
        "StealthEye-DESKTOPeye-Build001-Kernel",
        "StealthEye-DESKTOPeye-Build001-Fixture",
        "StealthEye-DESKTOPeye-Build001-Adversary"
    ];

    static async Task<int> Main(string[] args)
    {
        var command=args.FirstOrDefault()?.ToLowerInvariant()??"preflight";
        var repo=FindRepo();
        return command switch
        {
            "list"=>List(repo),
            "preflight"=>await Preflight(repo,false),
            "run"=>await Run(repo,args.Skip(1).ToArray()),
            _=>Usage()
        };
    }

    static int Usage(){Console.Error.WriteLine("usage: DESKTOPeye.Acceptance [list|preflight|run [--out PATH]]");return 2;}

    static int List(string repo)
    {
        var path=Path.Combine(repo,"tests","acceptance","manifests","hostile-50.json");
        using var doc=JsonDocument.Parse(File.ReadAllText(path));
        var cases=doc.RootElement.GetProperty("cases"); if(cases.GetArrayLength()!=50)throw new InvalidOperationException($"frozen hostile manifest expected 50 cases, got {cases.GetArrayLength()}");
        foreach(var g in cases.EnumerateArray())Console.WriteLine($"C{g.GetProperty("id").GetInt32():00}  {g.GetProperty("case").GetString()} => {g.GetProperty("requiredOutcome").GetString()}");
        Console.WriteLine("A  Persistent correspondence/recovery gate");
        Console.WriteLine("B  Retained/delta-first selective perception gate");
        Console.WriteLine("D  Canonical 60-call Node Program Host gate");
        return 0;
    }

    static async Task<int> Preflight(string repo,bool ensureTasks)
    {
        var explorers=Process.GetProcessesByName("explorer").Where(p=>p.SessionId>0).ToArray();
        if(explorers.Length!=1)
        {
            Emit(new{status="BLOCKED_INTERACTIVE",reason=explorers.Length==0?"no authenticated interactive Explorer desktop exists":"interactive desktop session is ambiguous",explorerSessions=explorers.Select(x=>x.SessionId).Distinct().ToArray(),requires=new[]{"authenticated StealthEye interactive session","WinSta0\\Default input desktop","SHELLeye process-authority task in that session","Session Host in that session"}});
            return 4;
        }
        var sessionId=explorers[0].SessionId;
        if(ensureTasks){await RunProcess("schtasks.exe",$"/Run /TN \"{ShellEyeTaskName}\"",10000);foreach(var task in TaskNames)await RunProcess("schtasks.exe",$"/Run /TN \"{task}\"",10000);}
        JsonElement? session=null;
        try
        {
            await using var rpc=new PipeRpcClient($"desktopeye-session-{sessionId}");await rpc.ConnectAsync(ensureTasks?12000:1200);
            session=await rpc.CallAsync("session.current",new{},2500);
        }
        catch(Exception ex)
        {
            Emit(new{status=ensureTasks?"RUNTIME_NOT_READY":"INTERACTIVE_SESSION_PRESENT_RUNTIME_NOT_READY",sessionId,error=ex.Message});return ensureTasks?5:3;
        }
        var s=session.Value;var unlocked=s.TryGetProperty("unlocked",out var u)&&u.ValueKind==JsonValueKind.True;var station=s.GetProperty("windowStation").GetString();var desktop=s.GetProperty("inputDesktop").GetString();var providerSession=(int)s.GetProperty("sessionId").GetInt64();
        if(!unlocked||providerSession!=sessionId||!string.Equals(station,"WinSta0",StringComparison.OrdinalIgnoreCase)||!string.Equals(desktop,"Default",StringComparison.OrdinalIgnoreCase))
        {
            Emit(new{status="BLOCKED_INTERACTIVE",reason="Session Host does not report the expected unlocked input desktop",sessionId,providerSession,windowStation=station,inputDesktop=desktop,unlocked});return 4;
        }
        Emit(new{status="READY",sessionId,providerSession,windowStation=station,inputDesktop=desktop,desktopEpoch=s.GetProperty("desktopEpoch").GetInt64(),displayEpoch=s.GetProperty("displayEpoch").GetInt64(),captureEpoch=s.GetProperty("captureEpoch").GetInt64()});return 0;
    }

    static async Task<int> Run(string repo,string[] args)
    {
        var pf=await Preflight(repo,true);if(pf!=0)return pf;
        var sessionId=Process.GetProcessesByName("explorer").Single(p=>p.SessionId>0).SessionId;
        var outArg=Arg(args,"--out");var root=outArg??Path.Combine(repo,"artifacts","acceptance","build001-"+DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ"));Directory.CreateDirectory(root);
        var node=Environment.GetEnvironmentVariable("DESKTOPEYE_NODE")??@"C:\AgentBrowser\tools\node-v24.18.1-win-x64\node.exe";
        var script=Path.Combine(repo,"tests","acceptance","build001.mjs");
        var result=await RunProcess(node,$"\"{script}\" --session {sessionId} --out \"{root}\"",60*60*1000,new Dictionary<string,string?>{{"DESKTOPEYE_REPO",repo}});
        Emit(new{status=result.ExitCode==0?"ACCEPTANCE_RUN_COMPLETED":"ACCEPTANCE_RUN_FAILED",sessionId,evidenceRoot=root,exitCode=result.ExitCode,stdout=result.Stdout,stderr=result.Stderr});return result.ExitCode;
    }

    static string? Arg(string[] args,string name){var i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
    static string FindRepo(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d!=null){if(File.Exists(Path.Combine(d.FullName,"DESKTOPeye.slnx")))return d.FullName;d=d.Parent;}var cwd=Directory.GetCurrentDirectory();if(File.Exists(Path.Combine(cwd,"DESKTOPeye.slnx")))return cwd;throw new DirectoryNotFoundException("DESKTOPeye repository root not found");}
    static void Emit(object value)=>Console.WriteLine(JsonSerializer.Serialize(value,JsonDefaults.Options));

    static async Task<ProcResult> RunProcess(string file,string arguments,int timeoutMs,IReadOnlyDictionary<string,string?>? env=null)
    {
        var psi=new ProcessStartInfo(file,arguments){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};if(env!=null)foreach(var kv in env)psi.Environment[kv.Key]=kv.Value;
        using var p=Process.Start(psi)??throw new InvalidOperationException($"failed to launch {file}");var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();using var cts=new CancellationTokenSource(timeoutMs);try{await p.WaitForExitAsync(cts.Token);}catch(OperationCanceledException){try{p.Kill(true);}catch{}throw new TimeoutException($"process timeout: {file} {arguments}");}return new(p.ExitCode,await stdout,await stderr);
    }
    sealed record ProcResult(int ExitCode,string Stdout,string Stderr);
}