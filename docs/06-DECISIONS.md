# 06 — Decisions

Status: **FROZEN FOR BUILD 001**

This register prevents implementation choices from silently changing the architecture. The normative details remain in [01-ARCHITECTURE.md](01-ARCHITECTURE.md) and [02-BUILD-001-SLICE.md](02-BUILD-001-SLICE.md).

## Frozen — build against these decisions

| ID | Decision | Consequence |
| --- | --- | --- |
| F-01 | ChatGPT is the product operator. | APIs optimize for compact retained objects, deltas, waits and local programs, not human selector authoring. |
| F-02 | DESKTOPeye is a persistent sparse multi-representation correspondence world. | No screenshot, HWND, UIA element, selector, tree, or plugin becomes the center. |
| F-03 | Providers own representation-specific current truth; DESKTOPeye owns agent continuity. | Every binding names its provider, epoch, scope and evidence. |
| F-04 | Loss of continuity is acceptable; false continuity is not. | Unknown replacements become stale/ambiguous rather than similar-object rebound. |
| F-05 | There is no canonical full desktop tree. | Expose sparse relations, provider views and on-demand projections. |
| F-06 | Query results are ephemeral; retention/watch/reuse/action promotes the minimum concept. | Deep global mirroring is prohibited. |
| F-07 | `app_*` and live `appinst_*` are separate. | Genuine app restart creates a new app instance and all new descendant concepts. |
| F-08 | `window_*` owns logical revisions plus current native/UIA facets. | An HWND change creates a new native incarnation; logical continuity requires exact lineage. |
| F-09 | HWND is a recyclable native incarnation witness, not identity. | `IsWindow`, PID, title, class, geometry or appearance cannot cold-rebind a window. |
| F-10 | UIA RuntimeId is an opaque, temporary same-provider/desktop witness. | It is never a durable DESKTOPeye ID or cold-recovery key. |
| F-11 | AutomationId is optional, scoped query/reconstruction evidence; Name is a property. | Neither authorizes exact mutation or cross-restart continuity alone. |
| F-12 | A surviving UIA COM reference is incumbent evidence only inside one provider epoch. | Worker death/restart invalidates the object universe. |
| F-13 | Control View is normal, Content View is content-oriented, Raw View is diagnostic/recovery. | Raw View is never mirrored wholesale. |
| F-14 | Retained interests compile to scoped UIA cache plans. | Caches improve round trips but are immutable observations, not identity/persistence. |
| F-15 | `dialog_*` is a first-class workflow concept/facet. | Sequential identical dialogs and windowless flyouts have distinct lifetimes. |
| F-16 | `control_*` is generic, with control type, patterns and provider facets. | Do not create permanent class-per-control ontology. |
| F-17 | `collection_*` carries a view epoch; durable `item_*` requires a stable documented key. | Containers, indexes and duplicate labels do not create item identity. |
| F-18 | Identity status is categorical and separate from current UI state. | Numeric confidence can rank candidates but never authorize mutation. |
| F-19 | Weak text/path/index/geometry/visual/OCR/embedding evidence is candidate/ranking evidence only. | No fuzzy rebound for retained mutation targets. |
| F-20 | Mutations pass the exact current-binding gate under exact ancestors. | Ambiguous, stale, unavailable or virtualized targets cannot mutate. |
| F-21 | The representation/actuation broker is operation-specific. | No rigid universal fallback ladder; use the strongest fit and current provider health. |
| F-22 | Operation results expose route, assurance and postcondition separately. | Assurance classes are `target_bound`, `provider_semantic`, `race_bounded_physical`, `delivery_uncertain`; they are not identity or authority tiers. `target_bound` requires an API-enforced lifetime-bound reference/key, not a bare HWND/PID/RuntimeId or preflight. |
| F-23 | Semantic operations do not claim a physical human event path. | Invoke/Value/Selection/etc. report provider-semantic behavior honestly. |
| F-24 | Physical pointer/keyboard delivery is race-bounded, not atomically target-bound. | Fresh preflight, smallest batch and postcondition are required; residual race remains. |
| F-25 | Foreground, active window, native focus, UIA focus, caret and selection are distinct. | Foreground denial/focus mismatch are typed failures, never reasons to inject blindly. |
| F-26 | Coordinate preflight is tied to a display topology epoch. | DPI/topology/movement change invalidates stale points; monitors need not be durable concepts. |
| F-27 | Visual capture is an on-demand facet; frames are ephemeral. | WGC is Build 001 per-window primary; no permanent screenshot stream. |
| F-28 | Vision/OCR may discover or rank but never prove exact retained identity alone. | Inaccessible UI needs current visual uniqueness plus race-bounded physical action. |
| F-29 | Sparse deep UIA is complemented by cheap session-wide top-level native observation. | Unexpected windows, modal surfaces, foreground and desktop/session changes remain visible. |
| F-30 | Events mark scopes dirty; current scoped provider queries establish truth. | Provider events are not an event-sourced history. |
| F-31 | WorldSequence orders DESKTOPeye commits only; deltas are bounded and gaps explicit. | Cursor expiration triggers scoped resnapshot/sync, never silent continuation. |
| F-32 | `world.sync` is scoped current reconciliation. | It is not global idle, complete history, causal barrier or separate verification architecture. |
| F-33 | Condition waits are first-class; fixed sleeps are not correctness. | Waits final-query current predicates after signals/reconciliation/poll fallback. |
| F-34 | `stable_for` means debounce, not semantic UI quiescence. | Silence does not certify truth. |
| F-35 | SQLite WAL stores sparse current correspondence and bounded delta metadata. | No graph database, full tree, event history, screenshot history or action ledger. |
| F-36 | Cold recovery is the decisive correspondence gate. | A new provider epoch reconstructs exact keyed objects only and emits a gap. |
| F-37 | Generic replacement during an observation gap can be epistemically unknowable. | DESKTOPeye retires correspondence rather than manufacturing certainty. |
| F-38 | One Session Host is required in each target interactive session. | Secure/inaccessible desktop and lock/disconnect are represented as native facts. |
| F-39 | UIA provider work never runs on the kernel critical path. | Replaceable worker process lanes contain hangs; exact lane count is experimental. |
| F-40 | Direct native COM `IUIAutomation*` is the identity-critical UIA foundation. | Wrappers may inform or assist non-critical code but do not define contracts. |
| F-41 | Remote Operations is an optional optimization seam. | It is not Build 001 correctness, identity, recovery or isolation. |
| F-42 | The Program Host is Node 24 with no model inside. | One invocation performs at least 40 meaningful typed operations; target 50+, planned 60. |
| F-43 | Cross-substrate correlation is sparse and owner-respecting. | eyeBROWSE owns browser semantics; SHELLeye process/file truth; CODEeye source/build truth; future DOCSeye document meaning. |
| F-44 | Build 001 uses supported public user-mode APIs. | No injection, driver or native helper by default. |
| F-45 | Deterministic fixture-private IDs are test oracle only. | DESKTOPeye runtime may not read them as identity or verification. |
| F-46 | The virtualization recycle with duplicate visible semantics is the primary identity killer. | The old item handle must never mutate the recycled different item. |
| F-47 | Physical zero metrics are deterministic suite observations. | They do not assert a universal Windows atomic delivery guarantee. |
| F-48 | Common Item Dialog is a real-world post-gate smoke. | A–D do not depend on full eyeBROWSE/SHELLeye composition. |
| F-49 | Build 001 stack is C#/.NET 10 x64, direct UIA COM, USER32/DWM/WTS/WinEvent, WGC, SQLite WAL, named pipes and Node 24. | No additional language/runtime is frozen without a measured need. |
| F-50 | The five charter constraints are exhaustive. | No DESKTOPeye-specific safety/guardrail/verification/ledger architecture may be invented. |

