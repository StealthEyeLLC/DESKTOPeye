# 05 — Research Baseline

Status: **SYNTHESIZED / PRIMARY-SOURCE VERIFIED / FROZEN FOR BUILD 001**
Research cutoff: **2026-08-08**

This document records how two intentionally independent Work Max reports were adjudicated. It is evidence and decision traceability, not a second architecture. [01-ARCHITECTURE.md](01-ARCHITECTURE.md) is canonical.

## 1. Inputs read in full

| Input | Size | Lines | SHA-256 |
| --- | ---: | ---: | --- |
| Report A — `DESKTOPeye — Definitive Independent First-Principles Research Pass` | 106,832 bytes | 1,915 | `92f032768e93baa01ea6891a9dbb952f136687adacd341b2b7193a374995cd13` |
| Report B — `DESKTOPeye — Definitive Independent First-Principles Research Pass`, dated 2026-08-08 | 146,062 bytes | 1,776 | `988574fc0c59620f3f3a471bbfa830b2ca0da5470d553ece5ca2dd64ba0463da` |

Both reports were read completely before synthesis. A duplicate copy of Report B was not counted as a third independent input. Neither report was treated as authority.

The canonical architecture and Build 001 results of eyeBROWSE, CODEeye, and SHELLeye were also read completely, read-only. Transferable evidence was limited to demonstrated principles: persistent underlying reality, logical IDs distinct from provider IDs, conservative identity, delta-first operation, current-condition waits, local typed Program Hosts, and wrong-object correctness. Desktop-specific facts were independently re-established.

## 2. Evidence vocabulary and source hierarchy

- **FACT** — a documented platform/API contract or directly observed repository/platform fact.
- **CURRENT EXTERNAL EVIDENCE** — a current product, sample, or implementation behavior; useful but not an OS guarantee.
- **ARCHITECTURAL INFERENCE** — a design conclusion drawn from facts and evidence.
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED** — documentation cannot decide the implementation behavior.
- **SPECULATION** — plausible but not strong enough to freeze or authorize mutation.

For Windows architecture claims, lower-level Microsoft API contracts outrank higher-level tool prose. Official source/sample behavior is current external evidence unless the API contract makes it a guarantee. Official competitor documentation describes public capability only; silence is not proof that a private implementation lacks a feature.

## 3. Convergence and adjudication matrix

