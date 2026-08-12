# 02 — Build 001 Slice

Status: **IMPLEMENTED / PENDING MEASURED ACCEPTANCE**
Architecture: **FROZEN FOR BUILD 001**
Acceptance: **NOT RUN**

Build 001 establishes the permanent DESKTOPeye spine. It proves identity, conservative recovery, virtualization safety, wrong-target resistance, sparse deltas, condition waits, provider isolation, operation-specific semantic/visual/physical routing, and local programmability. It does not prove every Windows framework.

## 1. Candidate stack

| Layer | Build 001 choice |
| --- | --- |
| Correspondence kernel | C# / .NET 10, x64 |
| UI Automation | direct COM `IUIAutomation*`; current HRESULTs, caches, events, timeout interfaces and UIA6 groups where supported |
| Native desktop | USER32, DWM, WTS, WinEvent, DPI/display APIs through managed interop |
| Capture | Windows Graphics Capture per-window primary; guarded screen-region fallback for composed pointer truth |
| Persistence | SQLite WAL |
| IPC | local Windows named pipes, versioned typed messages |
| Program Host | Node 24, separate local process |
| Fixtures | one WPF identity-torture app and one native Win32 adversary app |

No C++, Rust, DLL injection, driver, global package installation, service, scheduled task, source project, fixture, or runtime state was created by the architecture pass. A tiny native helper is permitted during implementation only if a measured capability is materially unavailable through managed interop/direct COM.

## 2. Process topology

Build the smallest topology that preserves the permanent isolation boundaries:

```text
desktop-eye gateway             stateless model-facing transport
desktop-eye program host        disposable Node 24 process per invocation
desktop-eye kernel              persistent correspondence/state owner
desktop-eye session host        one instance in target interactive session
  native observer lane          WTS/desktop/USER/DWM/WinEvent/display
  UIA coordination MTA          stable subscription/event lane
  UIA operation worker(s)       replaceable processes; no kernel-thread UIA
  capture lane                  WGC + requested composed-region fallback
  input actuator                focus/hit preflight + smallest input batch
fixture-wpf                     future deterministic test target only
fixture-win32                   future deterministic adversary only
```

The coordination MTA may initially share the Session Host process only if no provider call that can hang is made there. UIA query/pattern/cache work must occur in replaceable worker processes. Exact worker count and app affinity are resolved by Experiment E-01; the process contract must support recycling and reassignment without changing logical IDs.

## 3. Persistence model

Use one SQLite WAL operating database outside the repository. The schema must support these logical records; exact SQL is implementation-owned.

| Record family | Required content |
| --- | --- |
| concepts | opaque logical ID, type, lifecycle/status, creation/retirement sequence |
| app identities | package/AUMID/binary/domain evidence and current app-instance links |
| incarnations | app-instance lifetime, native window generation, dialog/control/item representation incarnations |
| provider bindings | provider kind, worker/provider epoch, opaque provider witness, exact ancestor scope, availability |
| identity evidence | evidence class, witness fields, contradictions, reconstruction decision |
| relations | sparse app→instance→window→dialog/control/collection/item and modal/focus/selection links |
| epochs | machine/boot links, logon/session/desktop/provider/view/display epochs |
| interests | retained/watch/action interests and intended scoped cache/subscription plan |
| deltas | bounded logical changes and gap markers only |
| cursors | consumer position, retention floor, expiration/gap metadata |
| provider health | current/recent operational state needed for routing/recovery |

Do not persist frames, screenshots, all provider events, all UIA elements, all text ranges, unretained rows, action history, action receipts, or a permanent provenance ledger.

## 4. Protocol direction

All boundaries use typed, versioned messages. Provider objects and COM pointers never cross process boundaries.

```text
gateway / Program Host
  → kernel: query, retain, release, watch, sync, wait, action, delta-read
  ← kernel: logical results, evidence/status, cursors, bounded deltas, typed errors

kernel
  → Session Host: scoped native/capture/input work with deadlines and expected epochs
  ← Session Host: observations, provider results, dirty signals, health, session/desktop changes

Session Host
  → UIA workers: scoped query/cache/pattern operation with worker deadline
  ← UIA workers: serialized observations/results/errors only
```

Every mutation request carries logical target ID, expected exact ancestor scope, relevant observed epochs/revisions, requested operation, deadline, and desired postcondition. An ephemeral operation ID correlates in-flight work and retries; it is not a permanent ledger or mutation authority.

Provider errors are typed at minimum as `element_unavailable`, `provider_timeout`, `provider_unhealthy`, `wrong_epoch`, `not_supported`, `disabled`, `no_clickable_point`, `capture_unavailable`, `protected_content`, `foreground_denied`, `focus_mismatch`, `no_interactive_desktop`, `target_moved`, `hit_test_mismatch`, `stale`, and `ambiguous`.

