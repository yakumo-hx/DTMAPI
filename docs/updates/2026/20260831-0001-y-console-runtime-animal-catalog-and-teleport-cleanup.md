# 20260831-0001：Y 键控制台运行时动物目录与传送旧辅助清理

## Metadata

- Update ID: `20260831-0001`
- Date: `2026-08-31`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Area: `debugconsole/productnative/animalpack/catalog/source-filter/ui/teleport/api-cleanup/technical-debt`
- Source: 用户要求先研究并最小实现 Y 键控制台对本地 DTMAPI AnimalPack 新动物的兼容，取消怪物/动物分类导致的左侧来源栏切换，并物理删除传送 CSV 导出和无意义的“当前位置”显示。

## Source Request

用户确认其他 Mod 物品进入 Y 键控制台是正常设计，并要求控制台兼容本地 DTMAPI
缺氧动物包的真实新动物；同时取消点击怪物/动物后左侧来源栏重建切换的技术债。
用户允许把生物卡数量并入总体来源计数。第二项要求物理删除已经完成历史用途的
传送地址 CSV 导出和无意义的“当前位置”显示。

## Owning Review

- [Y 键控制台动物包目录、来源栏切换与传送旧辅助功能复核](../../archive/reviews/manual-qa/2026/20260831-0001-y-console-animalpack-catalog-and-teleport-ui-debt.md)
- Prior retirement authority: [Y 键控制台 1.1.2 手测接收、0.3.1 退役授权与旧城守护者静态审查](../../archive/reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)

## Implementation Boundary

- Replace the four-ID animal allowlist with actual runtime `TbAnimal`
  enumeration. JSON metadata may attribute a loaded proto to a DTMAPI content
  owner, but it may never create a catalog row when that proto is absent from
  the native table.
- Project `Content/DTMAPI/custom-animals.json` species IDs through the existing
  content index into private ProductNative source metadata. Do not open or
  promote the frozen public `ICustomAnimalApi`.
- Keep one source list across item, monster and animal categories. Counts are
  catalog-card counts: item rows plus monster cards plus Child/Adult/Ready
  animal cards. Source and category selections remain independent filters.
- Remove the current-location UI row and all CSV export code. Retain native
  position snapshots used by teleport Before/AfterRequest and InstantSave
  diagnostics.
- Use the user's prior no-consumer decision plus this explicit deletion request
  to remove only `ITeleportDebugApi.ExportDestinationsCsv` and
  `TeleportCsvExportResult`; do not broaden this task into retirement of the
  remaining Diagnostic family or Compatibility Host.
- Keep product version and all Steam/public release authorities unchanged.

## Changed Files

- DebugConsole ProductNative:
  `src/ProductRuntimeAdapter.cs`, `src/Native/DebugConsoleNativeActions.Spawn.cs`,
  `src/Native/DebugConsoleNativeActions.Core.cs`,
  `src/Native/DebugConsoleNativeActions.Helpers.cs`, the catalog/action/
  reflection UI partials, private catalog/runtime/action models, product README
  and all nine `i18n/*.json` files.
- Retired Diagnostic projection:
  `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`,
  `src/DTMAPI.GameBridge.DolocTown/OwnerBoundGameBridgeApis.cs`, the exact
  GameBridge Compatibility proxy/service/runtime adapter, QA action adapter
  and fixture, plus Bootstrap fallback localization.
- Assurance:
  `tests/DTMAPI.UnitTests/DebugConsoleSpawnProductTests.cs`, the DebugConsole
  source/localization contracts in `tests/DTMAPI.UnitTests/Program.cs`,
  `tests/DTMAPI.AbiCompatibilityHarness/Program.cs`, and
  `tools/scripts/test-dtmapi-060-debugconsole-native-trace.ps1`.
- Authority records: the owning Manual QA Review,
  `docs/api/public-api-matrix.md`, this Update and the August monthly ledger.

## Validation Plan

- focused `debugconsole-product` Unit, including a runtime `TbAnimal` fixture
  with one DTMAPI-owned species that is not in the original four IDs;
- Abstractions/GameBridge/Compatibility builds and ABI harness;
- DebugConsole Author SDK build/package validation;
- localization JSON/key-set, public API matrix and document-governance checks;
- `git diff --check` on the final scoped changes;
- one bounded third-save `NoNativeSave` AnimalPack UI/spawn smoke only if the
  existing local fixture can exercise the exact new source filter without
  mutating package/release state.

## Validation