| Material point | Report A | Report B | Current primary evidence | Final synthesis decision |
| --- | --- | --- | --- | --- |
| Architectural center | persistent sparse multi-representation correspondence world | same | Windows exposes distinct USER32, UIA, rendering and input contracts; no API unifies their identity | **Freeze correspondence world** |
| Canonical desktop tree | rejects one tree | rejects one tree | HWND topology, UIA views and pixels have different containment/coverage | **No universal desktop tree** |
| Provider authority | providers own current truth; DESKTOPeye owns continuity | same, formalized as typed facets | UIA providers supply semantics; USER32 owns current window objects; capture supplies frames | **Freeze provider-authority rule** |
| HWND | native incarnation witness, recyclable | same | [`IsWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-iswindow) warns of destruction/recycling; [USER handles](https://learn.microsoft.com/en-us/windows/win32/sysinfo/user-objects) end at destruction | **Never durable identity**; require observed generation and exact process/session scope |
| Window recreation | logical continuity only with strong lineage | same; more explicit exact/no-lineage cases | no public cross-HWND generation token found | **Exact observed/domain lineage only**; similarity never enough |
| UIA RuntimeId | temporary provider-incarnation witness; reusable | same | [`GetRuntimeId`](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomationelement-getruntimeid) defines opaque equality; SDK source states values can be reused | **Same-epoch incumbent evidence only** |
| AutomationId / Name | optional/scoped query evidence; Name not identity | same | [AutomationId guidance](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/use-the-automationid-property) says optional, sibling-scoped, not assured across releases | **Candidate/reconstruction evidence only** |
| Live UIA reference | useful within provider epoch, not persistent | same, asks whether stronger than RuntimeId | [`CompareElements`](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomation-compareelements) compares RuntimeIds; `UIA_E_ELEMENTNOTAVAILABLE` includes destroyed/virtualized elements | **Surviving same-epoch incumbent binding**, never cold identity |
| UIA views/cache | Control normal; Content content; Raw diagnostics; scoped caches | same | [element acquisition](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-obtainingelements) warns against broad root traversal; [caching](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-cachingforclients) returns snapshots | **Freeze scoped compiled cache plans**, not identity |
| Events | dirty signals; current query is truth | same | [UIA event guidance](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-eventsforclients) does not guarantee every possible event | **Events invalidate/wake; reconciliation commits truth** |
| Provider hangs | UIA off kernel critical path | same | [connection](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomation2-put_connectiontimeout) and [transaction](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomation2-get_transactiontimeout) timeouts confirm blocking boundary | **Replaceable worker process isolation is required** |
| Session topology | per target interactive session | same | [window stations](https://learn.microsoft.com/en-us/windows/win32/winstation/window-stations), [desktops](https://learn.microsoft.com/en-us/windows/win32/winstation/desktops), and [interactive services](https://learn.microsoft.com/en-us/windows/win32/services/interactive-services) establish the boundary | **One Session Host per target interactive session** |
| Application lifetime | stable app vs live app instance | same | AUMID/package identity names an application, not one live multiprocess run | **`app_*` and `appinst_*` remain distinct** |
| Virtualization | primary identity killer; durable item only with stable key | same | [WPF recycling](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-controls) can reuse a container; [ItemContainer](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-implementingitemcontainer) and `VirtualizedItem` expose representation behavior | **First-class collection/view epoch; keyed item only** |
| Weak evidence | candidate/ranking only | same | labels, indexes, geometry and pixels lack documented generation guarantees | **Never authorize exact mutation alone** |
| Semantic vs physical | semantic patterns preferred by fitness; raw input weaker | same, explicitly names assurance classes | [`SendInput`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput) inserts into the input stream, not a logical target | **Operation-specific broker; physical is race-bounded** |
| Visual role | on-demand facet; OCR/vision not identity | same | WGC creates capture items/frames, not logical elements | **WGC on demand; frames ephemeral; OCR fallback** |
| Deltas/waits/sync | bounded semantic deltas, conditions, scoped reconcile | same | event incompleteness and provider snapshots require current queries | **WorldSequence is commit order; explicit gaps; no global-idle fiction** |
| Recovery | cold recovery is decisive; unknowable replacements retire | same | UIA references are process-local and HWND/RuntimeId can be reused | **Exact keyed reconstruction only; false continuity prohibited** |
| Persistence | SQLite WAL, sparse state; no permanent ledger/tree/screens | same | relational operating state is sufficient; no graph requirement found | **Freeze SQLite WAL sparse persistence** |
| Cross-substrate | sparse SHELLeye/eyeBROWSE/CODEeye links | same | each sibling has stronger native semantics in its domain | **Strongest-substrate routing; no universal StealthEye DB** |
| Program Host | Node 24; many typed local operations; at least 50 | Node 24; 30 hard floor and a 36-operation example | siblings proved 33, 45 and 52 operation programs | **Hard floor 40; target 50+; canonical planned workflow 60** |

Independent convergence was strong evidence, but each load-bearing platform claim above was rechecked.

## 4. Material disagreements and final decisions

### 4.1 Program Host threshold

- **Report A:** at least 50 typed operations.
- **Report B:** at least 30 typed operations; its canonical example contains roughly 36.
- **Decision:** hard floor **at least 40 meaningful typed DESKTOPeye operations**, target **50+**, with the specified workflow planning 60. The floor is above a merely demonstrative script, compatible with sibling evidence, and leaves no incentive to pad to 50. At least 90% of workflow primitives must be typed SDK calls.

### 4.2 Action assurance classes

- **Report A:** distinguishes semantic and physical guarantees but does not freeze a four-class vocabulary.
- **Report B:** proposes `target_bound`, `provider_semantic`, `race_bounded_physical`, `delivery_uncertain`.
- **Decision:** adopt the four classes **only on current operation results**. They are not identity confidence, permissions, or permanent ledger state. `target_bound` is rare: the selected API must enforce a lifetime-bound native/domain reference or immutable key at the operation boundary. A bare HWND/PID/RuntimeId or a preceding validation does not qualify. It does not claim a physical human event path or guaranteed business outcome. Route, postcondition and assurance remain separate.

### 4.3 Collection and item model

- **Report A:** retains selected virtual items but leaves the collection ontology comparatively implicit.
- **Report B:** introduces `collection_*`, `item_*`, representation incarnation and view/dataset epoch.
- **Decision:** promote `collection_*` when operationally useful and retain a `CollectionViewEpoch`. Promote durable `item_*` only when an app/domain/provider-documented repeatable key exists under the exact collection. Without it, row/container/index/label results stay ephemeral and cannot survive reorder, filter or recycle as exact items.

### 4.4 Session-wide observation

- **Report A:** sparse retained UIA observation, with native lifecycle signals but no equally explicit globally cheap layer.
- **Report B:** identifies a cheap session-wide top-level native lifecycle/focus layer as the correction to sparse blindness.
- **Decision:** adopt the refinement. Top-level create/destroy/show/hide, foreground/focus, popup/modal, session/desktop and display signals are observed cheaply; deep UIA remains interest-scoped.

### 4.5 UIA worker topology

- **Report A:** favors isolated worker lanes/small app-affine pool and rejects premature one-process-per-app permanence.
- **Report B:** freezes isolation but explicitly defers single host versus pool versus per-app sharding.
- **Decision:** freeze a stable per-session native observer and coordination/event MTA plus replaceable query/operation worker process lanes. Freeze the fault-containment outcome, not worker count. Build 001 experiment E-01 selects the coarsest topology that contains a blocking provider without stalling unrelated scopes.

### 4.6 Physical input metrics

- **Report A:** emphasizes no false physical-target claims and at least 50 operations; its wording is less explicit about deterministic observed zero versus universal API guarantee.
- **Report B:** explicitly separates `race_bounded_physical` assurance from zero observed wrong-target mutations in barrier-controlled tests.
- **Decision:** Milestone C requires zero wrong pointer and keyboard mutations in the deterministic suite. This is an acceptance observation, not a universal Windows atomicity claim. The route remains `race_bounded_physical` after a passing run.

### 4.7 Dialog and status vocabulary

- **Report A:** treats dialogs as logical workflow entities and separates lifecycle from UI state, but leaves the final type/status vocabulary more open.
- **Report B:** makes `dialog_*`, `rebound_exact`, virtualization and route assurance explicit.
- **Decision:** retain a first-class `dialog_*` concept/facet because generation, modality and waits matter even for windowless flyouts. Freeze categorical identity status separately from current UI state. `rebound_exact` is rare and always exposes its evidence class.

No other difference changed the permanent architecture. Differences in prose length, example ordering, or tentative API breadth were not material disagreements.

## 5. Load-bearing external verification

### HWND and native window generation

**FACT —** USER handles are valid until destruction and may be recycled; `IsWindow` is not a lease and warns that a later handle can identify a different window. `GetWindowThreadProcessId` reports current association, not a generation number. No documented public window-generation counter was found.

**CURRENT EXTERNAL EVIDENCE —** Windows App SDK `WindowId`/`AppWindow` is one-to-one with a current top-level HWND and follows that window lifetime. It does not supply cross-HWND continuity or an anti-reuse generation witness. See [`GetWindowIdFromWindow`](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.interop/nf-windows-ui-interop-getwindowidfromwindow) and [AppWindow lifetime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/guides/windowing).

**ARCHITECTURAL INFERENCE —** uninterrupted native create/destroy lineage plus exact session/desktop and SHELLeye process incarnation is the strongest generic witness DESKTOPeye can construct. A cold observation gap destroys that guarantee.

### UIA identity, views, caching, threading and failure

**FACT —** RuntimeId is opaque and reusable over time; AutomationId is optional/scoped; `CompareElements` compares RuntimeIds; cached data is a snapshot; desktop-wide UIA calls belong on a non-UI MTA thread; provider transactions have timeout controls.

**CURRENT EXTERNAL EVIDENCE —** [`IUIAutomation6::AddEventHandlerGroup`](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationclient/nf-uiautomationclient-iuiautomation6-addeventhandlergroup) is the preferred current grouping surface when available. It improves registration efficiency/coherence but not event completeness or identity.

**ARCHITECTURAL INFERENCE —** direct COM is the identity-critical foundation; cache requests compile retained interests; worker process death establishes a new provider epoch; provider calls never run on the kernel state writer.

### Virtualization

**FACT —** WPF recycling can reuse a control container for a different data item. UIA virtualization patterns expose realization and lookup mechanisms, not a universal durable item identity.

**ARCHITECTURAL INFERENCE —** the virtualized duplicate-row recycle is stronger than ordinary selector replacement because the same visual/provider container can legitimately represent different domain objects. It is the primary Milestone C killer.

### Physical input and focus

**FACT —** `SendInput` is stream injection, subject to UIPI; [`SetForegroundWindow`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow) is restricted; native active/focus, UIA focus, caret and selection are distinct; pointer hit-test APIs report current points/windows but do not create a target lease.

**CURRENT EXTERNAL EVIDENCE —** newer synthetic pointer/touch APIs add injection modalities and device description; [`InjectSyntheticPointerInput`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-injectsyntheticpointerinput) still takes virtual-screen pixel locations, and none reviewed binds a gesture atomically to a UIA element or DESKTOPeye logical ID.

**ARCHITECTURAL INFERENCE —** final preflight plus the smallest injection batch bounds observable races but cannot abolish the interval after the last check. `foreground_denied` and `focus_mismatch` are normal typed failures.

### Sessions, capture and events

**FACT —** USER objects and input desktops are session/desktop scoped. WGC can create a capture item for a current HWND. Protected content, frame absence, resize/device loss and session transitions are explicit limitations. WinEvent/UIA event streams are not complete world history.

**CURRENT EXTERNAL EVIDENCE —** Microsoft's official HWND capture sample says minimized windows are enumerated but not captured. Build 001 measures the exact target-build behavior rather than promoting a sample statement to an eternal OS contract.

**ARCHITECTURAL INFERENCE —** one interactive-session host is required; WGC frames are ephemeral facets; events mark interests dirty; bounded cursor gaps trigger scoped reconciliation.

### Current higher-level Microsoft and OpenAI tooling

**CURRENT EXTERNAL EVIDENCE —** Microsoft's current [`winapp` UI automation](https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/ui-automation) is UIA-first, uses physical interaction for some operations, re-resolves elements, exposes waits and WGC screenshots, and documents staleness/interactive-session limits. These techniques validate the provider layer, but do not strengthen HWND/RuntimeId beyond their lower-level contracts.

**CURRENT EXTERNAL EVIDENCE —** OpenAI's [computer-use guide](https://developers.openai.com/api/docs/guides/tools-computer-use) documents screenshot/action loops plus custom tools and code-execution harnesses. DESKTOPeye adds retained provider-independent objects, bounded semantic change, conservative recovery, and local condition programs beneath that model loop.

## 6. Targeted public-system comparison

This is not a market survey. It asks only whether reviewed public documentation exposes the complete DESKTOPeye combination.

| Public system | Publicly documented center | Relevant strength | Combination not found in reviewed public docs |
| --- | --- | --- | --- |
| Microsoft `winapp` | UIA-first CLI plus screenshots/input/waits | current Microsoft techniques and stale re-resolution | durable provider-independent concepts, cold reconstruction, bounded logical deltas, local retained Program Host |
| [Power Automate Desktop](https://learn.microsoft.com/en-us/power-automate/desktop-flows/ui-elements) | desktop flow/UI elements/selectors | mature automation surface and waits | documented conservative provider-independent identity/recovery model |
| [UiPath UI automation](https://docs.uipath.com/activities/other/latest/ui-automation/about-the-ui-automation-next-activities-pack) | selectors, fuzzy/image/CV targets and activities | broad targeting/fallback coverage | documented retained logical IDs with cold controller recovery and item-recycle semantics |
| [Appium Windows driver ecosystem](https://appium.io/docs/en/3.0/ecosystem/drivers/) | WebDriver sessions/elements | standardized remote automation protocol | provider-independent persistence after controller/provider death |
| [FlaUI](https://github.com/FlaUI/FlaUI) | .NET UIA2/UIA3 wrapper | accessible managed UIA surface | correspondence kernel beyond UIA element lifetime |
| [pywinauto](https://pywinauto.readthedocs.io/en/latest/) | Win32/UIA backends and specifications | practical Windows querying/action | conservative cross-provider persistence/deltas/Program Host |
| [AutoHotkey](https://www.autohotkey.com/docs/v2/) | hotkeys, windows, keyboard/pointer scripting | concise physical/native scripting | retained semantic correspondence and cold recovery |
| [OpenAI computer use](https://developers.openai.com/api/docs/guides/tools-computer-use) | screenshot/action loop with custom/code harnesses | model-native visual interaction and batching | OS-resident retained correspondence supplied by the tool itself |
| [Anthropic computer use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/computer-use-tool) | screenshots and mouse/keyboard actions | general visual computer interaction | public retained provider-independent Windows identity model |
| [Gemini computer use](https://ai.google.dev/gemini-api/docs/computer-use) | screenshot/action environment loop | general action planning | public Windows correspondence/recovery/delta substrate |
| [Microsoft agent computer use](https://learn.microsoft.com/en-us/microsoft-copilot-studio/computer-use) | hosted computer-use tool loop | managed agent integration | public DESKTOPeye-style retained Windows logical world |

Safe novelty conclusion:

> Current public documentation reviewed did not reveal a system exposing the full combination of provider-independent retained desktop concepts, conservative cold recovery, bounded semantic deltas, condition waits, multi-representation actuation, and a many-operation local Program Host.

This does not claim that no private system has similar architecture.

## 7. Material upgrades produced by synthesis

Fresh verification found no architecture-invalidating primitive that replaces correspondence. The final pass nevertheless tightened both inputs:

1. falsified `WindowId`/`AppWindow` as a hidden durable native generation upgrade;
2. falsified current synthetic pointer APIs as target-bound input;
3. placed UIA6 event-handler groups and Remote Operations explicitly as efficiency seams, not identity/correctness dependencies;
4. constructed an exact native incarnation witness and named which fields only corroborate;
5. separated identity status, current UI state, actuation route, assurance and postcondition;
6. adopted a cheap session-wide top-level observer without abandoning sparse deep UIA;
7. froze `collection_*`/view epoch while refusing durable unkeyed `item_*` promotion;
8. made cold recovery emit an explicit gap and exact-evidence results rather than a generic rediscovery success;
9. specified 50 unambiguous hostile cases and deterministic zero metrics;
10. reconciled the Program Host disagreement with a 40-call floor, 50+ target, and a concrete 60-call workflow;
11. made the Common Item Dialog a post-gate generalization smoke rather than a dependency that could obscure A–D;
12. split provider isolation into a frozen fault boundary and an experimentally selected worker count.

## 8. Falsification summary

The selected architecture was attacked with nine objections:

| Attack | Consequence accepted in final design |
| --- | --- |
| Windows lacks universal durable control IDs | exact continuity is optional; stale/ambiguous is correct |
| correspondence is complex | sparse promotion and typed facets bound the complexity |
| sparse state can miss unexpected UI | cheap session-wide native topology/focus observation |
| worker isolation can damage continuity/performance | cold recovery is correctness; warm continuity and topology are optimizations |
| physical input is not atomically target-bound | explicit race-bounded assurance and deterministic wrong-target metrics |
| domain providers can become a plugin zoo | optional operation facets behind one broker; no required per-app architecture |
| semantic actions differ from human input | route is caller/operation-specific and returned honestly |
| vision will improve | better candidates/coverage do not create external generation keys |
| full trees look simpler | virtualization, invalidation, provider hangs and false durability make them less correct |

The architecture survived because each attack changed a contract or bounded a claim; none revealed a stronger general center.

## 9. Unresolved implementation evidence

The canonical experiment list is in [02-BUILD-001-SLICE.md](02-BUILD-001-SLICE.md). The highest-impact open questions are:

- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** worker isolation granularity under an actually blocking provider;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** exact HWND recreation/reuse lineage under target frameworks;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** live COM reference behavior and provider reconstruction across worker epochs;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** WPF virtualization/placeholders/recycling with duplicate rows;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** cold recovery across keyed, generic-replaced and app-restarted states;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** WGC behavior under minimize/protection/session/device transitions on build 26100.8973;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** pointer and keyboard race ceilings with deterministic barriers;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** DPI/negative-origin/display-epoch coordinate transforms;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** UIA6 subscription and lost-wakeup behavior during worker recycle;
- **OPEN QUESTION — BUILD 001 EXPERIMENT REQUIRED:** Node 24 named-pipe behavior for 40/50/60-operation programs.

No experiment above was run during this pass, and no inference from it is recorded as a result.

```text
Architecture:           FINAL / SYNTHESIZED / VERIFIED / FROZEN FOR BUILD 001
Product implementation: NOT STARTED
Build 001 acceptance:   NOT RUN
```
