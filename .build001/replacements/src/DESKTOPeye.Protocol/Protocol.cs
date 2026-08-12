using System.Text.Json;
using System.Text.Json.Serialization;

namespace DESKTOPeye.Protocol;

public static class ProtocolVersion { public const int Current = 1; }

[JsonConverter(typeof(JsonStringEnumConverter))] public enum ConceptKind { Machine, Session, App, AppInstance, Window, Dialog, Control, Collection, Item }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum IdentityStatus { exact, rebound_exact, candidate, ambiguous, stale, destroyed, unavailable, virtualized }
[Flags] public enum UiState { None=0, Enabled=1, Visible=2, Offscreen=4, Cloaked=8, Minimized=16, Occluded=32, Focused=64, Selected=128, ModalBlocked=256, Hung=512, ProviderUnhealthy=1024 }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum AssuranceClass { target_bound, provider_semantic, race_bounded_physical, delivery_uncertain }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum ProviderHealth { healthy, degraded, timed_out, restarting, unavailable }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum ErrorCode { none, not_found, stale, destroyed, ambiguous, unavailable, virtualized, provider_unavailable, provider_timeout, access_denied, not_enabled, not_visible, focus_failed, foreground_denied, hit_test_mismatch, capture_unavailable, protected_content, unsupported, cursor_expired, timeout, native_error, wrong_epoch, target_moved, modal_blocked, no_interactive_desktop, delivery_uncertain }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum DeltaKind { ConceptCreated, ConceptChanged, ConceptRetired, BindingChanged, FocusChanged, SelectionChanged, ModalChanged, CollectionViewChanged, ProviderHealthChanged, SessionChanged, DisplayChanged, VisualInvalidated, Gap, Reconciled }
[JsonConverter(typeof(JsonStringEnumConverter))] public enum RouteKind { Domain, Native, UIA, VisualPointer, Pointer, Keyboard, None }

public sealed record RpcRequest(int Version, string Id, string Method, JsonElement Params, int DeadlineMs=5000);
public sealed record RpcResponse(int Version, string Id, bool Ok, JsonElement? Result=null, RpcError? Error=null);
public sealed record RpcError(ErrorCode Code, string Message, int? NativeCode=null, string? Detail=null);
public sealed record RpcNotification(int Version, string Method, JsonElement Params);
public sealed record RuntimeEpochs(long KernelEpoch, long SessionEpoch, long DesktopEpoch, long ProviderEpoch, long DisplayTopologyEpoch);
public sealed record RectD(double Left, double Top, double Width, double Height) { public double Right => Left+Width; public double Bottom => Top+Height; public bool Contains(double x,double y)=>x>=Left&&x<Right&&y>=Top&&y<Bottom; }
public sealed record NativeWindowObservation(long Hwnd, uint ProcessId, uint ThreadId, long ProcessStartFileTime, long NativeGeneration, string ClassName, string Title, long OwnerHwnd, long ParentHwnd, bool Visible, bool Enabled, bool Minimized, bool Cloaked, long BlockingPopupHwnd, RectD Bounds, int SessionId, string Desktop, long ObservedAtSequence);
public sealed record UiaElementObservation(string RuntimeId, int ProcessId, long NativeWindowHandle, string Name, string AutomationId, string ItemStatus, string FrameworkId, string ClassName, int ControlType, bool Enabled, bool Offscreen, bool HasKeyboardFocus, RectD Bounds, string[] Patterns, long ProviderEpoch, string? ParentRuntimeId=null, string? Value=null, bool? Selected=null, int? ToggleState=null, int? ExpandCollapseState=null);
public sealed record IdentityEvidence(string Class, Dictionary<string,string> Witnesses, string[] Contradictions, long ProviderEpoch, long DesktopEpoch, string Note);
public sealed record LogicalConcept(string Id, ConceptKind Kind, IdentityStatus Identity, UiState State, string? ParentId, string? AppInstanceId, string? StableKey, long CreatedSequence, long UpdatedSequence, long? RetiredSequence, JsonElement Properties, IdentityEvidence Evidence);
public sealed record Delta(long Sequence, DeltaKind Kind, string Scope, string? ConceptId, JsonElement Payload, DateTimeOffset At);
public sealed record DeltaRead(long Cursor, long Floor, long Head, bool Gap, Delta[] Deltas, string[] AffectedScopes);
public sealed record SessionSnapshot(string Machine, string User, int SessionId, string WindowStation, string InputDesktop, bool Interactive, bool Locked, RectD VirtualDesktop, uint Dpi, RuntimeEpochs Epochs, ProviderHealth UiaHealth, long WorldSequence);
public sealed record ActionResult(string TargetId, bool Success, RouteKind Route, AssuranceClass Assurance, ErrorCode Error, string Message, IdentityStatus Identity, long ProviderEpoch, long WorldSequence, JsonElement Postcondition, JsonElement Metadata);
public sealed record WaitResult(bool Satisfied, ErrorCode Error, string Message, long Cursor, JsonElement Current);
public sealed record VisualFrameRef(string Id, string SourceConceptId, long NativeIncarnation, long CaptureEpoch, long DisplayEpoch, long WorldSequence, DateTimeOffset Timestamp, RectD Bounds, int Width, int Height, string PixelFormat, string EphemeralPath, string[] Limitations);
public sealed record ProviderDescriptor(int WorkerId, int ProcessId, long ProviderEpoch, ProviderHealth Health, string Lane, DateTimeOffset StartedAt);
public sealed record QueryMatch(string EphemeralId, ConceptKind Kind, IdentityStatus Identity, JsonElement Properties, IdentityEvidence Evidence);
public sealed record MutationPreflight(string TargetId, bool Exact, bool AncestorsExact, bool EpochsMatch, bool Enabled, bool Visible, bool FocusOk, bool HitTestOk, string[] Reasons);

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy=JsonNamingPolicy.CamelCase, WriteIndented=false, Converters={ new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    public static JsonElement Element<T>(T value) => JsonSerializer.SerializeToElement(value, Options);
    public static T? Read<T>(JsonElement value) => value.Deserialize<T>(Options);
}

public static class LogicalIds
{
    public static string New(ConceptKind kind) => Prefix(kind)+Guid.NewGuid().ToString("N")[..12];
    public static string Prefix(ConceptKind k) => k switch { ConceptKind.App=>"app_", ConceptKind.AppInstance=>"appinst_", ConceptKind.Window=>"window_", ConceptKind.Dialog=>"dialog_", ConceptKind.Control=>"control_", ConceptKind.Collection=>"collection_", ConceptKind.Item=>"item_", ConceptKind.Session=>"session_", ConceptKind.Machine=>"machine_", _=>"obj_" };
}
