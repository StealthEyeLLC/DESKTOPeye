# 08 — Workflow Pressure Tests

Status: **SPECIFIED / NOT RUN**

These workflows explain what DESKTOPeye must mean under pressure. The exhaustive 50-case acceptance table and exact metrics are normative in [02-BUILD-001-SLICE.md](02-BUILD-001-SLICE.md).

## 1. Cold recovery — correspondence, not application survival

### Setup

Retain an exact app instance, native window, keyed and generic controls, a keyed virtual item, an unkeyed duplicate row, current focus, a dialog, and a delta cursor. Persist the evidence. Keep both fixture applications alive.

### Perturbation

1. stop the correspondence kernel and every UIA worker;
2. while all DESKTOPeye observers are absent:
   - leave one keyed control/item unchanged;
   - destroy a generic control and create an indistinguishable replacement;
   - close dialog A and open identical dialog B;
   - reorder/filter the collection;
   - optionally restart one separate application instance;
3. restart kernel and workers, establishing a new provider epoch.

### Required result

- emit an explicit observation/recovery gap;
- recover the unchanged app instance/window only where exact native/process evidence survives;
- reconstruct keyed descendants exactly only under exact ancestors with no competitor;
- expose every `rebound_exact` evidence class;
- mark the generic replacement and old dialog/control stale, destroyed or ambiguous;
- do not preserve any descendant across a genuine app-instance restart;
- reconstruct current focus/modal/selection as new current state, not missing history;
- mutate no object during recovery;
- record zero false rebounds.

### Invariant

> Applications surviving DESKTOPeye proves Windows continuity. Only conservative logical reconstruction proves DESKTOPeye continuity.

## 2. Virtualization identity killer

### Setup

A WPF recycling collection contains domain item A and item B with duplicate visible name, type, layout and accessible neighborhood. A has a stable fixture-exposed semantic key available through the supported provider path; fixture-private oracle IDs remain unavailable to DESKTOPeye.

### Perturbation

```text
item A
→ UIA container/provider element E
→ retain item_A
→ scroll/sort/filter/recycle
→ container/provider element E now represents item B
→ select(old item_A)
```

### Required result

- item B remains untouched;
- the old representation incarnation is rejected;
- if A's durable key supports unique reacquisition under the exact collection/view, DESKTOPeye resolves A and selects A;
- otherwise the result is `virtualized`, `stale` or `ambiguous` and no mutation occurs;
- row index, RuntimeId, container reference, name, geometry and visual similarity cannot upgrade the result to exact.

### Invariant

> A UI container is a representation incarnation, not a dataset-item identity.

This is the primary Milestone C identity killer.

## 3. Identical dialog generation

### Setup

Open Confirm dialog A, retain `dialog_A` and its OK `control_A_ok` under the exact owner and modal relation.

### Perturbation

1. close dialog A;
2. open dialog B with identical title, text, AutomationId, control tree and geometry;
3. invoke old `control_A_ok`.

### Required result

- old control action returns stale/destroyed/ambiguous as supported by evidence;
- dialog B remains open and untouched;
- dialog B receives new logical concepts unless exact operation-native evidence intentionally created it;
- no scalar similarity score or selector reconstruction can authorize the old action.

### Invariant

> Identical semantics do not collapse distinct interaction lifetimes.

## 4. HWND reuse and window recreation

### Reuse case

Observe window A at HWND H, destroy it, pressure USER handle allocation until a different window B receives H, then act through old `window_A`.

Required: current process/thread/session/desktop/native-generation evidence contradicts the old incarnation; B is untouched; `IsWindow(H)` cannot rescue the old concept.

### Recreation with exact lineage

An exact fixture operation on window A explicitly replaces its native window root and supplies an operation-ground-truth transition visible to the test oracle. DESKTOPeye observes destroy/create without a gap and no competing successor.

Required: the same logical `window_*` may advance to a new native incarnation only if the public/provider evidence available to DESKTOPeye itself meets the frozen exact-lineage rule. The private oracle checks afterward; it is not an identity provider.

### Recreation without lineage

The same title/class/geometry/appearance returns after an observation gap or unobserved transition.

Required: old window becomes stale/ambiguous; current discovery gets a new concept.

## 5. Pointer race

### Setup

Resolve an exact retained target, record its current visual/display epoch and obtain current geometry/clickable point.

### Perturbations

- move the target before current hit tests;
- introduce a destructive overlay before current hit tests;
- make UIA and native hit tests disagree;
- change display topology/DPI after geometry;
- use a deterministic barrier to change the target immediately after the last possible public preflight.

### Required result

- every observable pre-injection change aborts with no input;
- no stale coordinate is reused;
- injection occurs only after exact app/window/provider validation, display-epoch validation, UIA/native hit testing and overlay rejection;
- postcondition is reconciled immediately;
- a barrier win after the final check yields `race_bounded_physical` plus failed/uncertain postcondition, never target-bound success;
- deterministic suite wrong pointer mutations equal zero.

### Invariant

> Preflight minimizes the public-API race; it does not create an atomic logical-target lease.

## 6. Focus theft and physical keyboard

### Setup

Retain textbox A in exact window A. Establish foreground, native keyboard focus and UIA focused-element agreement.

### Perturbations

1. focus thief B activates before final preflight;
2. `SetForegroundWindow` is denied;
3. native focus and UIA focus disagree;
4. modal dialog blocks the expected parent;
5. input desktop changes/locks;
6. deterministic barrier steals focus immediately after final preflight.

### Required result