## Deferred — implementation evidence decides

| ID | Question | Required decision method |
| --- | --- | --- |
| D-01 | single disposable UIA worker, pool, app-affine lanes, or per-app workers | provider-hang experiment; choose the coarsest topology that contains failure |
| D-02 | exact cache-request shapes and refresh batch boundaries | retained-workflow measurement |
| D-03 | UIA6 event group/register/remove ordering through recycle | lost-wakeup and late-event experiment |
| D-04 | framework transitions that preserve a user-perceived window across HWND recreation | explicit fixture lineage plus exact observation |
| D-05 | observed HWND churn/reuse frequency and watcher generation implementation | hostile native fixture; architecture already rejects bare-handle identity |
| D-06 | exact live COM reference/IUnknown behavior across reconstruction | same-epoch and restart experiment; cannot strengthen cold identity |
| D-07 | WPF placeholder/recycle/view-epoch mechanics | virtualized duplicate-row fixture |
| D-08 | precise WGC minimize/protection/device/session outcomes on target build | capture-state matrix |
| D-09 | exact pointer hit-test/preflight ordering and residual barrier window | moving/overlay fixture |
| D-10 | exact keyboard focus preflight and residual barrier window | focus-thief fixture |
| D-11 | negative-origin/per-monitor DPI transform implementation | interactive display experiment |
| D-12 | Desktop Duplication, PrintWindow, BitBlt, MSAA and OCR fallback breadth | later capability evidence; none is identity authority |
| D-13 | Remote Operations support/performance by provider | optional benchmark after base correctness |
| D-14 | Node named-pipe batching and result envelope | 40/50/60-call benchmark |
| D-15 | exact Common Item Dialog projection on target build | post-gate smoke |
| D-16 | framework breadth after WPF/native Build 001 | evidence-driven later capability work |

