using DESKTOPeye.Protocol;

namespace DESKTOPeye.Integration.Tests;

public class LocalProgramIntegrationTests
{
    [Fact]
    public async Task OnePipeClientExecutesManyTypedOperationsLocally()
    {
        var pipe="desktopeye-integration-"+Guid.NewGuid().ToString("N");
        using var stop=new CancellationTokenSource();var state=0;var dispatches=0;
        var server=PipeRpcServer.RunAsync(pipe,(req,ct)=>
        {
            Interlocked.Increment(ref dispatches);
            RpcResponse response=req.Method switch
            {
                "state.add"=>new(ProtocolVersion.Current,req.Id,true,JsonDefaults.Element(new{value=Interlocked.Add(ref state,req.Params.GetProperty("amount").GetInt32())}),null),
                "state.get"=>new(ProtocolVersion.Current,req.Id,true,JsonDefaults.Element(new{value=Volatile.Read(ref state)}),null),
                "state.ambiguous"=>new(ProtocolVersion.Current,req.Id,false,null,new(ErrorCode.ambiguous,"deliberate integration ambiguity")),
                _=>new(ProtocolVersion.Current,req.Id,false,null,new(ErrorCode.unsupported,req.Method))
            };
            return Task.FromResult(response);
        },stop.Token);
        try
        {
            await using var client=new PipeRpcClient(pipe);await client.ConnectAsync(3000);
            for(var i=0;i<12;i++){var r=await client.CallAsync("state.add",new{amount=1},1000);Assert.Equal(i+1,r.GetProperty("value").GetInt32());}
            var current=await client.CallAsync("state.get",new{},1000);Assert.Equal(12,current.GetProperty("value").GetInt32());
            var error=await Assert.ThrowsAsync<RpcCallException>(()=>client.CallAsync("state.ambiguous",new{},1000));Assert.Equal(ErrorCode.ambiguous,error.Error.Code);
            current=await client.CallAsync("state.get",new{},1000);Assert.Equal(12,current.GetProperty("value").GetInt32());
            Assert.True(client.Connected);Assert.Equal(15,dispatches);
        }
        finally
        {
            stop.Cancel();try{await server;}catch(OperationCanceledException){}catch(ObjectDisposedException){}
        }
    }
}