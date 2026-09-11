# NPC 玩家反馈：机械大剑领取失败与旧包拒载的区别

- Date: 2026-09-11
- Lifecycle: recorded
- Scope: `DTMAPI-logsNPC.zip`、玩家截图、Y 控制台领取路径，以及与玩家堆栈 MVID 一致的游戏程序集。
- Constraint: 本次请求是“看一下……是不是一样的”。只分析并记录；附件文字作为反馈证据，不作为操作指令。此前 BOM 修复、测试和上传目录同步已经完成，本记录不扩展其实施范围。

实施衔接：用户随后授权小范围修复、最小测试、升为 1.1.3 并同步上传目录，实施结果由 [Update 20260911-0010](../../../updates/2026/20260911-0010-y-console-113-item-id-case.md)维护；下文保留原研究事实与建议。

## 结论

**这次是物品领取时的大小写兼容问题，和 `dtmapi-package.json` 的 BOM 拒载不是同一问题。** 玩家当前 Y 控制台为 **1.1.2**，已经完成 Entry、打开界面并执行领取。失败物品 ID 为 `HTL_dj`、`HTL_djex`；游戏字符串版 `TryPlaceInBackpack` 会把 ID 转成小写，而物品表按区分大小写的键查询，随后把未生成的空物品传给对象版接口，触发空引用。

这一原因得到玩家失败堆栈、原样 ID、同会话其他物品成功领取，以及匹配游戏程序集的具体方法实现支持。没有取得玩家第三方包的物品定义和 DLL，也没有在本机运行该包，故不把静态定位写成实机修复验收。

| 现象 | 发生阶段与证据 | 与此前问题的关系 |
| --- | --- | --- |
| 此包较早日志中的 Y 加载失败 | 14:45–15:37 共 7 轮出现 `TypeLoadException: Could not resolve the signature of a virtual method`，未完成 Y 的 Entry | 与[前案 Y 旧版 ABI 失败](20260911-0004-player-070-subscribed-mod-compatibility.md)的特征相同；本 ZIP 没有旧 DLL，不能单凭这些历史日志重做逐字节版本认定 |
| 当前 Y 能打开，但机械大剑领取失败 | 晚间 4 轮均完成 Y 的 Entry，随后共记录 19 次 `item-give failed`，内层异常是 `NullReferenceException` | **运行中的物品发放失败**，不是加载器拒载，也不是旧 Y 类型签名失败 |
| 管理页提示 `Mod monitor reported an error` | 导出快照明确 `statusCode=runtime-diagnostic loaded=true` | 表示这个 Mod 记录过运行错误，不能读成“未加载” |
| 此前“更好的体验”不被发现 | 前案 `package-marker-invalid`，失败在包发现阶段 | 本 ZIP 的 11 份 DTMAPI 启动日志中该错误为 0；[BOM 修复](../../../updates/2026/20260911-0009-runtime-070-package-marker-bom.md)不会改变游戏发物品接口 |

## 输入与证据身份

| 输入 | 身份与范围 |
| --- | --- |
| ZIP 原件 | `D:\下载\DTMAPI-logsNPC.zip`，362,641 bytes；SHA-256 `7D4092995988FE44BF900766E591FB2335F13ED45CC7C1563B4E10D58B0E3573`；分析后复核未变 |
| 包内范围 | 21 个文件，解压总量 2,769,820 bytes；latest + 10 份历史 DTMAPI 日志、BepInEx、Unity、安装收据、导出摘要及最后一次领取记录；不含第三方 manifest/marker/DLL、物品表或存档 |
| 截图反馈 | 玩家先说“现在能叫出来了”，再说手持机械大剑“显示目标呼叫异常”；最后贴出 `[DTMAPI.DebugConsoleMod] Mod monitor reported an error...`。这符合先恢复控制台加载、再遇到物品领取错误的时间线 |
| 玩家 Runtime | `release-manifest.json` 为 `0.7.0 / 0.7.0.0`，`BuildCommit=1002ae052dee`，Core SHA-256 `8D118980460E57432A8F7085AFBA87C526264D040310F61142297BB4555E1E35`；收据指向原 r6，不是后续 BOM 修复构建 `7d26482a2a95`。此处是安装收据观察，不冒充玩家当前 DLL 原件采集 |
| 玩家 Y | 导出快照为 Workshop `DTMAPI.DebugConsoleMod`、version `1.1.2`、`loaded=true`；最新堆栈中的产品 MVID 为 `9ad06c0c8a254590b3f9928e9b7075de`。支持该玩家实际运行 1.1.2，但不能替代完整订阅树 hash 收据 |
| 游戏对照 | 本地保留的 build `25163613_public_604898`，`Assembly-CSharp.dll` SHA-256 `60489873C645886C5A523FD0D17C4D451A7D68DF133F552C6667501245110AC6`，MVID `16b7aac53e79446a9a149c81e1fb8590`，与玩家异常堆栈一致；仅作静态方法/IL 取证，未执行程序集 |

