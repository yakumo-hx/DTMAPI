# AutoFishing / Manager 玩家界面与循环停滞手测审查

## 记录信息

- 日期：`2026-07-31`
- 状态：`recorded`
- 来源：用户在“仅启用 DTMAPI 0.5.5 与新 AutoFishing”的玩家式订阅环境中手测并提供五张截图。
- 范围：配置页差异、Mod/状态/错误/Hook 页的信息边界，以及 AutoFishing 疑似运行约 30 秒后停止。
- 本轮性质：只读日志、源码和冻结 DLL 审查；未修改 Runtime、AutoFishing、玩家存档或当前手测部署。
- 相关 Update：
  - [AutoFishing Workshop Copy and Runtime 0.5.5 Manual Retest](../../../updates/2026/20260731-0001-autofishing-workshop-copy-and-055-manual-retest.md)
  - [Manager/GMCM Player Information Architecture](../../../updates/2026/20260726-0003-manager-gmcm-player-information-architecture.md)
- 相关设计与问题：
  - [DTMAPI Manager UI MVP](../../../../design/dtmapi-manager-ui-mvp.md)
  - [ISSUE-010 long-run Mono GC crash](../../../../debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md)

## 本轮日志证据

近两次 DTMAPI 0.5.5 日志已只读保留在：

`E:\Python_project\DTMAPI-retained-artifacts\manual-test-leases\20260731-074235-autofishing-subscription-player\manual-qa-20260731-autofishing-30s`

- `latest-20260731-031656492.log`
  - 长度：`555441`
  - SHA-256：`FD336F227D262524EAE7CD2C3D8A20C41DD8C0333EA4E0EB023C33AE6F3170EF`
- `latest.log`
  - 长度：`1653539`
  - SHA-256：`E60475376A7547311BC36BC92A44BC77BFF2C1756665EC4EF27005F2727270C4`
- `BepInEx-LogOutput-latest.log`
  - 长度：`1694371`
  - SHA-256：`DE4B9B24E7662CA0A3891465992EA662A0CC0E2BEF420629A7566BDCB497ADD1`

## 问题 1：配置面板与记忆中的版本存在差异

### 原始反馈

- “好像感觉配置面板有差异，能找到记录么？”
- 图片转写：DTMAPI 设置 `0.5.5` 的“配置”页显示当前唯一已启用的
  `DTMAPI 自动钓鱼`。右侧包含 F6 状态段落、默认循环说明、切换热键、
  抛竿蓄力幅度、立即咬钩、跳过小游戏和钓鱼动画加速。
- 状态段落直接显示：
  `未启用 / phase=Idle / reason=lifecycle:ReturnedToTitle`。

### 审查记录

- 用户确认事实：AutoFishing 本体功能、三个额外功能及组合开启均可操作。
- 文档事实：
  - `20260726-0003` 明确记录了 0.5.5 Manager/GMCM 的分页、导航、
    本地化和玩家信息架构改版。
  - AutoFishing Advanced/ProductNative 迁移保留配置键、默认值和统一
    `IDtmConfigMenuApi` 注册；截图中的选项不是另一个未知版本的页面。
- 代码事实：
  - 当前产品配置页主动把内部 `phase` 与 `reason` 拼入玩家状态文本。
  - 截图中的 `ReturnedToTitle` 只表示截图时已经回到标题；它不是本轮
    游戏中途停止的原因证明。
- Codex 结论：
  - 面板布局差异有正式记录，主要来自 0.5.5 Manager/GMCM 改版和新
    Product 包真正成功载入配置页。
  - 原始 `phase/reason` 出现在默认玩家配置页，属于尚未收进高级诊断的
    技术文本泄漏。
- 归属：Manager/GMCM 平台呈现与 AutoFishing 产品文案各自负责自己的
  部分；不能由 Manager 吸收 AutoFishing 状态机逻辑。
- 验收点：
  - 默认配置页只显示玩家可理解的“已开启/已关闭”和必要原因；
  - `phase/reason` 保留到高级诊断或导出报告；
  - 标题页状态不得被误读成存档内的异常停用。

## 问题 2：Mod 页是否会向普通玩家显示全部订阅项

### 原始反馈

- “0.5.5 这些东西也会在玩家端显示么（图二）？尤其是第一个？这个界面显示的是什么？”
- 图片转写：
  - Mod 页显示 `1-10/14，第 1/2 页`；
  - 一行一个已发现 Mod，绝大多数带 `[disabled]`；
  - 第一项为 `Doloc Town QoL 0.2.3 | Codex.DolocTownQoL`；
  - 详情显示“未载入、官方已禁用、受限代码 Mod、创意工坊原生验证、
    legacy-input-identity-verified/game-build-unbound、依赖未声明、重启提示”。

### 审查记录

- 现场事实：Workshop `3754869009` 的 `info.json` 与 DTMAPI manifest
  确认为 `Doloc Town QoL 0.2.3 / Codex.DolocTownQoL`。
- 当前行为：
  - Manager 是只读投影，会枚举已订阅且可识别的 DTMAPI manifest，
    包括官方 Mod UI 当前禁用的项目；
  - 本次只加载 AutoFishing，因此状态汇总为已载入 `1`、已禁用 `13`；
  - 第一项只是“已订阅但官方禁用且未载入”，不表示 DTMAPI 正在运行、
    安装或接管它。
- 玩家可见性：是。持有同一批订阅的普通玩家在 0.5.5 中会看到这些行。
- Codex 结论：页面语义是支持/诊断用“所有已发现 Mod”，不是“当前正在
  运行的 Mod”。该语义本身有记录，但默认向玩家铺开 13 个禁用项以及
  provenance/兼容性术语，会产生明显信息噪声。
- 归属：Manager 平台 UI；官方启停仍只归游戏原生 Mod UI。
- 验收点：
  - 明确决定默认列表是“正在使用”还是“全部已发现”；
  - 若保留全部发现，默认玩家视图应优先显示已启用/需处理项，并把禁用
    订阅与精确 provenance/兼容字段放入可展开的支持信息；
  - 任何过滤都不得改变官方启停状态。

