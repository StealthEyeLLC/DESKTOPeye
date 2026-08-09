using DESKTOPeye.Protocol;
namespace DESKTOPeye.Unit.Tests;
public class PipeRpcTests
{
    [Fact] public async Task ConnectionRemainsOwnedUntilClientDisconnects()
    {
        var pipe="desktopeye-test-"+Guid.NewGuid().ToString("N");using var cts=new CancellationTokenSource();var server=PipeRpcServer.RunAsync(pipe,(r,ct)=>Task.FromResult(new RpcResponse(1,r.Id,true,JsonDefaults.Element(new{method=r.Method}),null)),cts.Token);await using var client=new PipeRpcClient(pipe);var a=await client.CallAsync("one",new{});var b=await client.CallAsync("two",new{});Assert.Equal("one",a.GetProperty("method").GetString());Assert.Equal("two",b.GetProperty("method").GetString());cts.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>await server);
    }
}