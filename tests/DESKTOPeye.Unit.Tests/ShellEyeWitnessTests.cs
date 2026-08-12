using DESKTOPeye.Kernel;

namespace DESKTOPeye.Unit.Tests;

public class ShellEyeWitnessTests
{
    static ShellProcessWitness Witness(uint pid=123,long creation=456,uint session=1,string path=@"C:\fixture.exe",ulong sequence=77,string state="current")
        =>new("proc_0001","boot_1",pid,sequence,creation,"fixture",session,path,state,"exact",null);

    [Fact] public void ExactWitnessMatchesDesktopProcessManifestation()
        =>Assert.True(Witness().MatchesNative(123,456,1,@"C:\fixture.exe"));
    [Fact] public void PidReuseOrCreationMismatchIsRejected()
    {
        Assert.False(Witness().MatchesNative(124,456,1,@"C:\fixture.exe"));
        Assert.False(Witness().MatchesNative(123,999,1,@"C:\fixture.exe"));
    }
    [Fact] public void SessionOrExecutableContradictionIsRejected()
    {
        Assert.False(Witness().MatchesNative(123,456,2,@"C:\fixture.exe"));
        Assert.False(Witness().MatchesNative(123,456,1,@"C:\other.exe"));
    }
    [Fact] public void WeakOrTerminalWitnessIsNeverExact()
    {
        Assert.False(Witness(sequence:0).IsExactCurrent);
        Assert.False(Witness(state:"exited").IsExactCurrent);
    }
    [Fact] public void AmbiguousSiblingCorrelationsCannotBecomeTargetBound()
    {
        var a=Witness();var b=a with{Id="proc_0002"};
        var r=SparseSiblingCorrespondence.Resolve(123,456,1,@"C:\fixture.exe",new[]{a,b});
        Assert.Equal(SparseCorrelationStatus.ambiguous,r.Status);Assert.False(r.TargetBoundAllowed);Assert.Null(r.Witness);
    }
    [Fact] public void InspectionMustMatchSameProcIncarnation()
    {
        var w=Witness();var ok=new ShellProcessInspection(w.Id,w.Pid,w.SequenceNumber,w.CreationFileTimeUtc,w.Name,w.SessionId,w.ExecutablePath,"current",new(null,"reported"));
        Assert.True(ok.Matches(w));
        Assert.False(ok with{Sequence=w.SequenceNumber+1} is var wrong && wrong.Matches(w));
        Assert.False(ok with{ProcessId="proc_other"} is var other && other.Matches(w));
    }
}