## 5. Per-session host contract

The Build 001 Session Host runs inside the target interactive `STEALTHEYELLC\StealthEye` session.

It must:

- identify its WTS session, window station, desktop and current input desktop;
- advance/announce desktop epoch on lock, unlock, desktop switch, reconnect/disconnect or inaccessible transition;
- maintain cheap session-wide top-level native lifecycle/foreground/focus observation;
- enumerate/reconcile current top-level HWND reality on startup and gaps;
- bind every HWND observation to exact session/desktop and SHELLeye process-incarnation evidence;
- reject physical input when not on the expected unlocked input desktop;
- track display-topology epoch for capture and input transforms;
- supervise/recycle UIA workers without losing the native observation lane;
- expose current health even while one UIA provider is hung.

Secure desktop control, UIAccess deployment, RDP product support, and multi-session orchestration are non-goals. Secure/inaccessible state must be reported, not bypassed.

## 6. Native provider contract

Build 001 native observations include:

- top-level/owned HWND enumerate, create, destroy, show, hide and location;
- creating thread/current PID correlated to exact SHELLeye process incarnation;
- class, styles/ex-styles, parent, owner and root-owner;
- title as a property, never identity;
- foreground, active/native focus via appropriate thread queries;
- visibility, minimize/show state and DWM cloaking;
- DWM extended frame bounds;
- current z-order/hit-test facts on demand;
- input desktop/session changes;
- display topology/DPI revision on demand and on change signal.

Native generation is constructed only under uninterrupted create/destroy observation. On any native observation gap, all possibly affected generations require enumeration/reconciliation and may lose exact continuity.

## 7. UIA provider/worker contract

### Foundation

Use direct COM, not `System.Windows.Automation` or a wrapper, for identity-critical work. Create the newest supported `CUIAutomation` interface and gracefully fall back by interface availability.

### Apartments and events

- initialize UIA worker and event threads as MTA;
- keep event registration/removal on the required stable MTA thread;
- use UIA6 handler groups on the target build when supported;
- callbacks capture only the event type, source witness/cache payload, epoch and minimal metadata, then return;
- never call back into kernel state mutation or arbitrary provider traversal from a callback;
- event/cache observations received after worker/provider retirement can only mark a gap/dirty scope.

### Views and caches

- Control View is normal projection;
- Content View is explicit content/document inspection;
- Raw View is diagnostics/recovery only;
- compile retained interests into bounded `CacheRequest` plans;
- use `FindFirst/FindAllBuildCache` or `BuildUpdatedCache` for scoped bulk observations;
- do not recursively walk or mirror the desktop tree;
- capture only properties/patterns needed for identity, result, waits and declared actions.

### Fault behavior

- kernel deadlines are shorter than any period that would make the world unresponsive;
- on timeout/RPC failure/hang, terminate/recycle the affected worker;
- advance provider epoch when provider-object continuity is lost;
- mark affected bindings unavailable/dirty and emit provider-health/gap deltas;
- keep native, kernel and unrelated app scopes responsive;
- never silently retry a mutation against a newly reconstructed target.

Remote Operations are a deferred optimization seam. Build 001 must work without them.

## 8. Capture provider contract

Build 001 must provide:

- on-demand WGC capture of an exact current top-level window/native incarnation;
- crop/region extraction bound to a source frame revision;
- frame metadata: source, native/capture/display epochs, size, QPC/system-relative time where available, bounds/transform, world cursor and limitations;
- explicit outcomes for minimized, closed, inaccessible, unsupported, protected, blank/stale frame and device/session loss;
- one on-demand composed screen-region observation for the pointer adversary/overlay check if WGC alone cannot represent the actual topmost pixels;
- no continuous screen stream and no persisted frame history.

PrintWindow may be tested only as a deadline-contained fallback. Desktop Duplication and OCR are not Build 001 dependencies.

## 9. Input actuator contract

### Semantic operations

Support UIA `Invoke`, `Value`, `SelectionItem`, `Toggle`, `ExpandCollapse`, `Scroll`, `ScrollItem`, and relevant `Window` patterns needed by the fixture. Validate exact binding, exact ancestors, current pattern availability and enabled state immediately before the provider call. Return `provider_semantic` assurance and reconcile the declared postcondition.

### Pointer

For the one physical pointer path:

1. resolve exact logical target;
2. validate exact app/window/provider/native scopes;
3. ensure expected unlocked input desktop and stable display epoch;
4. read fresh bounds/clickable point or current visual feature;
5. verify target state and current frame/visual revision;
6. perform UIA `ElementFromPoint`, native `WindowFromPoint`/z-order and overlay checks;
7. abort on motion, ambiguity, disagreement, stale coordinates or topology change;
8. inject the smallest pointer batch with `SendInput` virtual-desktop mapping;
9. reconcile postcondition and return `race_bounded_physical` or `delivery_uncertain`.

### Keyboard

For the one physical keyboard path:

1. resolve exact logical field and containing app/window;
2. verify expected unlocked input desktop and integrity reachability;
3. request foreground; verify actual foreground or return `foreground_denied`;
4. request focus with the strongest route;
5. verify native GUI-thread focus, UIA focus where exposed, modal state and modifier state;
6. abort `focus_mismatch` on a focus thief;
7. inject the smallest complete batch using Unicode or scan-code semantics appropriate to the test;
8. reconcile value/postcondition and report `race_bounded_physical` or `delivery_uncertain`.

No success result may claim Windows atomically bound physical delivery to the logical target.

## 10. Concept model required by Build 001

Build 001 implements only:

- `app_*` and `appinst_*`;
- `window_*` plus current native window incarnation and UIA root binding;
- `dialog_*` for native and in-window modal/flyout lifetimes;
- generic `control_*` with ControlType, patterns and provider facets;
- `collection_*`, collection/view epoch and sparse retained interest;
- keyed `item_*` plus ephemeral/unkeyed query results and representation incarnation;
- focus, selection and modal relations;
- ephemeral visual frame references.

It does not create permanent concrete classes for every button/textbox/list type. Typed SDK affordances are capabilities/facets over generic controls.

## 11. Identity rules required by Build 001

### Exact mutation gate

A mutation is eligible only when:

- target status is `exact` or rare `rebound_exact`;
- exact app instance and exact window/dialog ancestors remain current;
- all relevant session/desktop/provider/native/view epochs match;
- the selected operation is currently supported and enabled;
- no contradictory replacement, modal, focus or overlay evidence exists;
- route-specific preflight passes.

`candidate`, `ambiguous`, `stale`, `destroyed`, `unavailable`, or unkeyed-virtualized items are not mutable.

### HWND

Store HWND only inside a native incarnation record. Combine session/desktop + exact process incarnation + creating thread + observed create/destroy generation. Never cold-rebind from HWND/title/class/geometry alone.

### UIA

- RuntimeId is an opaque incumbent witness in one provider epoch;
- AutomationId is optional scoped reconstruction evidence;
- Name/value/text is property evidence only;
- a surviving UIA reference is same-epoch incumbent evidence, not persistent identity;
- `CompareElements` equality is RuntimeId equality and is not elevated above that contract;
- any exact rebound exposes its evidence class.

### Items

- hidden fixture data keys are not visible to DESKTOPeye runtime;
- the fixture must expose one stable key through a normal provider/app property selected for the product test, distinct from its private oracle ID;
- keyed items may survive sort/filter/virtualization within the exact collection/dataset lifetime;
- unkeyed duplicate rows never receive durable `item_*` continuity;
- every container/placeholder belongs to a representation incarnation and can be invalidated/recycled;
- advancing collection/view epoch invalidates positional evidence.

## 12. Deltas

Implement compact deltas for:

- app/app-instance lifecycle;
- window lifecycle and native-incarnation change;
- dialog/control lifecycle and provider binding;
- focus/modal/selection;
- value, enabled, visibility and geometry;
- collection/view epoch, keyed item representation and virtualization;
- provider health/restart;
- capture/display invalidation;
- session/desktop transition;
- explicit gap and reconciliation completion.

Retention must be bounded and configurable by bytes/count/time. The fixture test deliberately expires a cursor. An expired/discontinuous read must return `gap`, affected scopes and a scoped sync/resnapshot prescription. It may never silently return a misleading contiguous delta list.

## 13. Waits

Required typed waits:

- `wait.app_instance_exists/exits`;
- `wait.window_exists/closes`;
- `wait.dialog_exists/closes`;
- `wait.control_exists/disappears`;
- `wait.enabled`;
- `wait.focus`;
- `wait.value` with typed predicate;
- `wait.selection`;
- `wait.collection_view_epoch`;
- `wait.popup/menu`;
- `wait.provider_health`;
- `wait.visual` for deterministic fixture pixels;
- `wait.session_desktop`;
- optional `stable_for` debounce modifier.

Each wait performs current query → scoped interest/subscription → signal/reconcile with bounded polling fallback → final current query. Timeouts return current evidence and cursor. No fixture or workflow may use fixed sleeps as correctness.

