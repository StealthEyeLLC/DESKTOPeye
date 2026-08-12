using System.Diagnostics;
using DESKTOPeye.Kernel;
using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Integration.Tests;

public class KernelRecoveryIntegrationTests
{
    [Fact]
    public async Task InitializationAllowsRealisticSlowNativeSnapshot()
    {
        var pipe="desktopeye-slow-session-"+Guid.NewGuid().ToString("N");
        using var stop=new CancellationTokenSource();
        var server=Task.Run(()=>PipeRpcServer.RunAsync(pipe,async(req,ct)=>
        {
            object result=req.Method switch
            {
                "hello"=>new{hostEpoch=11L,desktopEpoch=1L,displayEpoch=1L},
                "native.snapshot"=>await SlowSnapshot(ct),
                _=>throw new NotSupportedException(req.Method)
            };
            return new RpcResponse(ProtocolVersion.Current,req.Id,true,JsonDefaults.Element(result),null);
        },stop.Token));

        var runtime=Path.Combine(Path.GetTempPath(),"desktopeye-recovery-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runtime);
        try
        {
            using var store=new WorldStore(Path.Combine(runtime,"world.db"));
            await using var session=new PipeRpcClient(pipe);
            await using var shell=new ShellEyeCorrespondenceClient("unused-shell-"+Guid.NewGuid().ToString("N"));
            var kernel=new KernelService(store,session,shell,"unused-kernel",runtime);
            var sw=Stopwatch.StartNew();
            await kernel.InitializeAsync();
            sw.Stop();
            Assert.InRange(sw.ElapsedMilliseconds,3000,9000);
        }
        finally
        {
            stop.Cancel();
            try{await server;}catch(OperationCanceledException){}
            try{Directory.Delete(runtime,true);}catch{}
        }
    }

    static async Task<object> SlowSnapshot(CancellationToken ct)
    {
        await Task.Delay(3250,ct);
        return Array.Empty<NativeWindowObservation>();
    }
}