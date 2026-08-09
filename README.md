# DESKTOPeye

Owner: **StealthEyeLLC**
Operator: **ChatGPT**

```text
Architecture:           FINAL / SYNTHESIZED / VERIFIED / FROZEN FOR BUILD 001
Build 001:              PLANNED / NOT IMPLEMENTED
Product implementation: NOT STARTED
Build 001 acceptance:   NOT RUN
```

DESKTOPeye is ChatGPT's persistent, sparse Windows desktop correspondence world. It gives ChatGPT conservative logical concepts for applications, application instances, windows, dialogs, controls, and supportable collection items while Windows providers continue to own the current truth of their own representations.

DESKTOPeye is not a screenshot agent, a UI Automation wrapper, a selector framework, an RPA designer, a macro recorder, or a human-facing automation product. It is the desktop substrate ChatGPT would ask for: retained logical objects, explicit lifetimes and evidence, compact deltas, condition waits, scoped reconciliation, operation-specific actuation, honest physical-input assurance, restart recovery, and a local many-operation Program Host.

## Architectural thesis

> Providers own current representation-specific truth. DESKTOPeye owns conservative agent continuity across those representations.

Windows exposes overlapping partial realities:

- USER32/DWM/WTS and native HWNDs;
- Microsoft UI Automation and legacy accessibility;
- application-specific semantic APIs;
- rendered pixels and capture frames;
- foreground, focus, input, session, and desktop state;
- sparse correlations to SHELLeye, eyeBROWSE, CODEeye, and a future DOCSeye.

DESKTOPeye deliberately does **not** define one canonical full desktop tree. It promotes only concepts ChatGPT retains, watches, reuses, or targets. Provider views remain queryable on demand.

The governing correctness rule is:

> Loss of continuity is acceptable. False continuity is not.

An HWND, UIA RuntimeId, AutomationId, visible name, path, geometry, screenshot, OCR result, or embedding is never promoted beyond its documented evidentiary strength. Physical mouse and keyboard injection remain race-bounded delivery attempts even when the logical target is exact.

## Build 001

Build 001 establishes the permanent spine without attempting broad framework coverage.

| Milestone | Decisive proof |
| --- | --- |
| **A — Persistent Desktop Correspondence** | Warm and cold recovery reconstruct only correspondence proven exact, emit an explicit recovery gap, and produce zero false rebounds. |
| **B — Retained Desktop Objects / Delta First** | Sparse retained concepts, scoped UIA caches, event-to-reconciliation, bounded deltas, condition waits, and `world.sync` operate without full-tree or screenshot rediscovery after each action. |
| **C — Recovery Continuity / Identity Killer** | A hostile deterministic suite proves stale-object rejection, provider isolation, and zero observed wrong-target mutations. The primary killer is virtualized container recycling with duplicate visible semantics. |
| **D — Programmable Desktop Operation** | One Node 24 Program Host invocation performs at least 40 meaningful typed DESKTOPeye operations, targets 50+, branches and waits locally, exercises semantic, visual, pointer, and keyboard paths, and returns one compact result. |

The exact contract is in [docs/02-BUILD-001-SLICE.md](docs/02-BUILD-001-SLICE.md). No milestone is implemented or accepted in this repository state.

## Canonical documents

Read in this order:

1. [Charter](docs/00-CHARTER.md)
2. [Architecture](docs/01-ARCHITECTURE.md)
3. [Build 001 slice](docs/02-BUILD-001-SLICE.md)
4. [STEALTHEYELLC platform](docs/03-PLATFORM-STEALTHEYELLC.md)
5. [Roadmap](docs/04-ROADMAP.md)
6. [Research baseline](docs/05-RESEARCH-BASELINE.md)
7. [Decisions](docs/06-DECISIONS.md)
8. [Capability matrix](docs/07-CAPABILITY-MATRIX.md)
9. [Workflow pressure tests](docs/08-WORKFLOW-PRESSURE-TESTS.md)
10. [Authority](docs/AUTHORITY.md)

`docs/01-ARCHITECTURE.md` is the single canonical architecture. Experimental findings remain non-canonical until the affected canonical documents are updated deliberately.

## Repository shape

```text
README.md
docs/
  00-CHARTER.md
  01-ARCHITECTURE.md
  02-BUILD-001-SLICE.md
  03-PLATFORM-STEALTHEYELLC.md
  04-ROADMAP.md
  05-RESEARCH-BASELINE.md
  06-DECISIONS.md
  07-CAPABILITY-MATRIX.md
  08-WORKFLOW-PRESSURE-TESTS.md
  AUTHORITY.md
```

There is intentionally no `src/`, fixture project, runtime directory, state database, Program Host implementation, or Build 001 results document. Product implementation begins in a separate authorized implementation pass.
