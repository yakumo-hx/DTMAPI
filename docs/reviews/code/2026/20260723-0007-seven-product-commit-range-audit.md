# Seven-Product Commit-Range Audit

**Review ID:** `20260723-0007`

**Date:** 2026-07-23

**Status:** `recorded — retain implementation; seventh product remains implemented/open; eighth-product implementation is blocked by two focused P1 closeouts`

## Scope

This audit reviews the current committed range after the earlier sixth-product
audit:

```text
97ecc759 fix: close MoreSaves sixth-product audit
6108d735 refactor: close Core and API cleanup tails
e76ffbc4 docs: admit ChestLocatorEnhancer as seventh product
9e1b2d4d feat: split ChestLocatorEnhancer as seventh product
```

The tracked tree was clean at `9e1b2d4dae747849661141f581647030a88de4dc`.
The unrelated untracked portable reverse-capture Update, tool directory and
builder were excluded from this review.

This is an audit-only Review. It creates no new Update, receipt, schema,
builder, checker family or acceptance ladder. Update
[`20260723-0007`](../../../updates/2026/20260723-0007-chestlocator-seventh-advanced-product.md)
remains the implementation lifecycle owner.

## Verdict

Keep all four commits. The previous MoreSaves, Host, Phase 1 and Phase 4
corrections are real, and the ChestLocatorEnhancer extraction is a real
default-loaded Runtime reduction rather than a facade over the old mandatory
executor.

The seventh product must nevertheless be treated as **implemented/open**, not
fully `verified/closed`, until two focused P1 gaps are closed:

1. execute the admission-required real Harmony two-order ownership and minimum
   config/behavior proof instead of relying on injected booleans and source
   string checks;
2. update and pass the existing official-local install and Runtime-only
   uninstall transaction tests, which still hard-code three Advanced products
   and now fail before testing the seven-product inventory.

The current implementation and its accepted Case/Loader behavior do not need
to be rolled back. No complete Release, L0-L5, GC ladder or long test is needed
for this closeout. One smallest committed-candidate game run can combine final
Runtime provenance with any compatibility-first proof that cannot be expressed
in a focused executable fixture.

No eighth product should start implementation before the two P1 items are
green. Read-only analysis and the already planned seven-product comparison may
continue.

## Confirmed Previous Closeout

The corrections in `97ecc759` and `6108d735` close the prior audit findings:

- Runtime `0.5.5+` Doctor and status checks require one coherent dormant Host
  component in both receipts, while the explicit `0.5.4` legacy case remains
  compatible.
- Frozen SaveSlots behavior distinguishes unregistered, cold-disabled and
  mixed-owner disabled state.
- MoreSaves subscribes to Update only while a retry is pending; healthy steady
  state has no frame listener, and terminal deactivation failure no longer
  promises an impossible same-instance retry.
- current, published and target version axes are independent; the earlier
  unauthorized Chest publish-version change was reverted.
- the real component move/place interruption windows are present in the
  existing Runtime transaction matrix.
- the historical sixth-product admission Review and route documents were
  restored to their correct lifecycle roles.
- the obsolete Core string-input and fatal-window endpoints are absent;
  CustomEntity remains Experimental/Frozen; AutoHarvest no longer consumes the
  debug instant-save API; owner/thread/stale semantics retain focused coverage.

The focused Unit groups `phase1-core-cleanup`, `phase4-api-cleanup`,
`api-metadata`, `moresaves-product`, `moresaves-acceptance-routing` and
`compatibility-host`, plus all eleven InstallDoctor tests, passed again in this
audit.

## Confirmed Seventh-Product Result

The physical and ownership split is valid:

- `DTMAPI.ChestLocatorEnhancerMod` is one SDK-generated `netstandard2.0`
  Advanced ProductNative product at version `1.0.0`, minimum Runtime `0.5.5`.
- it owns one exact
  `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)`
  Postfix under `dtmapi.mod.dtmapi.chestlocatorenhancermod`;
- it adds no SharedNative capability, public inventory API, second Host,
  receipt family or product-specific build authority;
- the frozen `IChestLocatorEnhancerApi` executor moved to the existing
  dormant-shipped Compatibility Host, while mandatory GameBridge retains a
  thin on-demand proxy and coordination boundary;
