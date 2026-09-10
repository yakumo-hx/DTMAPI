# 20260830-0003: Y Console 1.1.2 stackable fuel and maintenance

## Metadata

- Update ID: `20260830-0003`
- Date: `2026-08-30`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `open`
- Area: debugconsole/productnative/1.1.2/content/item-stack/ui-maintenance/compatibility-retirement/docs
- Source: 用户确认动物保存/重载/隐藏产物和普通机器无限燃料消耗/保存/重载可按手测通过接收；确认旧 0.3.1 Y 控制台无任何使用者并解除相关保留限制；允许机械拆分单体 UI；要求先详细静态审查旧城守护者、不先测试，然后先更新文档、再把无限燃料改为普通物品 `999` 堆叠。
- Review: [20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review](../../reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)

## Scope And Decisions

- Advance the source/target candidate to `1.1.2` while preserving the exact
  public `1.1.1` Steam artifact and `ActiveNoUploadAuthorization`.
- Change only `dtmapi_creative_generator.overlay` from `1` to `999`; retain its
  save-stable ID, one-billion finite energy, coal art and ordinary fuel-only
  item function.
- Accept animal save/reload/hidden-yield and compatible-machine fuel
  consumption/save/reload as user manual QA. Do not manufacture a
  disposable-run or smoke receipt.
- Record the exact `space_ship -> two space_ship_bastion` native composite
  chain and leave monster code/runtime untouched until a later bounded fix.
- Mechanically split the monolithic `DebugConsoleUi` into partial source files
  without changing behavior. Nullable warning correction is not implied.
- Mark the old 0.3.1 compatibility-retention prerequisite as user-released,
  but defer physical provider/UI/action/API removal to a separate breaking
  cleanup because the current product still consumes 28 Debug DTO/enum types.

## Documentation-First Changes

- Add the owning Review and this in-progress Update before product source
  changes.
- Supersede the old world-actions Planning route with current Monster/Animal,
  infinite-fuel and Resource truth.
- Correct the public API matrix, DebugConsole Hook Map, issue ledger, general
  source-arbitration issue and historical smoke-row follow-up without
  rewriting the facts of their original runtime runs.
- Append the new manual acceptance and static root cause to the 1.1.0 Update
  and Old City Guardian Review.

## Implemented Source Changes

- `products/first-party/DebugConsole/Content/item_tbitem.json`
- `products/first-party/DebugConsole/src/Ui/DebugConsoleUi*.cs`
- DebugConsole source/candidate version projections in product metadata,
  Catalog and focused contract checks
- `tests/DTMAPI.UnitTests/Program.cs` structural assertions for ordinary
  `overlay=999` and the behavior-neutral partial split

## Changed Files

- Product content and candidate metadata:
  `products/first-party/DebugConsole/Content/item_tbitem.json`,
  `manifest.json`, `official-info.json`, `README.md` and
  `tools/release/dtmapi-mod-publish-zh.json`.
- Product UI maintenance:
  `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.cs` plus the new
  `DebugConsoleUi.Layout.cs`, `DebugConsoleUi.Catalog.cs`,
  `DebugConsoleUi.Actions.cs` and `DebugConsoleUi.Reflection.cs` partials.
- Compile/test projections:
  `src/DTMAPI.GameBridge.DolocTown.Compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.csproj`,
  `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj`,
  `tests/DTMAPI.UnitTests/Program.cs`, the focused native-trace script and
  Catalog/release-contract checkers.
- Canonical facts and navigation: the linked Review, Planning route/index,
  public API matrix, DebugConsole Hook Map, ISSUE-015/020 ledger facts,
  historical Y-console smoke interpretation, this Update and the August
  monthly ledger.

## Validation

- Product JSON parsing and packaged item inspection passed. Source and generated
  package both contain `dtmapi_creative_generator`, `overlay=999` and
  `electric_energy=1000000000`.
- Focused DebugConsole Unit, native trace, Runtime-floor compatibility,
  Product Catalog, document governance and final diff/source-shape checks pass.
