# 20260730-0004: MoreEquipmentSlots Additional Independent Review Round 4

Status: `recorded / P0=0 P1=1 P2=1 / correction required`

## Scope

This is round 4 of the five additional independent subagent reviews requested
by the user. The read-only audit used clean HEAD
`dbb0a45dd761b89c77b3146db4735d566dcb5f92` and challenged the round-3
completed-winner/other-save empty-state correction after a later successful
native save. It also rechecked the related Product revision and previous
fallback rules.

No file was modified, no game or Runtime was launched, no save was changed and
no package or complete Release operation was run.

## Result

```text
P0 = 0
P1 = 1
P2 = 1
MoreEquipmentSlots = implemented / acceptance-open
```

## Findings

### P1-1: a completed winner expires as its Product revision advances

The completed winner permanently records the migration-time save scope at
clock `T0`. A later successful `SaveSaved` advances the Product document scope
to the current native save clock `T1` in
`MoreEquipmentSlotsNativeRuntime.cs`.

When a different new save B considers ordinary in-memory empty state, the
empty guard derives A's canonical Product path but calls
`ValidateCompletedPreSchemaGlobalAuthorities` with the winner's old
`claim.Scope(T0)`. That method uses the normal current-save
`TryLoadValidated` rule. Once `T1 > T0 + 300 seconds`, the valid A Product is
rejected as `scope-revision-regressed`, and that failure is intentionally not
eligible for `.previous` fallback.

Reproduction:

1. A migrates the schema-less global at `T0`, publishing exact Product,
   archive and completed winner/evidence;
2. A plays and completes a normal native save at `T1`, more than 300 seconds
   after `T0`; Product v3 correctly advances to `T1`, while the immutable
   migration winner remains at `T0`;
3. a never-used save B has no Product/global data and requests ordinary empty
   state;
4. B revalidates A's terminal Product against `T0`, receives
   `scope-revision-regressed`, and is blocked forever.

The completed migration proof must not reuse a current-save anti-ahead rule
against its immutable historical clock. The terminal proof still needs:

- canonical save identity and path;
- a structurally valid eligible live or previous Product generation;
- Product revision not older than the winner's migration revision;
- exact `PreSchemaGlobal` source stamp;
- exact archive hash and active-global absence.

It must reject identity drift, a Product revision older than the winner, an
invalid/future generation and a wrong source stamp.

### P2-1: the focused test and fact text prove only the immediate T0 state

The round-3 test creates A and immediately loads B without advancing A's
Product revision. README, Update and Batch 6 consequently describe a
cross-save terminal rule that is not stable after normal saves.

Correction gate:

- cover `T0 migration -> T1 Product revision > tolerance -> new B empty`;
- cover stale Product revision and identity drift as fail-closed controls;
- keep the status `implemented/acceptance-open` and the old package
  superseded until the remaining review/package/player gates finish.

## Confirmed Boundaries

- Round 3 reruns Product census for a pending winner.
- Current-scope pending/incomplete claim state, capture residue and an unbound
  archive remain recovery barriers.
- A true-empty check removes the transient operation lock it created.
- Loaded-owner terminal evidence suppression and absent-owner cold demand are
  physically present.
- Production SaveLoaded and SaveSaved remain single EquipmentSlots dispatches.

## Review Evidence

```text
HEAD = dbb0a45dd761b89c77b3146db4735d566dcb5f92
git status = clean
source trace:
  MoreEquipmentSlotsNativeRuntime.cs SaveSaved advances Product scope
  EquipmentSlotDocumentStore.Migration.cs empty guard uses winner scope
  EquipmentSlotDocumentStore.cs rejects Product >300 seconds ahead
```

This review did not rerun game, Runtime, package or complete Release checks.
The implementation lifecycle remains owned by Update `20260723-0008`; a later
fix reference is not this review's independent acceptance.