## 问题 3：状态页显示的含义

### 原始反馈

- “图三显示的是什么？”
- 图片转写：
  - 总体状态：存在错误；
  - Mod：已载入 `1`、被阻止 `0`、已禁用 `13`；
  - 依赖问题 `0`、当前需要重启 `0`；
  - 需要关注：错误 `8`、警告 `1`；
  - 状态刷新：正常；
  - 页面说明官方启用状态归官方 Mod UI 所有，详细注册表、兼容性、
    Hook 和功能证据在“高级”页。

### 审查记录

- 代码事实：Manager 只要看到任意 retained diagnostics error，
  `OverallStatus` 就直接变为 `failed`，不区分该错误是否属于备用路径、
  未启用 Mod 或当前玩家功能。
- Codex 结论：
  - 该页是 Manager 对当前发现/载入/诊断窗口的汇总，不是“游戏已经坏了”
    或“AutoFishing 已经停止”的直接判定。
  - 本轮 8 个错误中，多数不影响当前两 Mod 路线；因此红色总体状态对
    玩家而言是过度严重的聚合。
- 归属：Manager 诊断严重性与玩家摘要策略。
- 验收点：
  - 玩家状态只被当前启用、当前可操作且确实失败的项目标红；
  - 备用 Hook、禁用第三方 Mod 和支持级诊断应分别降级或移入高级页；
  - 仍可在导出报告中保留完整原始诊断。

## 问题 4：错误页的八项错误及玩家影响

### 原始反馈

- “图四的错误是什么？是否影响玩家？”
- 图片转写：错误页共有 `8` 项。可见内容包括六条
  `DTMAPI.GameBridge` 对 `DolocGridUI<T>`、`SetCapacity`、
  `ResetLayoutSize` 的 patch 失败，一条 Bootstrap coroutine loop
  `TargetInvocationException`，以及一条 Workshop manifest discovery
  failure。

### 审查记录

1. 六条 GameBridge UI Hook 错误

   - 来源是 `NativeUiLayoutDiagnosticsFeature` 安装 open/closed generic
     `DolocGridUI<T>` 的附加布局修复 Hook。
   - 同一运行的权威 Hook 状态为 `UI.NativeLayoutRepair = ready`；
     HomePage/MainMenu 的目标修复 Hook 已安装，失败的是替代/附加的
     generic 路径。
   - 本次截图布局正常，且这些 Hook 与 AutoFishing 状态机无关。
   - 当前影响：没有发现本轮玩家功能影响；但把可容忍替代路径失败记成
     六个 Error，会污染玩家状态并掩盖真实错误。

2. 一条 Bootstrap coroutine loop 错误

   - 反射调用备用 `StartCoroutine(IEnumerator)` 抛出
     `TargetInvocationException`。
   - 同一日志已证明主 `Unity PlayerLoop.Update` 驱动安装成功，并持续
     收到 `PlayerLoop` 帧回调；Bootstrap 后续正常加载。
   - 当前影响：备用帧驱动失败，没有发现本轮运行影响。将它保留为
     player-facing Error 与实际降级状态不相称。

3. 一条 ModScanner manifest discovery 错误

   - 精确路径是 Workshop `3759797170` 的“泰拉瑞亚物品包”。
   - 它把直接引用 `0Harmony`、`Assembly-CSharp`、`BepInEx` 和
     `UnityEngine.CoreModule` 的 DLL 声明为 Strict CodeMod，被
     `strict-native-reference-forbidden` 正确拒绝。
   - 当前影响：该 Mod 已禁用，不影响本次 DTMAPI + AutoFishing 手测；
     若玩家启用它，则该包本身不会按当前 Strict 规则加载。

- Codex 结论：图四的八项错误不是 AutoFishing 循环停滞的原因。它们中
  七项是当前可工作的 Runtime 备用/附加路径诊断，一项是禁用第三方包的
  真实兼容拒绝。错误页仍有产品问题，因为这种聚合会让普通玩家长期看到
  “总体错误”。
- 验收点：
  - 备用布局 Hook 与备用 coroutine 降级不再制造总体红门；
  - 禁用 Mod 的兼容拒绝与当前启用 Mod 的运行失败分栏；
  - Error 页仍能导出精确技术详情供支持排查。

## 问题 5：玩家端是否设计为展示这么多技术信息

### 原始反馈

- “玩家端 DTMAPI 设计上是要这样显示一大堆信息么？我记得当初设计是把
  高级信息隐藏了，难道说高级信息的边界比较小？”
- 图片转写：Hook 页顶层直接分页显示 `66` 条 Hook，含内部 ID、
  `pending`、`experimental`、Harmony Prefix/Postfix 和原生方法名。

### 审查记录

- 记录事实：
  - `20260726-0003` 的确把原 `Features` 路由改名为“高级”，并把
    registry/compatibility/feature evidence 放到其中；
  - 但同一记录明确写了“Hooks remain their own platform-health page”。
- Codex 结论：
  - 用户记忆并非错误：0.5.5 做过“玩家优先、技术信息进高级”的改版；
  - 但实现时“高级”被定义得很窄，只接收 Features/注册表/兼容性，
    Hook 仍是一级页，配置页也仍显示 raw phase/reason，Error 页仍接收
    备用诊断。
  - 所以当前 0.5.5 确实会向玩家显示截图中的大量信息；这不是安装错包，
    而是已定信息架构本身没有隐藏到用户预期的程度。
- 归属：Manager 平台 UI 信息架构，不属于任何玩法 Mod。
- 验收点：
  - 默认玩家层保留配置、当前 Mod 状态、可操作依赖/重启提示和真正影响
    当前启用项的错误；
  - raw Hook、Harmony 目标、phase/reason、provenance、注册表和完整日志
    统一进入高级/支持层；
  - 高级层仍能一键到达并导出，不删除诊断能力。

## 问题 6：AutoFishing 疑似约 30 秒后自动关闭

### 原始反馈

