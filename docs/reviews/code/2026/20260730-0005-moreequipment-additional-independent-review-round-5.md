# 20260730-0005: MoreEquipmentSlots Additional Independent Review Round 5

Status: `recorded / P0=0 P1=2 P2=1 / correction required`

## Scope

This is round 5 of the five additional independent subagent reviews requested
by the user. The read-only audit used clean HEAD
`173418f5655e95849da99b86a9c0e0ae8f7d3750`. It reviewed the round-4
terminal-revision split in both the original save and another save, then
challenged winner-first semantics in the empty-state path with retained loser
evidence.

No file was modified, no game or Runtime was launched, no save was changed and
no package or complete Release operation was run.

## Result

```text
P0 = 0
P1 = 2
P2 = 1
MoreEquipmentSlots = implemented / acceptance-open
```

## Findings

### P1-1: save A still cannot cold-load its own T1 Product

Round 4 fixed save B's empty-state terminal validator, but the original save A
uses a different path:

1. A migrates at `T0` and publishes a completed immutable winner;
2. a later successful native save advances Product scope to `T1`;
3. A cold-loads with current `T1`; `LoadOrMigrate` first validates the Product
   successfully against current native scope;
4. `FinalizeExactGlobalMigration`, with the global already absent, calls
   `CompletePreSchemaGlobalClaim(... document.Scope=T1)`;
5. completion reconciles and validates `winner.json` with
   `ValidateExactPreSchemaGlobalClaim`;
6. `PreSchemaGlobalClaimMatches` still calls `claim.Scope.Matches(T1)`, whose
   equality includes `TotalGameSeconds`, so immutable `T0 != T1` throws.

The original save therefore fails after every legitimate Product revision
advance. The round-4 test wrote a T1 Product and loaded only B, never A.

Correction gate:

- an exact completed winner must be classified before transition/loser
  evidence in completion;
- A at T1 must prove the winner's immutable save identity, terminal
  Product/stamp/archive/global state and then return without rewriting winner
  T0;
- pending winner completion must retain its existing exact-revision and
  Product-census rules;
- test A T1 cold load first, then B empty, with winner bytes unchanged.

### P1-2: empty-state handling is not winner-first

The empty guard enumerates and parses every transition and per-hash claim
before it finds and validates `winner.json`. A successful winner intentionally
allows stale loser claim/transition bytes to remain as optional evidence, but
those bytes can still:

- target the new save B and trigger the current-save barrier; or
- be malformed and throw during parsing.

Thus a late loser that is non-authoritative in the main migration path can
permanently block B's ordinary in-memory empty state. This contradicts the
round-1 winner-first rule.

Correction gate:

- classify `winner.json` first under the operation lock;
- if it is an exact completed other-save winner and its terminal
  Product/archive/global proof passes, retained non-winner claim/transition
  bytes are optional diagnostic evidence and cannot veto that authority;
- if there is no completed winner, invalid/current-save pending evidence
  remains fail closed;
- capture residue, unbound archive and current-scope backup remain barriers;
- cover a completed A plus B-targeting loser claim/transition and malformed
  loser evidence, all retained byte-exact while B returns in-memory empty.

### P2-1: round-4 coverage and fact text overstate lifecycle stability

The T1 test proves only B's empty-state path. README, Update and Batch 6 say
terminal proof remains valid after later saves without proving A's own
Finalize/Complete path or winner-first empty handling.

The overall `implemented/acceptance-open` status, superseded package and open
player gates remain correct.

## Confirmed Boundaries

- Pending winner resume and completion rerun Product census.
- The terminal validator distinguishes immutable save identity from mutable
  Product revision and rejects a Product older than winner.
- Current-save Product loads retain the anti-ahead rule.
- Active global absence, exact archive hash and exact source stamp remain part
  of completed terminal proof.
- True-empty lock cleanup and loaded/absent owner cold-demand routing remain
  physically present.

## Review Evidence

```text
HEAD = 173418f5655e95849da99b86a9c0e0ae8f7d3750
git status = clean
source trace:
  LoadOrMigrate -> FinalizeExactGlobalMigration
  -> CompletePreSchemaGlobalClaim(T1)
  -> ValidateExactPreSchemaGlobalClaim
  -> PreSchemaGlobalClaimMatches/Scope.Matches(T0 != T1)
  empty guard enumerates transitions/claims before completed winner
```

This review did not run game, Runtime, save, package or complete Release
operations. The implementation lifecycle remains owned by Update
`20260723-0008`; later fixes are not this review's independent acceptance.