Deferred does not mean “implementation may choose any architecture.” Each experiment is bounded by the frozen rules above.

## Rejected — intentionally not pursued

| Rejected architecture/claim | Reason |
| --- | --- |
| screenshot/vision-first computer use as permanent model | coordinate rediscovery, weak retained identity, inefficient change/recovery |
| UIA-wrapper-only architecture | inherits provider coverage, identity and hang boundary |
| HWND/Win32-first identity | handles recycle and many semantic elements are windowless |
| selector-centered RPA identity | a reconstruction recipe is not a lifetime witness |
| RuntimeId as durable identity | documented reuse and provider-epoch scope |
| AutomationId as universal identity | optional, scoped, duplicate/generated/unstable |
| Name/text/tree path/index/geometry/visual similarity/embedding as exact identity | no documented generation guarantee |
| full accessibility-tree mirror | volatile/virtualized nodes, high invalidation/recovery/context cost, false durability |
| stateless UIA + screenshot + input hybrid | coverage without retained continuity/deltas/controller recovery |
| application-plugin federation as the center | no generic desktop continuity and unbounded plugin zoo |
| rigid universal actuation fallback ladder | operation fitness and semantics differ |
| raw input first | targetless stream semantics and foreground/focus races |
| semantic call equals human input | provider contract does not promise physical event equivalence |
| model call per click/keystroke | unnecessary latency/context and no local deterministic waits/branches |
| one giant raw UIA/Win32/PowerShell/coordinate operation | evades typed retained-object architecture |
| permanent UI tree, screen stream, event log or action ledger | violates sparse operational state and project constraints |
| graph database/universal StealthEye world database | no demonstrated requirement; cross-substrate coupling cost |
| global idle/quiescence guarantee | silence is not semantic truth |
| scalar confidence authorizes mutation | hides evidence category and permits false continuity |
| mandatory verifier agent/proof pipeline | ordinary revalidation/waits/postconditions are correctness, not separate architecture |
| DESKTOPeye-specific approval/policy/guardrail tiers | outside the five exhaustive constraints |
| DLL injection/universal hooks/driver by default | public user-mode APIs cover the Build 001 spine; added risk/complexity buys no required identity |
| C++ or Rust by default | no capability gap justifies another language |
| secure-desktop control/UIAccess deployment in Build 001 | outside the smallest decisive slice |
| full Office/browser DOM/IDE/document semantics | owned by domain substrates, not desktop correspondence |

## Change rule

A frozen decision changes only when implementation produces named evidence that contradicts its premise. The change must update the canonical architecture, Build 001 contract, this register and affected issues in one coherent commit. Experiments remain non-canonical until explicitly promoted.

```text
Architecture:           FINAL / SYNTHESIZED / VERIFIED / FROZEN FOR BUILD 001
Build 001:              PLANNED / NOT IMPLEMENTED
Product implementation: NOT STARTED
Build 001 acceptance:   NOT RUN
```