- 用户确认：
  - 本体功能正常；
  - 三个额外功能分别正常；
  - 组合开启正常；
  - 但疑似运行约 30 秒后自动关闭，之后没有钓鱼表现。

### 日志时间线

第一份日志：

- `11:15:22.576`：F6 开启，ProductNative session 建立；
- `11:16:19.017`：`Fishing synthetic-input provider failed`，
  `OverflowException`，距开启 `56.441s`；
- `11:16:29.351`：因玩家返回标题才正式停用，距开启 `66.775s`。

第二份日志：

- 第一次：开启后 `39.039s` 发生相同 Overflow；随后返回标题停用；
- 第二次：开启后 `23.101s` 发生相同 Overflow；随后返回标题停用；
- 第三次：`14.426s` 内没有 Overflow，因返回标题停用；
- 第四次：开启后 `7.932s` 发生相同 Overflow；随后返回标题停用；
- 第五次：`6.300s` 后由 F6 正常手动停用。

所有明确的 automation disable 原因只有 `ReturnedToTitle` 或
`hotkey F6`。日志没有“30 秒计时器关闭”、manual-move、依赖失效或
session-unavailable 停用。

### 根因审查

- `SkipMiniGame=false` 时，AutoFishing 给可见原生小游戏安装
  `DecideMiniGameInput` provider。
- provider 返回 `TapBonus` 后，`TryDecideMiniGameInput()` 会把当前
  小游戏句柄与 note index 加入 `HashSet<BonusTapKey>`。
- 精确已安装 Workshop DLL 的反编译结果为：

  `checked(RuntimeHelpers.GetHashCode(Handle) * 397) ^ NoteIndex`

- 普通对象 identity hash 乘以 `397` 很容易产生 32 位有符号溢出。
  异常被当前 catch 捕获后会执行 `session.ReleaseInputProvider()`。
- 该 catch 不会把 AutoFishing 的 `enabled` 改为 false，也不会释放整个
  session。因此表面状态仍可显示启用，但小游戏自动输入已经永久撤掉，
  循环会停在可见小游戏，直到 F6 重建 session、保存配置重新配置 lease，
  或发生存档/标题生命周期重建。

### 结论

- 这不是固定 30 秒自动关闭，而是“遇到首个需要登记的 bonus note 时”
  触发的事件型故障；不同轮次在 `7.932s` 到 `56.441s` 出现，正好解释
  玩家对约 30 秒的感受。
- 这是 AutoFishing ProductNative 的真实功能阻断，影响默认的“显示并
  自动完成小游戏”路线。
- 图四的八个 Runtime/Manager 错误与该停滞无因果关系。
- 先前短测或跳过小游戏路线可以绕过 bonus-note provider，因此不能反证
  当前缺陷。

### 后续验收门

- 聚焦 Unit：
  - 对 `BonusTapKey` 的任意合法 identity hash 证明哈希组合不会抛出；
  - 覆盖 bonus note 首次、重复 note、多个小游戏句柄；
  - provider 内部失败不得静默留下“enabled 但无 input lease”的半状态。
- 精确当前 Workshop 产品包、第五存档、`NoNativeSave` 短测：
  - `SkipMiniGame=false`；
  - 至少完成包含 bonus note 的多个真实小游戏并继续下一次抛竿；
  - 日志中没有 `Fishing synthetic-input provider failed`；
  - F6 停用、再次启用、返回标题后 session/input/animation lease 清理正确。
- 三个额外功能与组合只需做受影响的最小交叉复测；本缺陷不要求重跑完整
  Release 或 GC 长测。

## 当前审查结论

- 玩家手测的“基础与三个额外功能可用”保留为用户确认事实。
- AutoFishing 循环停滞已定位为 bonus-note identity hash 的 checked
  overflow；状态并未真的按 30 秒自动关闭，而是小游戏自动输入 lease
  被异常路径撤销。
- 当前 0.5.5 Manager 会真实显示全部订阅 Mod、顶层 Hook 和 retained
  diagnostics。用户对“高级信息本应隐藏”的记忆有依据，但 0.5.5 的
  已实施边界确实只隐藏了 Features/注册表/兼容性的一部分，范围偏窄。
- 本 Review 只记录问题与验收门，不声明修复完成。

## 2026-07-31 后续代码审查与决策选项

### 后续约束

- 用户明确把 Manager/玩家 UI 信息边界推迟，本轮不实施 UI 修正。
- 当前首要发布阻断是 AutoFishing 默认可见小游戏路线；不能发布一个会在
  正常钓鱼过程中静默半失效的产品。
- 本次后续仍是只读源码、冻结/订阅 DLL 和日志审查；未修改 AutoFishing、
  Runtime、Workshop 订阅目录、玩家配置或存档。

### 后续问题 6A：AutoFishing 精确构建差异、测试漏口与修正选项

#### 原始反馈

- “聚焦自动钓鱼问题。不能发一个用不了的。”
- 要求说明代码问题并提出至少两个修复方式，由用户决定。

#### 补充代码事实

- `FishingPrimitivesService.TryDecideMiniGameInput()` 先在空
  `HashSet<BonusTapKey>` 上查询当前 bonus note；provider 返回
  `TapBonus` 后，才在其 `try` 内调用 `MarkBonusTapped()`。
- `BonusTapKey.GetHashCode()` 的源码是
  `(RuntimeHelpers.GetHashCode(Handle) * 397) ^ NoteIndex`，没有显式
  `unchecked`。
- Author SDK 不使用产品普通 csproj 的默认溢出语义。它以 Roslyn
  `CSharpCompilationOptions(checkOverflow: true)` 构建正式 Advanced
  产品，因此正式 DLL 中该乘法变为 checked。
- 当前实际 Workshop DLL
  `DB7EAE628E97B46047EF888F5FBD48EDF86A969CC4FD4421C918FDBBD6A2B267`
  的反编译结果精确为：

  `return checked(RuntimeHelpers.GetHashCode(Handle) * 397) ^ NoteIndex;`