原始文件、[输入收据](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/input-receipt.json)、[逐进程摘要](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/session-summary.json)及[案件摘要](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/case-summary.json)保留在 ignored evidence。原生 IL 同样只留在本地证据目录，不进入产品或发布包。

## 玩家日志中的直接事实

最新 [DTMAPI-latest.log](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/DTMAPI-latest.log) 的关键位置：

| 行 / 时间（+08:00） | 观察 |
| --- | --- |
| L16–20，21:28:06 | 发现 12 个受管包，manifest scanner `errors=0`，依赖错误和警告均为 0 |
| L35–39，21:28:07 | `com.doloc.omnimechgreatsword` / “全能的机械大剑” 1.0.0，Workshop `3798517599`，完成 CommitTransaction 和 Entry |
| L67–76，21:28:07 | Y 的 Advanced 准入通过，Host 绑定完成，提示“Y键控制台已加载”，完成 CommitTransaction 和 Entry；游戏 build 漂移未阻止激活 |
| L377–380，21:29:31 | Y 界面实际打开 |
| L386–395，21:29:43 | 第一次物品错误：`HTL_dj`，请求 1、发放 0；`TargetInvocationException` 内层为游戏 `TryPlaceInBackpack(Item,bool)` 的 `NullReferenceException` |
| L396–413，21:29:44 | 同样堆栈再出现两次；[last-give](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/DTMAPI-state/debug-console-last-give.txt) 保留最后一次 `HTL_djex`、请求 1、发放 0 |

注意名称边界：日志证明“全能的机械大剑”代码入口已加载，也证明 `HTL_dj` / `HTL_djex` 已走到物品领取阶段；**现有日志没有把这两个物品 ID 的内容来源绑定到该 Workshop 条目**，不能仅因名称相近就认定它们必由这个 DLL 定义。物品数据是否进入原生表，与某个受管代码入口是否加载，是两种证据。

对照并非只看“界面能打开”：

- 19:21:13，`recipe_drone_sword_old_sword` 成功增加 1 个，背包计数 1 → 2。
- 20:26:55，`gold_pickaxe` 成功增加 1 个，背包计数 0 → 1；约三秒后 `HTL_dj` 报空引用。
- 全部日志共有 35 条成功发放记录，涉及金镐、配方、垃圾收集器、垃圾粉碎机、废料框架、废金属和煤炭，对应 ID 均为小写。明细见[成功领取记录](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/successful-gives.json)。这排除了“领取功能全面失效”，不扩张为所有小写或所有第三方物品均已测试。

## 精确调用路径与责任边界

Y 产品的 [GiveItem](../../../../products/first-party/DebugConsole/src/Native/DebugConsoleNativeActions.Core.cs)（L335–437）先用原始 ID 查询 `QueryItemProto`，再调用 `CanPlaceItem(string,int)`，通过后才调用 `TryPlaceInBackpack(string,int,bool)`。因此玩家错误已经越过包准入、原生物品表查询和预先容量检查。

匹配游戏程序集的[身份](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/native-identity.json)与[静态 IL](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/native-methods.il.txt)显示：

1. `QueryItemProto` 原样把键交给 `TbItem.GetOrDefault`；该表使用默认字符串字典，区分大小写。
2. `CanPlaceItem(string,int)` 原样生成物品，所以 `HTL_dj` 能通过此前检查。
3. **`TryPlaceInBackpack(string,int,bool)` 在 `IL_0001` 调用 `String.ToLower()`**；`HTL_dj` / `HTL_djex` 因而变成 `htl_dj` / `htl_djex`。
4. 该重载丢弃 `ItemFactory.GenerateItem` 的成功/失败返回值，继续把 `item` 传入对象版重载。
5. 原生 `LinearInventory.CanPlaceIn(null)` 返回 true；对象版随后在 `IL_000d` 开始读取 `item.count`，没有空值保护。玩家堆栈报告的正是对象版 `[0x0000d]`，与该失败位置吻合。