- Focused `DTMAPI_UNIT_TEST_FOCUS=debugconsole-product`: passed. The physical
  table fixture exposes base `slime` plus non-allowlisted AnimalPack `hatch`;
  both receive Child/Adult/Ready cards, all Hatch cards retain the AnimalPack
  source, and `hatch.child` reaches the proto path before the fixture's
  deliberate `native-host-unavailable` stop. Existing Guardian/visible-partial
  tests also remain green. The Unit project retains the separately recorded
  `16` nullable warnings and has zero errors.
- Release builds passed for Abstractions, GameBridge, Compatibility Host, QA,
  Bootstrap, ABI harness and Unit. The first six build with zero warnings and
  zero errors; Unit has the same `16` known warnings and zero errors. Full QA
  Unit passed.
- Focused `public-api-status`, `api-metadata` and `compatibility-host` Unit
  routes passed. Synthetic retained ABI passed. The real retained-release ABI
  gate resolved all `463/463` public-product MemberRefs, including the old
  Y-console's `34/34`; its baseline diff contains exactly the authorized `20`
  CSV member/DTO metadata entries and `0` unexpected removals. Any other public
  deletion still fails the same gate.
- DebugConsole native trace passed against exact policy build `24456188` and
  current public build `24966367`; it now requires runtime `TbAnimal`, unified
  catalog-source code and absence of the stable animal allowlist, rollback/
  circuit and retired teleport UI tokens. Runtime-floor compatibility passed
  with game-loaded assemblies remaining `netstandard2.0`.
- Author SDK validate/build/pack passed against the tracked Steam-shaped
  `24456188` fixture: validate file count `51`, source file count `25`, source
  tree SHA-256
  `F2E301E85C17C96512ECEEA8D355E77B51F367BE9A737A3D31F68D1CC639FB32`,
  entry DLL SHA-256
  `96394BC658D507BEB9F9F7884CC6802AAA3741800029B17977F9B415EFA68F39`,
  and package SHA-256
  `F95FF5A2869B140C2E48A9D2F15612FC1747DA9329448299AF3A2EB29899DEC4`.
- Product Catalog passed (`27` products, `11` public, `22` Workshop items,
  `48` API rows). Twelve current product/localization JSON files parse and
  `git diff --check` reports no whitespace error. Document governance passed
  `7,386` checks.
- A non-acceptance full Unit diagnostic was also attempted. It stopped in the
  pre-existing `PreviewVersionMetadataIsConsistent` assertion: the test still
  pins Runtime Steam/payload hashes `8280dcfb...` / `c7335934...`, while the
  current HEAD Catalog contains `846665a9...` / `b4ec6a44...`. The Product
  Catalog checker and all task-relevant focused Unit routes above pass; this
  unrelated stale assertion was not changed as part of the Y-console task.
  Its exact `cleanup-pending` managed test session was removed through the
  tracked test-artifact cleanup protocol.
- No game process was launched. The existing automated smoke can prove generic
  Y-key lifecycle/spawn actions but cannot select and assert the exact new
  AnimalPack source/category UI combination; manufacturing a weaker runtime
  run would not close that behavior. Runtime status therefore remains
  `not-run`, and this Update remains `implemented`, not `verified`.

## Evidence

- The owning Review transcribes all three screenshots and identifies the two
  direct causes: the four-value `StableAnimalIds` list excluded loaded custom
  protos, while the virtual Monster/Animal branch replaced the normal source
  groups and reset `sourceFilter`.
- Current source enumerates only actual `TbAnimal.DataList` rows. Existing
  `Content/DTMAPI/custom-animals.json` contributes source attribution but can
  never manufacture a card without a loaded native proto. The source column is
  built once from item groups plus monster/animal card counts; category and
  source callbacks no longer clear each other.
- Current source and all projected runtime/QA assemblies contain no
  `ExportDestinationsCsv` or `TeleportCsvExportResult`. The UI contains no
  current-location row or CSV button, while `TeleportResult.Before` /
  `AfterRequest`, `GetCurrentSnapshot` and fixed native destinations remain.
- The Author SDK artifact under
  `temp/y-console-animal-catalog-build` is validation-only and does not modify
  Steam, the official upload folder, subscriptions or player saves.

## Rollback Notes

Restore the fixed animal allowlist, virtual-category source replacement,
ProductNative current/CSV controls and the exact removed Diagnostic member/DTO
together. A partial rollback that restores only the UI button without the
public/member projection, or restores JSON-declared animals without requiring a
runtime `TbAnimal` proto, is invalid. No save, subscription, official upload
folder or release authority belongs to this rollback.

## Follow-up

- General custom-monster source attribution remains outside this task because
  no current DTMAPI custom-monster content owner/protocol was found.
- Public `1.1.1`, local source candidate version and Steam upload authorization
  remain unchanged unless the user opens a separate release task.
