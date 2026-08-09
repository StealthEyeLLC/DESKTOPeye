using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using DESKTOPeye.Protocol;

namespace DESKTOPeye.Platform.Windows;

public static class NativeDesktop
{
    public const uint WINEVENT_OUTOFCONTEXT=0x0000, WINEVENT_SKIPOWNPROCESS=0x0002;
    public const uint EVENT_SYSTEM_FOREGROUND=0x0003, EVENT_SYSTEM_MINIMIZESTART=0x0016, EVENT_SYSTEM_MINIMIZEEND=0x0017;
    public const uint EVENT_OBJECT_CREATE=0x8000, EVENT_OBJECT_DESTROY=0x8001, EVENT_OBJECT_SHOW=0x8002, EVENT_OBJECT_HIDE=0x8003, EVENT_OBJECT_FOCUS=0x8005, EVENT_OBJECT_LOCATIONCHANGE=0x800B;
    public const int OBJID_WINDOW=0;
    public const int DWMWA_EXTENDED_FRAME_BOUNDS=9, DWMWA_CLOAKED=14;
    public const uint DESKTOP_READOBJECTS=0x0001, DESKTOP_SWITCHDESKTOP=0x0100;
    public const int SM_XVIRTUALSCREEN=76, SM_YVIRTUALSCREEN=77, SM_CXVIRTUALSCREEN=78, SM_CYVIRTUALSCREEN=79, SM_CMONITORS=80;
    public const uint GW_OWNER=4;
    public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2=new(-4);

