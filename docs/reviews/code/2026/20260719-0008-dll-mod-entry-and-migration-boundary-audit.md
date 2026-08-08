# DLL Mod Entry And Migration Boundary Audit

Status: recorded

Date: 2026-07-19

## Source request

以 Steam 创意工坊订阅中的 DTMAPI `0.5.2-alpha` 为历史基准：

1. 查找作者“姓王且名建林”的其他 DLL mod，确认其包结构和入口方式；
2. 检查 `E:\Python_project\SMAPIlearning` 中实际使用 SMAPI 的 mod，判断 SMAPI 是否同样要求专用入口、是否同样复杂；
3. 以“泰拉瑞亚物品包”为样本，说明如何 DTMAPI 化、实际工作量和代码量变化；
4. 再比较当前 DTMAPI `0.5.5` 中第一方/测试 mod 的写法。

用户明确要求不启动游戏；允许在有合法源文件的前提下保留第三方 mod 修改，但本次订阅样本没有附带源代码或授权文件。

## Scope and safety boundary

- 本次只做文件清单、manifest、程序集引用、类型/成员元数据和仓库源代码的静态审计。
- 未启动 Doloc Town，未获取共享 Runtime lock，未修改 Steam 订阅目录、游戏目录、玩家启停状态或任何第三方 DLL。
- 未反编译、复制或复述第三方方法体；第三方包仅作为兼容性样本。
- 未找到泰拉包或三份王姓作者包的源代码/许可证，因此没有制造“修改版”或可再分发派生包。
- 本记录提出迁移边界和工作量区间，不把 UI 成功、程序集可载入或 Harmony 类型存在冒充为稳定 API。后续若走完整 API 化，必须先审查每个功能的 native owner 方法体/状态持有者；未找到 native owner 前，不得通过 mod 层补丁冒充 API 重做完成。

## Executive conclusion

“继承 DTMAPI/SMAPI 的 mod 入口”本身并不复杂，通常只是一个入口类和一个 manifest。真正决定迁移工作量的是框架承诺的管理深度：

- DTMAPI `0.5.2` 可以发现包、读取启停状态、检查版本/依赖、构造 helper 并调用一个 `DtmMod.Entry`；它不自动接管第三方自己创建的 Harmony patch，也不能在程序集已加载后把任意旧插件安全卸载。
- SMAPI 同样要求 DLL 中存在专用 `Mod` 入口，而且本地源码实现要求入口程序集恰好有一个具体 `Mod` 子类。SMAPI 的体感更简单，主要来自成熟模板、构建打包、同目录依赖 DLL 解析、Content Pack 和 API registry，而不是免除了入口改造。
- 泰拉包当前并没有被 DTMAPI 管理其真实功能。DTMAPI 只加载一个 5.5 KiB 安装器；安装器把 25.5 KiB 的 `BaseUnityPlugin` 复制到 `BepInEx/plugins` 并要求重启。真正功能绕开了 DTMAPI 的启停、所有权和诊断边界。
- 如果泰拉作者提供源代码，按 `0.5.2` 能力做“薄 DTMAPI 化”通常是小改造：删除复制安装器，把真实功能程序集改成/加入一个 `DtmMod` 入口，manifest 直接指向它。若坚持当前 `0.5.5` Author SDK 的严格 API-only 规则，则不是小改造：泰拉的多组游戏/Harmony 功能缺少现成稳定 API，需要先在 DTMAPI 中补 native-owner/GameBridge/API，主要代码会增加在框架侧。
- 当前 `0.5.5` 的加载器保留了旧式 `DtmMod` 的兼容入口，但新的 Author SDK 比 `0.5.2` 运行时和 SMAPI 都严格。应区分“Runtime 还能加载什么”和“SDK 允许新作者生产什么”。

## Authority snapshots

### Published DTMAPI 0.5.2 baseline

- Subscription root: `D:\Steam\steamapps\workshop\content\2285550\3743016467`
- `info.json`: `0.5.2-alpha`
- `Content/DTMAPI/release-manifest.json`:
  - release version: `0.5.2-alpha`
  - binary version: `0.5.2.0`
  - build commit: `8caf8403b45c`
  - five runtime assemblies
- `git show 8caf8403b45c` was used as the exact matching source authority for loader behavior.

### Current 0.5.5 authority

`tools/release/dtmapi-runtime-version.props` currently declares:

- release version: `0.5.5`
- binary version: `0.5.5.0`
- assembly compatibility version: `0.5.3.0`

The current Author SDK and current first-party/test projects were audited from the working tree. Because the working tree contains unrelated in-progress Batch 5 changes, this review does not claim a clean commit snapshot and does not alter those source files.

## 1. “姓王且名建林” DLL mod structure

The exact Workshop author string is `姓王且名建林`. Three installed subscriptions match; only two are DTMAPI packages that the published `0.5.2` loader can actually enter.

| Workshop item | Mod | Package/entry | Static result under 0.5.2 |
| --- | --- | --- | --- |
| `3743644065` | 商店购物数量限制翻倍 | root `DolocStoreCapacityMod.dll`; `Content/DTMAPI/manifest.json` | Valid legacy DTMAPI CodeMod |
| `3743621104` | 纸箱与货架容量翻倍 | root `DolocStorageExpansionMod.dll`; `Content/DTMAPI/manifest.json` | Valid legacy DTMAPI CodeMod |
| `3742618545` | 开发者模式 | `Content/DolocSMAPI/DolocDevModeMod.dll`; same-folder manifest | Not discoverable/enterable by DTMAPI 0.5.2 |

### 1.1 商店购物数量限制翻倍

- Manifest identity: `com.user.dolocstorecapacity`, version `1.0.1`, legacy `MinimumApiVersion: 0.5.1`.
- No `Type`, `EntryType` or usable custom entry selector is declared.
- DLL: 5,632 bytes; SHA-256 `BC3511AA5ECF33CBF005C9F314C9625FBA6577EA10270BD77C89F71D6C2D3BBF`.
- Assembly metadata: assembly version `1.0.0.0`, .NET Standard 2.1, reference to `DTMAPI.Abstractions 0.5.1.0`.
- Type metadata contains one `DtmMod` entry, a config type and one Harmony patch type. It references DTMAPI helper/config/logging plus Harmony and the game store type.

Analysis: this is not a second hidden BepInEx plugin. Its DTMAPI integration is thin: one `DtmMod` entry starts the mod's own functional Harmony code. Because the entry assembly contains exactly one usable `DtmMod` subclass, `0.5.2` can select it without `EntryType`.

### 1.2 纸箱与货架容量翻倍

- Manifest identity: `com.user.dolocstorageexpansion`, version `1.0.10`, legacy `MinimumApiVersion: 0.5.1`.
- No `Type` or `EntryType` is declared.
- DLL: 10,752 bytes; SHA-256 `45B5522948770C04575E57C58CCC5F87AAD3091B680873FAB283F3C89372B366`.
- Assembly metadata: assembly version `1.0.0.0`, .NET Standard 2.1, reference to `DTMAPI.Abstractions 0.5.1.0`.
- Type metadata contains one `DtmMod` entry and about ten storage/UI Harmony patch types, with direct Harmony, `Assembly-CSharp` and Unity UI/game references.

Analysis: this is a larger version of the same pattern. “一个 CodeMod DLL”并不等于“只能写一个类”或“所有功能都必须塞进 Entry 方法”；一个 DLL 可以包含入口、配置、服务和很多 patch 类。这里的功能代码仍属于 mod，只是由一个很薄的 DTMAPI 入口启动。

### 1.3 开发者模式

