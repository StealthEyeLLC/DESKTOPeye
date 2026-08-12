using DESKTOPeye.Input;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Unit.Tests;

public class InputPreflightTests
{
    [Fact] public void PointerRejectsStaleBeforeNativeHitTest(){var r=InputExecutor.Click(new(0,10,10,1,1,false,true,true,true,true));Assert.False(r.Attempted);Assert.Equal(ErrorCode.stale,r.Error);}
    [Fact] public void PointerRejectsUnavailableDesktopBeforeNativeHitTest(){var r=InputExecutor.Click(new(0,10,10,1,1,true,false,true,true,true));Assert.False(r.Attempted);Assert.Equal(ErrorCode.no_interactive_desktop,r.Error);}
    [Fact] public void PointerRejectsDisplayEpochChange(){var r=InputExecutor.Click(new(0,10,10,1,2,true,true,true,true,true));Assert.False(r.Attempted);Assert.Equal(ErrorCode.target_moved,r.Error);}
    [Fact] public void PointerRejectsDisabledTarget(){var r=InputExecutor.Click(new(0,10,10,1,1,true,true,true,false,true));Assert.False(r.Attempted);Assert.Equal(ErrorCode.not_enabled,r.Error);}
    [Fact] public void PointerRejectsInvisibleTarget(){var r=InputExecutor.Click(new(0,10,10,1,1,true,true,true,true,false));Assert.False(r.Attempted);Assert.Equal(ErrorCode.not_visible,r.Error);}
    [Fact] public void PointerRejectsUiaDisagreement(){var r=InputExecutor.Click(new(0,10,10,1,1,true,true,false,true,true));Assert.False(r.Attempted);Assert.Equal(ErrorCode.hit_test_mismatch,r.Error);}
    [Fact] public void KeyboardRejectsStaleBeforeForegroundQuery(){var r=InputExecutor.TypeUnicode(new(0,0,false,true,true,true),"x");Assert.False(r.Attempted);Assert.Equal(ErrorCode.stale,r.Error);}
    [Fact] public void KeyboardRejectsUnavailableDesktopBeforeForegroundQuery(){var r=InputExecutor.TypeUnicode(new(0,0,true,true,true,false),"x");Assert.False(r.Attempted);Assert.Equal(ErrorCode.no_interactive_desktop,r.Error);}
    [Fact] public void KeyboardRejectsModalBeforeForegroundQuery(){var r=InputExecutor.TypeUnicode(new(0,0,true,true,false,true),"x");Assert.False(r.Attempted);Assert.Equal(ErrorCode.modal_blocked,r.Error);}
}
