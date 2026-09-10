# 20260823-0005：配置 Mod 列表翻页状态与布局修正

## Metadata

- Update ID: `20260823-0005`
- Date: `2026-08-23`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `mitigated`
- Source: 玩家反馈 DTMAPI `0.6.1` 在十六个配置页时显示 `第 1/2 页`，但点击 `>` 无法看到第二页；用户要求重新研究并调整翻页，询问是否由分辨率不同导致。

## Intended Boundary

- 修复配置 Mod 列表在显式翻页后被当前选中项立即拉回第一页的状态回弹。
- 保留外部打开指定配置页、新会话进入 Config 时的选中项跟随。
- 把配置列表分页移到左栏标题下的固定导航行并扩大前后页按钮，消除与右栏标题的重叠。
- 增加覆盖“翻页后普通重绘保持目标页”和“显式请求仍跟随选中项”的测试。

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
  - distinguishes a new Config request/session from an ordinary dirty render;
    selected-Mod following is now used only for the former, while an explicit
    pager click survives the latter;
  - clamps stale page indexes without pulling a manually selected list page
    back to the currently selected Mod;
  - moves the Config-list pager to a dedicated left-column row, keeps its right
    edge left of the detail pane at `x=326`, and enlarges its two buttons from
    `30x22` to `60x30` logical units.
- `tests/DTMAPI.UnitTests/Program.cs`
  - adds the focused `title-config-pager` state/layout regression covering
    page-two persistence, new-session/request following, stale-index clamping,
    non-overlap and enlarged targets.
- `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/DolocTownGameBridge.G4Fixtures.cs`
  - extends the real title Config smoke to click
    `DTMAPI.Config.ListPager.Next`, verify the page-two label, click its first
    Mod row, verify the matching detail title, capture page two, click Previous
    and verify page one;
  - when the installed set has at most fourteen config pages, registers enough
    QA-only owner-bound pages to reach sixteen, then deactivates every injected
    owner and proves zero remaining pages.
- `src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/QaScenarioController.cs` and
  `tests/DTMAPI.QaUnitTests/Program.cs`
  - add retry-safe QA close cleanup and source-boundary coverage for the
    second-page interaction fixture.
- `docs/reviews/manual-qa/2026/20260823-0004-config-mod-list-pager-snapback.md`,
  `docs/debug/issues/ISSUE-027-20260823-config-mod-list-pager-snapback.md`,
  `docs/debug/issues/README.md`,
  `docs/debug/regressions/smoke-matrix.md`, this Update and the August ledger
  preserve the diagnosis, implementation and runtime evidence.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests`: all projects,
  including Bootstrap and staged QA, compiled with zero errors. Existing
  DebugConsole nullable warnings remain unrelated to this correction.
- Repository-local `.tools/dotnet/dotnet.exe` with
  `DTMAPI_UNIT_TEST_FOCUS=title-config-pager`:
  `DTMAPI.UnitTests: OK (title-config-pager)`; the managed test session cleaned
  itself.
- Repository-local `.tools/dotnet/dotnet.exe` against the Release QA test DLL:
  `DTMAPI.QaUnitTests: OK`; the managed test session cleaned itself.
- `git diff --check`: passed; only the repository's existing LF/CRLF conversion
  warnings were reported.
- `tools/scripts/check-doc-governance.ps1`: passed `6969` governance, metadata,
  ledger and Markdown-link checks.
- A broader `tools/scripts/test.ps1 -Configuration Release` attempt compiled
  successfully and passed the focused Unit, QA and InstallDoctor tests, then
  stopped at the unrelated existing Workshop staging authority mismatch:
  the current route expects nine ProductNative Updates but finds ten.
- A prior full `tools/scripts/build.ps1 -Configuration Release` attempt also
  compiled the affected assemblies, then stopped at the unrelated existing
  frozen Catalog/source-projection assertion in
  `PreviewVersionMetadataIsConsistent`. Its cleanup-pending managed test
  session was removed through `cleanup-test-artifacts.ps1` after preview.
- `GAME-SMOKE/20260824-001355` passed the title-only `SaveSlot=0` route and
  produced a clean single-page layout screenshot, but the installed set had
  only ten config pages; it is baseline evidence, not pager acceptance.
- `GAME-SMOKE/20260824-002007` first closed the direct Next/Previous route, but
  did not click a page-two Mod row; it remains a valid precursor, not the final
  acceptance run.
- `GAME-SMOKE/20260824-002647` intentionally tightened that route by clicking
  the first page-two row. The production UI created a new Config request
  session as designed, while the new QA step still held the previous session
  receipt and failed closed before screenshot. This was a QA ownership-receipt
  defect, not a production pager failure. The retained stage's three files
  matched their recorded length/SHA-256 exactly and were removed only after
  proving `DolocTown.exe` absent; its empty run directories were then removed.
  The failed evidence directory remains durable and is not acceptance.
- `GAME-SMOKE/20260824-003058` is the definitive pager acceptance run. It used
  the current installed Runtime plus staged QA without changing official
  packages, subscriptions or any save. The fixture observed ten real config
  pages, injected six QA-only pages, and verified through real Unity controls:
  `1-14/16 第 1/2 页` -> `15-16/16 第 2/2 页` -> select
  `Yuuka.DTMAPI.ManboCardboardAudio` -> matching detail title -> Previous ->
  `1-14/16 第 1/2 页`. The page-two selected-Mod screenshot is `2560x1440`;
  all six QA owners were deactivated with `remaining=0`, QA lifecycle/cleanup
  passed, no fatal window appeared, `DolocTown.exe` exited, staged QA roots
  were absent, and no routine player-save backup or archive writeback occurred.
- The shared Runtime lock was acquired before the runtime-only install/game
  operations and released after the final process-exit check.

## Evidence

- Root-cause review: `docs/reviews/manual-qa/2026/20260823-0004-config-mod-list-pager-snapback.md`.
- Debug owner: `docs/debug/issues/ISSUE-027-20260823-config-mod-list-pager-snapback.md`.
- Player screenshots supplied in the source conversation; textual facts are preserved in the Review.
- Baseline title-layout run:
  [`GAME-SMOKE/20260824-001355`](../../debug/evidence/GAME-SMOKE/20260824-001355/).
- Rejected tightened-run diagnostic:
  [`GAME-SMOKE/20260824-002647`](../../debug/evidence/GAME-SMOKE/20260824-002647/).
- Definitive two-page selection interaction run:
  [`GAME-SMOKE/20260824-003058`](../../debug/evidence/GAME-SMOKE/20260824-003058/).
- Page-two screenshot:
  [`title-settings.png`](../../debug/evidence/GAME-SMOKE/20260824-003058/qa-host/g4/ui/title-settings.png).

## Rollback Notes

- Revert the Config-list selection-follow policy, Config-list-only pager layout,
  tests and these lifecycle records together.
- Do not roll back shared `ManagerPagination` clamping or other Manager/item
  pagers; they are not the cause of this issue.

## Follow-Up

- The deterministic state failure and design-coordinate overlap are corrected
  and covered by an actual sixteen-page next/previous interaction. ISSUE-027
  remains `mitigated`, not `verified`, until a player or bounded QA run confirms
  the enlarged controls at a lower display resolution comparable to the
  feedback environment.
- No Runtime Workshop upload, subscription mutation or version bump is
  authorized by this Update. Publication remains a separate user-authorized
  release task.
