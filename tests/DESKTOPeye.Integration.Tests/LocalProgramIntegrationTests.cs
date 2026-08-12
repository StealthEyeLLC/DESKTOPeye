using System.Collections.Concurrent;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Integration.Tests;

public class LocalProgramIntegrationTests
{
    [Fact]
    public async Task OnePipeConnectionSupportsOrderedMultiOperationProgram()
    {
        var pipe="desktopeye-integration-"+Guid.NewGuid().ToString("N");using var stop=new CancellationTokenSource();var seen=new ConcurrentQueue<string>();var server=PipeRpcServer.RunAsync(pipe,(request,ct)=>{seen.Enqueue(request.Method);return Task.FromResult(new RpcResponse(ProtocolVersion.Current,request.Id,true,JsonDefaults.Element(new{index=seen.Count,method=request.Method}),null));},stop.Token);await using var client=new PipeRpcClient(pipe);await client.ConnectAsync(3000);
        for(var i=1;i<=15;i++){var result=await client.CallAsync("op."+i,new{value=i},3000);Assert.Equal(i,result.GetProperty("index").GetInt32());Assert.Equal("op."+i,result.GetProperty("method").GetString());}
        Assert.Equal(15,seen.Count);Assert.Equal(Enumerable.Range(1,15).Select(i=>"op."+i),seen.ToArray());stop.Cancel();try{await server.WaitAsync(TimeSpan.FromSeconds(1));}catch(OperationCanceledException){}catch(TimeoutException){}
    }

    [Fact]
    public async Task TypedRpcErrorSurvivesNamedPipeBoundary()
    {
        var pipe="desktopeye-integration-error-"+Guid.NewGuid().ToString("N");using var stop=new CancellationTokenSource();var server=PipeRpcServer.RunAsync(pipe,(request,ct)=>Task.FromResult(new RpcResponse(ProtocolVersion.Current,request.Id,false,null,new(ErrorCode.ambiguous,"fixture ambiguity"))),stop.Token);await using var client=new PipeRpcClient(pipe);await client.ConnectAsync(3000);var ex=await Assert.ThrowsAsync<RpcCallException>(()=>client.CallAsync("mutate",new{},3000));Assert.Equal(ErrorCode.ambiguous,ex.Error.Code);stop.Cancel();try{await server.WaitAsync(TimeSpan.FromSeconds(1));}catch(OperationCanceledException){}catch(TimeoutException){}
    }
}