- 空 `HashSet` 的第一次 `Contains` 不必计算该 key 的 hash；第一次
  `Add` 才触发溢出。这解释了异常为什么进入 provider 的 `try/catch`
  并留下精确日志：
  `Fishing synthetic-input provider failed ... OverflowException`。
- catch 只执行 `session.ReleaseInputProvider()`，没有把 `ModEntry.enabled`
  改回 false、没有取消 updater，也没有释放 session。此后原生小游戏
  Hook 仍被调用，但 `TryGetInputProvider()` 永久返回 false；玩家状态
  看似“开启”，实际无法继续自动完成可见小游戏。
- 普通 Unit 工程只链接 `FishingDecisionEngine.cs`、
  `FishingProductContracts.cs` 和 `AutoFishingConfig.cs`，没有编译
  `FishingPrimitivesService.cs`。现有 Bonus 测试只证明决策返回
  `TapBonus`，没有执行正式 SDK 构建语义下的 `HashSet.Add`、重复 note
  或 provider 故障状态。

#### 因果结论

- 根因已经从“可能的 30 秒超时”收紧为一个可由正式构建语义解释的确定
  缺陷：首个需要登记的 bonus note 触发 checked identity-hash 乘法
  溢出。
- 日志中的 `7.932s`、`23.101s`、`39.039s` 和 `56.441s` 差异来自
  bonus note 出现时机，不是计时器。
- 六个 UI Hook、备用 coroutine、被拒的泰拉瑞亚包、PlayerLoop、
  F6 输入和 GC 均不是这次停滞的根因。反证之一是 PlayerLoop 在每次
  SaveLoaded/ReturnedToTitle 后都保持 `installed=True`，且小游戏
  Hook 已经运行到能触发 Product provider 的位置。

#### 修正选项

1. **A：最小 0.5.5 热修**

   - 把 `BonusTapKey.GetHashCode()` 的组合明确放进 `unchecked`；
   - 增加一个经 Author SDK checked 构建语义执行的聚焦测试，覆盖首次
     bonus、重复 note 和多个 game handle；
   - 用第五存档做一个包含真实 bonus note 的短 `NoNativeSave` 复测。
   - 优点：改动最小，保持当前状态机和行为；这是标准的 hash-code
     溢出处理。
   - 局限：将来 provider 的其他异常仍会留下“enabled=true、input
     provider=null”的半状态。

2. **B：最小根因修正加显式故障收口（建议用于发布）**

   - 包含方案 A；
   - 同时让小游戏输入故障通过 session 的明确 fault 回调/状态回到
     `ModEntry`，统一执行 `SetAutomation(false, reason)` 或等价的完整
     lease/session 清理，并向玩家显示可理解的停用原因；
   - catch 的边界覆盖 frame 去重查询、provider 和 bonus 登记整个事务，
     不能只包住 provider 后半段。
   - 优点：既修本次错误，也禁止未来再次出现“界面说开启、核心输入已经
     消失”的静默半状态。
   - 代价：比 A 多一个小型产品内部故障契约和对应状态测试。

3. **C：移除复合 HashSet key**

   - 把 bonus 去重改为“当前小游戏对象身份 + 当前 note index 集合”的
     有界 tracker，在小游戏停止、拉竿、session 释放和生命周期边界清空；
   - 可使用 reference comparer 的原始 identity hash，或小型线性 note
     集合，完全移除 `identityHash * 397`；
   - 仍应配合 B 的故障收口。
   - 优点：模型更贴近“只需记住当前小游戏已经点过哪些 note”，没有跨
     game 的复合 key 和无界累积风险。
   - 代价：改动面和生命周期矩阵大于 A/B，不适合作为只求最快发布的
     单行修正。

4. **D：临时禁用 bonus 自动点击或强制 SkipMiniGame**

   - 能绕过当前代码路径，但改变产品承诺，不能证明“自动完成小游戏”；
   - 只能作为紧急降级包，不应作为 0.5.5 正式修复。

#### 推荐与验收

- 推荐 **B**：A 的 `unchecked` 是根因修正，显式 fault 收口是防止同类
  静默失效的最小发布保险。若只接受最小字节变更，可选择 A，但必须把
  半状态债务明确留为 open。
- 不需要重跑完整 Release 或 GC。最小充分证据是：
  - Author SDK 构建后的真实产品 DLL 执行 bonus 去重聚焦测试；
  - 第五存档、可见小游戏、至少一个 bonus note、继续下一轮抛竿；
  - F6 关/开与返回标题清理；
  - 日志零 `Fishing synthetic-input provider failed`；
  - 若做 B，故障注入证明状态和所有 lease 一致回到关闭。

### 后续问题 7：Strict 规则会影响哪些玩家、能否在 0.5.5 放宽

#### 原始反馈

- 用户询问：其他玩家若订阅并启用“泰拉瑞亚物品包”，是否会失效；
  是否会阻拦所有类似直接安装的 DLL；0.5.5 能否先禁用这条尚未形成
  统一兼容规则的安全检查。
- 用户希望下半部路线加入对这类已发现 DLL 的卸载能力。

#### 补充现场与代码事实

- 是：在干净的 DTMAPI 0.5.5 玩家环境中，订阅并启用 Workshop
  `3759797170` 仍会被扫描到，但 manifest 在分类阶段被剔除，入口
  `Entry()` 不执行。该包本身因而不会完成安装。
- 若玩家曾在旧环境中成功运行过 installer，已经复制到
  `BepInEx/plugins` 的外部插件不会因本次 manifest 拒绝、禁用或退订
  自动消失；这种玩家可能继续运行一份遗留插件，而干净玩家则完全装不上。
- 失败按 manifest 隔离：`ModScanner` 对单个 manifest 的分类异常做
  catch、记录并继续。它不会阻止 DTMAPI、AutoFishing 或其他合法 Mod
  加载，但当前 Manager 会把该错误聚合为总体红色。