## 14. `world.sync`

Request shape must include scope, current interests or explicit projection, provider set, deadline and caller cursor. Result includes committed cursor, deltas, unresolved provider health, and any gap consumed.

Build 001 must prove that sync:

- reconciles only retained/requested scope;
- detects missed/replaced current state through queries;
- does not traverse the global UIA tree;
- does not claim desktop idle or complete history;
- can run while an unrelated app provider is hung.

## 15. Deterministic fixtures

Fixtures are future Build 001 test targets, not product providers.

### WPF identity-torture fixture

Include:

- standard semantic textbox, button, toggle, selection and expand/collapse controls;
- duplicate names;
- duplicate and missing AutomationIds;
- dynamic labels and values;
- asynchronously enabled control behind a deterministic barrier;
- sequential identical Confirm dialogs;
- simultaneous identical dialogs with exact owner/generation oracle;
- windowed and in-window/custom flyout/modal surfaces;
- control destroy/recreate and exact parent recreate;
- same-HWND navigation replacing the semantic subtree;
- recycling virtualized list with duplicate visible rows;
- sort/filter/view-epoch change;
- keyed and deliberately unkeyed items;
- virtualized placeholder/realize behavior;
- tree collapse/rematerialization;
- inaccessible owner-drawn canvas target whose visual feature moves under a barrier;
- deterministic state-change and private ground-truth log.

### Native Win32 adversary

Include:

- same-title/class windows;
- create/destroy/recreate and best-effort HWND churn/reuse pressure;
- native owner/parent/popups and disabled modal owner;
- moving pointer target and deterministic overlay;
- focus thief and foreground-denial setup;
- display/coordinate stimulus hooks that tests can coordinate without changing OS configuration broadly;
- optional deliberately blocking/broken UIA provider surface.

### Oracle boundary

Private fixture object IDs and logs are test oracle only. DESKTOPeye runtime, identity reconstruction and actuation must not read them. Acceptance compares product outcomes to the oracle after the operation. This is not a verifier architecture.

## 16. Provider-hang fixture

Provider isolation is load-bearing, so Build 001 must include one deterministic provider call that blocks longer than the broker deadline.

Required proof:

- the affected UIA worker reaches deadline and is terminated/recycled;
- the kernel remains responsive to world queries and native deltas;
- another retained application scope continues querying/waiting/acting;
- affected bindings become `unavailable`/dirty and provider health changes;
- worker/provider epoch advances;
- no mutation is silently replayed after restart;
- later reconciliation returns only exact reconstructable objects.

The fixture may implement the smallest custom provider behavior necessary. It must not hang the fixture's whole OS session or become product code.

## 17. Milestone A — Persistent Desktop Correspondence

### Setup

Retain:

- one `app_*` and exact `appinst_*`;
- main `window_*` and current native incarnation;
- one dialog generation;
- one keyed `control_*` and one deliberately replaceable generic control;
- one `collection_*`, one keyed `item_*`, and one unkeyed duplicate row result;
- current focus/modal state;
- a world cursor and identity evidence.

### Acceptance sequence

1. restart only the gateway; verify logical IDs/cursor remain usable;
2. restart the kernel while Session Host/UIA coordination remains; establish warm gap and validate same-epoch incumbents;
3. stop kernel and all UIA client/worker processes while both fixtures remain alive;
4. while absent, deterministically change values, recycle the virtualized container, replace the generic control, close dialog A/open identical B, and leave keyed control/item supportable;
5. restart into a new provider epoch;
6. enumerate native reality and correlate exact app instance through SHELLeye;
7. recover exact keyed objects only under exact recovered ancestors;
8. classify replaced/indistinguishable generic objects `stale` or `ambiguous`;
9. reconstruct current focus/modal state as new current state;
10. emit explicit recovery gap plus recovery/binding deltas;
11. attempt mutations through all old handles and verify only exact supportable objects act;
12. repeat with genuine app restart: same `app_*`, new `appinst_*`, all descendants new.

### Gate

- exact keyed app-instance/window/control/item reconstruction where the evidence contract supports it;
- no claim of exact window recovery across an unobserved native generation gap without a stronger key/lineage;
- replaced generic control and dialog-A button never rebound;
- explicit provider epoch and explicit gap;
- zero false rebounds.

Merely observing that fixture applications survived kernel death does not pass A.

## 18. Milestone B — Retained Desktop Objects / Delta First

Required proof:

- sparse promotion for app, app instance, window, dialog, controls, collection and keyed item;
- unretained query results remain ephemeral;
- cheap session-wide native top-level/foreground/focus observation remains active;
- deep UIA interest is scoped to retained/watched/action targets;
- scoped cache plans contain only requested properties/patterns;
- events mark dirty and current query reconciles;
- bounded logical deltas and explicit cursor expiration gap;
- every required condition wait works without correctness sleeps;
- `world.sync` reconciles a requested scope and works with an unrelated hung provider;
- normal semantic operation returns delta/postcondition without mandatory full tree or screenshot;
- no repeated selector rediscovery for retained exact targets.

Anti-cheat: B does not pass if normal observation is `FindAll → action → FindAll`, if the full UIA tree is serialized after every action, or if every semantic action requires a screenshot.

## 19. Milestone C — Recovery Continuity / Identity Killer

Run all 50 deterministic cases. Each case has one required outcome.

| # | Hostile case | Required outcome |
| ---: | --- | --- |
| 1 | Observed window A destroyed; later B receives recycled HWND H | A `destroyed`; B gets new window/native incarnation; old A mutation rejected |
| 2 | Cold gap spans possible HWND H destruction/reuse | no exact rebound from H alone; result stale/ambiguous unless stronger lineage exists |
| 3 | Two concurrent windows share title, class, geometry and appearance | distinct only by exact generation/owner/app evidence; weak lookup ambiguous |
| 4 | Exact incumbent operation causes documented HWND recreation | same `window_*` only when operation/strong lineage proves transition; new native incarnation |
| 5 | Unobserved recreation yields same title/class/geometry | no exact continuity; stale/ambiguous |
| 6 | Genuine application restart resembles prior run | same `app_*`, new `appinst_*`, all descendant mutations through old IDs rejected |
| 7 | Native parent/owner is destroyed and replaced | descendant bindings stale/destroyed; no reparent-by-similarity |
| 8 | Dialog A closes; identical dialog B opens | old A/OK rejected; B untouched |
| 9 | Two identical dialogs exist simultaneously | exact owner/generation keeps distinct or target is ambiguous; no guessed action |
| 10 | Modal dialog disables parent | parent controls report `modal_blocked`; mutation refused until modal closes |
| 11 | Unexpected popup/flyout appears outside retained UIA subtree | session-wide native signal triggers scoped discovery; popup represented without global mirror |
| 12 | Sibling controls have duplicate visible names | name lookup ambiguous; retained exact target not switched |
| 13 | Target has no AutomationId | other evidence may retain same-epoch incumbent; no fabricated durable identity |
| 14 | Siblings have duplicate AutomationId | AutomationId lookup ambiguous; exact mutation refused |
| 15 | Replacement control reuses AutomationId | old control stale/destroyed; replacement new; no AutomationId rebound alone |
| 16 | Provider reconstructs same element with a changed RuntimeId | exact rebound only with stronger exact witness; otherwise stale/candidate |
| 17 | RuntimeId/slug-like witness is reused after provider replacement | old retained object never maps to replacement from RuntimeId alone |
| 18 | Live UIA reference returns `UIA_E_ELEMENTNOTAVAILABLE` | binding invalidated; realize only if virtual-item contract applies; no blind retry |
| 19 | Control destroyed/recreated with identical properties | old control rejected; replacement new |
| 20 | Exact parent subtree is recreated | descendants stale unless independently strong keyed reconstruction under exact new parent is justified |
| 21 | Same HWND navigates to a replacement semantic page | window may remain; old page controls stale; no tree-path rebound |
| 22 | Exact target becomes disabled before semantic call | no mutation; typed `disabled`/state result |
| 23 | Exact target disappears between resolution and semantic call | no fallback target; typed stale/unavailable result |
| 24 | UIA/WinEvent signal is missing, duplicated, late or reordered | event only marks dirty; scoped current query establishes truth; no false delta history |
| 25 | Primary killer: A's recycled container E now represents duplicate-looking B | old A action never touches B; keyed A reacquired or stale/virtualized/ambiguous |
| 26 | Keyed item changes index after sort | same `item_*`, new representation/view epoch, action reaches keyed item |
| 27 | Keyed item filtered out then restored | same logical item may be virtualized/unrepresented, then reacquired by key |
| 28 | Unkeyed duplicate rows reorder after sort/filter | no durable item continuity; old row target ambiguous/stale |
| 29 | Virtual placeholder invalidated by another `FindItemByProperty`/viewport change | old placeholder not used; re-find by durable key or return virtualized/stale |
| 30 | `Realize` changes viewport/container materialization | item key remains authority; representation revision advances; no container identity |
| 31 | Provider exposes selection only for viewport items | selection result states scope/partiality and reconciles keyed selected item where supportable |
| 32 | Tree node collapse destroys/virtualizes descendants | descendants virtualized/stale; no positional rebound on re-expand |
| 33 | Pointer target moves before final check | abort `target_moved`; zero click mutation |
| 34 | Destructive overlay appears before final check | hit/overlay mismatch aborts; underlying/overlay targets untouched |
| 35 | UIA and native hit tests disagree or child is windowless | no raw click unless route-specific evidence resolves disagreement; otherwise abort |
| 36 | DPI/display topology/virtual origin changes after point computation | display epoch mismatch aborts stale-coordinate action |
| 37 | Target is minimized, cloaked, offscreen or materially occluded | state remains distinct; pointer path refused unless current interactability is proven |
| 38 | Focus thief activates B before final keyboard preflight | `focus_mismatch`; no text reaches B |
| 39 | Windows denies foreground transfer | `foreground_denied`; no key/pointer injection |
| 40 | foreground, active, native focus and UIA focus disagree during transition | no typing until declared target focus predicate is current; otherwise abort |
| 41 | Modifier state/Unicode path plus deterministic post-preflight focus barrier | smallest batch and postcondition measured; wrong recipient is a failure, assurance remains race-bounded |
| 42 | UIA worker/provider restarts while app stays | provider epoch advances; only strong exact reconstructions rebound; no weak mutation replay |
| 43 | Cold recovery with no change and strong app/control/item keys | exact supported concepts `rebound_exact` with evidence; cursor gap remains explicit |
| 44 | Cold gap: A destroyed, indistinguishable B created | A never exact-rebounds; B is candidate/new concept; old mutation rejected |
| 45 | Delta cursor expires and events are intentionally dropped | explicit `gap`; scoped sync/resnapshot; no silent continuity |
| 46 | One provider call blocks beyond deadline | worker recycled; kernel and other app scopes responsive; no mutation replay |
| 47 | Session locks or input desktop switches during pending work | desktop epoch advances; physical work refused; semantic availability reported honestly |
| 48 | Capture is protected/blank/unavailable or accessible value is protected | explicit protected/unavailable result; no OCR/value fabrication |
| 49 | Visual-only target is moving or multiple visual candidates are equally valid | current visual revision/uniqueness required; otherwise pointer action refused |
| 50 | `appinst_*` has multiple plausible SHELLeye cohorts or browser/window correlations | sparse link remains ambiguous; target-bound cross-substrate action refused |

