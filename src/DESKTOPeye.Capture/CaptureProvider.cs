using DESKTOPeye.Protocol;
namespace DESKTOPeye.Capture;

public sealed class CaptureProvider
{
    readonly string _dir; long _epoch;
    public long CaptureEpoch=>Volatile.Read(ref _epoch);
    public CaptureProvider(string runtimeDir){_dir=Path.Combine(runtimeDir,"frames");Directory.CreateDirectory(_dir);_epoch=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();}
    public async Task<VisualFrameRef> CaptureWindowAsync(string sourceConceptId,long hwnd,long nativeIncarnation,long displayEpoch,long worldSequence,RectD? crop=null,CancellationToken ct=default)
    {
        try
        {
            var raw=await WgcCapture.CaptureAsync(hwnd,ct); var selected=crop is null?raw:Crop(raw,crop); var id="frame_"+Guid.NewGuid().ToString("N")[..12]; var path=Path.Combine(_dir,id+".bmp"); WriteBmp(path,selected); var b=crop??new RectD(0,0,selected.Width,selected.Height); CleanupExcept(path); return new VisualFrameRef(id,sourceConceptId,nativeIncarnation,CaptureEpoch,displayEpoch,worldSequence,DateTimeOffset.UtcNow,b,selected.Width,selected.Height,selected.PixelFormat,path,Array.Empty<string>());
        }
        catch(OperationCanceledException){throw;}
        catch(Exception ex){Interlocked.Increment(ref _epoch);throw new CaptureException(ex is UnauthorizedAccessException?"protected":"unavailable",ex.Message,ex);}
    }
    static CapturePixels Crop(CapturePixels src,RectD r)
    {
        int x=Math.Clamp((int)Math.Floor(r.Left),0,src.Width),y=Math.Clamp((int)Math.Floor(r.Top),0,src.Height);int w=Math.Clamp((int)Math.Ceiling(r.Width),0,src.Width-x),h=Math.Clamp((int)Math.Ceiling(r.Height),0,src.Height-y);if(w<=0||h<=0)throw new CaptureException("unavailable","empty crop");var dst=new byte[w*h*4];for(int row=0;row<h;row++)Buffer.BlockCopy(src.Pixels,((y+row)*src.Width+x)*4,dst,row*w*4,w*4);return new(dst,w,h,src.PixelFormat);
    }
    static void WriteBmp(string path,CapturePixels src)
    {
        using var fs=File.Create(path);using var bw=new BinaryWriter(fs);int data=src.Width*src.Height*4,file=54+data;bw.Write((ushort)0x4D42);bw.Write(file);bw.Write(0);bw.Write(54);bw.Write(40);bw.Write(src.Width);bw.Write(-src.Height);bw.Write((ushort)1);bw.Write((ushort)32);bw.Write(0);bw.Write(data);bw.Write(2835);bw.Write(2835);bw.Write(0);bw.Write(0);bw.Write(src.Pixels);
    }
    void CleanupExcept(string keep){foreach(var f in Directory.EnumerateFiles(_dir,"frame_*.bmp")){if(!string.Equals(f,keep,StringComparison.OrdinalIgnoreCase)){try{File.Delete(f);}catch{}}}}
    public void DeviceLost()=>Interlocked.Increment(ref _epoch);
}
public sealed class CaptureException:Exception{public string Code{get;}public CaptureException(string code,string message,Exception? inner=null):base(message,inner){Code=code;}}