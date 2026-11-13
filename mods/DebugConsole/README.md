# Y 键控制台

在存档内按 Y 打开控制台。当前源码版本为 `1.1.10`，需要已安装的 DTMAPI Runtime `0.7.5`。新构建使用新的 Mod 版本号，与本机旧的 `1.1.9` 文件区分。Mod 和 Runtime 各有自己的版本号。

## 已发布的 1.1.3：升级 Runtime

已发布的 Y `1.1.3` 配合 Runtime `0.7.0` 使用。退出游戏，等 Steam 下载完成，再运行 Runtime 的 `1_install_dtmapi.bat` 和 `3_check_dtmapi_status.bat`，确认安装版本后从 Steam 启动。重新订阅只更新工坊缓存，还需要运行安装器。使用 Runtime `0.7.5` 时，安装配套的新 Y `1.1.10` 包。

当前 `1.1.10` 源码包含本体与模组来源筛选、控制台输入、批量给予和召唤、房间作物催熟，以及选中物品堆删除。它是普通的 `netstandard2.0` CodeMod，使用自己的界面模型，不再依赖已退役的诊断接口。

## Candidate build

Build with ordinary .NET 8 and the current API/native references, or pass the explicit `DTMAPI.DebugConsole.csproj` to `build/build-mod-project.ps1`. A source directory or explicit `-BuildOutput` only organizes existing files without evaluating a project. The optional SDK's `pack --build-output <finished-package-directory>` preserves the finished package layout and does not compile. This candidate builds against the current `0.7.5` API and declares minimum Runtime `0.7.5`. Native references stay outside the package. Native tables remain under `Content/`, while the manifest and entry retain their declared `Content/DTMAPI/` paths. Updating an earlier candidate with misplaced tables requires `deploy --clean`; ordinary overlay retains removed or renamed files and neither mode backs up the old installation.

## Current candidate behavior

The candidate consumes the optional [modal session interface](../../docs/authors/UI-MODAL-SESSIONS.md). Core owns occupancy identity; this product still owns Canvas, focused text protection, native input Hooks and Escape drain. Closing Y releases only the window, input gates and modal claim. God mode and the current movement/time multipliers continue until title, another load (including the same slot), or owner withdrawal. God mode fills health, energy and spirit to their current maxima; disabling it resumes native consumption without restoring old values. The former creative mode and its cost/crafting Hooks are absent from this product; the old Compatibility UI and executor have been removed.

Set the custom time multiplier in the DTMAPI configuration page: integer 1–20, default/reset 4. Saving config leaves current speed unchanged. Y's 1×, 4× and Custom time buttons apply the requested value. Native pause and later native time writes retain control.

The same configuration page sets right-click quantity for items, monsters and animals: integer 1–100, default/reset 10. Every right-click uses the latest saved quantity; left-click remains 1. Saving settings does not give or spawn anything. Capacity or native generation failures stop the batch and retain additions, with requested and actual counts shown. Old City Guardian quantities count main groups; its native attachments are counted separately. The current candidate also includes the crop and deletion behavior below; 100 is a quantity limit, not a full-scene performance guarantee.

- Press `Y` after `SaveLoaded` to open or close the console.
- Press `Escape` while the console is open to close it.
- The item browser includes ten native item categories plus searchable Monster
  and Animal catalogs. The source column is stable across all categories and
  its counts combine item rows, monster cards and animal state cards. Animals
  come from the merged runtime `TbAnimal` table, so loaded DTMAPI animal packs
  participate without a species allowlist. Infinite Fuel is an ordinary
  coal-shaped fuel item; Resource spawning remains hidden.
- World tools include time/weather-period actions, movement speed, fixed
  weather controls and exact teleport targets. The obsolete current-location
  label and manual-address CSV exporter are not part of the current UI.
