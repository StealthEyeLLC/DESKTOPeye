# Build 001 measured acceptance harness

Status: **IMPLEMENTED / PENDING AUTHENTICATED INTERACTIVE DESKTOP**

This directory is test infrastructure for the frozen Build 001 acceptance boundary. It is not a DESKTOPeye runtime subsystem and does not add a verifier-agent, approval layer, action ledger, or sibling-Eye semantics.

## Authority binding

- Frozen acceptance authority: `docs/02-BUILD-001-SLICE.md`.
- Exact hostile list: `manifests/hostile-50.json` (`caseCount = 50`).
- Measured result authority is intentionally absent until a real run completes: `docs/09-BUILD-001-RESULTS.md` must not be created from preparation or dry-run evidence.
- The runner requires a clean committed worktree before measured execution.

## Frozen gates

| Gate | Required measurement | Interactive desktop required | Primary providers | Evidence |
| --- | --- | --- | --- | --- |
| A | persistent correspondence and conservative recovery across kernel death | YES | kernel store, SHELLeye proc_* authority, USER32, UIA, Session Host | `gate-A.json`, retained IDs, epochs, identity evidence, sync/gap output |
| B | retained/delta-first selective perception, virtualization-safe keyed item continuity | YES | kernel store, UIA, native sentinel | `gate-B.json`, view epochs, retained IDs, bounded deltas |
| C | all 50 frozen hostile cases with zero hard-failure metrics | YES for the provider-backed suite; selected identity/preflight cases also reuse deterministic noninteractive unit evidence | SHELLeye sparse process correspondence, USER32/DWM, UIA, WGC, input, kernel recovery, fixture oracles | `gate-C.json`, `cases/C01.json` ... `cases/C50.json`, hard metrics |
| D | canonical Program Host workflow: exactly 60 meaningful typed calls, zero model round trips between primitives | YES | kernel plus all routes exercised by canonical workflow | `gate-D.json`, `program-host-60.json` |

The owner need only unlock/sign into the normal StealthEye Windows desktop. The harness first ensures the existing `shelleye-kernel-dev` process-authority task is available, then starts and resets the DESKTOPeye scheduled-task topology and controlled fixtures itself.

## Interactive session preflight

The C# acceptance executable refuses to proceed unless exactly one non-session-0 Explorer desktop exists and the DESKTOPeye Session Host in that same session reports:

- matching session ID;
- window station `WinSta0`;
- input desktop `Default`;
- `unlocked = true`;`r`n- the existing SHELLeye process-authority task can be started in the same authenticated user context before measured execution.

This explicitly prevents a SYSTEM/session-0 substitute from being treated as acceptance.

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' .\tests\DESKTOPeye.Acceptance\bin\Release\net10.0-windows10.0.26100.0\win-x64\DESKTOPeye.Acceptance.dll preflight
```

On the locked machine, the expected preparation result is `BLOCKED_INTERACTIVE`; that is a correct negative provider result and is not measured acceptance.

## Measured run

After the implementation freeze is committed, clean, and provider-published, run:

```powershell
& 'C:\Program Files\dotnet\dotnet.exe' .\tests\DESKTOPeye.Acceptance\bin\Release\net10.0-windows10.0.26100.0\win-x64\DESKTOPeye.Acceptance.dll run
```

The runner:

1. validates the authenticated interactive session;
2. archives any prior Build 001 acceptance runtime instead of reusing it as evidence;
3. starts/verifies the existing `shelleye-kernel-dev` process-authority task, then starts the interactive DESKTOPeye Session Host, kernel, WPF fixture, and Win32 adversary scheduled tasks;
4. establishes a known fixture/provider state;
5. runs gates A, B, all 50 C cases, and D without converting an unfavorable measured outcome into a retry-until-pass loop;
6. writes bounded evidence under ignored `artifacts/acceptance/build001-*`;
7. reports PASS only if A/B/C/D pass and the frozen hard metrics are actually measured at zero.

The test runner may use fixture-private oracle pipes to establish hostile stimuli and independently observe fixture material state. Those pipes are acceptance infrastructure only; the canonical Program Host gate still operates retained typed DESKTOPeye concepts through the kernel.

## Hard metrics

The C gate does not pre-fill zeroes. Metrics remain unknown until the complete 50-case suite executes. Only a complete zero-failure hostile run earns zeros for:

- false window rebounds;
- false dialog/control rebounds;
- false virtual-item rebounds;
- wrong semantic mutations;
- wrong pointer mutations;
- wrong keyboard mutations;
- mutations through ambiguous retained targets;
- silent event/delta gaps;
- application restarts misclassified as the same run;
- provider stalls escaping isolation into the kernel/unrelated scopes.

## Noninteractive validation already possible

These remain valid before unlock and should be rerun at the implementation freeze:

- full Release solution build;
- all unit tests;
- deterministic identity tests;
- SQLite WAL/persistence/reopen/schema tests;
- pipe/local-program integration tests;`r`n- fake-provider SHELLeye JSON-RPC/process-witness integration tests and sparse-correlation ambiguity tests;
- input-preflight rejection tests;
- Node SDK syntax and local single-connection multi-operation test;
- acceptance manifest enumeration;
- locked-session negative preflight.

Tests requiring the authenticated desktop are intentionally not simulated as SYSTEM because their material authorities are the real interactive USER32/UIA/WGC/input-desktop providers.

## Evidence and cleanup

Acceptance evidence root: `artifacts/acceptance/build001-*` (git-ignored).

Prior runtime archive root: `C:\ProgramData\StealthEye\DESKTOPeye-acceptance-archive\`.

Dirty-worktree recovery snapshot: `C:\ProgramData\StealthEye\DESKTOPeye-recovery\20260812T0310-0400`.

The harness confines destructive setup/cleanup to the four DESKTOPeye Build 001 scheduled tasks/processes and the controlled fixture/adversary applications. It uses SHELLeye only through its existing typed `rpc.hello`, `process.retain`, and `process.inspect` surface for exact process-incarnation authority; it does not modify SHELLeye code or absorb its process ontology. It does not operate arbitrary owner windows as fixture targets.