- 它不会阻拦“所有 DLL”：
  - `BepInEx/plugins` 下的第三方 BepInEx 插件位于 DTMAPI 管理之外，
    当前规则不阻止它；
  - 仅引用 `DTMAPI.Abstractions`/平台程序集的 Strict 入口可加载；
  - SDK receipt 与游戏构建绑定的 Advanced 产品可加载；
  - 三个 Catalog 精确允许的单入口 legacy Workshop 消费者可加载；
  - DTMAPI 管理目录中，入口自身含 Assembly-CSharp/Harmony/BepInEx/
    Unity 引用，或同包任意 sibling DLL 含此类引用的未知 Strict 包，
    当前都会被拒绝。
- 当前 Catalog 已知 `3759797170`，并把入口记录为 DTMAPI CodeMod、
  把 `Mxx_DolocTownMod_Plugins.dll` 记录为 External BepInEx Plugin；
  但 Runtime 的 `ValidateStrictAssemblyClosure()` 又把整个包下所有 PE
  当作 Strict 闭包检查，二者没有接合。
- 该包入口的实际代码本身只引用 DTMAPI/System。它在 `Entry()` 中把
  sibling `Mxx_DolocTownMod_Plugins.dll` 复制到共享
  `BepInEx/plugins`，提示玩家重启。被拒后，这次复制不会发生。
- 因此本例不是“DTMAPI 正准备直接加载 helper DLL”，而是一个 Strict
  installer + External plugin 的混合包被目录级闭包规则整体否决。

#### 能否禁用

- 技术上可以，但“全局关闭 Strict native-reference 检查”不是一个
  安全的 0.5.5 默认方案。它会把所有未知原生/Harmony 入口放进当前
  进程，而 Loader 不知道其 Harmony owner、静态状态、Unity 对象和
  回滚方法。
- 可以只放宽**目录级 sibling 闭包**，继续保留入口 DLL 的 Strict
  引用检查。这会修复本混合包类型，但也允许一个表面 Strict 的 installer
  用 `System.IO`/反射复制或加载 sibling 原生 DLL。Strict 从来不是完整
  安全沙箱，因此这是兼容性取舍，不是无风险等价替换。
- 已加载的 Mono 程序集不能在当前默认 AppDomain 中真正卸载。DTMAPI
  可以移除自己掌握的事件、输入、配置、API、已知 Harmony owner 等根，
  但未知 External 插件只能在下次进程重启后保证不再载入。

#### 处理选项

1. **S1：Catalog 精确混合包准入（0.5.5 推荐）**

   - 扩展现有 Catalog legacy admission，使它同时绑定 Workshop ID、
     UniqueID、installer 路径/hash 和 external payload 路径/hash；
   - 只对这组精确字节豁免 sibling 闭包，入口仍需满足 Strict 边界；
   - 明确投影为“兼容 installer + 外部插件、重启生效”，不冒充普通
     Strict 玩法 DLL，也不承诺进程内卸载；
   - 变更任一 DLL、额外增加 native sibling 或来源不是原生验证
     Workshop 时继续拒绝。
   - 优点：恢复已知玩家包，同时保持未知包 fail-closed。
   - 代价：每个混合包/版本需有精确 Catalog 维护和最小实机准入。

2. **S2：只检查 Strict 入口，不再检查同包 sibling DLL**

   - 删除或关闭 `ValidateStrictAssemblyClosure()` 对非入口 DLL 的拒绝；
   - 入口含 native 引用的“直接 DLL”仍被挡；本例这种干净 installer +
     external sibling 可以运行并复制插件。
   - 优点：规则简单、兼容更多历史混合包、不必逐包 allowlist。
   - 风险：包级原生闭包保证消失，Strict installer 可以成为外部原生
     payload 的通道。必须在 Manager/日志中标成 external/restart-required，
     不能继续宣称整个包都是 SDK160-safe。

3. **S3：原生验证 Workshop 的临时 legacy permissive 模式**

   - 对 `CodeModKind` 省略且来自原生验证订阅的历史包，允许入口自身含
     native 引用，隔离每个 Entry 失败并统一提示重启；
   - 优点：能兼容最多旧 DLL。
   - 风险：未知 Harmony owner、静态/Unity 状态和跨 Mod 冲突都进入进程；
     DTMAPI 无法证明卸载或回滚。它等价于显著放宽当前产品身份契约，
     不建议作为 0.5.5 默认。

4. **S4：保持当前硬门**

   - 发布风险最低，但已知混合包及同类玩家订阅继续失效；
   - 与“已扫描、已分类却长期禁用”的兼容目标不一致，只适合明确选择
     fail-closed 的版本策略。

#### 下半部卸载/移除路线

- 将“卸载”拆成两个可兑现动作：
  1. **载入前禁用/退订**：阻止下一次启动加载；
  2. **外部 payload 移除**：对 Catalog/安装收据能证明的精确目标文件，
     由玩家确认后移出 `BepInEx/plugins` 到可恢复隔离区，并提示重启。
- 只在目标路径、来源包、长度/hash 和预期安装关系全部一致时自动处理；
  文件已变更、可能被多个包共享或来源不明时拒绝删除，只给出路径和人工
  指引。
- 已载入 DLL 只能做 owner cleanup + restart-required，不能在 UI 中
  宣称“已从内存卸载”。
- 对本包尤其需要覆盖：启用后复制、禁用、退订、payload 更新、目标已被
  玩家改动、重复安装、移除后冷启动零载入。

### 后续问题 8：六个 UI Hook 与 coroutine 分析是否成立

#### 原始反馈

- 用户认为两组错误都是“新主路径完成后旧兜底没有退役，且错误分级错误”，
  建议删除六个泛型布局 Hook 和旧 coroutine，保留精确 UI 修复路径、
  主动布局修复、PlayerLoop/InputSystem/Update 与健康检查泵。
- 用户本轮明确推迟 UI 实施，只要求判断分析和提供处理方法。

#### 审查结论

