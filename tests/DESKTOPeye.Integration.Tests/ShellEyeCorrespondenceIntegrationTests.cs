using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using DESKTOPeye.Kernel;

namespace DESKTOPeye.Integration.Tests;

public class ShellEyeCorrespondenceIntegrationTests
{
    [Fact]
    public async Task ClientUsesTypedJsonRpcAndIgnoresDeltaNotifications()
    {
        var pipe="shelleye-fake-"+Guid.NewGuid().ToString("N");using var stop=new CancellationTokenSource();var calls=new List<string>();
        var server=Task.Run(async()=>
        {
            await using var s=new NamedPipeServerStream(pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
            await s.WaitForConnectionAsync(stop.Token);using var r=new StreamReader(s,Encoding.UTF8,false,8192,true);using var w=new StreamWriter(s,new UTF8Encoding(false),8192,true){AutoFlush=true};
            while(!stop.IsCancellationRequested)
            {
                var line=await r.ReadLineAsync(stop.Token);if(line==null)break;using var doc=JsonDocument.Parse(line);var root=doc.RootElement;var method=root.GetProperty("method").GetString()!;calls.Add(method);var id=root.GetProperty("id").GetInt64();
                Assert.Equal("2.0",root.GetProperty("jsonrpc").GetString());
                await w.WriteLineAsync(JsonSerializer.Serialize(new{jsonrpc="2.0",method="world.delta",@params=new{cursor=1}}));
                object result=method switch
                {
                    "rpc.hello"=>new{protocol="shelleye-rpc",version=1,kernelEpoch="kernel_1",providerEpoch="provider_1",bootEpoch="boot_1"},
                    "process.retain"=>new{id="proc_1",bootEpoch="boot_1",pid=321u,sequenceNumber=88ul,creationFileTimeUtc=900L,name="fixture",sessionId=1u,executablePath=@"C:\fixture.exe",state="current",parentQuality="reported",parentId=(string?)null},
                    "process.inspect"=>new{processId="proc_1",pid=321u,sequence=88ul,creationFileTimeUtc=900L,name="fixture",sessionId=1u,executablePath=@"C:\fixture.exe",state="current",parent=new{processId=(string?)null,quality="reported"}},
                    _=>throw new InvalidOperationException(method)
                };
                await w.WriteLineAsync(JsonSerializer.Serialize(new{jsonrpc="2.0",id,result}));
                if(calls.Count==3)break;
            }
        },stop.Token);
        await using var client=new ShellEyeCorrespondenceClient(pipe);
        var hello=await client.HelloAsync();Assert.Equal("boot_1",hello.BootEpoch);
        var retained=await client.RetainProcessAsync(321);Assert.Equal("proc_1",retained.Id);Assert.True(retained.MatchesNative(321,900,1,@"C:\fixture.exe"));
        var inspected=await client.InspectProcessAsync(retained.Id);Assert.True(inspected.Matches(retained));
        Assert.Equal(new[]{"rpc.hello","process.retain","process.inspect"},calls);
        stop.Cancel();try{await server;}catch(OperationCanceledException){}
    }

    [Fact]
    public async Task ProviderErrorIsTypedAndDoesNotBecomeExactWitness()
    {
        var pipe="shelleye-fake-"+Guid.NewGuid().ToString("N");
        var server=Task.Run(async()=>{await using var s=new NamedPipeServerStream(pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);await s.WaitForConnectionAsync();using var r=new StreamReader(s,Encoding.UTF8,false,8192,true);using var w=new StreamWriter(s,new UTF8Encoding(false),8192,true){AutoFlush=true};var line=await r.ReadLineAsync();using var doc=JsonDocument.Parse(line!);var id=doc.RootElement.GetProperty("id").GetInt64();await w.WriteLineAsync(JsonSerializer.Serialize(new{jsonrpc="2.0",id,error=new{code="stale",message="pid incarnation changed"}}));});
        await using var client=new ShellEyeCorrespondenceClient(pipe);var ex=await Assert.ThrowsAsync<ShellEyeRpcException>(()=>client.RetainProcessAsync(7));Assert.Equal("stale",ex.Code);await server;
    }
}