这条链最直接地解释“列表里看得到、点击才失败、其他物品可领取”。问题在 **Y 产品所选的原生字符串重载对大写 ID 不兼容**；没有证据指向 Runtime 的严格拒载、未知依赖或需再重启一次。源码比较还确认：0.6.1 历史提交 `db5e518a6d7f` 与当前源码的整个 `GiveItem` 方法相同，见[定向比较](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/give-route-history.json)。它不是此次 0.7.0 marker 校验新加的行为；本次没有证明同一机械大剑包在旧环境中的实测结果。

`TargetInvocationException` 是反射包装异常，不是根因。当前 [UI 领取结果](../../../../products/first-party/DebugConsole/src/Ui/DebugConsoleUi.Actions.cs)优先展示 `FailureReason`，而产品 catch 使用外层异常类型，因而玩家只看到“目标调用异常”一类提示。完整内层堆栈已经记入日志。

另外，[FileMonitor](../../../../src/DTMAPI.Core/Logging/FileMonitor.cs)会把 Mod 的 Error 日志转成 `Mod monitor reported an error`；[RuntimeSnapshotFactory](../../../../src/DTMAPI.Core/Runtime/RuntimeSnapshotFactory.cs)把它分类为 `runtime-diagnostic`。本案[导出快照](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/DTMAPI-runtime-context.txt) L63/L69 明确保留 `loaded=true`，不能按旧案的“需重启或检查依赖”处理。

## 补查：游戏是否默认小写，原生控制台是否同样失败

用户追问缺陷为何到现在才发现、是否源于游戏接口，以及游戏是否默认小写 ID。本轮继续静态取证，不启动游戏。

**实现和原生数据都支持“这些发物品入口以小写 ID 为默认前提”。但这与“官方明确禁止 Mod 使用大写 ID”不是同一事实。**

- 当前匹配 build 的原生 `item_tbitem.json` 共 990 个条目，含 ASCII 大写字母的 ID 为 0，统计和文件 hash 见[原生 ID 统计](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/native-item-id-case.json)。这是该 build 原生表的结论，不包含玩家 Mod，也不代表游戏所有种类的 ID。
- `ItemInfo` 反序列化只确认 `id` 是字符串，并原样赋值；`TbItem` 原样按区分大小写的键存储，没有将大写拒绝或统一转换。这解释了 Mod 的大写 ID 为什么能进表、被 Y 列出来。
- 已查本地 2026-07-15 官方文档快照的[新增道具说明](../../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260715/pages/OFS1w4gFSiDkHRkXX1Pcu4ihnNh/content.md)和[新增道具综合示例](../../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260715/pages/KWqewbBLEiRw1Pkcn6lcZpQ7nXg/content.md)：示例为小写 `fertile_soil`，未见必须全小写的明文条款。快照中的正文和嵌入 TSV 也未检出“大写 / 小写 / 大小写”等约束；这不冒充对当前在线文档所有材料的穷尽证明。

进一步检查匹配程序集中的原生 `CommandAttribute` 和调用目标，结果见[原生物品命令](../../../debug/evidence/PLAYER-SUPPORT-20260911-NPC-MECHANICAL-SWORD/native-item-console-commands.json)。`CommandSystem` 收集这些带特性的命令并注册到控制台执行器：

| 原生命令 | 对 ID 的处理 | 只有 `HTL_dj`、没有 `htl_dj` 时的静态结论 |
| --- | --- | --- |
| `genitem` | 自己先 `ToLower()`，然后 `ItemFactory.GenerateItem`；检查生成返回值 | 不能领取；显示生成失败、物品可能不存在。这个入口有错误处理，不应说它也必然空引用 |
| `obtain_item` | 同样先转小写、检查生成是否成功 | 不能领取；正常走失败提示，成功路径还会触发获取事件 |
| `try_place_in_backpack` | 委托 `DolocAPI.TryPlaceInBackpack(string,int,bool)` | 与 Y 所用接口一致，会进入已定位的空物品异常路径 |
| `place_in_backpack` | 委托同一个字符串重载，启用溢出邮件参数 | 同样无法避开转小写和空物品问题；邮件参数不修复 ID 查找 |