- the current committed product source rebuilds byte-for-byte to the recorded
  candidate package:
  `D43FB74329C7434726BAEB829E0FAE4D82D4CBA3D7661D1C44CCD44A24005F77`,
  with entry DLL
  `FB0EF1504ADE1503901CE6F889FCA0A498EE3EADA76AC823A2109FD17DB63F57`;
- `GAME-SMOKE/20260723-203127` is valid product-first evidence for one real
  Postfix, official `case_locator 0 -> 3 -> 1` Count/Cost behavior, title
  restoration, real Loader cleanup
  `actual1+callback1 -> instance0+actual0+callback0+roots0`, player-state
  restoration and clean exit;
- `GAME-SMOKE/20260723-210424` is valid same-process evidence for two third-save
  load/title cycles without a duplicate product owner.

The measured claim is also correctly bounded. The mandatory Chest boundary
changes from `542/475` physical/non-empty lines to `243/206`, and mandatory
GameBridge changes from `966,656` to `937,984` bytes. The 540-line frozen
executor and the 32,768-byte ProductNative DLL still ship, and the product
contains 1,407 physical source lines. This is default-loaded Runtime reduction,
not total source, download or installed-footprint reduction.

## Findings

### P1 — The admission-required real two-owner executable proof does not exist

Admission Review
[`20260723-0005`](20260723-0005-seventh-product-chestlocator-admission-review.md)
requires both product-first and compatibility-first order to inspect real
Harmony owners on the exact native target before a second Postfix can exist.
It also requires executable coverage for config reload, cleanup and the native
inventory policies.

Current tests do not meet that boundary:

- `ChestLocatorEnhancerProductTests.cs:35-51` calls the pure
  `DecideInstall(bool, bool)` decision only;
- `ChestLocatorCompatibilityHostTests.cs:47-98` injects
  `Func<bool>` as the managed-owner observation. The compatibility-first case
  reconciles demand before a real native target or compatibility Postfix is
  installed;
- `ChestLocatorEnhancerProductTests.cs:97-114` checks traversal behavior by
  reading source text and searching for tokens;
- the two game receipts keep the Compatibility Host dormant and therefore
  prove only the normal ProductNative owner path.

The implementation does use `Harmony.GetPatchInfo`, and no duplicate owner was
observed in the accepted product-first smoke. This is an evidence gap, not
proof of a current duplicate-Hook bug. However Final Review
[`20260723-0006`](20260723-0006-chestlocator-seventh-product-final-review.md)
and Update `20260723-0007` overstate the result when they say both physical
orders and the complete minimum matrix have passed.

Minimum closeout:

1. use the existing Unit/fixture mechanism to install and inspect real Harmony
   owners on one exact test/native target in both orders;
2. prove that the second owner fails closed or completes the admitted atomic
   handoff, and that cleanup never removes the unrelated owner;
3. execute config disable/re-enable and failed-unpatch residual observation;
4. execute shared Case and StorageShelf/ItemBox policy, native
   `autoUseBox`, option-off behavior and reference-identity deduplication rather
   than checking source tokens.

Prefer a focused fixture. Use one smallest game smoke only for facts that
cannot be expressed without the real Unity/Mono target.

### P1 — Existing install/uninstall transaction gates stop at three products

The production installer selects `AuthorSdkProject` definitions dynamically,
and the accepted game package proves that ChestLocatorEnhancer can be built and
installed. The existing focused ownership tests are nevertheless stale:

- `tools/scripts/test-developer-official-local-install-transaction.ps1:294-296`
  requires exactly AutoFishing, OneActionComplete and ActionSpeed;
- `tools/scripts/test-player-runtime-only-uninstall.ps1:367-373` repeats the
  same three-product contract.

Both focused entries fail on the current tree with:

```text
Expected exactly three managed Author SDK products ... found 7.
```

This blocks the claim that the seven-product generic install/uninstall
authority is fully green. Update the existing assertions to derive and
exact-match the authoritative Advanced set from the existing Catalog/policy
projection, then run both current tests. Do not create a Chest-specific
transaction script or another receipt family.

### P2 — ProductNative retains avoidable allocation and log amplification

The product adds no idle frame subscription or file polling, so it does not
create a new always-on root. Its patched query path still carries avoidable
work:

- `ChestLocatorEnhancerCallbacks.cs:20-39` receives the two value-type
  `Vector2Int` arguments and passes them to unused `object anchor/object area`
  parameters, boxing both values on every native query;