    public delegate bool EnumWindowsProc(IntPtr hwnd,IntPtr lp);
    public delegate void WinEventDelegate(IntPtr hook,uint evt,IntPtr hwnd,int idObject,int idChild,uint thread,uint time);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] public struct GUITHREADINFO { public int cbSize; public uint flags; public IntPtr hwndActive,hwndFocus,hwndCapture,hwndMenuOwner,hwndMoveSize,hwndCaret; public RECT rcCaret; }

    [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] public static extern uint GetDpiForSystem();
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int n);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb,IntPtr lp);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hwnd,StringBuilder sb,int max);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr hwnd,StringBuilder sb,int max);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd,out RECT rect);
    [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd,uint cmd);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(POINT p);
    [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint idThread,ref GUITHREADINFO info);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll",SetLastError=true)] static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint desiredAccess);
    [DllImport("user32.dll",SetLastError=true)] static extern bool CloseDesktop(IntPtr h);
    [DllImport("user32.dll",SetLastError=true,CharSet=CharSet.Unicode)] static extern bool GetUserObjectInformation(IntPtr h,int index,StringBuilder s,uint len,out uint needed);
    [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr mod,WinEventDelegate cb,uint pid,uint tid,uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd,int attr,out int value,int size);
    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr hwnd,int attr,out RECT value,int size);

    public static void EnablePerMonitorDpiV2(){ SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2); }
    static string UserObjectName(IntPtr h){ if(h==IntPtr.Zero)return "unavailable"; var b=new StringBuilder(256); return GetUserObjectInformation(h,2,b,512,out _)?b.ToString():"unavailable"; }
    public static string WindowStationName()=>UserObjectName(GetProcessWindowStation());
    public static string InputDesktopName(){ var h=OpenInputDesktop(0,false,DESKTOP_READOBJECTS|DESKTOP_SWITCHDESKTOP); if(h==IntPtr.Zero)return "unavailable"; try{return UserObjectName(h);}finally{CloseDesktop(h);} }
    public static RectD VirtualDesktop()=>new(GetSystemMetrics(SM_XVIRTUALSCREEN),GetSystemMetrics(SM_YVIRTUALSCREEN),GetSystemMetrics(SM_CXVIRTUALSCREEN),GetSystemMetrics(SM_CYVIRTUALSCREEN));
    public static long ProcessStartFileTime(uint pid){ try{return Process.GetProcessById((int)pid).StartTime.ToUniversalTime().ToFileTimeUtc();}catch{return 0;} }
    public static NativeWindowObservation? Observe(IntPtr hwnd,long generation,long seq)
    {
        if(hwnd==IntPtr.Zero||!IsWindow(hwnd))return null; var thread=GetWindowThreadProcessId(hwnd,out var pid); var cls=new StringBuilder(512); GetClassName(hwnd,cls,cls.Capacity); var title=new StringBuilder(2048); GetWindowText(hwnd,title,title.Capacity); RECT r; if(DwmGetWindowAttribute(hwnd,DWMWA_EXTENDED_FRAME_BOUNDS,out r,Marshal.SizeOf<RECT>())!=0) GetWindowRect(hwnd,out r); int cloaked=0; DwmGetWindowAttribute(hwnd,DWMWA_CLOAKED,out cloaked,sizeof(int)); var owner=GetWindow(hwnd,GW_OWNER); var parent=GetParent(hwnd); return new NativeWindowObservation(hwnd.ToInt64(),pid,thread,ProcessStartFileTime(pid),generation,cls.ToString(),title.ToString(),owner.ToInt64(),parent.ToInt64(),IsWindowVisible(hwnd),IsIconic(hwnd),cloaked!=0,new RectD(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top),Process.GetCurrentProcess().SessionId,InputDesktopName(),seq);
    }
    public static IReadOnlyList<IntPtr> EnumerateTopLevel(){ var list=new List<IntPtr>(); EnumWindows((h,_)=>{list.Add(h);return true;},IntPtr.Zero); return list; }
    public static (IntPtr active,IntPtr focus,IntPtr caret) GuiThreadState(uint tid){ var g=new GUITHREADINFO{cbSize=Marshal.SizeOf<GUITHREADINFO>()}; return GetGUIThreadInfo(tid,ref g)?(g.hwndActive,g.hwndFocus,g.hwndCaret):(IntPtr.Zero,IntPtr.Zero,IntPtr.Zero); }

    public sealed class WinEventObserver : IDisposable
    {
        readonly ConcurrentQueue<NativeSignal> _queue=new(); readonly Dictionary<long,long> _generation=new(); readonly List<IntPtr> _hooks=new(); readonly WinEventDelegate _cb; long _seq;
        public WinEventObserver(){ _cb=OnEvent; foreach(var h in EnumerateTopLevel())_generation[h.ToInt64()]=1; Hook(EVENT_SYSTEM_FOREGROUND,EVENT_SYSTEM_MINIMIZEEND); Hook(EVENT_OBJECT_CREATE,EVENT_OBJECT_LOCATIONCHANGE); }
        void Hook(uint min,uint max){var h=SetWinEventHook(min,max,IntPtr.Zero,_cb,0,0,WINEVENT_OUTOFCONTEXT|WINEVENT_SKIPOWNPROCESS); if(h!=IntPtr.Zero)_hooks.Add(h);}
        void OnEvent(IntPtr hook,uint evt,IntPtr hwnd,int obj,int child,uint thread,uint time){ if(hwnd==IntPtr.Zero)return; if(evt>=EVENT_OBJECT_CREATE&&obj!=OBJID_WINDOW&&obj!=-4)return; var key=hwnd.ToInt64(); lock(_generation){ if(evt==EVENT_OBJECT_CREATE){_generation[key]=_generation.TryGetValue(key,out var g)?g+1:1;} if(evt==EVENT_OBJECT_DESTROY&&_generation.TryGetValue(key,out var gd)){ _queue.Enqueue(new NativeSignal(Interlocked.Increment(ref _seq),evt,key,gd,thread,time)); _generation.Remove(key); return;} var gen=_generation.TryGetValue(key,out var cur)?cur:(_generation[key]=1); _queue.Enqueue(new NativeSignal(Interlocked.Increment(ref _seq),evt,key,gen,thread,time)); } }
        public NativeSignal[] Drain(int max=1024){ var l=new List<NativeSignal>(); while(l.Count<max&&_queue.TryDequeue(out var s))l.Add(s); return l.ToArray(); }
        public long Generation(long hwnd){lock(_generation)return _generation.TryGetValue(hwnd,out var g)?g:0;}
        public IReadOnlyList<NativeWindowObservation> Snapshot(){var seq=Interlocked.Increment(ref _seq);var l=new List<NativeWindowObservation>();foreach(var h in EnumerateTopLevel()){var key=h.ToInt64();long g;lock(_generation){if(!_generation.TryGetValue(key,out g))_generation[key]=g=1;}var o=Observe(h,g,seq);if(o!=null)l.Add(o);}return l;}
        public void Dispose(){foreach(var h in _hooks)UnhookWinEvent(h);_hooks.Clear();}
    }
}
public sealed record NativeSignal(long Sequence,uint Event,long Hwnd,long Generation,uint ThreadId,uint EventTimeMs);