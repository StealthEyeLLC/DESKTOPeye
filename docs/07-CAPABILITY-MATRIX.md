# 07 — Capability Matrix

Status: **FROZEN CLASSIFICATION FOR BUILD 001**

Ratings below compare architectural fit for ChatGPT, not vendor quality. `H`, `M`, and `L` mean high, medium, and low. Complexity is implementation cost, so `H` is costly. `—` means the architecture does not provide the capability as a governing primitive.

## 1. Architectural alternatives — correctness and coverage

| # | Alternative | Identity correctness | Semantic richness | General coverage | Visual coverage | Recovery | Wrong-target resistance | Information density | Complexity cost |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | screenshot/vision-first computer use | L | L–M | H | H | L | L | L | M |
| 2 | UI Automation wrapper | M within provider epoch | H where exposed | M | L | L–M | M | M | L–M |
| 3 | HWND/Win32-first automation | L–M for current native incarnation | L | M | L | L | M for exact live HWND, L after gaps | M | M |
| 4 | selector-centered RPA | M for re-query, L as retained identity | M–H | M–H | M with CV/image fallbacks | L–M | M | M | M |
| 5 | full accessibility-tree mirror | M initially, L under churn/virtualization | H where exposed | M | L | L–M | M–L after stale mirror | L | H |
| 6 | hybrid stateless UIA + screenshot + input | M per step | H where exposed | H | H | L | M | M | M |
| 7 | application-plugin federation | H inside documented domains | H | L–M across arbitrary apps | provider-dependent | H inside domain | H inside domain | H | H and unbounded |
| 8 | persistent sparse multi-representation correspondence | H where evidence permits; conservative loss otherwise | H | H | H on demand | H | H semantic; bounded physical | H | H |
| 9 | **adopted upgrade:** correspondence world + session native sentinel + isolated provider lanes + categorical assurance | H | H | H | H on demand | H | H with explicit physical ceiling | H | H, bounded by sparse promotion |

Alternative 9 is not a different center from 8. It is the falsification-hardened form: sparse deep semantics no longer implies blindness to unexpected top-level UI; provider failure cannot freeze the kernel; and actuation cannot inherit identity certainty it does not possess.

## 2. Architectural alternatives — operating model

| # | Alternative | Event support | Condition waits | Local programmability | Controller-death continuity | Provider-failure isolation | Cross-substrate composition | Future extensibility |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | screenshot/vision-first | image polling | visual polling | M with batching/code | L | M | M | H for visual breadth |
| 2 | UIA wrapper | UIA events | M | H | L | L unless added | L–M | M |
| 3 | HWND/Win32-first | WinEvent/messages | M | H | L | M | M | M |
| 4 | selector-centered RPA | product-specific | H | H | L–M | product-specific | M | H |
| 5 | full tree mirror | event-heavy invalidation | H | H | M, with stale risk | L | M | M |
| 6 | stateless hybrid | provider events/polling | M–H | H | L | M | M | H |
| 7 | plugin federation | domain-specific | H in domain | H | H in domain | H per plugin | H, but bespoke | M–H at high marginal cost |
| 8 | sparse correspondence | event→reconcile | H | H | H | H by design | H via sparse typed links | H |
| 9 | **adopted upgrade** | native sentinel + scoped provider invalidations | H, final current query | H, 40+/50+ typed calls | H, cold conservative | H, replaceable workers | H, strongest-substrate broker | H without universal ontology |

## 3. Final capability classification

