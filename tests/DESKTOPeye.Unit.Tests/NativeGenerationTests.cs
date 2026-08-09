using DESKTOPeye.Platform.Windows;
namespace DESKTOPeye.Unit.Tests;
public class NativeGenerationTests
{
    [Fact] public void ReusedHwndGetsNewGenerationAfterDestroy(){var g=new HwndGenerationTracker();Assert.Equal(1,g.Create(42));Assert.Equal(1,g.Destroy(42));Assert.Equal(2,g.Create(42));Assert.Equal(2,g.Current(42));}
    [Fact] public void DuplicateCreateWhileLiveDoesNotAdvance(){var g=new HwndGenerationTracker();Assert.Equal(1,g.Create(42));Assert.Equal(1,g.Create(42));Assert.Equal(1,g.Current(42));}
    [Fact] public void MissedCreateDiscoveredAfterAbsenceAdvances(){var g=new HwndGenerationTracker();Assert.Equal(1,g.DiscoverLive(9));g.Destroy(9);Assert.Equal(2,g.DiscoverLive(9));}
}