# DTMAPI 长期平台能力独立复核

- Status: recorded
- Role: 2026-09-07 长期规划的源码证据与设计理由；不是执行进度或新能力验收。
- Source Request: 用户指出上一阶段规划深度不足，要求从第三方作者最终能力展开完整产品路线、公共/内部/GameBridge 边界、真实产品验收及连续 Sol 接手范围；本轮不进入生产实现。
- Owning Update: [20260907-0005](../../../updates/2026/20260907-0005-platform-maturity-roadmap.md)。
- Accepted design: [主架构](../../../architecture/platform-next.md)、[能力地图](../../../planning/platform-next/capability-map.md)、[路线](../../../planning/platform-next/roadmap.md)。
- Current truth: [PROJECT](../../../../PROJECT.md)、[API matrix](../../../api/public-api-matrix.md)、发布目录和源码仍拥有现行事实。

## 调查边界

重新检查当前 SDK 构建/验证/打包/公开命令、Core 发现/Entry/owner/事件、公共 helper、GameBridge 会话重载和原生保存/内容接缝；并行从作者交付、Runtime、数据内容三个方向互相核对。没有以旧审核通过、历史游戏 smoke 或 Public API 名称作为能力成立的前提。

用户补充的已发布 Runtime 0.6.1、SDK 的 API 载荷停留 0.5.5 与当前版本源相符；SDK 工具自己的源码版本是 0.1.0，不能把 API 载荷版本当 SDK 工具版本。PN-001–003 的版本、协议、目标目录改动已经有各自源码/包验收；本轮保留，不重做它们，也不把它们宣称为新 Runtime/SDK 发布。

本轮无游戏运行、无 Mono 行为验收、无保存操作；下面的“已存在”是代码事实，“需要”是产品缺口或决定。详细调查笔记在忽略的 `reports/platform-next/20260907-long-horizon/`；本记录保存接手所必需的结论，不依赖该本地目录才能理解路线。

## 当前资产与真正缺口