### Milestone C hard metrics

Across this deterministic barrier-controlled suite:

```text
false window rebounds             = 0
false dialog/control rebounds     = 0
false virtual-item rebounds       = 0
wrong semantic mutations          = 0
wrong pointer mutations           = 0
wrong keyboard mutations          = 0
ambiguous retained-target mutations = 0
silent event/delta gaps           = 0
```

Additional gates:

```text
app restarts misclassified as same appinst = 0
provider stalls reaching kernel/unrelated scopes = 0
```

Pointer/keyboard zeros are measured acceptance results under deterministic hostile barriers. They are not universal mathematical guarantees about Windows physical input.

## 20. Milestone D — Programmable Desktop Operation

One fresh Node 24 Program Host process receives one local program and returns one compact result. No model call occurs between primitives.

Hard floor: **40 meaningful typed DESKTOPeye calls**.
Target: **50+**.
Typed-operation share: **at least 90%** of workflow operations.
Canonical planned workflow below: **60 typed calls**; none exists solely to inflate count.

### Canonical invocation

| # | Typed call | Purpose |
| ---: | --- | --- |
| 1 | `session.current` | bind target interactive session/desktop epoch |
| 2 | `world.cursor` | establish delta cursor |
| 3 | `app.query` | find fixture application concept |
| 4 | `app.retain` | promote app |
| 5 | `app_instance.current` | resolve exact live cohort |
| 6 | `app_instance.retain` | retain run lifetime |
| 7 | `window.query` | find main logical window |
| 8 | `window.retain` | retain window/native/provider facets |
| 9 | `window.get_state` | establish visibility/modal/focus facts |
| 10 | `world.sync` | reconcile retained window scope |
| 11 | `delta.read` | consume initial compact delta |
| 12 | `control.query` | find textbox under exact window |
| 13 | `control.retain` | retain textbox |
| 14 | `control.query` | find toggle under exact window |
| 15 | `control.retain` | retain toggle |
| 16 | `collection.query` | find virtualized collection |
| 17 | `collection.retain` | promote collection/view epoch |
| 18 | `collection.get_view` | record current sort/filter/epoch |
| 19 | `item.find_by_key` | find durable keyed item |
| 20 | `item.retain` | promote logical item |
| 21 | `item.realize` | realize provider representation |
| 22 | `item.scroll_into_view` | make representation actionable |
| 23 | `item.select` | semantic selection |
| 24 | `wait.selection` | wait/reconcile selection predicate |
| 25 | `delta.read` | consume selection/realization deltas |
| 26 | `collection.sort` | semantic sort change |
| 27 | `wait.collection_view_epoch` | await exact view transition |
| 28 | `item.resolve` | reacquire retained keyed item |
| 29 | `item.get_representation` | prove changed index/container |
| 30 | `collection.filter` | hide keyed item |
| 31 | `wait.control_state` | observe item virtualized/unrepresented |
| 32 | `collection.clear_filter` | restore view |
| 33 | `wait.collection_view_epoch` | await restored view |
| 34 | `item.resolve` | reacquire by key, not index/container |
| 35 | `control.query` | find semantic menu control |
| 36 | `control.retain` | retain menu control |
| 37 | `control.expand` | semantic menu/flyout action |
| 38 | `wait.popup` | wait for unexpected/top-level popup scope |
| 39 | `control.query` | find command in exact popup generation |
| 40 | `control.invoke` | invoke command semantically |
| 41 | `wait.value` | wait command postcondition |
| 42 | `control.set_value` | set retained textbox semantically |
| 43 | `wait.value` | verify exact value predicate |
| 44 | `control.query` | find Continue button |
| 45 | `control.invoke` | open identical Confirm dialog A |
| 46 | `wait.dialog_exists` | wait and bind exact dialog generation |
| 47 | `dialog.retain` | retain A |
| 48 | `control.query` | find A's OK/Cancel controls |
| 49 | `control.invoke` | branch locally and choose exact control |
| 50 | `wait.dialog_closes` | observe destruction of A generation |
| 51 | `wait.dialog_exists` | bind the later identical dialog B generation |
| 52 | `control.invoke` | deliberately invoke retained old A button; require stale rejection and no B mutation |
| 53 | `capture.window_region` | capture inaccessible canvas facet on demand |
| 54 | `visual.revalidate` | require current unique supplied feature/revision |
| 55 | `pointer.click` | one race-bounded physical pointer route |
| 56 | `wait.value` | reconcile canvas postcondition |
| 57 | `focus.ensure` | foreground/focus exact physical text target |
| 58 | `keyboard.type` | one race-bounded physical keyboard route |
| 59 | `wait.value` | reconcile typed-value postcondition |
| 60 | `delta.read` | consume the final bounded logical delta range |

