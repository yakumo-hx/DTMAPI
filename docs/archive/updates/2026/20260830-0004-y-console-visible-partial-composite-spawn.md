# 20260830-0004: Y Console visible partial composite spawn

## Metadata

- Update ID: `20260830-0004`
- Date: `2026-08-30`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Area: debugconsole/productnative/monster/animal/composite-spawn/partial-success/no-native-save
- Source: 用户要求修正旧城守护者，保留一倍和十倍生成且不增加确认；审查后取消自动回滚与共享熔断，采用可见的部分成功语义，并允许比较采用不保存运行。
- Review: [20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review](../../reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md#7-用户后续决策取消生成伪事务与共享熔断改为可见的部分成功)

## Scope And Decisions

- Keep the existing left-click `1` and right-click `10` root requests for every
  visible monster, including `space_ship`; add no confirmation or special
  disable path.
- Replace the false all-or-nothing batch model with sequential official calls.
  Stop the current batch on its first failed call or failed postcondition, but
  retain all entities the official owner has already added.
- Remove monster/animal rollback and the shared per-save batch-spawn circuit.
  A later explicit player request remains independently executable.
- Compare manager object sets around each call. Validate ordinary monsters as
  one root and `space_ship` as exactly one root plus two
  `space_ship_bastion` entities with the current Host, valid Controllers and no
  unknown additions.
- Keep the public `SpawnDebugResult` ABI unchanged: `SpawnedCount` is the
  number of roots whose requested postconditions passed. Carry actual manager
  additions through a product-internal outcome and expose requested roots,
  successful roots and actual additions in the status line and mutation log.
- Apply the same visible partial-success/no-circuit rule to animal batches;
  do not retain a shared failure switch after removing monster rollback.

## Changed Files

- `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Core.cs`
- `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Spawn.cs`
- `products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Advanced.cs`
- `products/first-party/DebugConsole/src/Ui/DebugConsoleCatalogModels.cs`
- `products/first-party/DebugConsole/src/Ui/IDebugConsoleActions.cs`
- `products/first-party/DebugConsole/src/Ui/DebugConsoleUi.Actions.cs`
- All nine DebugConsole localization files, product README, Author/publish
  metadata and the generated-candidate projections
- Compatibility/QA adapters and the AdvancedDebug fixture that consume the
  unchanged public result while observing the product-internal addition count
- `tests/DTMAPI.UnitTests/DebugConsoleSpawnProductTests.cs`, focused compile
  links/assertions and the native-trace gate
- The owning Manual QA Review, superseded Planning/current publish facts,
  public API notes, the active smoke matrix and its size-bound history router,
  this Update and the August monthly ledger

## Validation

- Focused `DTMAPI_UNIT_TEST_FOCUS=debugconsole-product`: passed. The physical
  fake official Host covers Guardian `1 -> 3`, `10 -> 30`, a fourth call that
  adds an entity and then throws, retained partial results, and a fresh request
  after that failure. The build retains the separately recorded `16`
  DebugConsole nullable warnings and has zero errors.
- QA Release build and QA Unit passed with zero warnings/errors. The current
  native trace passed and explicitly rejects restoration of spawn
  `RemoveMonster`, rollback or circuit tokens. Runtime-floor compatibility
  passed with game-loaded assemblies remaining `netstandard2.0`.
- Product Catalog passed (`27` products, `11` public, `22` Workshop items,
  `48` API rows). Document governance passed `7,324` checks after routing one
  superseded 2026-08-11 smoke row out of the size-bounded active matrix.
  Fourteen product/release JSON files parse, the stale current-fact scan is
  clean and `git diff --check` reports no whitespace error.
- Final Author SDK validate/build/pack passed against the exact tracked
  `24456188` fixture: validate file count `51`, source file count `25`,
  source-tree SHA-256
  `D271D0EE6CE056AF9241EA2B9FCEC38FCA654648D362F4279A866A73BD271A0C`,
  entry DLL `221,184` bytes / SHA-256
  `2D7DA1DBF72512C359E2E7DED362F98F90E14BA169D4448671330682D32CA38E`,
  and deterministic `26`-file package SHA-256
  `9BD471CE84BEA7D0BBE3990E99206AED7B8A79766C1972434126D7227E8F2C3C`.
  The final metadata-only rebuild kept the exact entry bytes used by runtime.
- Locked `NoNativeSave` `GAME-SMOKE/20260830-231652` passed from the disposable
  fixture's own official Local `MODS` root. It loaded the candidate as
  ProductNative, exercised Guardian `1+10`, proved archives and committed
  sidecars unchanged before cleanup, created no routine byte backup or player
  archive writeback, cleaned the fixture/profile/QA state, exited the process
  and released the shared Runtime lock.
- No Steam subscription, upload folder or public `1.1.1` artifact mutation is
  authorized.

## Evidence

- Pre-implementation review confirms the current failure is caused by the
  product requiring `manager delta == returned root count` after the official
  `space_ship` decorator synchronously creates two bastions.
- The existing rollback records only returned roots and cannot restore the
  official composite closure; the shared circuit then blocks later monster and
  animal requests. The linked Review records why those mechanisms are removed
  instead of treated as a valid transaction boundary.
- `GAME-SMOKE/20260830-231652` records the exact final action semantics:
  `space_ship requestedRoots=1 succeededRoots=1 addedEntities=3` and then
  `requestedRoots=10 succeededRoots=10 addedEntities=30`; the action owner is
  `ProductNative` and Compatibility remains dormant.
- Earlier runs are routing/safety evidence, not acceptance: live
  `20260830-225132` found unrelated MoreEquipment orphan-recovery sidecar
  mutation; `230032`, `230334`, `230615` and `231144` selected Compatibility or
  the immutable public `1.1.1` because the candidate was absent from the
  effective isolated official root. The temporary local-package comparison in
  `231144` restored the original `1.1.1` tree byte-exactly. The final run fixed
  only the fixture routing and did not mutate the player's official packages.

## Rollback Notes

- Revert the product source, localization and focused tests from this Update as
  one unit. Do not restore the Review's superseded recommendation as a current
  fact; the user's no-confirmation/no-circuit product decision remains durable.
- Reverting this candidate does not authorize mutation of the immutable public
  `1.1.1` Workshop artifact, subscription tree, local official upload folder or
  player saves.

## Follow-Up

- A future normal-save Boss persistence run is optional new evidence, not a
  missing condition of the user-authorized no-save comparison. Do not infer an
  upload or public release from this verified source candidate.
- Physical retirement of the unused `0.3.1` Compatibility/API route and the
  remaining `16` nullable warnings stay separate technical-debt tasks.