| 入口 | 直接核对事实 | 判断与任务 |
| --- | --- | --- |
| [CodeModBuilder](../../../../src/DTMAPI.AuthorSdk/CodeModBuilder.cs)、[模板](../../../../author-sdk/templates/codemod/__UNIQUE_ID__.csproj.template)、冻结 Author.props | CLI 自行枚举 src/*.cs、C#12、Release；模板导入的 props 使用 latest，完整 MSBuild project 不是 CLI 输入模型 | 两种 Build 成功不能代表相同程序集；PN-015 先统一 BuildPlan 和受支持工程语义 |
| [AuthorApplication](../../../../src/DTMAPI.AuthorSdk/AuthorApplication.cs)、[SDK Tests](../../../../tests/DTMAPI.AuthorSdk.Tests/Program.cs) 的 RunLegacyMutationCompatibilityFixture | 公开 deploy/update/install-local/source local select 仍暂停；部分测试适配器直接进入历史事务代码 | 旧 PASS 证明兼容恢复算法，不证明公开作者入口；PN-004 走官方 Local，PN-008 实际外部作者复现 |
| [DeterministicPackager](../../../../src/DTMAPI.AuthorSdk/DeterministicPackager.cs)、CodeModBuilder | 编译产生 PortablePdb，当前打包选择入口 DLL；没有完整实际 Mono attach 证明 | PN-017 补 Debug/符号部署、源码行与断点分别验证，R1 决定真实可支持的调试方式 |
| [target catalog](../../../../author-sdk/target-catalog.json)、[Shared reader](../../../../src/Shared/AuthorApiTargetCatalog.cs) | 当前 available 旧目标和 planned 新目标已拆分，planned 含初版反射意图但无冻结载荷 | PN-003 是骨架完成；PN-007 可修订未发布 planned 内容，新公共基础不必等反射；旧 payload 不回填 |
| [Runtime](../../../../src/DTMAPI.Core/Runtime/DtmApiRuntime.cs)、[Owner coordinator](../../../../src/DTMAPI.Core/Runtime/ModOwnerLifecycleCoordinator.cs) | Entry 事务、owner 关闭与清理、DLL 已驻留导致重启要求已有 | 保留；缺的是作者能依赖的 context、排队寿命和完整切档证明，PN-016/009，不重建第二套加载器 |
| [EventManager](../../../../src/DTMAPI.Core/Services/EventManager.cs)、[Hook callbacks](../../../../src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs) | SaveSaving handler 失败可取消 native save；该 slot 使用默认 failureThreshold=0，不会因高频事件 quarantine 规则被移除；SaveSaved 在 native 成功之后 | 不存在本轮早期怀疑的“连续异常自动放过保存”证据。保留失败关闭并增加连续失败回归；M4 才建设显式 required commit participants，与普通观察者区别 |
| [RegistryAndHelpers](../../../../src/DTMAPI.Core/Services/RegistryAndHelpers.cs)、[ProjectValidator](../../../../src/DTMAPI.AuthorSdk/ProjectValidator.cs) | GetApi 依赖准确 Type；SDK 限制额外 DLL，Advanced 仍受具体产品准入/引用约束 | PN-011/010 需同时完成独立工程、格式、Mono 加载和故障诊断；只加 manifest 值不是开放生态 |
| [Helpers](../../../../src/DTMAPI.Abstractions/Helpers.cs)、[Content query](../../../../src/DTMAPI.Core/Services/WorkshopContentInputUi.cs) | ContentQuery 是来源/文本/物品索引；IUiHelper 打开平台页；Input Suppress 是当前 DTMAPI 输入视图边界 | 不把名称当通用 GameContent、UI 或原生输入拦截；资源/内容 M4，UI/原生输入 M5 |
| Runtime 的 ContentPack 分支、[Author reload](../../../../src/DTMAPI.GameBridge.DolocTown/AuthorSessionReloadBridge.cs)、[CustomEntityRegistry](../../../../src/DTMAPI.Core/Services/CustomEntityRegistryService.cs) | 非代码包可进入 loaded/owner 集合；reload 有界到 Audio-only；自定义实体创建返回 runtime-creation-blocked | loaded、索引刷新、影子登记不是实际 Host 激活、通用热刷新、原生实体生效；PN-025/013/029 分别交付 |
| [ConfigService](../../../../src/DTMAPI.Core/Services/ConfigService.cs)、[TranslationService](../../../../src/DTMAPI.Core/Services/TranslationService.cs)、[DiagnosticsService](../../../../src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs)、[ManifestReader](../../../../src/DTMAPI.Core/Manifesting/ManifestReader.cs) | 配置原子替换/备份/迁移已有；翻译有当前语言与回退；日志导出含原始日志；UpdateKeys 是元数据 | 保留成熟资产，补 schema/语言/支持旅程；不宣称日志已完全脱敏或更新服务已实现；PN-019/017/031 |

## 原生事实与不可套用 SMAPI 的前提

本地调查 build 为 `24456188_test_E861E0`，路径由 [references 规则](../../../../references/README.md)进入，不分发反编译内容。

1. `DolocTown.Config/ModManager.UpdateCache`、`LoadWithMods` 及 `DolocConfig.Loader` 拥有官方启用内容合并，`DolocAssetCache.GetAsset<TAsset>` 的 Sprite 路径咨询官方覆盖。GameContent 必须接入官方合并结果和真正资产缓存；修改平台索引不能代替它。
2. `DataPersistenceManager.SaveGame` 只有 `LocalSave.SaveGame` 成功后才调用 `AfterSaveData`。保存后仅写 sidecar 留有原生已提交而 Mod 数据未持久化的中断窗口；需要保存前 durable candidate 与可核对的 native commit 关联。
3. 查阅的 `ArchiveDataHandle`/`ExtraArchiveData` 是显式 JSON OptIn 字段，没有足够证据证明可直接复用通用 Mod 字典。选择 Core sidecar 候选，先做 PN-024 身份、复制/删除和提交实验；不能因为 SMAPI Data 使用游戏保存数据就承诺 Doloc 同样可行。
4. slot、一次加载的 epoch 与持久 SaveIdentity 各自不同；旧保存失败、复制另档、云同步与 orphan recovery 不能靠一个 slot key 混合处理。通用 Data 更不保证作者任意 native inventory 副作用与 JSON 自动形成一个事务。

## SMAPI 参考与设计取舍

本地源码根为 `E:/Python_project/SMAPIlearning/SMAPI`；实际历史研究目录在同级 `SMAPI_version_study`。用户先前提供的另一拼写路径不存在，因此使用已定位的本地目录。仅参考责任与演进理由，未复制源码或许可证敏感实现。

| 参考入口 | 借鉴的原则 | DTMAPI 自己的选择 |
| --- | --- | --- |
| `src/SMAPI/IModHelper.cs`、Data/ModContent/GameContent interfaces | 作者能力按职责完整分组，而非只增加反射 | 通过可选服务保护旧 IDtmHelper ABI；先 lifecycle，再数据/内容/开放生态 |
| `Framework/ModHelpers/ReflectionHelper.cs`、`IReflectionHelper.cs` | 明确查找失败、调用结果和成员类型 | 保留 A06 已选择的匹配和寿命规格，独立 M3 交付，不做全局关键路径 |
| `Framework/ModHelpers/ModRegistryHelper.cs` | 跨 Mod API 与类型兼容需要明确政策 | 先真实共享契约 DLL，暂不复制结构代理层 |
| `Framework/ModHelpers/DataHelper.cs`、`IDataHelper.cs` | 区分配置、全局数据和随档数据 | Doloc 无已证实通用原生容器；提交/身份实验先行 |
| `src/SMAPI.ModBuildConfig/build/smapi.targets` | 作者 Build、部署、符号和包应串成一致工程体验 | M1 IDE 委托 CLI 的单模型，M3 扩工程库集成；官方 MODS 来源和 Unity Mono 不照搬 Stardew 启动器 |
| `docs/release-notes-archived.md` 中 3.14.0 的内容 API 演进 | GameContent/ModContent 与内容请求、冲突责任分开 | 第一份新内容 API 即分离，Host 只承诺有证据的 schema/资源家族 |

历史研究另核对 `SMAPI_version_study/SMAPI-1.0/src/StardewModdingAPI/ModHelper.cs` 与 1.2 的 `IModHelper.cs`：从具体 helper 到公开接口；2.0 的 `src/SMAPI/IModHelper.cs` 已有 Content、Reflection、ModRegistry、Commands、Translation 多组能力。3.0 的 `docs/release-notes.md` 回顾 2.8 引入 save/global Data、2.10 区分 save loaded 与 world initialized；3.0 又将 Entry 提前以便截获内容，并由 GameLaunched 表达初始化结束。3.0 `IDataHelper.WriteSaveData` 还明确未保存退出的数据语义。这些演进支持“时序、持久语义、内容传播和工具链一起成长”，不支持机械照抄某个版本的 API 清单。

## 不采用的路径

- 反射先完成再验证作者平台：无法解决当前构建/安装/诊断断点，调整到 M3；不是取消通用反射。
- 一次性推倒 Core/Runtime：已有事务和清理承担真实不变量，按 PN-014 小切片抽取。
- 直接将现有 Experimental/Frozen 领域改 Stable：没有 native 生效、寿命与维护证据，按领域逐项验收。
- 用修改官方保存 schema 获得快捷 Mod 字典：当前无稳定可用接缝；先独立 sidecar 候选实验，失败就缩小承诺。
- 先造完整 UI/实体/内容 DSL：先一个真实家族和双作者组合；条件性远期能力留地图，不阻塞核心成熟。
- 把所有能力最终交付捆绑一个版本：各子能力按目标 payload、真实产品证明和兼容政策独立演进。

## 规划出口

近期 M1/M2 具有连续任务规格；中期 M3/M4 有格式/存档/内容复盘与明确产品出口；M5/M6 有领域、发行、兼容和维护入口，远期由 C01–C33 覆盖。每次复盘必须输出具体契约或有界修订，不能把 Sol 再送回全仓调查。

本轮文档交付、检查结果与源码未改证据见 owning Update。公共 API matrix、Hook map、smoke 和发布 Catalog 没有因为本规划而改变状态。