- cases 1–5 abort with `focus_mismatch`, `foreground_denied`, `modal_blocked` or desktop/session unavailability;
- no text reaches B;
- only the smallest complete keyboard batch is inserted after all current checks;
- modifiers/current key state and Unicode route are reconciled;
- final value/postcondition is queried;
- case 6 remains `race_bounded_physical`, not a claimed OS guarantee;
- deterministic suite wrong keyboard mutations equal zero.

## 7. Provider hang and restart

### Setup

Retain scopes in two independent fixture applications. One exposes a deliberately blocking/broken UIA provider path; the other remains healthy.

### Perturbation

Call the blocking path beyond the broker deadline, then recycle the affected worker/provider epoch while querying/waiting in the healthy scope.

### Required result

- the kernel state writer and native observer remain responsive;
- the healthy app scope continues to answer and satisfy waits;
- affected bindings become unavailable/dirty with a provider-health delta;
- the old worker is abandoned/replaced without assuming cancellation succeeded;
- provider restart creates a new epoch and explicit gap/reconciliation boundary;
- exact reconstruction follows the same evidence rules as any provider rebound;
- provider stalls reaching kernel or unrelated scopes equal zero.

### Invariant

> Timeout configuration is not fault isolation; the replaceable process boundary is.

## 8. Event gap and bounded delta recovery

### Setup

Retain a window/control/collection scope and a starting world cursor. Configure a deliberately small test delta buffer.

### Perturbation

Omit, duplicate, reorder and delay provider events; restart a worker; overflow the bounded buffer; request deltas from the expired cursor.

### Required result

- events only mark interests dirty or supply lifecycle evidence;
- current scoped provider queries determine committed state;
- duplicate/late old-epoch signals do not create duplicate logical lifetimes;
- expired/discontinuous cursor returns an explicit gap with affected scope and resync requirement;
- `world.sync(scope)` reconciles only requested retained/current interests;
- no silent event/delta gaps occur.

## 9. Sparse-but-aware unexpected UI

### Setup

Retain a deep semantic scope in application A but no deep tree for application B.

### Perturbation

Application B creates an unexpected top-level/modal/popup surface and changes foreground.

### Required result

- cheap session-wide native observation reports lifecycle/foreground/modal evidence;
- DESKTOPeye promotes only a bounded discovery scope around the unexpected surface;
- it does not enumerate or persist the entire desktop UIA tree;
- old intended targets become `modal_blocked` or lose focus as current facts require.

## 10. Visual fallback

### Setup

The semantic parent exists, but one child target is intentionally inaccessible/owner-drawn. ChatGPT interprets one requested WGC crop and supplies a deterministic visual feature/intent once.

### Perturbation

Move the feature, change the visual/capture or display epoch, introduce a duplicate, or protect/fail the capture before action.

### Required result

- frame reference is tied to exact source/native incarnation, capture epoch, cursor, bounds and transform;
- Program Host revalidates current frame/revision and uniqueness locally;
- changed/duplicate/unavailable visual evidence aborts;
- the physical route still executes the normal pointer preflight;
- visual similarity does not become durable control identity;
- frames are not permanently archived.

## 11. One Program Host invocation

The canonical Milestone D invocation uses retained session, app instance, window, controls, a keyed virtual item, view epochs, semantic actions, dialog lifecycle, waits, deltas, one visual fallback, one physical pointer action, one physical keyboard action, and an intentional stale-dialog branch.

Required:

- at least 40 meaningful typed DESKTOPeye operations;
- target 50+, canonical plan 60;
- at least 90% of workflow primitives through the typed SDK;
- no model call between primitives;
- no direct COM/UIA, USER32, PowerShell, AutoHotkey, coordinate macro or fixture backdoor;
- compact final result containing promoted current state, cursor/deltas, route, assurance, postconditions and named ambiguity/staleness branches.

The exact numbered 60-call workflow is in section 20 of [02-BUILD-001-SLICE.md](02-BUILD-001-SLICE.md).

## 12. Common Item Dialog post-gate smoke

After A–D pass, initiate a standard Save As flow from a bounded application such as Notepad and operate the Windows Common Item Dialog through current native/UIA facets. A future composition may be:

```text
eyeBROWSE initiates browser upload
→ DESKTOPeye detects and operates native file picker
→ SHELLeye supplies exact file truth
→ eyeBROWSE resumes browser-native semantics
```

Build 001 smoke does not need the full three-substrate flow. It must only show that fixture-frozen identity/routing principles generalize to a non-toy OS dialog without becoming selector- or screenshot-first.

## 13. Information-density pressure test

Normal model-facing state should resemble:

```text
window_12 "Save As"
modal_to: window_8
focus: control_18
control_18 type: Edit value: "report.pdf"
control_41 type: Button name: "Save" enabled: true
preferred_action: Value / Invoke
world_cursor: 1842
```

It should not require a full 4K screenshot, raw UIA XML, full desktop tree, or coordinate rediscovery after every semantic action. Deep structure and visual frames remain available on explicit demand.

## 14. Hard metrics across deterministic hostile tests

```text
false window rebounds                  = 0
false dialog/control rebounds          = 0
false virtual-item rebounds            = 0
wrong semantic mutations               = 0
wrong pointer mutations                = 0
wrong keyboard mutations               = 0
ambiguous retained-target mutations    = 0
silent event/delta gaps                = 0
app restarts misclassified as same run = 0
provider stalls reaching kernel or unrelated app scopes = 0
```

The pointer and keyboard zeros describe the barrier-controlled suite. They do not assert universal atomic target binding by public Windows input APIs.

```text
Product implementation: PRESENT IN DEVELOPMENT / NOT YET ACCEPTED
Build 001 acceptance:   NOT RUN
```