The local program additionally branches on the expected stale result at call 52, aborts if visual/focus evidence becomes ambiguous, and computes the compact return object locally. Those local calculations are not counted as typed DESKTOPeye calls.

### Required compact result

Return only:

- exact app/app-instance/window/dialog/control/item logical IDs used;
- initial/final world cursor and compact delta counts/classes;
- item key plus before/after view epoch/index representation;
- semantic postconditions;
- stale old-dialog branch result;
- visual frame/revision and pointer postcondition;
- keyboard focus/value postcondition;
- route and assurance for every mutation;
- provider health/gap summary;
- bounded timing/provider-call metrics.

No raw full UIA XML, full tree, screenshot history, action ledger, or verbose per-call transcript is required.

### Anti-cheat

D does not pass through one giant PowerShell script, raw UIA script, raw Win32 program, coordinate macro, fixture-private API, or single opaque `executeWorkflow` call. The workflow must operate retained typed DESKTOPeye concepts through the kernel's waits, deltas and broker.

## 21. Real-world post-gate smoke

After A–D pass, operate one Windows Common Item Dialog initiated from Notepad Save As. This is the smallest non-toy generalization target because it combines native frame/dialog ownership, standard semantic controls, focus, path/value and lifecycle.

It is a smoke test, not a core A–D dependency and not permission to automate the desktop during architecture setup. A later cross-substrate smoke may have eyeBROWSE trigger file upload, DESKTOPeye operate the native picker, SHELLeye supply exact file truth, and eyeBROWSE resume.

Calculator, Settings, Explorer and VS Code are not stronger first smoke targets: they either prove too little or introduce domain/framework breadth that does not improve the core gate.

## 22. Benchmark

The benchmark is subordinate to correctness and freezes no performance threshold before measurement.

### Compared systems

| Mode | Loop |
| --- | --- |
| screenshot computer use | screenshot → model → coordinates/input → screenshot → rediscover |
| UIA query wrapper | selector/query → action → re-query |
| DESKTOPeye | retained concepts → brokered action → condition wait → logical delta → local Program Host; visual fallback only where required |

### Tasks

