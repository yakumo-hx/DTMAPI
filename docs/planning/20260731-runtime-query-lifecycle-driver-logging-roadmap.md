# Runtime 查询、生命周期、帧驱动与日志简化路线图

> 日期：2026-07-31
> 状态：本版最小修正已实现，下一版本简化待逐项设计
> Owning Update：[20260731-0003](../updates/2026/20260731-0003-contentquery-lifecycle-frame-log-closeout.md)
> 根因记录：[20260731-0001 手测审查](../reviews/manual-qa/2026/20260731-0001-autofishing-manager-player-ui-and-loop-stall.md#2026-07-31-autofishing-复测通过后的平台告警审查)

## 1. 当前版本边界

当前版本只完成四项已经选定、可以独立验证的收口：

1. ContentQuery 采用 C1；
2. 生命周期只删除 `SecondSaveLoaded` 内部诊断分类及其假警告；
3. 帧驱动采用 F1 相对进度健康判断；
4. 默认连续诊断采用 G1 Lite。

本版不实施下一版本架构，不借机修改产品状态机、公开生命周期事件、
SaveLoad coordinator、owner cleanup、保存事务或 DebugConsole 物品权威。

## 2. 当前版本：C1 官方内容输入兼容

ContentQuery 仍是实验性的来源查询索引，不是官方内容加载器。本版行为为：

- 只有官方明确启用的来源才读取 `info.json`、PNG 和
  `item_tbitem.json`；disabled/unknown 只保留包级 Mod 状态；
- 第三方官方/Workshop JSON 使用窄兼容 reader，只复现官方已证明的
  引号外 `//` 行注释语法；字符串中的 URL、转义和 BOM 保持原值；
- 一个第三方坏文件只形成有界 Warning，其他有效 enabled 来源仍原子发布；
- DTMAPI 自有 ContentPack 继续使用严格 `JsonFile` 和既有 last-good
  原子失败语义；manifest、receipt、配置和其他权威文件完全不放宽。

## 3. 下一版本：冻结 ContentQuery 能力，改为轻薄官方层

下一版本不继续扩展当前文件扫描器。现有 Experimental 公共签名只作为兼容
表面冻结；任何语义迁移必须先审计真实消费者并保持二进制兼容。

目标实现：

```text
官方最终 TbItem
    + 官方 ModManager.EnabledMods / CachedConfigs
    + 官方实际成功的 item_tbitem 合并顺序
    -> itemId -> winning ModInfo
    -> 薄的稳定 DTO/查询适配器
```

- `TbItem` 决定物品是否真实存在、能否浏览或给予；
- winning `ModInfo` 只补充来源标题、Workshop ID 和分类；
- 没有 winning Mod 的物品归为原版；
- disabled、unknown 和磁盘上未加载的 item 不在玩家 Runtime 启动时扫描；
- “磁盘存在但未加载”的检查若仍有真实需求，只能进入按需高级诊断；
- DebugConsole 的给予动作继续由原生最终表复核，来源分类不得成为可用性的
  阻断条件。

进入实现前必须回答：

1. 官方成功合并点能否直接记录 item winner，还是必须按官方顺序重放；
2. 覆盖同一 item ID 时如何证明最终 winner；
3. 主菜单无 native table 时 API 返回空、last-known 还是 not-ready；
4. 现有邮件实验接口是否仍有真实消费者；
5. 旧 `FindAssets`/`TryReadTextAsset` 是否只保留兼容代理或按需路径。

## 4. 当前版本：生命周期最小止血

- 所有正常存档载入统一记录为可重复的 `SaveLoaded`；
- 删除 `SecondSaveLoaded` 常量、policy、Normalize 分支和两条错误规则；
- 每次 `SaveLoaded` 仍开启新的 ResourceLifecycle save generation；
- 不修改公开事件分发、产品回调、Hook、SaveLoad coordinator、标题清理、
  owner lease 或保存事务。

## 5. 下一版本：逐项建立简单生命周期范式

下一版本不直接实现大一统 L3，也不在现有 coordinator 旁新增第二套状态机。
每项必须按“原生事实 → 真实消费者 → 最小平台边界 → 失败语义 → 验收”单独
讨论、决定、实施：

1. `SaveDataReady`、`NativeLoadReturned`、`SceneReady` 是否需要区分；
2. 每个 owner/transaction 的 exactly-once 分发是否由一个现有权威承担；
3. cleanup pending/failed/retry 是否需要平台通用 lease，还是留在产品；
4. 返回标题应只有 session close，还是需要 TitleSceneReady/TitleStable；
5. 哪些产品真正需要 save transaction identity；
6. UI、Hook、modal、输入和原值 lease 的共同边界是否足以证明共享 owner；
7. 生命周期上下文是否只供健康检查宽限，不进入产品玩法状态机。

默认范式保持简单：产品优先跟随官方原生状态；平台只提供可靠的会话出现、
会话结束和 owner 停用信号。AutoFishing 等产品必须把所有停止原因汇入自己的
唯一幂等 Stop 事务；平台不管理其玩法、调度器或原值恢复细节。

## 6. 当前版本 F1 与下一版本 F3

本版 F1：

- Timer 只做健康观察，不派发 Mod Update；
- health post 比较排队前后 InputSystem、PlayerLoop、native 和 Unity Update
  callback count；
- 所有来源一起停止是 startup/load/global pause，不退订、不 Warning；
- 只有 sibling 驱动继续推进而 InputSystem 单独停滞才恢复订阅；
- 恢复成功为普通信息，恢复失败才是 Warning。

下一版本 F3：

- 审查并删除 InputSystem 的“帧驱动/恢复状态机”角色；
- 以 native gameplay drain 和 PlayerLoop 为主，MonoBehaviour Update 为后备；
- 输入仍由每个接受的 Unity 帧统一 latch/consume，不恢复多源重复采样；
- 验收 F6、Manager、主菜单、存档、返回标题、失焦/回焦、快速按键和零重复
  dispatch 后，才物理删除 InputSystem 订阅与 F1 恢复代码。

## 7. 当前版本 G1 与下一版本 G2

本版 G1：

- 普通玩家默认对象图模式由 Full 改为 Lite；
- QA/smoke 仍可显式要求 Full；
- lifecycle contract 完整摘要只在 phase/title 边界、状态变化或新诊断时写入；
- Hook 自身仍只在状态改变时发布；Warning/Error 不因 Lite 被过滤。

错误可见性是不变量：

- Error 必须保留完整 `Exception.ToString()`，包括 inner exception 与 stack；
- Error 必须附带最小因果上下文，例如 owner、operation、transaction/request；
- Lite/Off 只能减少连续健康状态和对象图证据；
- Full 只能增加连续状态证据，不能成为发现 Error、定位直接失败 owner 或读取
  根异常的前提。

下一版本 G2：

- 为 FileMonitor 建立真正的 MinimumLevel；普通玩家默认 Info；
- 对象图、delta、contract trace 和 Hook fanout 迁到 Debug/Trace；
- startup、Mod 结果和生命周期里程碑保留紧凑 Info；
- Warning/Error 永远可见，Error 继续满足完整异常与最小因果上下文；
- 高级诊断开关明确其生效时机、保留窗口和导出行为。

## 8. 验收顺序

当前版本自动门：Release build、完整 Unit、QA Unit、Catalog、文档治理和
test-artifact governance。玩家手测只需确认：

- ContentQuery 不再因 disabled 注释 JSON 告警；
- 连续至少三次载入/返回标题无重复 SaveLoaded Warning；
- 启动全局停顿不再产生两条帧驱动 Warning；
- AutoFishing F6/组合功能仍正常；
- Error 页在注入或真实失败时仍可见完整直接异常；
- 同等短测窗口下日志明显小于 Full 基线。

下一版本四个方向分别建独立决策和聚焦验收，不把它们合并成一次无边界的
Runtime 重构。
