using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace DESKTOPeye.Kernel;

internal sealed record ShellProcessWitness(
    string Id,
    string BootEpoch,
    uint Pid,
    ulong SequenceNumber,
    long CreationFileTimeUtc,
    string Name,
    uint SessionId,
    string? ExecutablePath,
    string State,
    string ParentQuality,
    string? ParentId)
{
    public bool IsExactCurrent => State == "current" && !string.IsNullOrWhiteSpace(Id) && Id.StartsWith("proc_",StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(BootEpoch) && SequenceNumber != 0;
    public bool MatchesNative(uint pid,long creationFileTime,int sessionId,string? executablePath)
        => IsExactCurrent
           && Pid==pid
           && CreationFileTimeUtc==creationFileTime
           && (sessionId<=0 || SessionId==(uint)sessionId)
           && (string.IsNullOrWhiteSpace(executablePath) || string.IsNullOrWhiteSpace(ExecutablePath) || string.Equals(Path.GetFullPath(ExecutablePath),Path.GetFullPath(executablePath),StringComparison.OrdinalIgnoreCase));
}

internal sealed record ShellProcessInspection(
    string ProcessId,
    uint Pid,
    ulong Sequence,
    long CreationFileTimeUtc,
    string Name,
    uint SessionId,
    string? ExecutablePath,
    string State,
    ShellProcessParent Parent)
{
    public bool Matches(ShellProcessWitness witness)
        => string.Equals(ProcessId,witness.Id,StringComparison.Ordinal)
           && Pid==witness.Pid
           && Sequence==witness.SequenceNumber
           && CreationFileTimeUtc==witness.CreationFileTimeUtc
           && SessionId==witness.SessionId
           && string.Equals(State,"current",StringComparison.Ordinal)
           && (string.IsNullOrWhiteSpace(witness.ExecutablePath) || string.IsNullOrWhiteSpace(ExecutablePath) || string.Equals(Path.GetFullPath(ExecutablePath),Path.GetFullPath(witness.ExecutablePath),StringComparison.OrdinalIgnoreCase));
}
internal sealed record ShellProcessParent(string? ProcessId,string Quality);
internal sealed record ShellHello(string Protocol,int Version,string KernelEpoch,string ProviderEpoch,string BootEpoch);
internal enum SparseCorrelationStatus { exact, ambiguous, unavailable }
internal sealed record SparseCorrelationResolution(SparseCorrelationStatus Status,ShellProcessWitness? Witness,bool TargetBoundAllowed,string Reason);
internal static class SparseSiblingCorrespondence
{
    public static SparseCorrelationResolution Resolve(uint pid,long creationFileTime,int sessionId,string executablePath,IReadOnlyList<ShellProcessWitness> candidates)
    {
        var exact=candidates.Where(x=>x.MatchesNative(pid,creationFileTime,sessionId,executablePath)).ToArray();
        return exact.Length switch
        {
            1=>new(SparseCorrelationStatus.exact,exact[0],true,"one exact SHELLeye proc_* witness matches the desktop manifestation"),
            >1=>new(SparseCorrelationStatus.ambiguous,null,false,"multiple exact-looking sibling process correlations remain plausible"),
            _=>new(SparseCorrelationStatus.unavailable,null,false,"no exact SHELLeye process correlation is available")
        };
    }
}

internal sealed class ShellEyeRpcException : Exception
{
    public string Code { get; }
    public ShellEyeRpcException(string code,string message):base(message)=>Code=code;
}

internal sealed class ShellEyeCorrespondenceClient : IAsyncDisposable
{
    readonly string _pipe;
    readonly SemaphoreSlim _serial=new(1,1);
    NamedPipeClientStream? _stream;
    StreamReader? _reader;
    StreamWriter? _writer;
    long _nextId;

    public ShellEyeCorrespondenceClient(string? pipe=null)
        => _pipe=pipe??Environment.GetEnvironmentVariable("DESKTOPEYE_SHELLEYE_PIPE")??Environment.GetEnvironmentVariable("SHELLEYE_PIPE")??"shelleye-dev";
    public string PipeName=>_pipe;

    public async Task<ShellHello> HelloAsync(int timeoutMs=1800,CancellationToken ct=default)
        => (await CallAsync("rpc.hello",new{},timeoutMs,ct)).Deserialize<ShellHello>(JsonOptions) ?? throw new ShellEyeRpcException("provider_unavailable","SHELLeye returned an invalid hello response.");
    public async Task<ShellProcessWitness> RetainProcessAsync(uint pid,int timeoutMs=2500,CancellationToken ct=default)
        => (await CallAsync("process.retain",new{pid},timeoutMs,ct)).Deserialize<ShellProcessWitness>(JsonOptions) ?? throw new ShellEyeRpcException("provider_unavailable","SHELLeye returned an invalid process witness.");
    public async Task<ShellProcessInspection> InspectProcessAsync(string processId,int timeoutMs=2000,CancellationToken ct=default)
        => (await CallAsync("process.inspect",new{processId},timeoutMs,ct)).Deserialize<ShellProcessInspection>(JsonOptions) ?? throw new ShellEyeRpcException("provider_unavailable","SHELLeye returned an invalid process inspection.");

    internal async Task<JsonElement> CallAsync(string method,object parameters,int timeoutMs,CancellationToken ct=default)
    {
        await _serial.WaitAsync(ct);
        try
        {
            if(_stream?.IsConnected!=true)await ConnectAsync(Math.Min(timeoutMs,2000),ct);
            var id=Interlocked.Increment(ref _nextId);
            var request=JsonSerializer.Serialize(new{jsonrpc="2.0",id,method,@params=parameters,timeoutMs},JsonOptions);
            using var cts=CancellationTokenSource.CreateLinkedTokenSource(ct);cts.CancelAfter(timeoutMs);
            await _writer!.WriteLineAsync(request.AsMemory(),cts.Token);
            for(;;)
            {
                var line=await _reader!.ReadLineAsync(cts.Token);
                if(line is null)throw new IOException("SHELLeye pipe closed.");
                using var doc=JsonDocument.Parse(line);var root=doc.RootElement;
                if(!root.TryGetProperty("id",out var responseId) || responseId.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)continue;
                if(!responseId.TryGetInt64(out var rid) || rid!=id)continue;
                if(root.TryGetProperty("error",out var error)&&error.ValueKind==JsonValueKind.Object)
                {
                    var code=error.TryGetProperty("code",out var c)&&c.ValueKind==JsonValueKind.String?c.GetString()??"provider_unavailable":"provider_unavailable";
                    var message=error.TryGetProperty("message",out var m)&&m.ValueKind==JsonValueKind.String?m.GetString()??code:code;
                    throw new ShellEyeRpcException(code,message);
                }
                if(!root.TryGetProperty("result",out var result))return JsonSerializer.SerializeToElement(new{},JsonOptions);
                return result.Clone();
            }
        }
        catch(OperationCanceledException){await ResetAsync();throw;}
        catch(IOException){await ResetAsync();throw;}
        finally{_serial.Release();}
    }

    async Task ConnectAsync(int timeoutMs,CancellationToken ct)
    {
        await ResetAsync();
        _stream=new NamedPipeClientStream(".",_pipe,PipeDirection.InOut,PipeOptions.Asynchronous);
        using var cts=CancellationTokenSource.CreateLinkedTokenSource(ct);cts.CancelAfter(timeoutMs);
        await _stream.ConnectAsync(cts.Token);
        _reader=new StreamReader(_stream,new UTF8Encoding(false),false,8192,true);
        _writer=new StreamWriter(_stream,new UTF8Encoding(false),8192,true){AutoFlush=true};
    }
    async Task ResetAsync(){_reader?.Dispose();if(_writer!=null)await _writer.DisposeAsync();_stream?.Dispose();_reader=null;_writer=null;_stream=null;}
    public async ValueTask DisposeAsync(){await _serial.WaitAsync();try{await ResetAsync();}finally{_serial.Release();_serial.Dispose();}}

    static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web){PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
}