using DESKTOPeye.Input;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Unit.Tests;

public class InputPreflightTests
{
    [Fact] public void PointerRejectsStaleBeforeNativeHitTest()
    {
        var r=InputExecutor.Click(new PointerPreflight(1,10,10,1,1,false,true,true,true,true));
        Assert.False(r.Attempted);Assert.Equal(ErrorCode.stale,r.Error);
    }
    [Fact] public void PointerRejectsUnavailableOrLockedDesktopBeforeNativeHitTest()
    {
        var r=InputExecutor.Click(new PointerPreflight(1,10,10,1,1,true,false,true,true,true));
        Assert.False(r.Attempted);Assert.Equal(ErrorCode.no_interactive_desktop,r.Error);
    }
    [Fact] public void PointerRejectsDisplayEpochChangeBeforeNativeHitTest()
    {
        var r=InputExecutor.Click(new PointerPreflight(1,10,10,7,8,true,true,true,true,true));
        Assert.False(r.Attempted);Assert.Equal(ErrorCode.target_moved,r.Error);
    }
    [Fact] public void KeyboardRejectsStaleBeforeForegroundInspection()
    {
        var r=InputExecutor.TypeUnicode(new KeyboardPreflight(1,1,false,true,true,true),"x");
        Assert.False(r.Attempted);Assert.Equal(ErrorCode.stale,r.Error);
    }
    [Fact] public void KeyboardRejectsLockedDesktopBeforeForegroundInspection()
    {
        var r=InputExecutor.TypeUnicode(new KeyboardPreflight(1,1,true,true,true,false),"x");
        Assert.False(r.Attempted);Assert.Equal(ErrorCode.no_interactive_desktop,r.Error);
    }
    [Fact] public void KeyboardRejectsModalBlockBeforeForegroundInspection()
    {
        var r=InputExecutor.TypeUnicode(new KeyboardPreflight(1,1,true,true,false,true),"x");
        Assert.False(r.Attempted);Assert.Equal(ErrorCode.modal_blocked,r.Error);
    }
}