因此，对这两个大写 ID，**原生常用生成命令也不能正确获取**；区别在于 `genitem` / `obtain_item` 能报告查找失败，而 Y 所选接口继续使用空对象。以上是匹配代码的确定调用路径及其条件推导，没有标记为本次游戏控制台实测。也不能由此推断合成、掉落、购买和所有对象版入口都无法获得该物品。

责任需要拆开说明：游戏这些入口依赖隐含的小写约定，表加载与调用之间缺少统一规则；字符串版 `TryPlaceInBackpack` 忽略生成失败并访问空对象，是明确的错误处理缺陷。Y 作为面向 Mod 的物品浏览器，列出原始 ID 并标记可领取，却没有避开这个限制，且只展示外层反射异常，仍有自己的兼容和诊断责任。不能仅用“原生接口如此”解释为 Y 无需处理，也不能在缺少明文规则时直接认定第三方作者违规。

## 补查：此前测试为什么没有暴露

不是没有做过领取测试，而是没有覆盖触发条件。沿[1.1.2 最终验收记录](../../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)核对真实日志：

- `GAME-SMOKE/20260831-145440/DTMAPI-latest.log` L329/L333：真实左键领取 1 个、右键领取 10 个，均为全小写 `old_pickaxe`。
- 同一日志 L468–469：G5 库存动作给的是全小写 `wood`，并明确记录 **`modItem=not-found`**。此前 G5 单轮 `20260831-143221` L315 也是 `wood` / `modItem=not-found`。
- [QA 选择逻辑](../../../../src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/DebugConsoleFixture.cs) L492–589 优先选择 `wood` 等原版物品；Mod 分支优先 `mod_butter`，找不到则允许跳过，仍记 `DebugInventory OK`。因此“动作组 PASS”并不证明该轮测过第三方物品，更不证明测过含大写字母的物品 ID。
- [原生 trace 检查](../../../../tools/scripts/test-dtmapi-060-debugconsole-native-trace.ps1)主要检查既定 Hook、天气、消耗参数、生成及 UI 边界，没有核对 `TryPlaceInBackpack` 的 ID 转换；已查的 DebugConsole 定向测试也没有大写物品领取案例。

这些历史 PASS 在其实际范围内仍成立。缺口是 **第三方物品分支可缺席，且没有独立覆盖原生数据未出现的大写 ID**；不能把“Y 已包含在版本验收中”扩张成“所有 Mod 物品的领取兼容性已验收”。本次反馈才提供这两个大写 ID 的真实失败，并不意味着缺陷刚由 0.7.0 引入。

## 最小修复方向与待验收范围

建议修复 **Y 控制台 ProductNative 的领取路径**：复用已经查到的原始物品 proto，按当前分批数量生成物品对象，检查生成结果，再调用 `CanPlaceItem(Item)` / `TryPlaceInBackpack(Item,false)`。这样保留原始 ID，避开字符串重载的转小写，同时保持原生容量检查、领取提示和不发溢出邮件的现有语义。反射边界同时解开内层异常，给出具体失败原因；单独改提示不能修复领取。

无需放宽包准入、改 BOM reader 或全局 Hook 游戏的发物品方法。也不应把现有第三方物品 ID 强行改名为小写：应保留它与现有配方和存档引用的对应关系。

若后续实施，必要验收聚焦于：

- 保留大写的 `HTL_dj` / `HTL_djex`，或等价的原生内容 fixture，验证发放的是正确物品；混合大小写和全小写键并存时不能发错物品。
- 同时验证普通小写物品、数量分批、容量不足、空生成结果和内层异常展示；容量不足不转成邮件，不重复发放。
- 使用最终 Y 包按产品验证流程进行 `NoNativeSave` 实测，确认领取数量、来源和干净退出。合成 fixture 不能替代尚未取得的玩家第三方包验收。

## 本轮完成边界

完成只读日志分流、11 轮历史归纳、当前源码及旧提交定向比较、匹配游戏程序集的静态 IL 核对，并记录独立原因。没有启动游戏、执行第三方代码、改存档、修改产品/Runtime 源码、刷新订阅或改上传目录；没有新增游戏 smoke PASS。文档收尾执行 `tools/scripts/check-doc-governance.ps1`，结果留在同案证据目录。