- On Doloc Town 1.00, weather buttons delegate to the exact official current-room command and verify `LocalWeatherType`; `+tech` adds 100 once to NATURE, OPERATE, SCIENCE, and ANIMAL.
- The product is an ordinary CodeMod; the SDK and generator proofs are optional. It owns its UI, typed
  input, fixed native-action allowlist and save-session movement/time-scale/god state.
- It exposes no arbitrary command parser and consumes no frozen DebugConsole or GameBridge Diagnostic API.
- Formal installs package it as official-local
  `MODS/DTMAPI_YKeyConsole/Content/DTMAPI`. The retired game-root `Mods` lane
  is not an ordinary player install path. It must not be installed under
  `BepInEx/plugins`.

The current 1.1.10 source includes the saved crop maturity radius introduced in 1.1.7 (5–100 cells,
default/reset 25). A click captures the current room and player cell. Any covered
cell inside the circle includes the whole basin; only absent coverage falls back
to its anchor. Buildings and other rooms are excluded, and there is no count cap.
Ordinary crops, fruit/timber trees and every existing forage-grass plant use their
own native growth method. Empty/dead plants stay unchanged; no produce is harvested.
The result separates basins, grass basins, new crops/trees and new grass plants,
already mature, empty/dead, invalid, failed and unexecuted targets. A failed native
notification may overlap a confirmed new maturity; that change is retained and
never replayed. Configuration saves affect the next click only. Gameplay follows
the normal native save/unsaved-exit policy.

The current 1.1.10 candidate provides **Delete stack** for the currently selected
backpack quick slot. Confirmation shows the native item name and entire count;
filled containers explicitly include all contents. Cancelling keeps the Y window
and session modes open. A changed selection, item, contents or session requires
new confirmation. Equipment/drone slots are excluded; native non-disposable and
non-salable items are allowed without changing their flags.

Confirmation invokes one native whole-slot removal, without clearing container
inventories first. A notification failure after removal is reported separately;
uncertain results never trigger a second deletion or automatic replacement.
There is no automatic save or recovery sidecar. Unsaved exit/reload follows the
normal game behavior.

The implementation slices have limited acceptance. The 2026-10-26 paired candidate passed real Y/Escape input, repeated and held keys, catalog access, exact patch/Owner cleanup and UI/session invalidation. A normal money operation passed failed-save/cold-old-value and successful-save/cold-new-value cases in an isolated fixture. This does not claim public publication.

## 物品与来源

打开物品页后，默认查看“全部”。物品列表只读取游戏当前的 `TbItem`，名称、分类、堆叠、图标和可给予状态来自当前生效的原生行。搜索、分类、分页和给予物品沿用这些值；不会再补入只在文件中声明、尚未进入原生表的物品。

来源只分“本体”和“模组”：游戏本体配置里有的算本体，其余算模组。能确认来自哪个 Mod 的，再列到该 Mod 下；不能确认具体包的，仍计入模组总数。不设“未知”筛选项。物品 ID 保留原始大小写，仍使用原生物品对象放入背包。

动物包的来源使用已加载包的 `PackData/content.json` 及包标识；多个包声明同一物种时，只归入模组总数，不猜测某一个包。整个来源数据读不出来时，只显示“全部”，并报告读取失败的原因。

## Retained release and development history

The `1.1.3` release preserves item ID casing during native generation and
uses the object overload for backpack placement. It retains partial
capacity results and checks failed generation before placement.

The historical `1.1.2` implementation recognizes the official Old City Guardian
(`space_ship`) as one root plus two synchronously created
`space_ship_bastion` entities. Its monster and animal batches used fixed
left-click `1` / right-click `10` controls, sequential official calls, stopped
at the first failure, retained every entity already added and showed requested
roots, successful roots and actual additions. The current candidate retains
this root/attachment accounting with the configurable quantity described above.
There is no spawn rollback, shared circuit, Guardian-specific disable path or
extra confirmation. The previously published `1.1.1` artifact predates this fix.
