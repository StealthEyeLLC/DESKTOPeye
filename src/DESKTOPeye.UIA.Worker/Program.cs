using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using DESKTOPeye.Protocol;
using DESKTOPeye.UIAutomation;

namespace DESKTOPeye.UIA.Worker;

public static class Program
{
    [MTAThread]
    public static async Task<int> Main(string[] args)
    {
        var pipe=Arg(args,"--pipe")??"desktopeye-uia-worker";
        var epoch=long.TryParse(Arg(args,"--epoch"),out var e)?e:1;
        var workerId=int.TryParse(Arg(args,"--worker-id"),out var w)?w:1;
        using var client=new DirectUiaClient(epoch);
        var subscriptions=new Dictionary<string,GroupSubscription>(StringComparer.Ordinal);
        Console.WriteLine(JsonSerializer.Serialize(new{ready=true,pipe,epoch,workerId,pid=Environment.ProcessId,apartment=Thread.CurrentThread.GetApartmentState().ToString()},JsonDefaults.Options));
        while(true)
        {
            await using var server=new NamedPipeServerStream(pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync(); using var reader=new StreamReader(server,Encoding.UTF8,false,8192,true); using var writer=new StreamWriter(server,new UTF8Encoding(false),8192,true){AutoFlush=true};
            string? line; while(server.IsConnected&&(line=await reader.ReadLineAsync()) is not null)
            {
                RpcResponse response; try { var req=JsonSerializer.Deserialize<RpcRequest>(line,JsonDefaults.Options)??throw new InvalidDataException("null request"); response=await Dispatch(req,client,subscriptions,workerId); }
                catch(Exception ex){ response=new RpcResponse(ProtocolVersion.Current,"unknown",false,null,Error(ex)); }
                await writer.WriteLineAsync(JsonSerializer.Serialize(response,JsonDefaults.Options));
            }
        }
    }
    static async Task<RpcResponse> Dispatch(RpcRequest req,DirectUiaClient uia,Dictionary<string,GroupSubscription> subscriptions,int workerId)
    {
        object? result=req.Method switch
        {
            "hello" => new{workerId,pid=Environment.ProcessId,providerEpoch=uia.ProviderEpoch,capabilities=uia.Capabilities(),apartment=Thread.CurrentThread.GetApartmentState().ToString()},
            "uia.observe_handle" => uia.ObserveHandle(Get<long>(req.Params,"hwnd")),
            "uia.query" => uia.Query(Get<long>(req.Params,"rootHwnd"),GetOpt<string>(req.Params,"name"),GetOpt<string>(req.Params,"automationId"),GetOpt<string>(req.Params,"itemStatus"),GetOpt<int?>(req.Params,"controlType"),GetOpt<int?>(req.Params,"limit")??256),
            "uia.focused" => uia.Focused(),
            "uia.element_from_point" => uia.ElementFromPoint(Get<double>(req.Params,"x"),Get<double>(req.Params,"y")),
            "uia.action" => uia.Action(Get<long>(req.Params,"rootHwnd"),Locator(req.Params.GetProperty("locator")),Get<string>(req.Params,"operation"),GetOpt<string>(req.Params,"value")),
            "uia.clickable_point" => uia.ClickablePoint(Get<long>(req.Params,"rootHwnd"),Locator(req.Params.GetProperty("locator"))),
            "uia.subscribe" => Subscribe(uia,subscriptions,Get<long>(req.Params,"rootHwnd"),Get<string>(req.Params,"scopeId")),
            "uia.unsubscribe" => Unsubscribe(subscriptions,Get<string>(req.Params,"scopeId")),
            "uia.dirty" => uia.DrainDirty(GetOpt<int?>(req.Params,"max")??512),
            "debug.block" => await Block(GetOpt<int?>(req.Params,"milliseconds")??30000),
            "shutdown" => throw new WorkerShutdownException(),
            _ => throw new NotSupportedException(req.Method)
        };
        return new RpcResponse(ProtocolVersion.Current,req.Id,true,JsonDefaults.Element(result),null);
    }
    static object Subscribe(DirectUiaClient uia,Dictionary<string,GroupSubscription> subs,long root,string scope){ if(subs.Remove(scope,out var old))old.Dispose(); var s=uia.Subscribe(root,scope);subs[scope]=s;return new{scope,providerEpoch=s.ProviderEpoch,grouped=true}; }
    static object Unsubscribe(Dictionary<string,GroupSubscription> subs,string scope){var removed=subs.Remove(scope,out var s);s?.Dispose();return new{scope,removed};}
    static async Task<object> Block(int ms){await Task.Delay(ms);return new{blockedMs=ms};}
    static RpcError Error(Exception ex)
    {
        if(ex is WorkerShutdownException)Environment.Exit(0);
        if(ex is UiaResolveException ur)return new(ur.Code=="ambiguous"?ErrorCode.ambiguous:ErrorCode.not_found,ur.Code);
        if(ex is COMException ce){var code=ce.HResult switch{unchecked((int)0x80040201)=>ErrorCode.unavailable,unchecked((int)0x80040200)=>ErrorCode.unsupported,_=>ErrorCode.provider_unavailable};return new(code,ce.Message,ce.HResult,ce.ToString());}
        if(ex is NotSupportedException)return new(ErrorCode.unsupported,ex.Message);
        return new(ErrorCode.native_error,ex.Message,null,ex.ToString());
    }
    static UiaLocator Locator(JsonElement e)=>new(GetOpt<string>(e,"runtimeId"),GetOpt<string>(e,"automationId"),GetOpt<string>(e,"name"),GetOpt<string>(e,"itemStatus"),GetOpt<int?>(e,"controlType"));
    static T Get<T>(JsonElement e,string n)=>e.GetProperty(n).Deserialize<T>(JsonDefaults.Options)!;
    static T? GetOpt<T>(JsonElement e,string n){if(!e.TryGetProperty(n,out var v)||v.ValueKind==JsonValueKind.Null)return default;return v.Deserialize<T>(JsonDefaults.Options);}
    static string? Arg(string[] args,string key){var i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
    sealed class WorkerShutdownException:Exception{}
}