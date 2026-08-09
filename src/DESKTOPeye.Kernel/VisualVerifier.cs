using System.Buffers.Binary;
using DESKTOPeye.Protocol;
namespace DESKTOPeye.Kernel;

internal static class VisualVerifier
{
    public static (int components,int pixels,double x,double y) FindUnique(string bmpPath,VisualFeature feature)
    {
        var b=File.ReadAllBytes(bmpPath); if(b.Length<54||b[0]!=(byte)'B'||b[1]!=(byte)'M')throw new InvalidDataException("invalid BMP");
        int offset=BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(10,4)), width=BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(18,4)), rawHeight=BinaryPrimitives.ReadInt32LittleEndian(b.AsSpan(22,4)); bool topDown=rawHeight<0; int height=Math.Abs(rawHeight); ushort bits=BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(28,2)); if(bits!=32)throw new InvalidDataException("expected 32bpp BMP");
        var mask=new bool[checked(width*height)]; int count=0; for(int y=0;y<height;y++){int srcY=topDown?y:height-1-y;int row=offset+srcY*width*4;for(int x=0;x<width;x++){int p=row+x*4;byte bb=b[p],gg=b[p+1],rr=b[p+2];if(Math.Abs(rr-feature.R)<=feature.Tolerance&&Math.Abs(gg-feature.G)<=feature.Tolerance&&Math.Abs(bb-feature.B)<=feature.Tolerance){mask[y*width+x]=true;count++;}}}
        var seen=new bool[mask.Length]; int comps=0,bestCount=0; long bestX=0,bestY=0; var q=new Queue<int>();
        for(int i=0;i<mask.Length;i++) if(mask[i]&&!seen[i]){comps++;int cc=0;long sx=0,sy=0;seen[i]=true;q.Enqueue(i);while(q.Count>0){var cur=q.Dequeue();int x=cur%width,y=cur/width;cc++;sx+=x;sy+=y;Try(x-1,y);Try(x+1,y);Try(x,y-1);Try(x,y+1);void Try(int nx,int ny){if(nx<0||ny<0||nx>=width||ny>=height)return;var ni=ny*width+nx;if(mask[ni]&&!seen[ni]){seen[ni]=true;q.Enqueue(ni);}}}if(cc>bestCount){bestCount=cc;bestX=sx;bestY=sy;}}
        if(count<feature.MinPixels||bestCount<feature.MinPixels)return(0,count,0,0);return(comps,count,(double)bestX/bestCount,(double)bestY/bestCount);
    }
}