| Capability | Classification | Build 001 role and frozen boundary |
| --- | --- | --- |
| Win32/USER32 top-level windows | **Build 001 core** | lifecycle, current HWND incarnation, parent/owner/root, class/style/state, foreground/focus correlations |
| WTS/window station/desktop/input desktop | **Build 001 core** | session/desktop epochs and availability; no secure-desktop control |
| WinEvent hooks | **Build 001 core** | cheap session-wide invalidation/lifecycle/focus signals; never complete history |
| DWM extended frame/cloak state | **Build 001 core** | current visual bounds/cloaking facets; not identity ontology |
| DWM/composition breadth | **core later** | only as capture/visibility needs earn it |
| UI Automation direct COM | **Build 001 core** | primary desktop semantic provider, exact HRESULTs/patterns/events/cache |
| UIA Control View | **Build 001 core** | normal ChatGPT semantic projection |
| UIA Content View | **Build 001 core** | bounded document/content queries in fixture workflows |
| UIA Raw View | **Build 001 core, diagnostic use** | scoped recovery/provider traversal only; no full mirror |
| UIA caching/CacheRequest | **Build 001 core** | compile retained interests into scoped plans |
| UIA6 event-handler groups | **experimental core seam** | preferred when supported; Build 001 verifies lifecycle ordering |
| UIA Remote Operations | **experimental / deferred optimization** | may reduce round trips; not identity, recovery or fault isolation |
| System.Windows.Automation | **fallback/reference** | not identity-critical foundation |
| FlaUI/other UIA wrappers | **fallback/reference** | test/reference convenience only; not canonical provider contract |
| Legacy MSAA | **fallback / core later** | only when UIA coverage is absent and value is demonstrated |
| WPF | **Build 001 core framework** | semantic fixture plus recycling virtualization identity killer |
| Native Win32 UI | **Build 001 core framework** | HWND churn, owner/popups, moving/overlay/focus adversary |
| Windows Common Item Dialog | **Build 001 post-gate smoke** | bounded real-world generalization after A–D |
| WinUI / Windows App SDK | **core later** | framework breadth; `WindowId` is current HWND facet, not identity upgrade |
| WinForms | **core later** | standard UIA/Win32 coverage after spine |
| Electron/Chromium native frame | **core later** | DESKTOPeye for native frame/OS dialog; eyeBROWSE for target content |
| Qt | **advanced** | framework/provider coverage where official accessibility surface exists |
| Java Accessibility | **advanced** | provider adapter after core; no identity assumptions without evidence |
| Office desktop UI | **advanced / domain facet** | DESKTOPeye operates frame; Office/document APIs should own deeper semantics |
| Windows Graphics Capture per window | **Build 001 core** | on-demand visual facet and one intentional fallback |
| Desktop Duplication | **fallback / core later** | monitor/composed-screen truth, not per-window primary |
| PrintWindow | **fallback** | guarded blocking application-render request, never truth guarantee |
| BitBlt/screen DC | **fallback** | current visible composed-region evidence for pointer checks |
| OCR | **fallback** | only when accessible/domain text is absent; never identity alone |
| General computer vision/embeddings | **advanced / fallback** | candidate discovery and visual-only surfaces, not exact continuity |
| Semantic Invoke/Value/Selection/Toggle/Expand/Scroll/Window patterns | **Build 001 core** | provider-semantic routes with current capability/precondition/postcondition |
| Native window operations | **Build 001 core where fit** | route-specific semantics; a bare recyclable HWND call is not automatically `target_bound` |
| Physical pointer via SendInput | **Build 001 core fallback** | one intentional race-bounded route, hit-test/display preflight |
| Physical keyboard via SendInput | **Build 001 core fallback** | one intentional race-bounded route, foreground/native/UIA focus preflight |
| Synthetic touch/pen/touchpad | **deferred** | adds modality, not target binding; no Build 001 spine value |
| Clipboard | **core later / fallback** | operation-scoped transfer/interference detection; no history |
| Drag/drop | **deferred** | broad protocol/input/provider complexity beyond decisive slice |
| Continuous z-order/occlusion model | **rejected for core** | query on demand for pointer/capture; do not maintain continuously |
| Durable `monitor_*` concepts | **rejected for Build 001** | use ephemeral descriptors plus display topology epoch |
| Full UIA tree mirror | **rejected** | volatile, virtualized, expensive and false-durability prone |
| Permanent screenshot/video stream | **rejected** | frames are on-demand ephemeral observations |
| Provider worker fault isolation | **Build 001 core** | blocking provider cannot freeze kernel/unrelated scopes |
| Per-app permanent UIA process | **deferred** | adopt only if hang measurement requires that granularity |
| SQLite WAL persistence | **Build 001 core** | sparse concepts/evidence/epochs/interests/bounded deltas |
| Graph database | **rejected** | no requirement; relations are sparse and typed |
| Named-pipe typed IPC | **Build 001 core** | compact versioned local protocol; exact batching measured |
| Node 24 Program Host | **Build 001 core** | one 40+ meaningful typed-operation invocation; target 50+ |
| SHELLeye process/file correlation | **Build 001 core seam** | exact `appinst_* ↔ proc_*` witness; no process redefinition |
| eyeBROWSE browser correlation | **core later / post-gate composition** | web semantics stay in eyeBROWSE; native frame/dialog in DESKTOPeye |
| CODEeye workspace link | **advanced** | sparse correlation only; source/build meaning stays in CODEeye |
| DOCSeye document link | **deferred** | future document semantics; DESKTOPeye retains viewport/window UI only |
| General per-app plugin architecture | **rejected as center** | domain providers remain optional facets behind the broker |
| DLL injection or target-process hooks | **rejected by default** | no Build 001 capability gap justifies them |
| Kernel driver | **rejected** | no Build 001 capability/identity need |
| DESKTOPeye-specific permissions/guardrails/verifier/ledger | **rejected** | prohibited by the five exhaustive project constraints |

## 4. Build 001 coverage boundary

Build 001 proves the permanent spine with WPF, native Win32, current Windows APIs, one visual fallback, one pointer route, one keyboard route, cold recovery, provider failure, bounded deltas/waits, and the Program Host. It does not prove every UI framework or every physical modality.

```text
Product implementation: IMPLEMENTED / PENDING MEASURED ACCEPTANCE
Build 001 acceptance:   NOT RUN
```
