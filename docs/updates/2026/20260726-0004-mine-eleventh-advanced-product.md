# Mine Eleventh Advanced Product

## Metadata

- Update ID: `20260726-0004`
- Date: `2026-07-26`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`

## Source Request And Authority

After separately committing the Catalog/C1/Manager audit repairs, admit and
split Mine. Admission authority is Review
[`20260726-0002`](../../reviews/code/2026/20260726-0002-eleventh-product-mine-admission-review.md).

The admitted boundary is:

- official JSON remains ContentOwner;
- Mine ProductNative owns only its session scheduler, weighted production,
  fixed native-power transaction, optional recipe/tech mutation and 2x visual
  lifecycle;
- no Mine sidecar or precise cross-exit scheduler continuity;
- no SharedNative, public API expansion, new Host or new receipt/checker
  family.

## Implemented Atomic Switch

1. Convert `products/first-party/Mine` to the existing SDK-generated Advanced
   product identity `1.0.0` / minimum DTMAPI `0.5.5`.
2. Replace the prototype `IMachineProductionApi` client with a self-contained
   ProductNative runtime.
3. Remove the mandatory MachineProduction executor, demand/provider and
   product-specific visual callbacks.
4. Freeze the old Machine ABI shape as a warning shell with no provider,
   because no retained binary consumer exists.
5. Add focused product, lifecycle, transaction, package and zero-leftover
   evidence; then run one bounded game acceptance.

## Changed Files

- `products/first-party/Mine`: replaced the legacy project/entry with the
  SDK-authored `DTMAPI.Mine` Advanced product, private ProductNative source,
  manifest/author intent, localized ConfigMenu text and updated package docs.
- `author-sdk/advanced-reference-policies` and `tools/release`: added the
  exact build `23762374` Mine policy and projected the eleventh identity,
  package, owner and live zero-leftover rules through Catalog, policy
  registry, release common data and Batch 6 contracts/baseline.
- `src/DTMAPI.GameBridge.DolocTown` and `src/DTMAPI.Abstractions`: removed the
  mandatory MachineProduction executor, demand/provider/callback/visual paths;
  retained the exact public Machine ABI only as a deprecated/frozen warning
  shell with no Runtime or Compatibility Host provider.
- `src/DTMAPI.GameBridge.DolocTown.QA` and `tools/scripts/run-game-smoke.ps1`:
  added Mine-only native content, scheduler, production, UI, exact-owner and
  cleanup acceptance without a force-poll, force-due or product bypass.
- `tests/DTMAPI.UnitTests`, `tests/DTMAPI.QaUnitTests`,
  `tests/DTMAPI.AbiCompatibilityHarness`, `DTMAPI.sln` and build scripts:
  added native/Harmony transaction fixtures, fixed-contract/source/ABI,
  package, lifecycle and zero-leftover coverage.
- this Update, admission Review, API matrix, architecture contract, Hook map,
  smoke matrix and monthly ledger now own the final lifecycle facts.

## Validation

Passed:

- Release build of the five Runtime assemblies and all tracked Unit/QA/test
  projects with zero warnings/errors;
- full `DTMAPI.UnitTests` and `DTMAPI.QaUnitTests`;
- Catalog, Batch 6 phase-0 ownership, synthetic ABI and Author SDK/package
  focused gates;
- SDK `validate/build/pack` for Catalog id `mine`, producing exactly one
  product DLL and no bundled native/Runtime dependencies;
- current-DLL third-save `NoNativeSave`
  `GAME-SMOKE/20260726-205813`.
- corrected focused `mine-product` Unit coverage for stable object identity,
  authoritative pruning, two Mines, move/remove/index reuse, low/full/rejected
  preflight, exact rollback and bounded retry allocation;
- corrected QA Host build with zero warnings/errors;
- final SDK `validate/build/pack` candidate
  `DTMAPI-Mine-advanced-pilot.zip`;
- cold-disabled third-save `NoNativeSave`
  `GAME-SMOKE/20260726-223105`;
- restored-enabled final third-save `NoNativeSave`
  `GAME-SMOKE/20260726-223236`.

No complete Release suite, L0–L5 ladder, GC gradient or long test was run;
none is required for this bounded admission.

## Evidence

- Corrected final package SHA-256:
  `C744A52D2FBAC73BA61D1834966E2FA68F3467B041841B810AF0BF22E92D1A45`.
- Corrected entry DLL: 86,016 bytes, SHA-256
  `3E396C9ED6DBA919E81A519756AA0C3294D4EF7307CCD7B945D5A482A5479774`.
- Advanced receipt SHA-256:
  `A703D026AACBA42BA97F5587ECF68DF7AFD24CCF30D959580918085AB2F7EA2B`;
  policy SHA-256:
  `FE28A9197AD63F38BE19A7E574764FE7602CEE449AAE1FA3A08B12DF813BDA6A`.
- `205813` proves official `EComProtoAppliance/10`, native tech UI, exact
  `3/3` Harmony owner, session-derived scheduler, parameterless native
  `Launch()`, `coal x1`, native `16/4` inventory, 2x visual containment,
  title zero and Loader `instance0+actual0+callback0+roots0`.
- The same run proves player archive and committed sidecars unchanged before
  cleanup, QA/profile/source restoration, no fatal window and no remaining
  game process.
- Diagnostic attempts `195704` through `205254` are retained and classified
  in Review `20260726-0002`; they do not count as acceptance.

At the original admission snapshot the five mandatory Runtime projects remain
148 compiled files / 64,738 physical / 58,034 non-empty lines / 2,089,984
DLL bytes. GameBridge remains 73 / 25,050 / 22,162 / 743,936. The eleven
ProductNative trees are 102 files / 27,139 physical / 25,032 non-empty lines /
633,344 DLL bytes. Mandatory plus products is 91,877 physical / 83,066
non-empty lines / 2,723,328 DLL bytes. This proves physical ownership, not a
smaller repository/download/install or all-products-enabled process.

## Rollback

Revert the Mine implementation commit as one atomic unit. Do not restore only
the old provider or only the old prototype client; that would recreate dual
or absent ownership.

## Acceptance Reopened 2026-07-26

Independent Review
[`20260726-0003`](../../reviews/code/2026/20260726-0003-c1-manager-fixes-and-mine-split-audit.md)
accepted the physical ProductNative split but found two P1 implementation
defects and a narrower runtime-evidence scope:

- scheduler entries use a reusable room/index-derived key and are never pruned
  after Mine removal, movement or index reuse;
- full/low-power failures select and construct output, clone the inventory and
  may invoke native `Launch()` before all non-mutating preflight checks finish;
- `205813` proves one charged Mine success plus title/Loader cleanup, not the
  cold-disabled, activation-failure, low-power, full-storage, two-Mine,
  move/remove/index-reuse and re-enable cases.

The Update therefore temporarily returned to `in-progress/open`. The accepted
physical ownership boundary and the narrow `205813` evidence remained valid,
but no twelfth product was admitted before the focused source correction and
the smallest missing Mine acceptance matrix passed.

## Corrective Closeout 2026-07-26

The reopened findings are closed by the final candidate:

- `MineSessionScheduler` keys entries by live equipment object identity, scans
  the authoritative placed-equipment set and prunes absent identities after
  each completed poll. Moving an object preserves only its own due state;
  removal drops it; a new object reusing the old native index receives a fresh
  `current TotalTUs + cycle` due time.
- the production path now verifies native contract, free capacity and power
  before output selection, item construction, inventory snapshot or
  parameterless native `Launch()`. Low/full stable failures are fingerprinted
  until their relevant native resources change, so the retained due cycle
  does not allocate throwaway outputs every 0.5 seconds. The exact snapshot
  remains only for failures after `Launch()`.
- the former 1,447-line renamed general machine engine was removed. The
  periodic path is Mine-specific: one official identity, electric-only power,
  four fixed output rules, one native case and a four-cycle catch-up cap. The
  current product has 13 C# files / 3,434 physical / 3,213 non-empty lines;
  its largest engine file is 647 lines. No multi-machine registration, fuel
  mode or public probability-override surface remains.
- player text no longer mentions a Machine API. The borrowed well sprite,
  `8x6` footprint and runtime 2x scaling are explicitly retained only as an
  unpublished `LocalDeveloper` / `RebuildBlocked` prototype; dedicated Mine
  art and removal of scaling Hooks remain publication work, not a Runtime
  admission promise.

`223105` starts the exact final DLL with the persisted config temporarily set
to `Enabled=false` and records
`status=disabled, active=False, hooks=0/3, scheduler=0`; the original config
was then restored byte-for-byte to SHA-256
`A99B5BE192F968D15C464A2E96526B8ECE7881044FE095AB4957199FA1BBD5CD`.

`223236` starts again after that restoration and passes the combined native
matrix: two Mines are independently scheduled; both low-power cases retain
due state without output; a `16/16` full Mine consumes no power while the
second Mine produces; hot disable removes all three Hooks and clears the
scheduler; re-enable derives fresh entries; moving the same object preserves
its due time; dismantling prunes it; a replacement reusing the old index gets
a fresh identity and due time. Title and Loader cleanup report
`actual3+callback1+sessionDerived -> actual0+callback0 ->
instance0+actual0+callback0+roots0`. The protected third-save archives and
sidecars, config, official profile, SDK source and QA staging are restored,
no fatal window appears and no game process remains.

## Independent Reopen And Focused Correction 2026-07-27

Independent Review
[`20260727-0001`](../../reviews/code/2026/20260727-0001-mine-fixes-and-debugconsole-split-audit.md)
retains the `223105`/`223236` behavior evidence but found that
`RestoreNativeMutations()` cleared every recipe/tech restoration closure even
when one reverse step threw. The normal runtime matrix remains accepted; the
Update returns to `implemented/open` for this source-only cleanup invariant.

The correction removes each restoration closure only after that exact step
succeeds. Failed closures and their original reverse order stay queued, status
remains `cleanup-failed`, and diagnostics report the pending count. The
fault-injection fixture mutates two native values, fails one reverse step,
proves the successful closure is not replayed, then retries the pending closure
to its exact original and reaches an empty ledger.

Dedicated art and removal of prototype 2x scaling remain explicit
pre-publication blockers. Mine stays `implemented/open` until the independent
review accepts this focused correction; no Mine game-matrix replay is required.

## Cleanup-State Follow-up 2026-07-27

Code Review
[`20260727-0002`](../../reviews/code/2026/20260727-0002-mine-and-debugconsole-fix-recheck.md)
accepted the retained restore ledger but identified two remaining P2 gaps. The
focused correction now drives the real activation path through:

1. native content activation failure;
2. fault-injected exact restore failure;
3. rejected reactivation with zero Hooks and zero active runtime;
4. title cleanup failure followed by a configuration refresh that must retain
   `cleanup-failed`; and
5. a later successful cleanup that restores the original exactly and returns
   to `waiting-for-save`.

The Release `MineHarmonyOwnerFixture` build passed with zero warnings/errors,
and focused `DTMAPI_UNIT_TEST_FOCUS=mine-product` passed. This is source/unit
coverage only; the accepted `223105`/`223236` game matrix was not rerun because
neither correction changes Mine gameplay behavior. Mine remains
`implemented/open` pending independent acceptance.

## Deployable Byte Correction 2026-07-27

Independent Review
[`20260727-0003`](../../reviews/code/2026/20260727-0003-mine-debugconsole-transaction-fix-audit.md)
accepted the source and focused lifecycle correction but found that the author
output, installed product and SDK package still contained pre-fix bytes.

The existing Author SDK `validate/build/pack` route now produces
`temp/batch6-mine-advanced-pilot/DTMAPI-Mine-advanced-pilot.zip` with:

- package SHA-256
  `848154FBB6816F0F0D18CBEF7A574539400FA6540C29A9B0A50F52BD3DECCE2B`;
- entry DLL length `86,528` and SHA-256
  `E2EEEA2BECE5A71B779A32BE4A4637D2C56C42E602CB7A0D08B61D4C0D4FCCBE`;
- Advanced receipt SHA-256
  `01489C5DC760B627A1B36A5FB21470798EE406C0F024A774FEB5B491DF4DDC69`.

The SDK transaction updated the managed destination and selected its exact
`LocalDevelopment` source. Installed entry bytes match the package entry;
destination tree SHA-256 is
`108F02157C7C3144DD5227548072291E7579A6D57DDE27EED46BE365567301CD`
and source digest is
`BBF3E3F45981ADB0A971D52D12DB9030AAFF013012112466B4AE54613141CBD7`.
Product Catalog and Batch 6 G0 identity checks pass. Per the independent
Review, the accepted Mine game matrix was not rerun; this was a deployable-byte
and package correction only. Mine remains `implemented/open` pending the final
independent acceptance review.

## Final Independent Acceptance 2026-07-27

Independent Review
[`20260727-0004`](../../reviews/code/2026/20260727-0004-lifecycle-closeout-product-inventory-release-route-audit.md)
accepted the restoration lifecycle and exact deployable bytes with no remaining
P0/P1/P2. Mine is therefore `verified/closed` as the eleventh ProductNative
product. Borrowed well art, runtime 2x presentation and product-publication
work remain separate `RebuildBlocked` debt and were not reclassified by this
acceptance.