- Author SDK validate/build/pack passed against the repository-built exact
  Steam-shaped `24456188` fixture and retained the game-loaded
  `netstandard2.0` boundary. The full multi-product Release contract was not
  run: it requires primary and repeat Author SDK artifacts for every current
  release product, while this task owns one focused Y-console candidate. Its
  changed 1.1.2 projection is covered by the focused Unit and Catalog gates.
- No game launch, native save, local official-package deployment, upload-tree
  preparation or Steam subscription write is authorized by this Update.

## Evidence

- Mechanical equivalence check: the five partial files recompose to the exact
  pre-split `3,942` class-body lines with ordinal equality.
- Focused `DTMAPI_UNIT_TEST_FOCUS=debugconsole-product`: passed after adding
  the explicit partial compile links; `16` pre-existing DebugConsole nullable
  warnings remain and are not claimed fixed.
- `test-dtmapi-060-debugconsole-native-trace.ps1`: passed between policy build
  `24456188` and current public `24966367`. The exact monster Generate/Remove
  methods and composite `space_ship` source files are unchanged; product
  monster implementation remained untouched.
- `test-dtmapi-060-runtime-floor-compatibility.ps1`: passed; Author API target
  remains `0.5.5`, product floor remains `0.6.1`, and the tracked policy floor
  remains `0.6.0`.
- `check-product-catalog.ps1`: passed (`27` products, `11` public,
  `22` Workshop items, `48` API rows). The intentional source-version change
  advances the protected public identity/path digest to
  `29ccf96309f49861e9a06e5d9ad81b2f9fe2fa79811db08ac267df9ced4d6ec5`;
  the separate immutable published-artifact digest and public `1.1.1` row did
  not change.
- `check-doc-governance.ps1`: passed `7,292` checks. Product and release JSON
  parsing plus final whitespace/diff checks passed.
- Author SDK exact fixture validate/build/pack: build source file count `25`,
  source-tree SHA-256
  `bddf361ebba732c50450c9dfd2eefbd3ab057f796c21b618bfbac54e9d071c86`,
  entry DLL `221,184` bytes / SHA-256
  `774224ef9f11ce06a6ac63d033d3f180d03402b8227e8a2a86440ed2cc79b863`,
  package `26` files / SHA-256
  `CB2717880A0BD97D46ADD1A1E906A9E5DB2D148E7F1AF651D05992886924E4C6`.
  The package contains one product DLL and no bundled native/Runtime DLL.
  Temporary fixture and package roots were deleted after inspection.
- Runtime/game evidence: not run by explicit scope. User manual acceptance is
  recorded only in the owning Review and is not rewritten as an automated run.

## Rollback Notes

- Revert the `1.1.2` source/candidate projections, `overlay=999`, focused tests
  and partial-file split together; preserve the immutable public `1.1.1`
  artifact, Workshop manifest and current-subscription authority.
- Do not restore stale Planning/API/Hook facts merely to roll back source.
  The user-confirmed animal/fuel behavior, 0.3.1 no-user decision and
  `space_ship` static root cause remain durable review facts.
- A future physical 0.3.1 Compatibility removal is not part of this rollback
  because it is not implemented here.

## Follow-Up

- A future authorized player/package pass may confirm inventory merging from
  `1` through `999`; this does not reopen the already accepted ordinary-machine
  fuel consumption/save/reload behavior and is not an upload prerequisite
  created by this task.
- Old City Guardian was completed as the separate
  [20260830-0004 ProductNative correction](20260830-0004-y-console-visible-partial-composite-spawn.md):
  it uses visible partial success with no spawn rollback or shared circuit and
  passed the bounded ProductNative `NoNativeSave` comparison.
- Physical retirement of the unused 0.3.1 provider/UI/action Compatibility
  route remains a separate breaking cleanup after the current 28 DTO/enum uses
  are internalized or deliberately retained.
- The `16` nullable warnings remain explicit technical debt; the mechanical
  split made their Reflection/native ownership visible but did not change
  annotations or runtime behavior.
- Any Workshop upload or official-folder synchronization requires a new exact
  authorization; this Update creates none.
