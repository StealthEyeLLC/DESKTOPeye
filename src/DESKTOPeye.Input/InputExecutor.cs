using System.ComponentModel;
using System.Runtime.InteropServices;
using DESKTOPeye.Platform.Windows;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Input;

public sealed record PointerPreflight(long ExpectedRootHwnd,double X,double Y,long ExpectedDisplayEpoch,long CurrentDisplayEpoch,bool TargetCurrent,bool InputDesktopAvailable,bool UiaHitAgrees,bool Enabled,bool Visible);
public sealed record KeyboardPreflight(long ExpectedForegroundHwnd,long ExpectedNativeFocusHwnd,bool TargetCurrent,bool UiaFocusAgrees,bool ModalClear,bool InputDesktopAvailable);
public sealed record InputDelivery(bool Attempted,bool Accepted,ErrorCode Error,string Message,uint EventsSent,AssuranceClass Assurance);

public static class InputExecutor
{
    const uint INPUT_MOUSE=0, INPUT_KEYBOARD=1;
    const uint MOUSEEVENTF_MOVE=0x0001,MOUSEEVENTF_LEFTDOWN=0x0002,MOUSEEVENTF_LEFTUP=0x0004,MOUSEEVENTF_ABSOLUTE=0x8000,MOUSEEVENTF_VIRTUALDESK=0x4000;
    const uint KEYEVENTF_KEYUP=0x0002,KEYEVENTF_UNICODE=0x0004;
    const uint GA_ROOT=2;
    const int VK_SHIFT=0x10,VK_CONTROL=0x11,VK_MENU=0x12,VK_LWIN=0x5B,VK_RWIN=0x5C;
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] struct MOUSEINPUT { public int dx,dy; public uint mouseData,dwFlags,time; public UIntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] struct KEYBDINPUT { public ushort wVk,wScan; public uint dwFlags,time; public UIntPtr dwExtraInfo; }
    [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint cInputs,[In] INPUT[] pInputs,int cbSize);
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vKey);

    public static InputDelivery Click(PointerPreflight p)
    {
        if(!p.TargetCurrent)return Reject(ErrorCode.stale,"target_not_current");
        if(!p.InputDesktopAvailable)return Reject(ErrorCode.no_interactive_desktop,"input_desktop_unavailable_or_locked");
        if(p.ExpectedDisplayEpoch!=p.CurrentDisplayEpoch)return Reject(ErrorCode.target_moved,"display_epoch_changed");
        if(!p.Enabled)return Reject(ErrorCode.not_enabled,"disabled"); if(!p.Visible)return Reject(ErrorCode.not_visible,"not_visible"); if(!p.UiaHitAgrees)return Reject(ErrorCode.hit_test_mismatch,"uia_hit_mismatch");
        var pt=new NativeDesktop.POINT{X=(int)Math.Round(p.X),Y=(int)Math.Round(p.Y)}; var hit=NativeDesktop.WindowFromPoint(pt); var root=hit==IntPtr.Zero?IntPtr.Zero:GetAncestor(hit,GA_ROOT); if(root.ToInt64()!=p.ExpectedRootHwnd)return Reject(ErrorCode.hit_test_mismatch,$"native_hit_root={root.ToInt64()}");
        var v=NativeDesktop.VirtualDesktop(); if(v.Width<=1||v.Height<=1||!v.Contains(p.X,p.Y))return Reject(ErrorCode.not_visible,"point_outside_virtual_desktop");
        int nx=(int)Math.Clamp(Math.Round((p.X-v.Left)*65535.0/(v.Width-1)),0,65535), ny=(int)Math.Clamp(Math.Round((p.Y-v.Top)*65535.0/(v.Height-1)),0,65535);
        var ins=new[]{ Mouse(nx,ny,MOUSEEVENTF_MOVE|MOUSEEVENTF_ABSOLUTE|MOUSEEVENTF_VIRTUALDESK), Mouse(nx,ny,MOUSEEVENTF_LEFTDOWN|MOUSEEVENTF_ABSOLUTE|MOUSEEVENTF_VIRTUALDESK), Mouse(nx,ny,MOUSEEVENTF_LEFTUP|MOUSEEVENTF_ABSOLUTE|MOUSEEVENTF_VIRTUALDESK)};
        var sent=SendInput((uint)ins.Length,ins,Marshal.SizeOf<INPUT>()); return sent==(uint)ins.Length?new(true,true,ErrorCode.none,"accepted",sent,AssuranceClass.race_bounded_physical):new(true,false,ErrorCode.delivery_uncertain,new Win32Exception(Marshal.GetLastWin32Error()).Message,sent,AssuranceClass.delivery_uncertain);
    }
    public static InputDelivery TypeUnicode(KeyboardPreflight p,string text)
    {
        if(!p.TargetCurrent)return Reject(ErrorCode.stale,"target_not_current");
        if(!p.InputDesktopAvailable)return Reject(ErrorCode.no_interactive_desktop,"input_desktop_unavailable_or_locked"); if(!p.ModalClear)return Reject(ErrorCode.modal_blocked,"modal_blocked");
        var fg=NativeDesktop.GetForegroundWindow(); if(fg.ToInt64()!=p.ExpectedForegroundHwnd)return Reject(ErrorCode.foreground_denied,$"foreground={fg.ToInt64()}");
        if(!p.UiaFocusAgrees)return Reject(ErrorCode.focus_failed,"uia_focus_mismatch");
        if(p.ExpectedNativeFocusHwnd!=0){var tid=GetWindowThreadProcessId(fg,out _); var gs=NativeDesktop.GuiThreadState(tid); if(gs.focus.ToInt64()!=p.ExpectedNativeFocusHwnd)return Reject(ErrorCode.focus_failed,$"native_focus={gs.focus.ToInt64()}");}
        var modifiers=new[]{(VK_SHIFT,"shift"),(VK_CONTROL,"control"),(VK_MENU,"alt"),(VK_LWIN,"lwin"),(VK_RWIN,"rwin")}.Where(x=>(GetAsyncKeyState(x.Item1)&0x8000)!=0).Select(x=>x.Item2).ToArray();if(modifiers.Length>0)return Reject(ErrorCode.delivery_uncertain,"modifier_state_not_neutral:"+string.Join(",",modifiers));
        var list=new List<INPUT>(); foreach(var ch in text){list.Add(Key(ch,KEYEVENTF_UNICODE));list.Add(Key(ch,KEYEVENTF_UNICODE|KEYEVENTF_KEYUP));}
        var arr=list.ToArray(); var sent=SendInput((uint)arr.Length,arr,Marshal.SizeOf<INPUT>()); return sent==(uint)arr.Length?new(true,true,ErrorCode.none,"accepted",sent,AssuranceClass.race_bounded_physical):new(true,false,ErrorCode.delivery_uncertain,new Win32Exception(Marshal.GetLastWin32Error()).Message,sent,AssuranceClass.delivery_uncertain);
    }
    static INPUT Mouse(int x,int y,uint flags)=>new(){type=INPUT_MOUSE,U=new InputUnion{mi=new MOUSEINPUT{dx=x,dy=y,dwFlags=flags}}};
    static INPUT Key(char c,uint flags)=>new(){type=INPUT_KEYBOARD,U=new InputUnion{ki=new KEYBDINPUT{wScan=c,dwFlags=flags}}};
    static InputDelivery Reject(ErrorCode e,string m)=>new(false,false,e,m,0,e==ErrorCode.delivery_uncertain?AssuranceClass.delivery_uncertain:AssuranceClass.race_bounded_physical);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
}