- **整体判断成立，但它们是同一类债务，不是同一个技术异常。**
  - 六个 UI 错误分别是 open generic 的 `ResetLayoutSize`/
    `SetCapacity` 两项，以及 `TextButton`、`MenuButton` closed generic
    `ResetLayoutSize` 的 prefix/postfix 四项。
  - `TargetedRepairReady` 确实只依赖
    `HomePage.RenderTextMenu`、`MainMenuPanel.OnStartShow` 和
    `MenuUI.SetCapacity`；`UpdateActiveMenuLayout()` 每帧还会对当前
    HomePage/MainMenu 直接修正。
  - 现有 QA 明确构造“所有 generic Reset Hook=false，但 targeted
    repair=true”并要求状态为 ready。这是泛型路径不属于生产 readiness
    的直接证据。
  - `TryPatchClosedGeneric*` 当前只有该 UI feature 使用；若删除四个
    closed-generic 尝试，可以一并评估删除这组未使用 helper。
- coroutine 判断也成立：
  - `TryStartCoroutineLoop()`/`RuntimeCoroutine()` 来自较早的
    MonoBehaviour 帧驱动，启动流程在安装 PlayerLoop 和订阅
    InputSystem 后仍无条件调用它；
  - `TickFromUnity()` 在 `playerLoopFrameDriverInstalled=true` 时拒绝
    Coroutine、Update、FixedUpdate、LateUpdate 等普通来源，所以当前
    成功启动的 coroutine 也不会参与实际更新；
  - 若 PlayerLoop 标志仍为 true 但回调停滞，coroutine 仍被该标志挡住，
    也不能真正救援这一状态；
  - 250ms Timer 只是健康检查，不派发 Mod Update。它会重试
    InputSystem；生命周期边界会重装 PlayerLoop。应保留这些真实路径，
    但不能把 Timer 描述成普通帧驱动。
- 不能从当前日志断言 Unity 为什么拒绝反射
  `StartCoroutine(IEnumerator)`；外层只保留了
  `TargetInvocationException`。不过这不影响“本轮主驱动正常、该错误
  与 AutoFishing overflow 无关”的结论。

#### 处理选项

1. **U1：删除七条失效路径（建议在 UI 债务恢复时采用）**

   - 删除六个 generic Hook 安装、只服务这些 Hook 的回调/字段/状态文本
     和旧 coroutine 启动/枚举器；
   - 保留精确三 Hook、`GameDataPanel.SetCapacity`、主动布局修复、
     PlayerLoop、InputSystem、MonoBehaviour Update 降级和健康检查泵；
   - 把原“generic=false 仍 ready”的 QA 改成“源码/状态中不存在这些
     安装尝试”，继续验证标题与主菜单布局。

2. **U2：条件安装为真正的后备**

   - 只有精确 UI 修复未 ready 时才尝试 generic Hook；
   - 只有 PlayerLoop 安装失败且 InputSystem/Update 均不可用时才尝试
     coroutine；
   - 后备未被需要时发布 `not-required`，真正需要但失败时才记 Warning。
   - 优点：保留旧游戏构建兼容可能；代价是继续维护目前没有成功证据的
     反射/泛型复杂度。

3. **U3：0.5.5 仅停止尝试/降级诊断，后续再物理删除**

   - 用版本内开关不注册七条路径，并从玩家总体错误中移除；
   - 后续收集一次精确路径与 fallback 证据后再删源码。
   - 优点：发布改动最小；代价是死代码和错误模型仍留在仓库。

#### 推迟状态

- 本轮不实施 U1/U2/U3，也不把 UI/Bootstrap Update 改动混进
  AutoFishing 修复。
- 若后续选择 U1，最小验证仍应包括标题入口/布局、进入存档与返回标题后
  PlayerLoop 连续回调、F6/Manager 输入，以及这七项不再污染玩家错误数。

## 2026-07-31 AutoFishing 复测通过后的平台告警审查

### 新增手测事实与日志边界

- 用户确认 AutoFishing 本体、三个额外功能以及组合开启均可正常工作；本轮
  不再把 AutoFishing 作为这些告警的嫌疑来源。
- 当前 `latest.log` 覆盖约 489 秒，共 1,271 行：1,265 条 Info、6 条
  Warning、0 条 Error、0 条 Fatal。6 条 Warning 分别为 1 条
  ContentQuery、2 条帧驱动健康检查和 3 条重复 `SaveLoaded` 诊断。
- 游戏进程仍在运行，因此本节只证明这段在场运行窗口内没有错误、Fatal
  或 AutoFishing 停滞；尚不把最终退出、进程清理和 Steam waiting-for-exit
  记为本轮证据。
- 日志约 1.39 MB/8.15 分钟，折算约 9.8 MiB/h；其中大部分是对象图、
  lifecycle、Hook 状态和 delta 诊断。短时转场的瞬时速率明显高于稳定游戏。

### 后续问题 9：ContentQuery 与官方内容加载器行为不一致

#### 原始反馈

1. 已禁用的内容仍被解析，应跳过。
2. DTMAPI 比官方加载器严格：带 `//` 注释的 JSON 会被 DTMAPI 拒绝，
   但官方加载器接受。

#### 代码证据与根因

- `WorkshopContentInputUi.AddOfficialContentDirectory()` 先读取官方状态并计算
  `sourceEnabled`，但没有在枚举 `item_tbitem.json` 前做 eligibility gate；
  因此 disabled/unknown 来源仍会读取 info、扫描 PNG 并解析每个 item。
- item 文件继续调用平台通用的 `JsonFile.Read()`；该 reader 使用
  `DataContractJsonSerializer`，会拒绝行注释。它同时服务 manifest、配置、
  receipt 和其他 DTMAPI 权威文件，不能为了官方内容兼容而全局放宽。
- 当前反编译官方加载链只从 `ModManager.EnabledMods` 建立缓存；文件使用
  `SimpleJSON.JSON.Parse()`，而官方随附 SimpleJSON 明确启用
  `allowLineComments=true`。因此用户的两项判断均由当前游戏构建直接支持。
- 还存在第三处偏差：官方加载器按文件捕获异常并继续加载其他文件；DTMAPI
  会把单个第三方 item 的解析失败上抛到 generation，拒绝整代候选并保留
  last-good publication。截图中的 ContentQuery Warning 正是这条链，而不是
  AutoFishing 错误。
