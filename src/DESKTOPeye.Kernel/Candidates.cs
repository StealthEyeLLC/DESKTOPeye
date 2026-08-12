using DESKTOPeye.Protocol;
namespace DESKTOPeye.Kernel;

internal abstract record Candidate(string Id,ConceptKind Kind,string? ParentId,string? AppInstanceId,IdentityStatus Identity,IdentityEvidence Evidence);
internal sealed record AppCandidate(string Id,string AppKey,string Name,string ImagePath,uint ProcessId,long ProcessStartFileTime,string WindowTitle,IdentityEvidence Evidence):Candidate(Id,ConceptKind.App,null,null,IdentityStatus.exact,Evidence);
internal sealed record AppInstanceCandidate(string Id,string AppId,uint ProcessId,long ProcessStartFileTime,string ImagePath,IdentityEvidence Evidence):Candidate(Id,ConceptKind.AppInstance,AppId,null,IdentityStatus.exact,Evidence);
internal sealed record WindowCandidate(string Id,ConceptKind WindowKind,string AppInstance,string? Parent,NativeWindowObservation Native,UiaElementObservation? Uia,bool Unique,IdentityEvidence Evidence):Candidate(Id,WindowKind,Parent,AppInstance,Unique?IdentityStatus.exact:IdentityStatus.candidate,Evidence);
internal sealed record UiaCandidate(string Id,ConceptKind UiaKind,string Parent,string AppInstance,long RootHwnd,UiaElementObservation Uia,bool Unique,string Affinity,IdentityEvidence Evidence):Candidate(Id,UiaKind,Parent,AppInstance,Unique?IdentityStatus.exact:IdentityStatus.candidate,Evidence);
internal sealed record ItemCandidate(string Id,string CollectionId,string AppInstance,long RootHwnd,UiaElementObservation Uia,string StableItemKey,string Affinity,IdentityEvidence Evidence):Candidate(Id,ConceptKind.Item,CollectionId,AppInstance,IdentityStatus.exact,Evidence);
internal sealed record VisualDescriptor(string Id,string WindowId,VisualFrameRef Frame,RectD Crop,double LocalX,double LocalY,double ScreenX,double ScreenY,int Components,int PixelCount,VisualFeature Feature,long NativeGeneration,long DesktopEpoch,long DisplayEpoch);
internal sealed record VisualFeature(byte R,byte G,byte B,int Tolerance,int MinPixels,int MaxComponents=1);
internal sealed record UiaActionReplyWire(bool Success,string Status,UiaElementObservation Current,object? Extra);
internal sealed record NativeSignalWire(long Sequence,uint Event,long Hwnd,long Generation,uint ThreadId,uint EventTimeMs);
internal sealed record UiaDirtySignalWire(long ProviderEpoch,string Kind,string RuntimeId,int EventId,int PropertyId,DateTimeOffset At);
