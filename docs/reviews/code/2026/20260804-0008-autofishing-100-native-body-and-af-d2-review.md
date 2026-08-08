# AutoFishing 1.00 Native Body and AF-D2 Review

Date: 2026-08-04
Status: `recorded / movement repair required / AF-D2 fault-close selected / implementation pending`
Audited HEAD: `570db5b30a99e376c6c90f331c1a1bb9b1c98d68`

Owning Update:

- [`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

Prior authorities:

- [`20260802-0001`](20260802-0001-autofishing-qiuzy-semantic-size-and-feature-baseline.md)
- [`20260731-0002`](../../../updates/2026/20260731-0002-autofishing-legacy-native-and-runtime-fallback-closeout.md)
- [Fishing compatibility Hook map](../../../hook-map/focused/FishingAutomationCompatibility.md)

## Scope

The 0.6 roadmap requires an exact old/current native-body comparison before
changing AutoFishing. This review binds the admitted AutoFishing ProductNative
source to the old `23762374_public_C416D4` and current
`24456188_test_E861E0` game baselines and answers two questions:

1. which movement, Wait, Ready or Pull assumptions are invalid on game 1.00;
2. whether InstantBite finding `AF-D2` should use snapshot/rollback or an
   explicit fault-close/reconcile transaction.

This pass does not change Runtime or Product code, split the atomic 22-Hook
inventory, create a current AutoFishing policy/package, install DTMAPI, start
Doloc Town, use the fifth save, or authorize publication.

## Bound Native Inputs

The reverse sources are local-only evidence and remain outside the repository
distribution boundary:

| Build | `Assembly-CSharp.dll` length | SHA-256 |
| --- | ---: | --- |
| `23762374_public_C416D4` | 5,993,984 | `C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404` |
| `24456188_test_E861E0` | 6,384,128 | `E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923` |

The reviewed Product source is `Yuuka.DTMAPI.AutoFishing` `1.0.0`, still
compiled through the frozen `0.5.5` Author SDK target. Its current admission
policy remains the old `doloctown-23762374-autofishing-v1`; a future current
policy is an implementation/package output, not an input to this Review.

## Native Responsibility Comparison

### Movement owner changed and the keyboard fallback is invalid

The old `AgentPhysicalStatus.HorizontalMoveFactor` property is gone. Current
`AgentPhysicalStatus` owns a `MoveModifier`; direct device-independent input
is `MoveModifier.inputMultiplier`, while `VelocityX` carries effective
horizontal motion.

Current `AgentStateFishingWait` consumes both signals:

- `NextState` fails fishing when `MoveModifier.inputMultiplier != 0`;
- `OnPlay` first fails when `abs(VelocityX) > 0.001`;
- `AgentStateBase.OnPlay` then projects `MoveModifier.OffsetX` into
  `VelocityX`, after which Wait also fails on non-zero velocity.

AutoFishing currently tries to compile a getter for the removed property. On
failure it polls cached A/D/Space/Shift buttons. That fallback is not the
native responsibility, misses controller/remapped input and conflates two
non-movement keys with movement. The required repair is one exact current
snapshot containing `MoveModifier.inputMultiplier` and `VelocityX`. Missing
either accessor must fail closed; it must not fall back to hard-coded input.

### Wait body changed, but native ownership remains usable

Current Wait adds `HookingTimeMultiplier` to idle and roll intervals, calls
`base.OnPlay`, observes the movement signals above, calls `base.OnEnter`, and
uses the current one-argument rod renderer animation. Its bite state fields,
`RollFish`, `InvokeFishOnHookTip`, and `NextState` responsibilities remain on
the same native owner with the same relevant signatures.

`FishingCache.RollFish` now obtains the proto through
`DolocAPI.RollFish(poolName, seasonGroupId, level)` instead of the old pool
instance method, and adds `FishEscape`; it still writes `FishProto`, then
creates and writes `FishItem`, and returns whether a fish result was produced.
The Product already invokes the native `RollFish` owner rather than copying
the fish table algorithm. It must continue doing so.

The Product's Wait hooks are Postfix observers. InstantBite is deliberately
applied only from the `OnPlay` Postfix, after the native current-state
transition is complete. No Wait Prefix or copied HookingTimeMultiplier logic
is required.

### Ready changes do not require a Product replacement

Current Ready calls `base.OnPlay` instead of clearing status directly, sets
`body.CurrentTool` on enter and exposes the current interaction-tip policy.
AutoFishing only observes Ready through `OnEnter` and `OnPlay` Postfixes and
adjusts its own charge/animation policy. It neither suppresses the native body
nor owns `CurrentTool`. The current changes therefore remain intact without a
Ready code replacement.

### Pull changes do not require a Product replacement

Current Pull calls `base.OnPlay` and adds the native island-badge energy return
after a fish escape. AutoFishing observes Pull enter/exit and renderer duration
results; it advances through native Wait `NextState` and does not replace Pull
`NextState` or `OnPlay`. The equipment and velocity additions therefore remain
native and require no Product copy.

## AF-D2 Root Cause

`FishingNativeTransactionCache.TryPrepareNativeBite` currently performs this
sequence after a successful native roll:

```text
RollFish -> wait=false -> hasRolled=true -> probability=1 -> duration
         -> tip -> renderer -> read FishProto.IsFish
```

The outer catch converts any later exception into `false`. If an exception
occurs after `wait=false`, the native state is bite-ready while the Product
session remains `WaitPlayable`; the next update can report a failed action
against already-committed native state. A tip/read failure can therefore
misclassify a successful commit as an uncommitted operation.

The mutation cannot be represented as a true rollback transaction. A
successful current `RollFish` consumes native random choices, writes
`FishingCache.FishProto`, creates an Item through `ItemFactory`, and writes
`FishingCache.FishItem`. Restoring four private Wait fields and two cache
references would not restore the native RNG or prove that item construction
had no external effects. Calling that a complete snapshot/rollback would be a
false assurance claim.

## Selected AF-D2 Closure

Select **explicit fault-close/reconcile**, with the native roll as the
irreversible prepare point and `_waitForFishBite=false` as the final commit
bit.

The required field order is:

```text
RollFish succeeds
  -> hookProbability=1
  -> fishOnHookDuration=current native duration
  -> hasRolled=true
  -> waitForFishBite=false  (commit bit, last)
```

This order preserves a safe native continuation at every pre-commit failure:

- before `hasRolled=true`, native Wait may roll again normally;
- after `hasRolled=true` but before the commit bit, probability is already
  one and duration is already valid, so native Wait can finalize the bite;
- if a setter or commit verification is uncertain after the native roll,
  AutoFishing must stop its automation session, release its scoped inputs and
  leave the still-current native Wait to the player/game rather than retrying
  a second Product transaction against uncertain state;
- if the commit bit is observed false, the transaction is committed even when
  a setter reported after-write failure;
- tip and renderer refresh are post-commit cosmetic work. Their failure is
  diagnostic only and must never turn a committed bite into a rejected one;
- fish-result observation is also post-commit. An observation failure may be
  recorded, while native `NextState` remains the authoritative result router.

The implementation must expose a distinct fault-close result so `ModEntry`
disables the F6 automation owner immediately. Returning the existing generic
`native-bite-failed` and silently keeping the session active is insufficient.
Unit fault injection must cover every required write, an after-write commit
exception, commit verification failure, tip failure and fish-result read
failure.

## Hook Transaction Decision

Keep `BuildPlans()` as one 22-patch atomic ProductNative transaction. The body
diff found no evidence that a subset can safely remain active after another
fishing target fails. Unknown/current builds must still resolve the complete
inventory, and one installation failure must roll back the Product owner.

The frozen Fishing Compatibility Hook map describes the old-ABI Host and is
not Product behavior proof. This Review does not change that owner or promote
a fishing public API.

## Rejected Hypotheses

- **Reuse the A/D/Space/Shift fallback for current game.** Rejected because it
  is not the native device-independent owner and misses remapped/controller
  input.
- **Use only `inputMultiplier`.** Rejected because current Wait independently
  observes effective `VelocityX`, including the base-state offset projection.
- **Copy current Wait/Ready/Pull bodies into the Product.** Rejected; the
  Product's Postfix/native-owner route preserves their new timing, tool,
  equipment and base-state invariants.
- **Rollback only the four Wait fields.** Rejected as a complete rollback
  claim because native roll has already consumed randomness and changed the
  fishing cache/item object.
- **Treat tip or renderer failure as a failed bite.** Rejected because those
  effects occur after the native result exists and are not the commit owner.
- **Drop InstantBite from 0.6 acceptance.** Rejected by the roadmap; it is an
  existing public Product feature and `AF-D2` is a release blocker.

## Required Implementation and Acceptance

Before current AutoFishing can be packaged:

1. replace the removed-property/fallback path with exact current
   `MoveModifier.inputMultiplier` plus `VelocityX` accessors and a fail-closed
   movement policy;
2. implement and unit-test the ordered bite commit plus explicit automation
   fault-close path;
3. keep the atomic 22-Hook inventory and pass exact current signature/body
   trace checks;
4. generate a current exact policy/package only through the tracked Author SDK
   authority after source validation.

The later fifth-save acceptance must include the roadmap's short-right-move
preflight, keyboard/remapped input, movement during each phase, F6 off, the
three precondition stops, bounded retry, title/reload, owner cleanup, normal
base loop and InstantBite. Controller remains explicit non-blocking `not-run`
unless actually exercised. No game claim is made by this Review.

## Validation

This was a read-only native/source comparison. It inspected the complete old
and current bodies for `AgentPhysicalStatus`, `AgentStateBase`,
`AgentStateFishing`, Wait, Ready, Pull, `FishingCache`, `ItemFishingRod`,
current `MoveModifier`, and the Product state/transaction/hook paths.

No Runtime install, Workshop/user directory write, game process, save access,
package generation or Runtime lock was used. Focused code and product tests
belong to the implementation Update, not this pre-implementation Review.

## Rollback

This Review is the frozen reasoning input for the next implementation slice.
Reverting it does not restore `HorizontalMoveFactor` or make a partial cache
rollback exact. If implementation evidence contradicts a bound native fact,
append a new review/resolution link; do not rewrite this comparison after
implementation begins.