- 现有实验性 `IContentQueryHelper` 测试特意允许通过 `GetAny...`/
  `GetAll...` 看到 disabled item 行。因此提前跳过会改变一项实验性诊断行为；
  disabled Mod 的包级状态仍可由 `IDtmModStatusInfo` 提供，不必为此解析其
  gameplay 内容。

#### 修复选项

1. **C1：在 Core 内建立有界的官方输入兼容层（建议用于 0.5.5）**

   - 在读取 info、PNG 和 item 前先做 eligibility gate：官方明确 disabled
     或不在官方 enabled authority 中的来源不进入内容索引；DTMAPI 来源还需
     具备有效且启用的 owner。
   - 只为官方内容增加 `OfficialJsonCompatReader`，以字符串状态机跳过引号外
     的 `//` 注释，保留字符串中的 URL、转义和 BOM；不得修改通用
     `JsonFile` 的严格语义。
   - 按文件或来源隔离解析失败，记录一条有界诊断后继续构建其他有效来源；
     最终候选仍一次性发布，不能留下半代索引。
   - 更新实验性 API 文案/测试：disabled 包仍可见，但其 item 不再被主动解析。
   - 优点是改动小、直接贴合官方当前行为；代价是兼容 reader 必须用注释位于
     字符串内外、转义、BOM、坏文件和多来源隔离用例锁定。

2. **C2：索引官方已经合并完成的原生 `TbItem` 权威**

   - 在 GameBridge/native ready 后从游戏最终表建立查询索引，再结合 enabled
     路径补充来源信息。
   - 优点是语法、覆盖顺序与“真正加载了什么”天然服从官方结果。
   - 代价是 Core 查询时机变晚，来源/覆盖归因更难，并新增 SharedNative 边界；
     对 0.5.5 是明显更大的结构变更。

3. **C3：通过 GameBridge 调用官方 SimpleJSON 解析器**

   - 仍自行枚举 enabled 文件，但由 native adapter 调官方 parser，再转成
     DTMAPI DTO，并保留逐文件隔离。
   - 优点是语法最接近当前游戏；代价是 Core 内容发现依赖游戏程序集与 native
     ready 时序，且仍重复官方的文件枚举/合并逻辑。

#### 建议验收

- disabled、unknown、enabled 三种官方状态；带字符串内 `//`、行注释、BOM
  和损坏 JSON；一个坏来源与一个好来源并存；last-good 只在 DTMAPI 自己的
  publication 失败时保留，而不是被任意第三方坏文件整体劫持。

### 后续问题 10：`SecondSaveLoaded` 只能表示第二次，诊断器却要求全局唯一

#### 原始反馈

- 第一次载入为 `SaveLoaded`，第二次为 `SecondSaveLoaded`，第三次及以后又
  回到 `SaveLoaded`；诊断器规定再次看到 `SaveLoaded` 就报警。

#### 代码证据与根因

- `DtmApiRuntime` 在计数为 0 时发布 `SaveLoaded`，计数为 1 时发布
  `SecondSaveLoaded`，计数为 2 及以后重新发布 `SaveLoaded`。这不是日志
  时序猜测，而是当前条件表达式的精确结果。
- `LifecycleBoundaryContractService` 的 `phaseCounts` 跨整个进程累计，返回标题
  时不重置；任何第二次出现的 `SaveLoaded` 都被判为重复。
- 因此该模型最多能容纳两次载入，无法表达正常的第三次、第四次载入。当前
  三条 Warning 都是正常多轮载入触发的确定性假阳性。
- 项目已有 `SaveLoadRequestCoordinator` 跟踪 request、native enter/return、
  duplicate、timeout 和 SaveLoaded 完成；继续用 phase 名称另建一套全局唯一
  判据会形成重复且冲突的生命周期权威。

#### 修复选项

1. **L1：统一为可重复的 `SaveLoaded`，重复判定交还 coordinator（建议）**

   - 删除 `SecondSaveLoaded` 特判；每次成功完成都发布 `SaveLoaded`。
   - 诊断详情携带 `loadOrdinal`、request/boundary/save generation，而不是把
     次数编码进 phase 名称。
   - lifecycle contract 不再把正常重复 phase 当错误；同一 transaction 的
     重入或多次完成由现有 coordinator 报告。
   - 优点是删除错误权威，且资源生命周期当前本来就把两种 phase 等价处理。

2. **L2：保留“首次/后续”语义，但后续可以无限重复**

   - 首次为 `InitialSaveLoaded`，以后全部为 `SubsequentSaveLoaded`；后者不要求
     全局唯一，仍用 ordinal 区分具体轮次。
   - 优点是日志保留首次冷启动的可读差异；代价是一个并无产品语义的名称仍会
     扩散到策略和测试中。

3. **L3：建立按 transaction/boundary 编号的完整状态机**

   - 对 requested → native enter → native return/SaveLoaded → title return
     做逐事务验证，不再按进程级字符串计数。
   - 证明力最强，但与现有 coordinator 高度重叠；除非先合并两者，否则会新增
     第二套生命周期权威，不建议作为本次小修。

#### 建议验收

- 连续至少五轮“载入存档→返回标题”不得产生重复警告；同一 request 内真实的
  双分发、缺失 native return 和 timeout 仍必须各自被 coordinator 捕获。

### 后续问题 11：启动时两条帧驱动 Warning 是同一次停顿的两种误报

#### 原始反馈

- 两条帧驱动 Warning 只在启动时出现一次，需确认含义。

#### 代码证据与根因

- Bootstrap 的 250 ms Timer 是健康检查泵，不是 Mod Update 帧驱动。只有当
  native、PlayerLoop、InputSystem 和 MonoBehaviour Update 来源全部超过
  500 ms 未推进时，它才向 Unity 主线程排队一次 health check。
- 本轮日志先记录 InputSystem 已订阅、PlayerLoop 已 observed；约 6 秒后同一
  时间戳记录两条 Warning，稍后 InputSystem 又成功订阅。此后各来源持续推进，
  `updateFailures=0`。