- `ChestLocatorInventoryTraversal.cs:30-155,234-323` creates new
  `List`, `HashSet` and `Stack` instances, walks overlapping room/building
  roots, and repeats property/field/method reflection on every call;
- `ChestLocatorEnhancerNativeRuntime.cs:150-203` builds a long diagnostic
  string on every call and logs every successful widening because
  `AppendedInventoryCount > 0` remains true whenever shared containers exist.

Reverse metadata shows this target under inventory Count/Cost, backpack,
workbench, synthesizer and related material-query paths. This is therefore a
real action/UI query hot path even though it is not proven to execute every
frame. The old mandatory executor had similar debt, so this is not a newly
introduced behavioral regression; it is unresolved ProductNative GC/log debt
that contradicts a fully clean `1.0.0` baseline.

Remove unused native arguments from the Postfix, cache immutable reflection
metadata, avoid overlapping graph scans, lazily build diagnostics, and log only
on first observation/state transition/error unless verbose logging is enabled.
Add one focused repeated-call allocation/log-count check. No long GC run is
required for this correction.

### P2 — Runtime smoke is not bound to the final committed candidate

Both positive smoke roots record Runtime `BuildCommit=e76ffbc495c3`, the
documentation-only admission commit, because the extraction was still
uncommitted. The final implementation is `9e1b2d4d`.

The product side is strongly recoverable: rebuilding current committed source
with the same frozen Author SDK reproduces the exact package and DLL hashes
used in both smokes. The Runtime side lacks an equivalent dirty-tree source
digest, and current deterministic Core/GameBridge outputs do not match the
hashes recorded in those receipts. Therefore the receipts cannot alone prove
that every final mandatory Runtime byte is the tested byte.

After the P1 corrections, run one smallest smoke from the frozen committed
candidate and record exact Runtime plus product package hashes. This may be
combined with the compatibility-first proof if a real-game fixture is still
needed. Do not run a complete Release, L0-L5, GC or a long soak for this
provenance correction.

### P2 — Catalog checker has a Windows PowerShell 5.1 compatibility regression

`tools/scripts/check-product-catalog.ps1:2161` calls
`String.Contains(string, StringComparison)`, an overload unavailable in Windows
PowerShell 5.1/.NET Framework. The checker passes under PowerShell 7 but fails
under Windows PowerShell before reaching its Catalog assertions.

This line predates the four audited commits, so it is a newly discovered
baseline defect rather than a seventh-product regression. Replace it with the
existing PS5.1-safe ordinal `IndexOf(...) -ge 0` pattern and run the same
checker once under both hosts. No broader release validation is implied.

## Validation Performed

- tracked tree and commit-range inspection: PASS;
- `git diff --check 19cb81c5..9e1b2d4d`: PASS;
- focused Unit project Release build: PASS, zero warnings and zero errors;
- Unit focuses:
  `chestlocator-product`,
  `batch5-gamebridge-demand`,
  `compatibility-host`,
  `api-metadata`,
  `phase1-core-cleanup`,
  `phase4-api-cleanup`,
  `moresaves-product`,
  `moresaves-acceptance-routing`: PASS;
- InstallDoctor eleven-test suite: PASS;
- Product Catalog under PowerShell 7:
  `products=27`, `public=11`, `workshop-items=21`, `api-rows=48`: PASS;
- Batch 6 Phase 0 historical contract: PASS;
- current committed ChestLocatorEnhancer Author SDK rebuild: PASS, exact
  recorded package hash;
- developer official-local install transaction test: FAIL at stale
  three-product assertion (`found 7`);
- player Runtime-only uninstall ownership test: FAIL at stale three-product
  assertion (`found 7`);
- Product Catalog under Windows PowerShell 5.1: FAIL at the unsupported
  `String.Contains` overload.

No game, complete Release, L0-L5, GC ladder, long test, Workshop upload or
0.5.5 publication flow was run by this audit.

## Route

1. Close the two P1 items using the existing Unit/transaction authorities.
2. Correct the local Chest query allocation/log amplification with a focused
   budget; do not turn it into SharedNative.
3. Produce one smallest committed-candidate proof only if the real Harmony
   order or final Runtime provenance still needs Unity/Mono.
4. Restore Update `20260723-0007` to `verified/closed` only after those checks
   pass.
5. Then perform the already authorized seven-product comparison and decide the
   eighth product separately.

The portable reverse-capture work, G7, full 0.5.5 release, L0-L5, GC and long
soak remain outside this closeout.
