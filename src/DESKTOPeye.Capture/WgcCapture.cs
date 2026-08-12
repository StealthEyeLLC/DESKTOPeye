using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Win32;
using Windows.Win32.Foundation;
using D3D=Windows.Win32.Graphics.Direct3D11;
using D3DCommon=Windows.Win32.Graphics.Direct3D;
using DxgiCommon=Windows.Win32.Graphics.Dxgi.Common;
using WinRT;

namespace DESKTOPeye.Capture;

public static partial class WgcCapture
{
    static readonly Guid GraphicsCaptureItemGuid=new("79C3F95B-31F7-4EC2-A464-632EF5D30760");
    static readonly Guid GraphicsCaptureItemInteropGuid=new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    static readonly Guid Direct3DDxgiInterfaceAccessGuid=new("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1");
    static readonly Guid DxgiDeviceGuid=new("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
    static readonly Guid D3D11Texture2DGuid=new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");
    const uint D3D11_SDK_VERSION=7;
    public static bool IsSupported(){try{return GraphicsCaptureSession.IsSupported();}catch{return false;}}
    public static async Task<CapturePixels> CaptureAsync(long hwnd,CancellationToken ct=default)
    {
        if(!IsSupported())throw new PlatformNotSupportedException("Windows.Graphics.Capture unavailable");
        PInvoke.D3D11CreateDevice(null,D3DCommon.D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,default,D3D.D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,default,D3D11_SDK_VERSION,out var device,out _,out var context).ThrowOnFailure();
        try
        {
            var winrt=CreateDirect3DDevice(device); var item=CreateItemForWindow(new IntPtr(hwnd));
            using var pool=Direct3D11CaptureFramePool.CreateFreeThreaded(winrt,DirectXPixelFormat.B8G8R8A8UIntNormalized,2,item.Size); using var session=pool.CreateCaptureSession(item); session.IsCursorCaptureEnabled=false;
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(2)); using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,timeout.Token);
            var tcs=new TaskCompletionSource<Direct3D11CaptureFrame>(TaskCreationOptions.RunContinuationsAsynchronously); int seen=0;
            pool.FrameArrived+=(s,_)=>{Direct3D11CaptureFrame? f=null;try{f=s.TryGetNextFrame();if(f!=null&&!tcs.TrySetResult(f))f.Dispose();}catch(Exception ex){f?.Dispose();tcs.TrySetException(ex);}};
            session.StartCapture();
            while(true){linked.Token.ThrowIfCancellationRequested();using var frame=await tcs.Task.WaitAsync(linked.Token).ConfigureAwait(false);var px=CopyFrame(device,context,frame);seen++;if(!IsBlank(px.Pixels))return px;if(seen>=5)throw new InvalidOperationException("Windows.Graphics.Capture produced five blank current frames");await Task.Delay(25,linked.Token);tcs=new(TaskCreationOptions.RunContinuationsAsynchronously);}
        }
        finally{(context as IDisposable)?.Dispose();(device as IDisposable)?.Dispose();}
    }
    static IDirect3DDevice CreateDirect3DDevice(D3D.ID3D11Device device)
    {
        IntPtr d3d=IntPtr.Zero,dxgi=IntPtr.Zero,gfx=IntPtr.Zero;
        try
        {
            d3d=Marshal.GetIUnknownForObject(device);
            Marshal.QueryInterface(d3d,in DxgiDeviceGuid,out dxgi).ThrowIfFailed("QI IDXGIDevice");
            CreateDirect3D11DeviceFromDXGIDevice(dxgi,out gfx).ThrowIfFailed("CreateDirect3D11DeviceFromDXGIDevice");
            var managed=MarshalInspectable<IDirect3DDevice>.FromAbi(gfx); gfx=IntPtr.Zero; return managed;
        }
        finally{if(gfx!=IntPtr.Zero)Marshal.Release(gfx);if(dxgi!=IntPtr.Zero)Marshal.Release(dxgi);if(d3d!=IntPtr.Zero)Marshal.Release(d3d);}
    }
    static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
    {
        using var factory=ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem"); IntPtr interopPtr=IntPtr.Zero,itemPtr=IntPtr.Zero; IGraphicsCaptureItemInterop? interop=null;
        try
        {
            Marshal.QueryInterface(factory.ThisPtr,in GraphicsCaptureItemInteropGuid,out interopPtr).ThrowIfFailed("QI IGraphicsCaptureItemInterop");
            interop=(IGraphicsCaptureItemInterop)Marshal.GetObjectForIUnknown(interopPtr);
            interop.CreateForWindow(hwnd,in GraphicsCaptureItemGuid,out itemPtr).ThrowIfFailed("CreateForWindow");
            var item=MarshalInspectable<GraphicsCaptureItem>.FromAbi(itemPtr); itemPtr=IntPtr.Zero; return item;
        }
        finally{if(itemPtr!=IntPtr.Zero)Marshal.Release(itemPtr);if(interop!=null&&Marshal.IsComObject(interop))Marshal.FinalReleaseComObject(interop);if(interopPtr!=IntPtr.Zero)Marshal.Release(interopPtr);}
    }
    static unsafe CapturePixels CopyFrame(D3D.ID3D11Device device,D3D.ID3D11DeviceContext context,Direct3D11CaptureFrame frame)
    {
        var captured=GetTexture(frame.Surface);try{var size=frame.ContentSize;int width=size.Width,height=size.Height;if(width<=0||height<=0)throw new InvalidOperationException("empty frame");var desc=new D3D.D3D11_TEXTURE2D_DESC{Width=(uint)width,Height=(uint)height,MipLevels=1,ArraySize=1,Format=DxgiCommon.DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,SampleDesc=new(){Count=1,Quality=0},Usage=D3D.D3D11_USAGE.D3D11_USAGE_STAGING,BindFlags=0,CPUAccessFlags=D3D.D3D11_CPU_ACCESS_FLAG.D3D11_CPU_ACCESS_READ,MiscFlags=0};device.CreateTexture2D(in desc,null,out var staging);try{context.CopyResource(staging,captured);context.Map(staging,0,D3D.D3D11_MAP.D3D11_MAP_READ,0,out var mapped);try{var pixels=new byte[checked(width*height*4)];fixed(byte* dst=pixels){int rowBytes=width*4;for(int row=0;row<height;row++)Buffer.MemoryCopy((byte*)mapped.pData+(row*mapped.RowPitch),dst+(row*rowBytes),rowBytes,rowBytes);}return new(pixels,width,height,"B8G8R8A8");}finally{context.Unmap(staging,0);}}finally{(staging as IDisposable)?.Dispose();}}finally{(captured as IDisposable)?.Dispose();}
    }
    static D3D.ID3D11Texture2D GetTexture(IDirect3DSurface surface)
    {
        var surfacePtr=((IWinRTObject)surface).NativeObject.ThisPtr; IntPtr accessPtr=IntPtr.Zero,texturePtr=IntPtr.Zero; IDirect3DDxgiInterfaceAccess? access=null;
        try
        {
            Marshal.QueryInterface(surfacePtr,in Direct3DDxgiInterfaceAccessGuid,out accessPtr).ThrowIfFailed("QI IDirect3DDxgiInterfaceAccess");
            access=(IDirect3DDxgiInterfaceAccess)Marshal.GetObjectForIUnknown(accessPtr);
            access.GetInterface(in D3D11Texture2DGuid,out texturePtr).ThrowIfFailed("GetInterface Texture2D");
            return (D3D.ID3D11Texture2D)Marshal.GetObjectForIUnknown(texturePtr);
        }
        finally{if(texturePtr!=IntPtr.Zero)Marshal.Release(texturePtr);if(access!=null&&Marshal.IsComObject(access))Marshal.FinalReleaseComObject(access);if(accessPtr!=IntPtr.Zero)Marshal.Release(accessPtr);}
    }
    static bool IsBlank(byte[] pixels){var span=MemoryMarshal.Cast<byte,long>(pixels.AsSpan());foreach(var x in span)if(x!=0)return false;for(int i=span.Length*sizeof(long);i<pixels.Length;i++)if(pixels[i]!=0)return false;return true;}
    [LibraryImport("d3d11.dll")] private static partial int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice,out IntPtr graphicsDevice);
    [ComImport,Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IGraphicsCaptureItemInterop{[PreserveSig]int CreateForWindow(IntPtr window,in Guid iid,out IntPtr result);[PreserveSig]int CreateForMonitor(IntPtr monitor,in Guid iid,out IntPtr result);}
    [ComImport,Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] internal interface IDirect3DDxgiInterfaceAccess{[PreserveSig]int GetInterface(in Guid iid,out IntPtr ppvObject);}
    static void ThrowIfFailed(this int hr,string op){if(hr<0)throw new COMException($"{op} failed HRESULT 0x{hr:X8}",hr);}
}
public sealed record CapturePixels(byte[] Pixels,int Width,int Height,string PixelFormat);