using System.Text.Json;
using DESKTOPeye.Protocol;
using DESKTOPeye.World;

namespace DESKTOPeye.Kernel;

internal sealed partial class KernelService
{
    async Task<object> ShellStatus()
    {
        try
        {
            var hello=await _shell.HelloAsync(1200);
            return new{available=true,pipe=_shell.PipeName,hello.Protocol,hello.Version,hello.KernelEpoch,hello.ProviderEpoch,hello.BootEpoch};
        }
        catch(Exception ex) when(ex is IOException or TimeoutException or OperationCanceledException or ShellEyeRpcException)
        {
            return new{available=false,pipe=_shell.PipeName,error=ex.Message};
        }
    }

    async Task<ShellProcessWitness> ResolveShellProcess(uint pid,long nativeCreation,int sessionId,string imagePath,CancellationToken ct)
    {
        ShellProcessWitness witness;
        try{witness=await _shell.RetainProcessAsync(pid,2500,ct);}
        catch(ShellEyeRpcException ex){throw new KernelException(MapShellError(ex.Code),$"SHELLeye process authority unavailable for pid {pid}: {ex.Message}");}
        catch(OperationCanceledException){throw new KernelException(ErrorCode.provider_timeout,$"SHELLeye process authority timed out for pid {pid}");}
        catch(IOException ex){throw new KernelException(ErrorCode.provider_unavailable,$"SHELLeye process authority unavailable for pid {pid}: {ex.Message}");}
        var correlation=SparseSiblingCorrespondence.Resolve(pid,nativeCreation,sessionId,imagePath,new[]{witness});
        if(correlation.Status!=SparseCorrelationStatus.exact||correlation.Witness==null)
            throw new KernelException(correlation.Status==SparseCorrelationStatus.ambiguous?ErrorCode.ambiguous:ErrorCode.stale,$"SHELLeye process witness cannot establish exact desktop correlation for pid {pid}: {correlation.Reason}",detail:JsonSerializer.Serialize(new{nativeCreation,sessionId,imagePath,shelleye=witness},JsonDefaults.Options));
        return correlation.Witness;
    }

    static ErrorCode MapShellError(string code)=>code switch
    {
        "not_found"=>ErrorCode.not_found,
        "destroyed"=>ErrorCode.destroyed,
        "stale"=>ErrorCode.stale,
        "timeout"=>ErrorCode.provider_timeout,
        "inaccessible" or "access_denied"=>ErrorCode.access_denied,
        _=>ErrorCode.provider_unavailable
    };

    static ShellProcessWitness? ShellWitness(LogicalConcept ai)
    {
        if(!ai.Properties.TryGetProperty("shellProcess",out var e)||e.ValueKind!=JsonValueKind.Object)return null;
        return e.Deserialize<ShellProcessWitness>(JsonDefaults.Options);
    }

    void MarkAppInstanceUnavailable(LogicalConcept ai,string reason)
    {
        if(ai.RetiredSequence!=null)return;
        if(ai.Identity!=IdentityStatus.unavailable)
            _store.UpsertConcept(ai with{Identity=IdentityStatus.unavailable,Evidence=ai.Evidence with{Note=reason}},DeltaKind.BindingChanged,ai.Id,new{identity="unavailable",provider="SHELLeye",reason});
        foreach(var child in _store.ListConcepts(includeRetired:false).Where(x=>x.AppInstanceId==ai.Id||x.ParentId==ai.Id).ToArray())
        {
            if(child.Identity is IdentityStatus.exact or IdentityStatus.rebound_exact)
                _store.UpsertConcept(child with{Identity=IdentityStatus.stale,Evidence=child.Evidence with{Note="exact app-instance process authority unavailable"}},DeltaKind.BindingChanged,child.Id,new{identity="stale",reason="shelleye_process_authority_unavailable"});
        }
    }

    async Task<bool> ReconcileShellProcessAuthority(LogicalConcept ai,NativeWindowObservation[] native,CancellationToken ct)
    {
        var witness=ShellWitness(ai);
        if(witness==null){MarkAppInstanceUnavailable(ai,"missing persisted SHELLeye proc_* witness");return false;}
        ShellProcessInspection inspection;
        try{inspection=await _shell.InspectProcessAsync(witness.Id,1800,ct);}
        catch(ShellEyeRpcException ex) when(ex.Code is "destroyed" or "stale" or "not_found") {RetireTree(ai.Id,$"SHELLeye process incarnation terminal: {ex.Code}");return false;}
        catch(Exception ex) when(ex is ShellEyeRpcException or IOException or OperationCanceledException)
        {
            MarkAppInstanceUnavailable(ai,$"SHELLeye process authority unavailable: {ex.Message}");return false;
        }
        if(!inspection.Matches(witness)){RetireTree(ai.Id,"SHELLeye process witness no longer matches retained proc_* incarnation");return false;}
        if(!native.Any(n=>n.ProcessId==witness.Pid&&n.ProcessStartFileTime==witness.CreationFileTimeUtc&&n.SessionId==(int)witness.SessionId))
        {RetireTree(ai.Id,"desktop no longer contains retained SHELLeye process incarnation");return false;}
        if(ai.Identity is not (IdentityStatus.exact or IdentityStatus.rebound_exact))
            _store.UpsertConcept(ai with{Identity=IdentityStatus.rebound_exact,Evidence=ai.Evidence with{Class="shelleye_exact_process_reconstruction",Note="exact retained proc_* revalidated by SHELLeye and current desktop manifestation"}},DeltaKind.BindingChanged,ai.Id,new{identity="rebound_exact",shelleyeProcessId=witness.Id});
        return true;
    }
}