- standard form plus async enabled/dialog;
- identical dialog replacement;
- virtualized list sort/filter/recycle;
- moving/ambiguous visual-only canvas target;
- focus-stealing physical input;
- cold recovery with unseen mutations;
- provider hang isolation;
- Common Item Dialog post-gate smoke.

### Metrics

- task success and required zero correctness metrics;
- model turns;
- typed operations per model turn;
- screenshot count and image bytes;
- semantic bytes and tree bytes;
- object rediscoveries;
- provider/cache calls and cache bulk ratio;
- wrong-target attempts/refusals/mutations;
- action, wait and recovery latency;
- worker recycle/unrelated-scope latency;
- compact final-result bytes.

Record measured baselines; do not invent pass/fail latency or byte thresholds.

## 23. Explicit non-goals

Build 001 excludes:

- macOS and Linux;
- RDP or remote/virtualized desktop as a product target;
- secure desktop control;
- full UIAccess/elevation deployment;
- all Windows UI frameworks and full Office semantics;
- browser DOM/network automation;
- terminal/shell process semantics;
- IDE/source/build/diagnostic semantics;
- document-domain semantics;
- continuous screen/video stream;
- full UIA-tree mirror;
- graph database or universal StealthEye world database;
- custom OCR model or general CV pipeline;
- Desktop Duplication as a core dependency;
- touch, pen, full drag/drop breadth;
- toast/notification product;
- virtual-desktop management;
- universal application plugins;
- DLL injection, in-process arbitrary hooks, kernel driver;
- human dashboard, macro recorder, selector-authoring UI, RPA designer;
- DESKTOPeye-specific approval/policy/safety architecture;
- permanent action/screenshot/event ledger;
- performance thresholds without measurement.

## 24. Required implementation experiments

Every item below is explicitly:

**OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED**

| ID | Smallest decisive experiment | Decision unlocked |
| --- | --- | --- |
| E-01 | Block one provider call; compare one disposable worker, small pool and app-affine recycling while another app scope is active | exact UIA worker count/granularity |
| E-02 | Register/remove UIA6 handler groups on stable MTA; induce worker recycle and late events | event-lane topology and epoch invalidation |
| E-03 | Compare live `IUIAutomationElement`/`CompareElements`/IUnknown behavior across reconstruction and provider restart | same-epoch live-reference treatment; no durable promotion expected |
| E-04 | Exercise WPF/native window recreation transitions under observed operations | which transitions supply exact logical-window lineage |
| E-05 | Force HWND churn/reuse pressure with and without observation gaps | confirm native generation witness and cold-gap retirement behavior |
| E-06 | Measure scoped `CacheRequest` plans versus current-property loops under the retained workflow | cache-plan defaults and batch boundaries |
| E-07 | Exercise WPF recycling, placeholders, `ItemContainer`, `VirtualizedItem`, sort/filter and duplicate rows | item representation/view-epoch rules |
| E-08 | Cold-restart kernel/workers around no-change, keyed change, generic replacement, dialog replacement and app restart | exact reconstruction envelope and gap deltas |
| E-09 | Capture exact window while occluded, minimized, protected, resized, closed and through desktop/session change | WGC result-state contract and fallback trigger |
| E-10 | Barrier target movement/overlay between geometry, hit tests and injection | pointer preflight order and observed race ceiling |
| E-11 | Barrier foreground/focus theft before and immediately after final preflight | keyboard abort semantics and measured residual race |
| E-12 | Per-monitor DPI/negative virtual origin/display-change barrier | physical coordinate transform and display-epoch invalidation |
| E-13 | Deliberately omit/reorder/duplicate events and expire cursors | dirty/reconcile, gap and scoped sync semantics |
| E-14 | Invoke/Value/Selection/Toggle versus physical input in fixtures | route fitness, provider-semantic behavior and postconditions |
| E-15 | Inspect Common Item Dialog through native/UIA/capture facets after A–D | real-world smoke projection; not core architecture |
| E-16 | Measure Node 24 named-pipe typed workflow with 40/50/60 calls | protocol batching/result compactness, not identity |

The architecture/specification pass itself ran no experiment. The current authorized Build 001 implementation pass may execute these experiments and acceptance gates prospectively; measured results remain non-canonical until recorded under the completion boundary below.

## 25. Build completion artifacts

Only after all gates pass may an implementation pass add `docs/09-BUILD-001-RESULTS.md`. It must include machine/toolchain versions, case-by-case outcomes, exact zero metrics, benchmark measurements, discovered deviations and canonical decision updates. Until then:

```text
Product implementation: IMPLEMENTED / PENDING MEASURED ACCEPTANCE
Build 001 acceptance:   NOT RUN
```
