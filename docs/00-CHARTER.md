# 00 — Charter

Status: **FINAL / FROZEN FOR BUILD 001**
Owner: **StealthEyeLLC**
Operator: **ChatGPT**

## Mission

DESKTOPeye gives ChatGPT a persistent, compact, programmable correspondence with the current Windows desktop. Its job is not to make human-authored automation scripts pleasant. Its job is to expose the objects, identities, deltas, waits, recovery behavior, and actuation semantics ChatGPT would choose if it could design its own desktop interface.

DESKTOPeye preserves or conservatively retires correspondence. Windows owns application and window reality; DESKTOPeye does not keep applications alive by pretending that stale objects remain current.

## Five exhaustive StealthEye constraints

These are the complete project-level constraints. Intrinsic Windows access, integrity, session, desktop, and provider limitations remain native facts.

### 1. No extra safety architecture

DESKTOPeye adds no approvals, confirmations, permission systems, authority tiers, restricted modes, or action-approval product.

### 2. No extra guardrails

DESKTOPeye adds no allowlists, denylists, policy engine, artificial capability classes, privilege tiers, or action restrictions.

### 3. No architecture theater

Every component must buy measurable correctness, capability, continuity, performance, resilience, or model-context efficiency.

### 4. No separate verification architecture

There is no verifier agent, second model, mandatory independent verification stage, or proof pipeline. Revalidation, assertions, waits, tests, postconditions, and stale-object rejection are ordinary correctness.

### 5. No permanent action ledger

There is no permanent click history, receipt archive, screenshot history, proof-of-action archive, audit-trail product, or provenance ledger. Bounded operational state required for current correspondence is allowed.

## Governing principles

### Providers own truth; DESKTOPeye owns correspondence

- USER32, DWM, WTS, and window-station/desktop APIs own current native desktop facts.
- UIA providers own the accessibility semantics they expose.
- application/domain APIs own their documented semantic objects.
- capture owns observed pixels and its limitations.
- Windows input APIs own the physical delivery attempt.
- SHELLeye owns generic process, file, service, and machine truth.
- eyeBROWSE owns browser-native target, DOM, AX, and network truth.
- CODEeye owns source, symbol, build, test, and diagnostic truth.
- DESKTOPeye owns ChatGPT-facing logical correspondence across these representations.

### No canonical desktop tree

DESKTOPeye exposes sparse logical relations, provider-specific views, retained concepts, and on-demand structural projections. A complete accessibility-tree mirror would turn volatile provider structure into false durability and excessive context.

### Conservative identity

Logical identity is categorical and evidence-bearing. A scalar similarity score can rank candidates but can never authorize mutation. Name, text, tree path, row index, geometry, visual similarity, OCR, and embeddings may generate or rank candidates; none establishes exact continuity alone.

> Loss of continuity is acceptable. False continuity is not.

### Sparse retention

A query result is ephemeral by default. A result becomes a logical concept when ChatGPT retains, watches, reuses, correlates, or targets it. Deep UIA observation is scoped to those interests. A cheap session-wide native lifecycle/focus layer prevents sparse observation from becoming blind to unexpected top-level windows, modal dialogs, popups, and foreground changes.

### Delta first, with explicit gaps

DESKTOPeye emits compact logical deltas from a bounded buffer. Cursor expiration and observation discontinuities are explicit gaps requiring scoped reconciliation. The system does not maintain a permanent event ledger.

### Conditions, not sleeps

Waits evaluate current state, establish interest, wake on signals, reconcile the affected scope, poll when necessary, and perform a final current predicate query. A `stable_for` option is a named debounce interval, never a claim of global UI idle.

### Scoped synchronization

`world.sync(scope)` reconciles retained/current interests in the requested scope against current provider reality and commits resulting logical deltas. It is not global desktop idle, complete event history, a causal barrier, or a separate verification phase.

### Program Host

ChatGPT sends one local Node program that can issue dozens of typed operations, wait, branch, loop, and return a compact result. No model runs inside the host. Build 001 requires at least 40 meaningful typed operations in one invocation and targets 50+ without padding.

### Semantic, visual, and physical reality

Semantic provider patterns are preferred when they express the requested operation. Visual capture is an on-demand facet, not the permanent world model. Physical pointer and keyboard input are used only when operation semantics require them and are reported as race-bounded, never atomically target-bound.

## Cross-substrate boundaries

- Browser content and browser-native semantics route to eyeBROWSE; native browser frame/chrome, file pickers, and OS dialogs route to DESKTOPeye.
- Generic process and file truth route to SHELLeye; the perceived application instance and desktop UI route to DESKTOPeye.
- IDE UI may route to DESKTOPeye, but source, symbols, builds, and diagnostics route to CODEeye.
- A future DOCSeye may own document meaning while DESKTOPeye owns the document window, viewport, and visible desktop interaction.
- Correlations are sparse links, not a universal StealthEye world database.

## Current boundary

Architecture is frozen for Build 001. Product implementation, provider hosts, UI fixtures, capture workers, input executors, Program Host code, runtime state, and acceptance results do not exist in this repository.