- Manifest identity: `com.user.dolocdevmod`, version `1.0.1`, `MinimumApiVersion: 0.8.21`.
- DLL: 10,240 bytes; SHA-256 `B550FC549E9A4B1757B1E511494DDD995E618DBD991EFD9805171161F0A6E4A4`.
- Assembly metadata contains a BepInEx `BaseUnityPlugin`, not a `DtmMod`.
- Its package uses `Content/DolocSMAPI`, which is not one of the `0.5.2` DTMAPI manifest locations.

Analysis: this包看起来属于另一套/更晚的 `DolocSMAPI` 约定。即使手工把 DLL 路径交给 `0.5.2`，程序集里也没有可构造的 `DtmMod` 入口，所以它不能证明 DTMAPI 可以管理普通 BepInEx 插件。

### 1.4 What 0.5.2 actually manages

For a valid `DtmMod` package, `0.5.2` owns:

- discovery from supported local/official/Workshop roots;
- manifest parsing and path confinement;
- cold-start enable/disable admission through official `mod_infos.json` state;
- dependency and minimum-API checks;
- entry assembly load, unique entry selection, helper/log/config setup, and `Entry` invocation.

It does not own:

- a general adapter that can instantiate any `BaseUnityPlugin` as a DTMAPI mod;
- automatic removal of Harmony patches created by the mod itself;
- unloading an already-loaded managed assembly;
- a mature, diagnosed multi-assembly/private-dependency packaging contract comparable to SMAPI's.

Therefore, for these legacy direct-Harmony mods, “DTMAPI 管理启停”应解释为冷启动时决定是否发现/启动，并在 UI/诊断中管理包；若已经加载后再关闭，通常必须重启，不能承诺即时卸载。

## 2. SMAPI comparison from local source and installed mods

### 2.1 Entry model

Local SMAPI source authority:

- `E:/Python_project/SMAPIlearning/SMAPI/src/SMAPI/Mod.cs` defines abstract `Mod` and `Entry(IModHelper)`.
- `E:/Python_project/SMAPIlearning/SMAPI/src/SMAPI/Framework/SCore.cs` scans the entry assembly and rejects zero or multiple concrete `Mod` subclasses.

Analysis: SMAPI 不是“把任意 DLL 扔进去就能管理”。普通 C# mod 同样必须有 SMAPI `Mod` 入口；就入口选择而言，本地 SMAPI 源码甚至比 DTMAPI `0.5.2` 更固定，因为它要求入口程序集恰好一个 `Mod` 子类，而 `0.5.2` 在多个 `DtmMod` 时还允许 manifest 指定 `EntryType`。

### 2.2 Installed sample inventory

`E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\Mods` contains 20 manifests:

- 18 code mods;
- 2 `ContentPackFor` content packs;
- all 18 audited code packages contain one mod DLL in the local installed sample;
- the two content packs contain no DLL.

Representative source/package sizes:

| Sample | Shape | Observation |
| --- | --- | --- |
| SaveBackup | 1 C# file, about 184 physical LOC | One thin SMAPI entry can own a small complete mod |
| ConsoleCommands | 33 C# files, about 2,505 physical LOC | A large mod still exposes one entry; internal code remains split normally |
| YetAnotherFishingMod | one approximately 87 KiB DLL, optional GMCM dependency | Gets the GMCM API through SMAPI's registry; no separate GMCM adapter DLL is required |
| Canon dialogue content pack | 0 DLL, 44 files | Data-only integration through `ContentPackFor` |
| Seasonal Cute Characters | 0 DLL, 1,013 files (969 PNG, 44 JSON) | A large asset/data package does not need a code entry |

Analysis: package里“一个入口 DLL”不是 source architecture 限制。ConsoleCommands demonstrates that many source files/classes build into a single managed assembly while keeping one framework entry. Content packs avoid code only when the framework/game already supplies the necessary data contract.

### 2.3 Why SMAPI feels less complex

The local SMAPI implementation and technical package documentation provide mature support for:

- same-directory referenced assembly resolution, including recursive referenced dependencies;
- explicit build packaging of extra assemblies (`BundleExtraAssemblies`);
- content-pack declarations separate from code mods;
- mod-to-mod API lookup through the registry;
- stable templates, build tasks, manifest validation and author documentation.

An unreferenced orphan DLL still does not execute, and a plain class library with no `Mod` subclass still is not a SMAPI mod. The difference is ecosystem maturity and declared packaging semantics, not the absence of an adapter entry.

### 2.4 Direct game/Harmony code boundary

SMAPI generally permits a mod to retain mod-specific game/Harmony code behind its `Mod` entry. DTMAPI's current Author SDK instead rejects direct BepInEx, Harmony, `Assembly-CSharp` and Unity references for ordinary new mods.

Analysis: this is the largest architectural reason current DTMAPI migration can feel heavier than SMAPI. The strict lane improves stable ownership and isolation, but every new native behavior first needs a reviewed native bridge/capability boundary. Product policy and player-facing feature loops should still remain in the mod; AutoFishing is the current counterexample to any claim that strict authoring requires the product body to live in GameBridge. The risk is specifically that one-off native primitives may accumulate in GameBridge when no advanced compatibility lane exists, not that mod logic should be moved back into the framework.

## 3. Terraria package migration

### 3.1 Current package facts

- Workshop item: `3759797170`, “泰拉瑞亚物品包”, author `mxx`, version `1.0.0`.
- Total package: 156 files, 696,151 bytes; 96 PNG, 58 JSON, 2 DLL.
- Manifest identity: `com.mxx.doloc.itemlimiter.installer`.
- Manifest points to `Content/DTMAPI/Mxx_DolocTownMod_Installer.dll` and uses noncanonical field `Entry` rather than `EntryType`. The field is ignored by `0.5.2`, but the fallback succeeds because the installer has one `DtmMod` subclass.

DLL inventory:

| DLL | Bytes | SHA-256 | Role |
| --- | ---: | --- | --- |
| `Mxx_DolocTownMod_Installer.dll` | 5,632 | `F2E92A2A1310194EFE2E4C05E393B9FA30BEE4941F095CAEEBA5E715E7AADE2D` | Two-type DTMAPI installer; copies the plugin and asks for restart |
| `Mxx_DolocTownMod_Plugins.dll` | 26,112 | `43005B5CE96CCEFC17EB150B765F4200BB9B51BB9EC9C83D7EDA25F00D9D1E76` | `BaseUnityPlugin`; approximately 17 functional/compiler-generated types and 53 methods |

The plugin's type/member metadata indicates several independent behavior families: item-limit/fruit use, power initialization, weather/moondial state, localization, void-bag/storage UI, value-tip/crash guards, and special generator/device handling. This is a metadata-level responsibility inventory, not a method-body reconstruction.

Analysis: 96 PNG and 58 JSON are already the package's official-content side and do not need to become C# merely to migrate the code loader. The boundary defect is the two-stage DLL path: DTMAPI manages only an installer that creates an unmanaged BepInEx plugin installation.

### 3.2 Minimal 0.5.2-style migration, if the author provides source

Target layout:

```text
Workshop package
├─ official JSON/PNG content (unchanged)
└─ Content/DTMAPI/manifest.json
   └─ EntryDll -> the real single code-mod assembly
      ├─ exactly one DtmMod entry
      └─ the existing feature/config/patch classes
```

Required work:

1. Delete the installer/copy-to-`BepInEx/plugins` behavior and the restart instruction associated with installing it.
2. Change the real plugin's lifecycle root from `BaseUnityPlugin.Awake` to one `DtmMod.Entry`, or place a thin `DtmMod` entry in the same final assembly.
3. Route logging/config through `IModHelper` where practical.
4. Point `EntryDll` directly at the final code-mod DLL; use canonical `EntryType` only if the assembly contains more than one `DtmMod` candidate.
5. Package no second executable plugin. Keep feature classes as ordinary classes inside the same assembly.
6. For a new maintained build, target `netstandard2.0` per current DTMAPI Unity Mono toolchain rule. The sampled Terra assemblies identify as .NET Framework 4.7.2; the two Wang samples identify as .NET Standard 2.1 and should be treated as legacy compatibility observations, not current template targets.

Estimated source delta when source is available:

- new/changed DTMAPI entry and manifest wiring: approximately 15–40 lines;
- logger/config/lifecycle adapter replacement: approximately 30–100 lines depending on how deeply BepInEx services are used;
- deletion of the installer/copy path offsets part or all of that increase;
- expected final binary shape is one real code DLL, likely near the existing 26 KiB plugin order of magnitude rather than the current 31,744-byte two-DLL total.

Analysis: the best estimate is “net source roughly unchanged or smaller”, not “必须重写整个 mod”. Exact LOC cannot be recovered honestly because the subscription contains binaries only.

### 3.3 Current strict 0.5.5 Author SDK migration

The current SDK lane enforces:

- `netstandard2.0`;
- only `DTMAPI.Abstractions` as the intended game-loaded project reference;
- one declared CodeMod DLL and no extra DLL payload;
- rejection of BepInEx, HarmonyLib, `Assembly-CSharp` and Unity references/tokens;
- canonical author manifest/template metadata including `EntryType`, `Type: CodeMod`, minimum DTMAPI `0.5.5`, and `dtmapi.author.json`.

Therefore the existing Terra plugin cannot simply be recompiled through the current Author SDK. Its direct native responsibilities must first be expressed as public/native-backed capabilities, for example item-use limits, device power/capacity rules, weather/time state, localization, storage/void-bag behavior and relevant UI/crash-guard ownership.

Rough planning estimates, explicitly not implementation quotations:

- subscription binary metadata plus comparable current first-party mod sizes suggest the original functional source may be on the order of 300–700 physical LOC;
- after suitable stable APIs exist, a strict mod-side implementation might be about 150–350 LOC because native patch details move out of the mod;
- the corresponding DTMAPI production/tests/docs work is plausibly 2,000–5,000 added or substantially revised LOC across several native-owner domains, not because an entry class is large but because each public capability needs owner discovery, GameBridge implementation, lifecycle/cleanup, tests and documentation.

These are order-of-magnitude ranges only. They must not be used as a delivery quote until the author supplies source and each responsibility has a native-owner review.

### 3.4 No-source case

With only the current binaries, there is no safe “small source modification” path. The legitimate choices are:

- obtain source and redistribution/modification permission from the author, then perform the thin migration;
- obtain a documented behavior specification and permission, then do a clean-room rewrite;
- leave it classified as an external BepInEx plugin and let DTMAPI Doctor detect/report it as unmanaged.

This review deliberately does not create a source facsimile from metadata.

## 4. How current 0.5.5 mods are written

### 4.1 Audited repository projects

The current repository contains 18 valid first-party/test code projects in the audited set. All 18:

- target `netstandard2.0`;
- subclass `DtmMod`;
- reference only `DTMAPI.Abstractions` as their DTMAPI project dependency;
- do not directly reference BepInEx, Harmony, Unity or `Assembly-CSharp`;
- build one code-mod DLL;
- currently rely on the runtime's unique-`DtmMod` fallback in their shipped manifests, although the newer Author SDK template requires explicit `EntryType`.

Representative physical/non-empty source counts:

| Mod | Physical LOC | Non-empty LOC | Main boundary |
| --- | ---: | ---: | --- |
| Hello | 13 | 12 | Minimal `DtmMod` entry |
| Zoom | 221 | 194 | Config/input/events plus `ICameraView` lease |
| AutoFishing | 512 | 465 | First-party/internal fishing primitives |
| ActionSpeed | 216 | 195 | `IActionSpeed` API |
| MoreSaves | 93 | 80 | Save API |
| MoreEquipment | 92 | 81 | Equipment API |
| YConsole | 148 | 130 | Multiple diagnostic APIs |

### 4.1.1 AutoFishing ownership correction from user feedback

User feedback on 2026-07-19: “我不是把 mod 拆出 GameBridge 了么？由 mod 自己代码，比如 AutoFishing？”

Analysis: yes. The first-party cutover is real, and the earlier aggregate interpretation that AutoFishing's small mod body meant its product complexity had been moved into GameBridge was wrong. Current source ownership is:

| Current area | Physical LOC | Actual responsibility |
| --- | ---: | --- |
| `first-party-mods/AutoFishingMod` | 512 | Complete player product loop: config/hotkey, enable state, recast timing, movement cancel policy, state machine/decision engine, InstantBite/SkipMinigame choices, visible-minigame input decisions, and input/animation lease requests |
| `GameBridge/Features/FishingAutomation` | 5,390 | Native bridge: Harmony callback routing, game-state/native accessors and caches, sequence-checked cast/bite/reel transactions, native input/animation application, session ownership, demand and cleanup |
| `GameBridge/Compatibility/FishingAutomation` | 3,003 | Frozen legacy `IFishingAutomationApi` compatibility path, dominated by the 2,899-line `LegacyFishingAutomationService`; ordinary first-party AutoFishing does not construct this executor |

The previously quoted `8,393` GameBridge lines were the sum of the 5,390-line native feature area and 3,003-line legacy compatibility area. That number must not be described as the current AutoFishing mod implementation or as evidence that the product was not split.

Git history nevertheless confirms a separate and important weight problem. Comparing the parent of the first-party cutover with the cutover commit, the latest committed boundary, and the current dirty Batch 5 worktree gives:

| Snapshot | Product mod | First-party/native GameBridge | Legacy compatibility | Fishing-domain C# total |
| --- | ---: | ---: | ---: | ---: |
| Pre-cutover `d68c0dbc` | 261 (`testmods`) | 2,897 | 0 | 3,158 |
| Cutover `f9f767dd` | 493 | 5,342 | 2,997 | 8,832 |
| Latest committed `d01c2ca7` | 493 | 5,050 | 3,003 | 8,546 |
| Current 2026-07-19 worktree | 512 | 5,390 | 3,003 | 8,905 |

The current worktree row includes in-progress Batch 5 modified/untracked fishing files and is not a clean release snapshot. Even with that qualification, two conclusions are durable:

1. For the ordinary first-party execution path, the source area changed from about `3,158` lines before cutover to about `5,902` lines now (`512 + 5,390`). The ownership boundary became cleaner, but the active implementation did not become lighter.
2. For repository maintenance, the old implementation was not removed. It was retained as the roughly 3,003-line compatibility path while the new first-party path was added, producing about 8,905 lines of current fishing-domain C#—roughly 2.8 times the pre-cutover footprint.

Therefore the accurate project-level assessment is: the split succeeded as an ownership cutover but did not succeed as a lightweight refactor. Compatibility retention explains about 3,003 lines; it does not by itself explain why the new first-party/native path is also materially larger than the former complete path. A separate root-cause audit must classify the 5,390 lines into irreducible native adaptation, lifecycle/ownership safeguards, diagnostics/QA, and duplicated or over-specialized policy before deciding what can be removed.

The precise boundary is:

```text
AutoFishingMod (product behavior and decisions)
  -> internal IFirstPartyFishingPrimitivesApi (scalar/session transaction seam)
    -> DolocTown GameBridge (fragile native state, Harmony and lifecycle adaptation)
```

This conforms to the intended architecture. The remaining “not completely clean” aspects are narrower: Fishing Primitives are first-party-internal rather than a public reusable API; native files retain the historical `FishingAutomation` folder name; and the frozen legacy compatibility implementation still exists for binary compatibility. None of those means the AutoFishing product loop remains in GameBridge.

More generally, current first-party mod source is clean and usually small because fragile native access is behind APIs/internal capabilities, while product policy remains in the mod. That valid managed split still does not prove an arbitrary existing Harmony/BepInEx mod can be converted cheaply when its native capability boundary does not exist.

### 4.2 Runtime compatibility versus SDK policy

The current `0.5.5` runtime still retains the compiled `DtmMod` entry-selection model for legacy packages and adds stronger admission/transaction, owner cleanup/restart-required state, native subscription authority and Doctor diagnostics. It does not retroactively transform `BaseUnityPlugin` into `DtmMod`.

The current Author SDK is intentionally stricter than the loader. Therefore:

- a legacy Wang-style direct-Harmony `DtmMod` may remain runtime-compatible after normal version/admission checks;
- the same source would fail the current SDK's strict new-author policy;
- “运行时能加载” must not be documented as “推荐/受完全管理的 0.5.5 写法”.

### 4.3 SMAPI and Yet Another Fishing Mod comparison: corrected split objective

User clarification on 2026-07-19: the purpose of splitting AutoFishing was to reduce DTMAPI Runtime source and maintenance weight, not merely to move product decisions into a separate assembly while retaining or expanding a fishing-specific GameBridge implementation.

The matching Stardew sample is `Yet Another Fishing Mod 1.2.0`:

- local package: `E:\Python_project\SMAPIlearning\StardewValley_SMAPI_reference\game-root\Mods\【快速钓鱼】YetAnotherFishingMod`;
- local DLL: 87,040 bytes, SHA-256 `18FCC296C2C4CF5757C417C01C8836661AB9DAD18CFA34179D9CA5219CA4406D`;
- published source: [Dunc4nNT/StardewMods/YetAnotherFishingMod](https://github.com/Dunc4nNT/StardewMods/tree/yetanotherfishingmod-v1.2.0/YetAnotherFishingMod), tag `yetanotherfishingmod-v1.2.0` at `e23b8d72ea1d5551cf770733801d712dec28e83f`;
- license: MPL-2.0;
- project enables Harmony and targets `net6.0`; the built assembly directly references `Stardew Valley`, `0Harmony`, SMAPI and MonoGame.

Tagged author-source inventory:

| File/group | Physical LOC | Responsibility |
| --- | ---: | --- |
| `ModEntry.cs` | 119 | SMAPI entry, events, config, input, GMCM registration root |
| `FishHelper.cs` | 250 | Auto cast, minigame skip, instant bite, animation, loot and product behavior |
| `Patches.cs` | 417 | Five direct Harmony patches/transpilers/prefixes against Stardew fishing/game methods |
| `SFishingRod.cs` | 117 | Direct `FishingRod` wrapper, auto hook, bait/tackle/enchantment/native field mutation |
| `Enums.cs` | 20 | Product enums |
| `GenericModConfigMenu.cs` | 450 | Mod-owned GMCM option registration |
| `ModConfig.cs` | 101 | 43 product/config fields |
| Total | 1,474 | Complete mod-owned source in the tagged product directory |

The five direct patch targets are `FishingRod.tickUpdate`, `BobberBar.update`, private `FishingRod.doDoneFishing`, `GameLocation.GetFishFromLocationData`, and `FishingRod.pullFishFromWater`. The mod also directly reads/writes Stardew objects and fields and calls native game actions such as the use-tool button, fishing-rod function, bobber-bar update and bite timers.

SMAPI owns none of those fishing behaviors. A source scan finds zero SMAPI source files referencing `NeverToxic` or `YetAnotherFishingMod`. SMAPI provides only generic facilities used by the mod: one `Mod.Entry`, events, logging, config IO, reflection helper, mod registry/GMCM lookup, assembly/dependency loading and Harmony compatibility/detection. Its three `Fishing*Facade` files total 126 physical lines and are generic Stardew 1.5.6/1.6 API-rewrite compatibility for the whole ecosystem, not automatic-fishing code and not specific to this mod.

This produces the key architectural contrast:

```text
SMAPI
  generic loader/events/config/reflection/mod API support
YetAnotherFishingMod
  product logic + direct game access + Harmony patches + GMCM page

DTMAPI 0.5.5 current
  generic loader/events/config support
  + 5,390-line first-party fishing-native GameBridge
  + 3,003-line legacy fishing compatibility GameBridge
AutoFishingMod
  512-line product policy/loop
```

The Stardew mod exposes more player options than current DTMAPI AutoFishing yet adds zero feature-specific code to SMAPI itself. This is not a proof that its implementation is safer: direct transpilers/field writes carry game-version and conflict risk, the author explicitly warns about same-feature conflicts, and the mod does not provide DTMAPI-style hot cleanup/session diagnostics. It does prove that those risks can remain owned by the functional mod instead of becoming framework source.

Under the user's clarified lightweight objective, the current DTMAPI split is directionally wrong even though its ownership seam is internally coherent. DTMAPI should not build a first-party-only 5,390-line native capability merely to keep a functional mod free of Harmony/game references. The closer SMAPI-style target is:

```text
DTMAPI Runtime/Core
  discovery, cold-start enablement, dependency/version checks,
  events/config/log/input, generic patch-owner and restart-required diagnostics

AutoFishingMod
  DtmMod entry + product loop + its own DolocTown/Harmony/native implementation
  + its own cleanup/owner rules

GameBridge
  only stable or genuinely shared cross-mod capabilities,
  not first-party-only automatic-fishing implementation
```

This creates a deliberate policy consequence: the current strict Author SDK rule that forbids Harmony, Unity and game references cannot be the only code-mod lane if reducing Runtime weight is the primary goal. A managed native/advanced lane is required. DTMAPI can still control package discovery and cold-start enablement; disabling an already-loaded direct-patch mod should be explicitly restart-required unless that mod supplies and proves its own cleanup.

The 3,003-line legacy path is a separate compatibility decision. Keeping ABI declarations in Abstractions does not require the complete executor to remain in the always-shipped GameBridge forever; a future breaking release or optional/on-demand compatibility package should be reviewed. No deletion or policy change is implemented by this audit.

### 4.4 Historical origin of the strict boundary and the 8,832-line cutover

The current absolute-looking rule was not created by Unity and was not present in exactly its current form from the first traceable design. The repository history shows four distinct steps:

| Date | Durable evidence | What was decided |
| --- | --- | --- |
| 2026-06-01, earliest traceable workspace baseline `98f6e3a8` | `AGENTS.md`, `PROJECT.md`, `docs/planning/DolocTownModdingAPI.md` | Fragile Unity/Harmony/reflection implementation belongs in GameBridge and raw game types should not leak into stable Abstractions. However, the same planning baseline explicitly allowed advanced mods to use Harmony and said irreplaceable patches should register through the planned `helper.Patching`; reflection likewise had a planned `helper.Reflection` path. |
| 2026-07-05 | `docs/reviews/api/2026/20260705-0001-functional-mod-dtmapi-boundary-review.md` | The boundary was strengthened from “stable public API plus managed advanced escape hatch” to “GameBridge owns all Doloc native/Unity/Harmony/reflection; functional mods own only policy/UX.” AutoFishing was called the healthiest example. The SMAPI comparison inspected generic helpers, ConsoleCommands and GMCM, but not a direct-Harmony gameplay mod such as Yet Another Fishing Mod. |
| 2026-07-10 to 2026-07-12 | AutoFishing split Goals/Reviews/Updates and the lightweight roadmap | Fishing Primitives were deliberately first-party-only and internal. The old executor was retained as a compatibility island while a new native/cache/session/lease/hook-runtime path was built. Source gates then prevented the product mod from referencing Harmony, reflection, raw fishing types or the legacy API. The July 12 roadmap codified the resulting rule that a product mod directly holding substantial native/Harmony/reflection code was a stop condition. |
| 2026-07-15, Author SDK commit `dd7492b3` | `src/DTMAPI.AuthorSdk/ProjectValidator.cs`, fixed `author-sdk/compatibility/0.5.5/DTMAPI.Author.props` | The ordinary-mod boundary became executable SDK policy. `SDK160` scans source/project text for `BepInEx`, `HarmonyLib`, `Assembly-CSharp` and `UnityEngine`, while the fixed offline build references only .NET Standard and `DTMAPI.Abstractions`. |

The June baseline is important: `helper.Patching` was a planned managed escape hatch, not a promise that every native patch would be rewritten as a framework capability. No current `DTMAPI.Abstractions`/Core implementation of that helper exists. The later strict lane therefore kept the stable-API half of the original design but dropped its advanced-mod half.

`SDK160` is also demonstrably a product-policy check, not a CLR or Unity technical limit: its unit fixture appends only the comment `// BepInEx direct reference` and expects rejection. The SDK itself remains valuable as a deterministic offline tool for ordinary API-only mods. The defect is treating that safe default lane as the only DTMAPI-managed code-mod lane.

#### Why the cutover became 8,832 lines

At commit `f9f767dd` on 2026-07-11 the measured fishing-domain total was exactly 8,832 physical source lines:

| Component at cutover | LOC | Reason it existed |
| --- | ---: | --- |
| `AutoFishingMod` | 493 | Product policy, state machine, input and player configuration moved out of GameBridge |
| New/current first-party GameBridge fishing path | 5,342 | Cached native adapters, hook routing/runtime, primitive sessions, scheduler, state/transaction caches, input and animation leases, diagnostics, rollback and lifecycle safeguards |
| Retained legacy compatibility path | 2,997 | Preserve the old `IFishingAutomationApi` executor and published binary compatibility |
| Total | 8,832 | New protected path plus old compatibility path, not a simple source move |

The pre-cutover parent `d68c0dbc` contained 3,158 lines across the equivalent complete product/native area. The cutover therefore added 5,674 lines net. This was not an unexplained formatter/file-count artifact:

1. the migration retained the old approximately 3,000-line executor instead of replacing it;
2. it built a second defensive implementation rather than moving the old native implementation into the product package;
3. it combined the ownership split with hot-path cache work, transaction/rollback, scoped leases, telemetry, shadow comparison and source architecture gates;
4. the Hook-runtime goal explicitly moved the old executor to Compatibility “without prioritizing mechanical line-count reduction.”

Those safeguards are not automatically waste. They answer real Unity Mono lifecycle, patch collision, process-lifetime assembly, stale native object, allocation and diagnostic risks. But making the framework own every one-off fishing safeguard converted product complexity into base-framework maintenance complexity. Unity magnified the cost of the chosen boundary; it did not require the boundary.

#### What the existing lightweight roadmap does and does not plan

The 2026-07-12 roadmap defines lightweight primarily as less player-shipped/loaded QA and compatibility code, no inactive Hook/session/lease cost, and a smaller public compatibility surface. It explicitly says that product extraction does not mean moving Unity/Harmony adaptation into mods. Its concrete AutoFishing follow-up is therefore only partial:

- thin or optionalize the old compatibility executor, create it only for a real legacy consumer, and remove it only after a warning/migration/breaking-version boundary;
- extract QA/performance code from the player Runtime;
- activate hooks and sessions on demand and optionally split physical capability assemblies;
- remove unused shadow/legacy residue after evidence permits it.

That route can reduce the default package and loaded roots, and can eventually remove roughly the 3,000-line legacy implementation from the normal Runtime. It does **not** plan to move or substantially eliminate the 5,342-line first-party fishing-native layer. It may even retain the same repository source total in an optional capability DLL. Therefore there was no complete existing plan for the user's actual objective: “adding AutoFishing should grow the AutoFishing package, not the DTMAPI base Runtime.”

#### Corrected lightweight definition and AutoFishing route

For the major-update program, the proposed definition should be:

> Lightweight DTMAPI means that base Runtime size and maintenance cost do not grow with each first-party feature implementation. The base owns generic loading, identity, dependency/version admission, events/config/log/input, diagnostics and truly shared native infrastructure; a feature-specific native implementation is owned and packaged by that feature Mod.

This does not require exposing raw game types through `DTMAPI.Abstractions`, nor does it require putting mods under `BepInEx/plugins`. It requires a second DTMAPI-managed advanced lane. A future AutoFishing refactor should proceed in evidence-first stages:

1. classify the current 5,390-line first-party/native area into AutoFishing-specific implementation, genuinely shared platform code, QA/diagnostics and removable duplication; freeze behavior while producing the dependency/hook map;
2. implement an advanced `DtmMod`/Author SDK lane that remains under DTMAPI manifest ownership but may declare game/Harmony references, uses a unique patch owner, and reports restart-required after code is loaded unless cleanup is explicitly proven;
3. move AutoFishing-specific hook runtime, native adapter/cache, transactions and animation/input behavior into `AutoFishingMod` or a private AutoFishing-owned assembly; keep only demonstrably cross-mod mechanisms in GameBridge;
4. move the full legacy executor to an optional compatibility package or remove it no earlier than the reviewed `0.6.0` breaking boundary after consumer scan, warning window and migration note; retain only required ABI shells where applicable;
5. measure base Runtime/package LOC and loaded assemblies separately from total repository/test LOC, then run the existing functional, lifecycle, GC and clean-exit gates before claiming completion.

This is a correction to the future architectural direction, not authorization to move code immediately. The first stage must determine how much of 5,390 lines is reusable safety infrastructure versus AutoFishing-specific over-generalization.

### 4.5 User-instruction trace and Batch 6 scale-risk audit

The available durable records support the user's memory that the intended product was SMAPI-like and that the framework should remain a platform rather than become the implementation body of every functional Mod. The exact raw July 5/July 12 chat transcript is not present in `.codex/conversation-history`, so this review does not invent a verbatim instruction. The recoverable requirement chain is:

1. `docs/planning/DolocTownModdingAPI.md` preserves the original project request to build a multi-Mod Doloc Town Modding API using the available SMAPI source as reference and to implement SMAPI-equivalent capabilities. Its capability table includes loader, manifests, events, input, config, registry, reflection, content, content packs, commands, update checks, compatibility, advanced Harmony management and save data.
2. `docs/updates/2026/20260705-0006-smapi-style-boundary-split.md` records the trigger as the user asking how DTMAPI should adjust when following SMAPI and explicitly requesting boundary-splitting research.
3. The resulting `docs/reviews/api/2026/20260705-0004-smapi-style-boundary-split-research.md` correctly states that SMAPI Core is a “boring platform,” code mods own product logic, content owners interpret their own domain schema, and a mod-owned API does not automatically become framework API. It also correctly diagnoses that current product-shaped GameBridge APIs make DTMAPI act as the main feature mod while visible Mods own mostly config/keybind/presentation.
4. The 2026-07-12 lightweight roadmap was then written after the AutoFishing cutover and uses Zoom/AutoFishing as the repeatable productization template. Its Update says to “continue copying” that pattern after QA extraction.

The divergence therefore occurred in the interpretation, not in a lack of SMAPI direction. Four decisions turned “SMAPI-like platform” into a growing native-feature host:

- the SMAPI research validated framework interfaces and content-pack ownership, but did not inspect a representative direct-Harmony gameplay mod before deciding where feature-native code physically belongs;
- “do not expose raw Unity/game types in stable Abstractions” was incorrectly treated as equivalent to “a managed Mod may never reference Unity/game/Harmony itself”;
- “GameBridge exposes primitives” had no consumer-count or marginal-base-cost gate, so a first-party-only primitive of several thousand lines still counted as acceptable platform architecture;
- lightweight was redefined as default package/loading, inactive cost and public API surface. The roadmap explicitly says it is not trying to delete code and records source LOC only as a trend, so it lacks a completion gate for DTMAPI base-source growth per product.

The result contradicts the scale property of a SMAPI-like ecosystem. The current route tends toward:

```text
DTMAPI base = generic platform
            + Σ(native capability/adapter built for each functional Mod)
            + compatibility copies
```

The required scalable route is:

```text
DTMAPI base = generic platform + genuinely shared native infrastructure
each Mod    = product policy + feature-specific native implementation
```

#### Current structural scale evidence

Current dirty-worktree physical C# counts on 2026-07-19 are:

- complete `DTMAPI.GameBridge.DolocTown`: 41,168 lines across 94 files;
- `GameBridge/Features`: 25,703 lines;
- the 11 Catalog `PublishedProduct` code roots: 3,046 lines in total.

The following pairings are structural co-location evidence, not a claim that every GameBridge line is exclusively attributable to the named Mod. Some domains, especially Audio and Camera, can serve more than one consumer. They nevertheless show the current marginal-direction problem because most listed APIs have only one real product consumer:

| Product/domain | Mod LOC | corresponding GameBridge feature LOC | GameBridge/Mod ratio |
| --- | ---: | ---: | ---: |
| AutoFishing / FishingAutomation | 590 | 5,400 | 9.2x, excluding 3,003-line legacy Compatibility |
| Zoom / Camera | 299 | 1,513 | 5.1x |
| ActionSpeed | 320 | 1,400 | 4.4x |
| OneActionComplete / ActionCompletion | 249 | 612 | 2.5x |
| MoreSaves / SaveSlots | 197 | 885 | 4.5x |
| ChestLocator | 205 | 542 | 2.6x |
| FishBreeding / FishRoeTooltip | 287 | 365 | 1.3x |
| AnimalProgress / AnimalViewer | 261 | 1,097 | 4.2x |
| StrongPlanting | 223 | 1,019 | 4.6x |
| Mine / MachineProduction | 325 | 1,468 | 4.5x |
| MoreEquipment / EquipmentSlots | 196 | 2,805 | 14.3x |
| Manbo audio / AudioReplacement | 190 | 3,428 | 18.0x |
| Paired total | 3,342 | 20,534 | 6.1x |

A naive 100-Mod projection must not be presented as a forecast because future Mods vary and real shared capabilities can amortize cost. It is still a valid architectural stress test: at the current paired average of roughly 1,711 GameBridge feature lines per product/domain, 100 similar one-off native products would imply about 171,000 feature lines in the base before Core, UI, compatibility, QA or tests. The current rule has no mechanism that prevents that linear growth.

#### Batch 6 and later risk

The current full-audit Batch 6 explicitly productizes OneActionComplete, ActionSpeed, MoreSaves, DebugConsole, ChestLocator, FishBreeding, AnimalProgress, StrongPlanting, Mine and MoreEquipmentSlots while also forbidding Hook/reflection implementation from moving into an ordinary Mod. Under that unchanged rule, the likely outcomes are:

| Batch area | Scale risk under the current rule | Reason |
| --- | --- | --- |
| OneAction, Chest, Fish tooltip | medium | Existing GameBridge bodies may remain rather than shrink; new demand adapters/owner contracts can add framework code around a single consumer. |
| ActionSpeed | high | The route calls for splitting native action categories and arbitration. With an API-only Mod, every subdomain remains a new GameBridge responsibility. |
| MoreSaves | high | Fixed-12 native behavior remains in GameBridge and later naming/scrolling/pagination adds UI/native seams unless the product owns them. |
| DebugConsole | mixed | Moving its UI out of Bootstrap can shrink the base only if the diagnostic host owns the implementation; retaining every native command implementation in GameBridge merely moves the shell. |
| AnimalProgress, StrongPlanting, Mine | high | Each currently has a single product consumer. A general machine/animal/planting engine created for one Mod is product code disguised as a platform primitive. |
| MoreEquipmentSlots | severe | It already has a 2,805-line GameBridge domain against a 196-line Mod and the route plans further isolation of recovery/UI/stats/shield behavior. |
| Audio and future BGM | high | A bounded shared audio bridge is legitimate; product-specific playback policy, mapping and lifecycle engines in the base are not. BGM must remain a separate product/research domain. |
| Pets, vehicles, multiplayer | severe if treated as GameBridge capabilities | These are independent large products. Applying the current “all native code stays in GameBridge” rule would make each project a permanent DTMAPI base subsystem. |

Not every Batch 6/7 item is problematic. Versioned cross-Mod API exchange, save identity/data helpers, i18n, content-pack discovery/error isolation, update/deprecation infrastructure, SDK validation and genuinely generic events are SMAPI-like platform capabilities and may legitimately grow the base. A Content Patcher-class system is also coherent as a separately versioned content-owner product; it should not become mandatory Core merely because it integrates with DTMAPI.

#### Required scale invariant before Batch 6 product work

The existing Batch 6 productization sequence should not use AutoFishing as a reference implementation until the advanced managed lane and ownership rule are decided. Each candidate needs a pre-implementation marginal-cost decision:

1. `Platform`: generally reusable loader/helper/lifecycle capability; base ownership allowed.
2. `SharedNative`: native mechanism with at least two independent real consumers or an unavoidable global safety invariant; narrow GameBridge ownership may be allowed.
3. `ProductNative`: feature-specific Harmony/reflection/game implementation or a single-consumer adapter; must belong to the product's advanced DTMAPI package, not base GameBridge.
4. `ContentOwner`: domain schema/engine such as CustomAnimals or a future Content Patcher; ship as an explicit owner product with content packs targeting it, not as implicit Core.

Default acceptance for a single-consumer product should be zero new feature-specific base Runtime implementation. Any exception must state the second consumer/global invariant, base LOC/DLL impact, inactive cost, compatibility promise and removal owner. `InternalsVisibleTo` access granted to one first-party product is evidence of product ownership, not evidence that the implementation belongs in the framework.

This review records a scale blocker in the current route. It does not edit the canonical roadmap or start a Batch 6 refactor. A dedicated decision/update must revise the strict ordinary/advanced Mod boundary and then re-baseline Batch 6 before implementation proceeds.

### 4.6 Batch 1-5 retrospective under the corrected lightweight definition

The Batch 6 scale finding does not mean every earlier batch was wasted or repeated the AutoFishing mistake. Batch 1-5 mixed four different kinds of work:

- platform authority and safety that legitimately belongs to DTMAPI;
- tooling/QA that belongs in the repository but not necessarily the player Runtime;
- compatibility code that is deliberately temporary but still increases the base;
- per-product native ownership/demand scaffolding that risks preserving the wrong GameBridge boundary.

The following physical C# diff is a structural aid, not a performance result. Batch 1-4 use committed boundary ranges; Batch 5 compares final Batch 4 commit `d01c2ca7` with the current dirty worktree and includes current untracked C# files, so its number is provisional.

| Batch | Main purpose | Player Runtime C# net | Non-player/repository C# movement | Corrected verdict |
| --- | --- | ---: | --- | --- |
| Batch 1 | Runtime/installer ownership safety; remove Oil hard-code and use official JSON | GameBridge `+9` net | Tests `+72`; installer/scripts not represented in C# count | Directionally correct. It deleted 284 Oil feature/hook lines and removed the 34-line Oil CodeMod shell, but added roughly the same amount of embedded Smoke evidence. The product leak was fixed; QA was temporarily put in the wrong physical assembly and later corrected by Batch 4. |
| Batch 2 | One version/release/Catalog authority; stale-Runtime rejection; ABI compatibility | `+618` across the five Runtime assemblies | Tests `+1,214`; substantial release scripts/fixtures outside this C# count | Mostly legitimate platform/release work, not a new gameplay engine. The caution is compatibility accumulation: about 121 Runtime lines restore the retired Lamp declaration/provider, four lines extend legacy fishing, and much of the GameBridge increase was then-embedded Smoke. If every single-consumer product becomes public ABI, this pattern scales badly even without new gameplay code. |
| Batch 3 | Author SDK, receipts/source modes, deterministic packaging, Doctor, author session/reload and Player Doctor | `+3,971` (`Bootstrap +38`, `Core +2,896`, `GameBridge +1,037`) | Tooling source `+5,834`; tests `+2,890` | The first major base-weight warning. Offline SDK/Doctor/receipts are legitimate platform tools, but Author Session/source selection/audio reload placed about 3,549 net Runtime lines into the mandatory player assemblies. It also made the strict API-only rule executable. This is not per-product linear growth, but it applies the same “optional capability lives in the base because it is dormant” definition of lightweight. |
| Batch 4 | Extract embedded Smoke/QA into an optional host and preserve a five-DLL player package | `-8,171` (`GameBridge -7,651`, Core `-328`, Bootstrap `-192`) | Optional QA `+8,815`; tests `+3,732` | The clearest successful player-base reduction. Repository source grew because QA remained, as it should, but player Runtime shrank. The route twice overclaimed completion while 12,733 lines of QA behavior still remained in production; G8/G9 source/IL ownership gates finally corrected that. Narrow optional-host seams remain, but this batch moved in the right direction. |
| Batch 5, current in-progress tree | Event kernel, demand coordination, content generations, retained-Hook lifecycle, inactive cost and GC evidence | approximately `+7,187` (`Core +3,963`, GameBridge `+3,224`, including untracked files) | Optional QA about `+2,830`; tests about `+4,868`; product Mods `-11` | Closest repeat of the lightweight-definition problem. Much of the event/demand/generation substrate is genuinely generic, but the batch optimizes and catalogs the existing feature hosts instead of first deciding whether single-consumer native hosts belong in DTMAPI. It makes inactive work cheaper while materially enlarging the base and preserving all feature implementations there. Batch 5 remains incomplete. |

#### Batch 1

Batch 1A/1B did two important things:

- remove the hard-coded `crude_oil` GameBridge feature/service and its OneAction/ToolCollider coupling in favor of official JSON;
- make player uninstall Runtime-only and developer publication fail-closed/recoverable.

This is the desired ownership direction: a content/product rule left the base. The apparent `+9` GameBridge net is misleading unless decomposed: the batch removed `OilCoalDropFeature`/`OilCoalDropService` and related hook work, while adding more than 400 lines of Smoke orchestration/cases. That embedded QA debt was real, but it was not a replacement Oil engine and Batch 4 later removed it from production.

Verdict: keep Batch 1. Its lesson is that proving a deletion must not re-add an equally large permanent test body to the player assembly.

#### Batch 2

Batch 2 established generic release authority that a SMAPI-like framework needs: Runtime/product/minimum version projection, Catalog identity, package hashes, pre-assembly compatibility rejection, stale installed Runtime recovery, retained-binary ABI checks and release gates.

It did not create a new native product capability. Its product-shaped weight came from the zero-deletion compatibility policy:

- retired Lamp public types and a disabled provider shell were restored;
- exact old AutoFishing binding remained supported;
- additional Smoke/UI evidence was added before Batch 4 extraction.

Verdict: keep the version/Catalog/release machinery. Reclassify product-specific ABI implementations as optional Compatibility and stop publishing new single-consumer framework APIs, otherwise every future product creates permanent Batch-2-style retention cost.

#### Batch 3

Batch 3 contains two very different products:

1. offline author/player tooling: deterministic compiler/package assets, schemas, validation, receipts/journals, deployment recovery, metadata-only Doctor and separate Player Doctor executable;
2. player Runtime integration: author source state, authenticated session host/wire protocol, Workshop snapshot bridge, Runtime-thread reload transport and transactional Audio last-good reload.

The first group is valid repository/tooling growth and does not make the game-loaded DTMAPI DLLs proportionally larger. The second group does. The Author SDK commit alone added approximately:

- `AuthorSessionHost.cs`: 855 lines;
- author source/session/contracts/descriptor/wire/digest Core support: more than 1,300 lines;
- `AuthorSessionReloadBridge.cs`: 383 lines;
- `WorkshopSubscriptionSnapshotBridge.cs`: 289 lines;
- Audio reload changes: roughly 362 net lines;
- additional Core/Bootstrap integration around those owners.

Ordinary startup is correctly dormant when no descriptor exists, so this did not create constant polling. It still ships several thousand author-only Runtime lines to every player. The “exact five player DLLs” invariant prevented QA from shipping, but was also treated as a reason to place optional author-session behavior inside those same five DLLs rather than an optional developer host.

Verdict: preserve the SDK, receipts, Doctor and generic native Workshop snapshot authority. Review moving the author session/reload endpoint into an optional development host and keep only a narrow authenticated Runtime seam if needed. Change `SDK160` from universal prohibition to strict-lane policy when an advanced managed lane is designed.

#### Batch 4

Batch 4 directly attacked one of the largest known sources of false Runtime weight. The first G0-G7 implementation successfully created an optional QA assembly and excluded it from player packages, but mostly renamed/moved case bodies under production `QaHost/Fixtures`; a detailed review found 25 production files and 12,733 lines still owning QA policy. G8/G9 then removed production scenario bodies, public fixture authority and no-QA relays and added semantic/source/IL negative gates.

The final committed result is materially lighter for players even though total repository LOC increased. This is an acceptable and necessary distinction:

- player Runtime/source/package reduction: successful;
- repository test-source reduction: neither required nor desirable;
- implementation/process complexity: high, because the first completion criterion checked DLL exclusion before semantic ownership.

Verdict: keep Batch 4 and its final G9 gates. Future extraction should use semantic ownership as the initial acceptance rule so it does not require two correction rounds.

#### Batch 5

Batch 5 is not a product split. Its intended platform work is defensible:

- zero-listener event fast paths and deterministic event membership;
- a bounded cross-thread event queue;
- change-driven content generations and atomic last-good publication;
- generic demand sources/lifetimes and owner cleanup;
- explicit physical-patch/lifecycle/restart states;
- removal of per-frame file signatures, unnecessary snapshots and evidence-only product ticks.

The scale problem is how that substrate is applied. Current source still eagerly constructs the existing Feature objects, then represents a large route catalog covering Fishing, FishRoe, Chest, SaveSlots, StrongPlanting, AnimalViewer, Audio, ActionSpeed, ActionCompletion, Mine visuals, Equipment, Creative, DebugConsole and other product/native domains. Most patches become `ProcessPinnedDormant`; the product implementations remain in GameBridge. Thus Batch 5 currently changes:

```text
large eager/recurring GameBridge product host
```

into:

```text
larger, better-instrumented and mostly dormant GameBridge product host
```

rather than changing its ownership.

This is still useful for the capabilities that remain shared platform infrastructure, and it reduces real disabled-state/GC pressure. It is not sufficient evidence of a lightweight base. The current worktree also contains product/domain-specific growth alongside the generic kernel, including large Audio/CustomAnimals transaction changes, per-domain demand descriptors, a 150-line fishing energy gate and a 103-line production-side fishing QA-control session. The latter QA seam needs the same semantic scrutiny used in Batch 4.

Batch 5 already exposed the cost of its instrumentation complexity:

- the first completion audit found detailed diagnostics rebuilt twice per frame, retained Hook callbacks doing work at zero demand, non-atomic content publication, an event unsubscribe race and non-authoritative lifecycle receipts;
- later corrections addressed those source defects;
- an optional-QA Camera re-entry path caused a real lease flood/crash before correction;
- the first six-stage AutoFishing GC run sampled full resource/owner diagnostics at roughly 60 Hz, creating observer effect and invalidating its performance evidence.

Verdict: do not discard Batch 5, but do not close it as the final architecture of all current feature hosts. Preserve only the generic event/demand/generation/lifecycle substrate and the proven inactive-cost corrections. Before finalizing per-product demand routes, apply the `Platform` / `SharedNative` / `ProductNative` / `ContentOwner` classification from section 4.5. A ProductNative route that will move into an advanced Mod should not gain a larger permanent GameBridge demand implementation merely to complete Batch 5.

#### Net conclusion for Batch 1-5

- Batch 1: no AutoFishing-style repeat; it removed a product leak.
- Batch 2: no new product engine, but it demonstrates the long-term cost of public single-consumer ABI.
- Batch 3: related base-weight problem; optional author workflow and strict policy were embedded in the mandatory Runtime.
- Batch 4: successful correction; it reduced player Runtime while keeping QA optional.
- Batch 5: related architectural problem and current priority for re-scoping; it improves inactivity but can cement the oversized feature-owner model.

No completed Batch 1-4 behavior should be reverted wholesale from this review. The immediate planning consequence is narrower: finish only safe, generic Batch 5 work and its required evidence, then reclassify product-specific routes before declaring Batch 5 architecture final or entering Batch 6.

### 4.7 Focused AutoFishing rehome disposition

The task-specific API review `docs/reviews/api/2026/20260719-0010-autofishing-smapi-rehome-boundary-review.md` converts the preceding scale conclusion into a file/responsibility disposition.

Its decisive result is:

- cancel the **universal** rule that every DTMAPI-managed Mod must avoid Unity, Harmony and game references;
- retain the API-only restriction as the default strict lane;
- add an advanced DTMAPI-managed `DtmMod` lane with direct native references, Mod-owned Harmony and restart-required semantics;
- keep DTMAPI's bundled GMCM-class registry/UI and ordinary platform services;
- move AutoFishing's native Hooks, caches, transactions, input/animation behavior and product diagnostics into AutoFishing;
- delete the one-consumer first-party primitive/friend/provider/demand/cleanup scaffolding instead of promoting it to public API;
- move product QA out of mandatory GameBridge;
- treat the frozen 3,007-line legacy fishing executor/facades as transitional compatibility, honoring the already recorded warning-preview gate before final deletion.

The local SMAPI build targets and tagged Yet Another Fishing Mod source support this split: the framework supplies game references and optional Harmony to the Mod build, while the 1,474-line Mod owns all feature-specific gameplay/patch/config-page code and adds no AutoFishing engine to SMAPI. The focused review remains docs-only and changes neither the current strict SDK nor the compatibility promise.

## Decision recommendation: define three lanes

The evidence supports a product decision rather than silently pushing every old mod into framework core:

| Lane | Entry/package | Allowed native access | DTMAPI promise |
| --- | --- | --- | --- |
| Managed CodeMod | `DtmMod`, strict Author SDK, one declared DLL | Stable public APIs only | Strong admission, owner/lifecycle/diagnostic contract where APIs support it |
| Advanced/Compatibility CodeMod | `DtmMod`, stays inside DTMAPI package; declared private dependencies only if/when formally supported | Explicit direct Harmony/game refs under warnings and unique owner rules | Discovery, cold-start enablement, manifest/config/log/Doctor; restart-required and no universal automatic unpatch guarantee |
| External BepInEx plugin | `BaseUnityPlugin` under `BepInEx/plugins` | Direct | Detected/reported only; not a DTMAPI-managed mod |

Recommended direction: retain the strict Managed lane as the default tutorial, but design a separately named Advanced/Compatibility lane before claiming third-party DLL migration is easy. This is closer to the actual Wang/SMAPI evidence and prevents the API/GameBridge from absorbing every one-off patch solely to satisfy packaging policy.

The advanced lane is not authorized or implemented by this review. It needs explicit decisions on:

- whether direct Harmony/game references are acceptable at all;
- whether disable always means “next restart” for this lane;
- Harmony owner naming/unpatch responsibility and collision diagnostics;
- declared private dependency DLL layout/resolution;
- Doctor/UI wording so players can distinguish managed guarantees;
- whether Workshop submission validation warns or blocks unsafe patterns.

## Answer to the original “普通类库 / 单一 DLL” confusion

- A plain library with no framework entry is inert: DTMAPI and SMAPI need one known object to call.
- Adding that entry is normally small. It can initialize all existing classes in the same assembly.
- “单一 CodeMod DLL” describes the current SDK package artifact, not a one-class or one-file source restriction.
- A traditional BepInEx mod can often become a legacy/thin DTMAPI mod with a small lifecycle adapter if source is available. That gives DTMAPI package discovery and cold-start admission; it does not magically make every direct patch hot-disableable or API-isolated.
- The difficult form is the current strict API-only conversion of a feature whose stable API does not yet exist.

## Validation and evidence limits

- Compared the retained published `0.5.2-alpha` subscription package with its recorded exact build commit `8caf8403b45c`.
- Inspected the four named Workshop roots, manifests, file sizes and SHA-256 digests.
- Inspected managed assembly reference/type/member metadata without reading third-party method bodies.
- Inspected local SMAPI entry/loader/assembly-resolution source, technical package documentation, and 20 installed sample manifests.
- Inspected current `0.5.5` version authority, loader/SDK policy and 18 first-party/test code projects.
- No game/runtime validation was required or run. No claim here upgrades static compatibility into proven in-game behavior.

## Follow-up

1. User/product decision: keep one strict lane only, or add the recommended Advanced/Compatibility CodeMod lane.
2. If advanced is accepted, open a dedicated loader/SDK/Doctor boundary review before implementation; do not weaken the strict lane implicitly.
3. Ask the Terra author for source and modification/redistribution terms. With source, first produce a minimal one-DLL `0.5.2`-style branch preserving the existing functional classes; without source, do not fabricate it.
4. Independently map each Terra responsibility to existing/currently missing public APIs before quoting a strict `0.5.5` migration.
5. Update author documentation to state plainly that ordinary DTMAPI mods belong in a DTMAPI-managed package, only the bootstrap belongs in `BepInEx/plugins`, and advanced direct-patch mods (if approved) require restart semantics.