- 第一条来自 health check：主线程恢复后只看 InputSystem 的旧时间戳，便将其
  退订并标为 stalled。实际是启动期间所有 Unity 回调同时停顿，不能证明
  InputSystem 单独失效。
- 第二条只是“第一次 fallback health check 曾运行”的一次性日志；它并不说明
  fallback 接管成功，也不是第二个故障。
- 退订后立即重订还会受到 10 秒订阅冷却限制；虽然本轮 PlayerLoop/native
  主路径仍正常，但在主路径真的缺席时会制造不必要的短暂后备空窗。

#### 修复选项

1. **F1：用相对进度判断单一驱动故障（建议）**

   - 排队时记录各 callback generation/count；回到 Unity 线程后若任何主路径已
     推进，则先重新评估而不是依据旧墙钟直接退订。
   - 只有 PlayerLoop/native/Update 仍推进而 InputSystem 单独停止时，才把它
     判为 InputSystem stall；所有来源一起停顿属于全局 pause/startup，不卸载
     任一来源。
   - 恢复成功只记 Debug/Advanced；持续失败或无可用主路径才记 Warning。

2. **F2：加入启动/加载宽限并降级一次性诊断**

   - 启动、载入和返回标题后的 10–15 秒内不执行 unsubscribe；首次全局停顿及
     随后恢复仅写 Debug/Advanced。
   - 改动最小，适合发布收口；但运行后其他全局卡顿仍可能被误判为单一路径
     故障，属于缓解而非根治。

3. **F3：移除冗余 InputSystem 驱动，只保留输入观测所需路径**

   - 以 PlayerLoop/native 为主、MonoBehaviour Update 为后备，删掉 InputSystem
     的驱动角色及恢复状态机。
   - 状态最简单，但会失去一条输入/焦点兼容冗余；必须重新覆盖 F6、Manager、
     主菜单、载入/返回标题和失焦恢复，不建议在 0.5.5 临近发布时直接采用。

### 后续问题 12：默认日志没有轻量级别门，Full 诊断成为玩家默认

#### 代码证据与影响

- `FileMonitor` 当前不按 MinimumLevel 过滤；每条消息都同步
  `File.AppendAllText` 并转发 host。虽然 API 定义了 Trace/Debug，生产链没有
  真正的默认级别策略。
- `saveLoadObjectSnapshotMode` 默认是 `Full`；载入/标题边界会发布完整对象图、
  delta 和 boundary ledger。Lifecycle、Hook queue/status 和 GameBridge health
  也大量以 Info 发布，即使没有状态变化。
- 本轮约 87% 行数属于这类内部诊断。既往 GC 故障栈还曾经过对象快照字符串
  拼接和 BepInEx 日志转发，因此它不仅是文件体积问题，也会增加字符串分配、
  主线程同步 I/O 和 GC 压力。
- 仅扩大轮转容量或减少 UI 显示行数不会降低生成、格式化和写盘成本。

#### 修复选项

1. **G1：复用现有模式做 0.5.5 轻量默认（最小方案）**

   - 玩家默认把对象快照从 `Full` 改为 `Lite`（若 Lite 仍超量则 Off）；Full
     只由 QA/环境开关/高级诊断显式开启。
   - lifecycle、Hook 和 health summary 只在状态改变、新诊断或边界结束时写一
     条紧凑 Info，不在同一轮 fanout 中重复发布。
   - 优点是无需先建完整日志框架即可删除主要体积；代价是仍没有统一级别门。

2. **G2：建立真正的日志级别策略（长期建议）**

   - `FileMonitor` 增加 MinimumLevel，玩家默认 Info；完整对象图、delta、contract
     trace 和 Hook fanout 全部改为 Debug/Trace。
   - 启动结果、Mod 状态、一次性生命周期里程碑保留紧凑 Info，Warning/Error
     永不被过滤；高级 UI 或启动配置允许下一次会话开启 Debug/Trace。
   - 优点是语义边界清楚；代价是需审计现有 Info 调用和相应测试，改动面大于
     单纯切换 snapshot mode。

3. **G3：玩家日志与有界诊断 trace 分流**

   - `latest.log` 保留轻量玩家事件；完整诊断进入有大小上限的 ring buffer 或
     单独 trace，仅在高级模式/错误窗口/“收集日志”时落盘。
   - 优点是故障时仍能保留上下文；代价是新增缓冲、轮转和崩溃尾部保留规则，
     若默认仍持续格式化完整字符串则不能解决全部 GC 成本。

#### 推荐组合与决策边界

- ContentQuery：**C1**。
- 生命周期：**L1**。
- 帧驱动：根治选 **F1**；若只允许最小发布补丁，可先做 **F2**，但需明确仍有
  晚期全局停顿误判债务。
- 日志：0.5.5 先做 **G1**；完整级别治理后续做 **G2**。G3 只有在明确需要
  “默认轻量但故障可追溯”且能承担保留协议时再上。
- 本节是分析/审查记录，未修改运行时代码，也未把任何方案记为已实施或通过。

## 2026-07-31 用户决策与实现归属

- 用户选择当前版本实施 **C1、生命周期最小诊断修正、F1、G1**。
- 用户明确不授权本版扩大生命周期重构；下一版本必须逐项讨论原生事实、产品
  需求、最小边界和验收后再实施。
- 用户选择下一版本冻结并轻薄化 ContentQuery、以 F3 替换 F1、以 G2 完成
  日志级别治理。
- Lite 的硬约束是 Error 仍保留完整异常和最小因果上下文；Full 只能增加连续
  状态证据，不能成为发现错误的前提。
- 实现生命周期、验证和回滚由
  [`20260731-0003 Update`](../../../../updates/2026/20260731-0003-contentquery-lifecycle-frame-log-closeout.md)
  所有；下一版本边界由
  [`20260731 简化路线图`](../../../../planning/20260731-runtime-query-lifecycle-driver-logging-roadmap.md)
  所有。本 Review 不回写实现 PASS 或运行结论。
