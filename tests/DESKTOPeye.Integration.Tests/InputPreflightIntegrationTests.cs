using DESKTOPeye.Input;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Integration.Tests;

public class InputPreflightIntegrationTests
{
    [Fact]
    public void PointerRefusesUnavailableOrLockedDesktopBeforeNativeHitTest()
    {
        var result=InputExecutor.Click(new PointerPreflight(1,100,100,7,7,true,false,true,true,true));
        Assert.False(result.Attempted);
        Assert.False(result.Accepted);
        Assert.Equal(ErrorCode.no_interactive_desktop,result.Error);
        Assert.Equal(0u,result.EventsSent);
    }

    [Fact]
    public void PointerRefusesChangedDisplayEpochBeforeNativeHitTest()
    {
        var result=InputExecutor.Click(new PointerPreflight(1,100,100,7,8,true,true,true,true,true));
        Assert.False(result.Attempted);
        Assert.Equal(ErrorCode.target_moved,result.Error);
        Assert.Equal(0u,result.EventsSent);
    }

    [Fact]
    public void PointerRefusesStaleTargetBeforeNativeHitTest()
    {
        var result=InputExecutor.Click(new PointerPreflight(1,100,100,7,7,false,true,true,true,true));
        Assert.False(result.Attempted);
        Assert.Equal(ErrorCode.stale,result.Error);
        Assert.Equal(0u,result.EventsSent);
    }

    [Fact]
    public void KeyboardRefusesUnavailableOrLockedDesktopBeforeForegroundInspection()
    {
        var result=InputExecutor.TypeUnicode(new KeyboardPreflight(1,1,true,true,true,false),"x");
        Assert.False(result.Attempted);
        Assert.False(result.Accepted);
        Assert.Equal(ErrorCode.no_interactive_desktop,result.Error);
        Assert.Equal(0u,result.EventsSent);
    }

    [Fact]
    public void KeyboardRefusesStaleTargetBeforeForegroundInspection()
    {
        var result=InputExecutor.TypeUnicode(new KeyboardPreflight(1,1,false,true,true,true),"x");
        Assert.False(result.Attempted);
        Assert.Equal(ErrorCode.stale,result.Error);
        Assert.Equal(0u,result.EventsSent);
    }
}