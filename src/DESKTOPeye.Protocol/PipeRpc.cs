using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace DESKTOPeye.Protocol;

public sealed class PipeRpcClient : IAsyncDisposable
{
    readonly string _pipe; NamedPipeClientStream? _stream; StreamReader? _reader; StreamWriter? _writer; readonly SemaphoreSlim _serial=new(1,1);
    public string PipeName=>_pipe; public bool Connected=>_stream?.IsConnected==true;
    public PipeRpcClient(string pipe)=>_pipe=pipe;
    public async Task ConnectAsync(int timeoutMs=3000,CancellationToken ct=default)
    {
        await DisposeStream(); _stream=new NamedPipeClientStream(".",_pipe,PipeDirection.InOut,PipeOptions.Asynchronous); using var cts=CancellationTokenSource.CreateLinkedTokenSource(ct);cts.CancelAfter(timeoutMs);await _stream.ConnectAsync(cts.Token);_reader=new StreamReader(_stream,Encoding.UTF8,false,8192,true);_writer=new StreamWriter(_stream,new UTF8Encoding(false),8192,true){AutoFlush=true};
    }
    public async Task<JsonElement> CallAsync(string method,object? parameters=null,int timeoutMs=5000,CancellationToken ct=default)
    {
        await _serial.WaitAsync(ct);try{if(!Connected)await ConnectAsync(Math.Min(timeoutMs,3000),ct);var id=Guid.NewGuid().ToString("N");var req=new RpcRequest(ProtocolVersion.Current,id,method,JsonDefaults.Element(parameters??new{}),timeoutMs);using var cts=CancellationTokenSource.CreateLinkedTokenSource(ct);cts.CancelAfter(timeoutMs);await _writer!.WriteLineAsync(JsonSerializer.Serialize(req,JsonDefaults.Options).AsMemory(),cts.Token);var line=await _reader!.ReadLineAsync(cts.Token);if(line is null)throw new IOException("pipe closed");var resp=JsonSerializer.Deserialize<RpcResponse>(line,JsonDefaults.Options)??throw new InvalidDataException("invalid response");if(resp.Id!=id&&resp.Id!="unknown")throw new InvalidDataException("response id mismatch");if(!resp.Ok)throw new RpcCallException(resp.Error??new(ErrorCode.native_error,"unknown rpc error"));return resp.Result??JsonDefaults.Element(new{});}catch(OperationCanceledException){await DisposeStream();throw;}catch(IOException){await DisposeStream();throw;}finally{_serial.Release();}
    }
    async Task DisposeStream(){_reader?.Dispose();if(_writer!=null)await _writer.DisposeAsync();_stream?.Dispose();_reader=null;_writer=null;_stream=null;}
    public async ValueTask DisposeAsync(){await _serial.WaitAsync();try{await DisposeStream();}finally{_serial.Release();_serial.Dispose();}}
}
public sealed class RpcCallException:Exception{public RpcError Error{get;}public RpcCallException(RpcError e):base($"{e.Code}: {e.Message}"){Error=e;}}

public static class PipeRpcServer
{
    public static async Task RunAsync(string pipe,Func<RpcRequest,CancellationToken,Task<RpcResponse>> dispatch,CancellationToken ct=default)
    {
        while(!ct.IsCancellationRequested)
        {
            var server=new NamedPipeServerStream(pipe,PipeDirection.InOut,16,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
            try { await server.WaitForConnectionAsync(ct); _ = ServeConnection(server,dispatch,ct); }
            catch { await server.DisposeAsync(); throw; }
        }
    }
    static async Task ServeConnection(NamedPipeServerStream server,Func<RpcRequest,CancellationToken,Task<RpcResponse>> dispatch,CancellationToken outer)
    {
        await using(server){using var reader=new StreamReader(server,Encoding.UTF8,false,8192,true);using var writer=new StreamWriter(server,new UTF8Encoding(false),8192,true){AutoFlush=true};try{string? line;while(server.IsConnected&&(line=await reader.ReadLineAsync(outer))!=null){RpcResponse response;try{var req=JsonSerializer.Deserialize<RpcRequest>(line,JsonDefaults.Options)??throw new InvalidDataException("null request");using var cts=CancellationTokenSource.CreateLinkedTokenSource(outer);cts.CancelAfter(Math.Max(1,req.DeadlineMs));response=await dispatch(req,cts.Token);}catch(Exception ex){response=new RpcResponse(ProtocolVersion.Current,"unknown",false,null,new(ErrorCode.native_error,ex.Message,null,ex.ToString()));}await writer.WriteLineAsync(JsonSerializer.Serialize(response,JsonDefaults.Options));}}catch(OperationCanceledException){}catch(IOException){}}
    }
}