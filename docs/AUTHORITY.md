# Authority

Status: **Canonical**
Owner: **StealthEyeLLC**

## Repository and project authority

StealthEyeLLC owns `StealthEyeLLC/DESKTOPeye` and the project's technical direction. The connected GitHub authority used for this synthesis had administrative and push access to that repository.

This pass was authorized to synthesize and verify architecture, inspect sibling repositories and the STEALTHEYELLC platform read-only, canonicalize repository documentation, create the five Build 001 issues, and commit directly to `main`.

This pass was not authorized to implement product code, create fixtures or runtime state, automate the Windows desktop, install tooling, execute Build 001 experiments, or claim acceptance results.

```text
Product implementation: NOT STARTED
Build 001 acceptance:   NOT RUN
```

## Current Build 001 implementation authority — 2026-08-12

Owner direction now authorizes continuation of Build 001 implementation: dirty-worktree recovery, lossless preservation, completion and hardening of the frozen Build 001 design, Program Host and SDK completion, deterministic/noninteractive tests, hostile fixtures, acceptance-harness preparation, and publication of truthful development checkpoints. Interactive measured acceptance is authorized only against a genuine authenticated interactive Windows session. Canonical Build 001 completion is authorized only if the frozen measured acceptance gates actually pass.

This authority does **not** authorize architecture redesign, sibling-Eye redesign or mutation, World Kernel mutation, premature `COMPLETE`/`ACCEPTED` status, or creation of `docs/09-BUILD-001-RESULTS.md` from anything other than actual measured acceptance evidence.

```text
Product implementation: IMPLEMENTED / PENDING MEASURED ACCEPTANCE
Build 001 acceptance:   NOT RUN
```
## Canonical ordering

When documents appear to conflict, use this order:

1. owner direction and the five governing constraints;
2. `docs/AUTHORITY.md` for scope and canonicality;
3. `docs/00-CHARTER.md` for mission and constraints;
4. `docs/01-ARCHITECTURE.md` for the permanent architecture;
5. `docs/02-BUILD-001-SLICE.md` for the implementation gate;
6. `docs/06-DECISIONS.md` for frozen/deferred/rejected adjudications;
7. the remaining canonical documents and GitHub issues.

The five Build 001 issues must agree with `docs/02-BUILD-001-SLICE.md`; the document controls if issue wording drifts.

## Research and experiments

The two independent Work Max reports are research inputs, not canonical authority. External sources are evidence, not project decisions. Build experiments are non-canonical until their result is deliberately promoted by updating every affected canonical document.

No `docs/09-BUILD-001-RESULTS.md` may exist until an authorized implementation pass completes acceptance. No unchecked proposal, scratch note, branch, PR, or alternative architecture file outranks `docs/01-ARCHITECTURE.md`.

## Change discipline

A frozen decision changes only when new platform or implementation evidence demonstrates a material correctness or capability improvement. Any change must update architecture, Build 001 contract, decisions, relevant capability/workflow documents, and issues coherently. A second competing architecture document is prohibited.
