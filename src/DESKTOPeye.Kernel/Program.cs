using System.Text.Json;
using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Kernel;

public static class Program
{
    [MTAThread]
    public static async Task<int> Main(string[] args)
    {
        var sessionId=int.TryParse(Arg(args,"--session"),out var sid)?sid:1;
        var runtime=Arg(args,"--runtime")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"StealthEyeLLC","DESKTOPeye","Build001");
        var pipe=Arg(args,"--pipe")??$"desktopeye-kernel-{sessionId}";
        var sessionPipe=Arg(args,"--session-pipe")??$"desktopeye-session-{sessionId}";
        Directory.CreateDirectory(runtime);
        using var store=new WorldStore(Path.Combine(runtime,"world.db"));
        await using var session=new PipeRpcClient(sessionPipe);
        await session.ConnectAsync(5000);
        var service=new KernelService(store,session,pipe,runtime);
        await service.InitializeAsync();
        await File.WriteAllTextAsync(Path.Combine(runtime,"kernel.json"),JsonSerializer.Serialize(new
        {
            pid=Environment.ProcessId, pipe, sessionPipe, runtime, database=store.Path, kernelEpoch=service.KernelEpoch,
            startedAt=DateTimeOffset.UtcNow
        },JsonDefaults.Options));
        Console.WriteLine(JsonSerializer.Serialize(new{ready=true,pid=Environment.ProcessId,pipe,sessionPipe,kernelEpoch=service.KernelEpoch,worldSequence=store.Head},JsonDefaults.Options));
        await service.RunAsync();
        return 0;
    }
    static string? Arg(string[] args,string key){var i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
}