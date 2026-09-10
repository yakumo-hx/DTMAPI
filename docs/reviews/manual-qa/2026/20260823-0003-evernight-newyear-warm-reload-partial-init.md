# 长夜节年夜饭在泽尼瑟支线后暖读档半初始化审查

## Review Header

- Time: `2026-08-23`
- Status: `recorded`
- Source:
  - 用户提供的 5 张现场与聊天截图；
  - 只读玩家支持包 `D:\下载\DTMAPI-logs寂静无声\DTMAPI-player-support-20260823-162116-155-049d7aba`；
  - 用户单独保留的 `D:\下载\DTMAPI-logs寂静无声\doloc-save-1.data`。
- Scope: 解释泽尼瑟“酒馆帮工”支线、返回标题读档与长夜节年夜饭异常之间的关系；判断当前及滚动备份存档状态；不修改游戏、模组、配置或任何存档，不实施兼容补丁。
- User constraints: 先理解实际问题；判断现存存档是否损坏、是否需要上一或上上次备份；只有静态证据不足时才请求人工复现。
- Related review/update/debug records:
  - [1.00.05 全量反编译与资源快照](../../../archive/updates/2026/20260823-0002-public-10005-full-reverse-capture.md)
  - 官方 Steam `1.00.03` 已知问题与 `1.00.04` 修复公告：<https://steamcommunity.com/app/2285550/allnews>
- Files/docs inspected:
  - 支持包的 `collection-summary.txt`、当前 `Player.log`、`BepInEx/LogOutput.log`、DTMAPI `latest.log`、Runtime/官方 Mod 状态与槽位 1 的 current/prev0..prev4/`.bak`；
  - 单独保存的 `doloc-save-1.data`；
  - `references/doloc-town/reverse/builds/24788406_public_F06183` 中的 `FestivalState.cs`、`Npc.cs`、`NpcTaskRenderController_Lightman.cs`、`Room.cs`、泽尼瑟好感剧情、年夜饭剧情与任务资源；
  - 较早公开 build 与当前 build 的上述原生路径差异；
  - 玩家实际部署的 `DolocNoWeedsMod.dll` 反编译结果；
  - `PROJECT.md`、Debug 索引、反馈转约束与文档治理规则。
- Not inspected:
  - 未启动游戏或执行运行时复现；
  - 未取得或反编译外部 `《多洛可小镇》增强功能 Mod 内置版 by Qiuzy 1.6.1` 的实际 DLL；
  - 未把解密后的存档正文写入磁盘，全部存档检查均为只读、内存内解析；
  - 未验证冷进程从同一备份进入年夜饭是否必然成功，因而“暖进程残留是必要前提”仍需一组对照才能最终证明。

## Issue Review

### Issue 1: 泽尼瑟支线后返回标题读档，年夜饭只初始化一半

Original feedback:

- “长夜节无法正常触发年夜饭。”
- 玩家先完成泽尼瑟好感剧情，在游戏内正常返回标题并重新载入；之后睡到晚间以避开支线触发时间，再进入酒馆触发年夜饭。
- 年夜饭场景中泽尼瑟悬空、其他 NPC 错位，无法正常交付材料；返回标题读档后位置回到农场。
- 玩家随后继续睡过当晚并认为当前存档“坏了”，希望判断是否应读取上一或上上次备份。

Screenshot/log transcription:

- 截图 1：游戏 `v1.00.05`，`X32年烈旱季28日` `20:55`，地点为“泽尼瑟酒馆”；年夜饭装饰已出现，但 NPC 密集重叠或处于异常位置，泽尼瑟位于吧台后的异常高度，当前交互提示指向“澳柯玛”。
- 截图 2：局部画面显示多个 NPC 重叠、坐卧位置混乱，当前交互提示指向“加百列”，右下出现空白虚线 UI 框。
- 聊天截图确认：本次不是手工替换存档后的首发，而是在同一游戏进程内“泽尼瑟支线 -> 返回标题 -> 读档 -> 睡到晚间 -> 进入酒馆”；玩家后来又继续睡到下一年第一天。
- 支持包采集完成，`FilesCopied=44`、其中存档 `28` 个，采集器无 warning/error。
- 同一进程共发生 6 次槽位载入和 7 次返回标题；直到最后才正常退出进程。首次载入中运行了 `zenis_favorability2`“酒馆帮工”，后续两次独立进入年夜饭均复现同一个官方异常。
- 泽尼瑟剧情把时间推进至 `18:00` 时，原版先后两次在 `CharacterRenderer.set_positionWS -> BaseDialogueTargetObject.WalkTo -> CommandDefines.WalkToTarget` 抛出 `NullReferenceException`，但 Yarn 命令链继续执行；后续将灯男设置到站街点时又记录“没有站街点”。
- 第一次年夜饭于墙钟约 `13:53` 触发，第二次于约 `13:58` 触发；两次都在命令 `start_festival evernight_newyear` 内得到相同调用栈：
  - `NpcTaskRenderController_Lightman.OnRenderNpc`
  - `Npc.RenderNpc`
  - `Npc.set_Renderer`
  - `Npc._SetRendererStatusFromCurrentScene`
  - `Npc.ManualSetPosition/ManualSetToMarkPoint`
  - `FestivalState.InitNpcInFestival`
  - `FestivalState.InitFestivalState`
  - `FestivalState.AppendFestival`
  - `DolocAPI.WaitToEnterFestivalState`
  - `CommandDefines.StartFestival`
- 异常后日志仍出现对 NPC 的交互尝试，并有 4 次 Dialogue VM 空引用及缺少/不在场 NPC `kenenimuu` 的记录；这与画面中的“节庆装饰和部分 NPC 已到位，但参与者、站位、对话状态不完整”一致。

Review record:

- User-confirmed facts:
  - 泽尼瑟好感剧情在年夜饭前运行过；剧情结束后没有通过睡觉完成原生保存，而是正常返回标题，再从已有存档重载。
  - 玩家为避开泽尼瑟剧情的白天触发时段，读档后睡到夜间再去酒馆。
  - 年夜饭画面异常且无法正常交材料；随后玩家继续睡过该晚并产生了更新的当前存档。
- Screenshot/log observations:
  - 可见问题不是纯 UI 偏移：官方节庆初始化命令明确在灯男渲染控制器内中断。
  - `FestivalState.InitFestivalState` 已按顺序改动部分 NPC 后才到达灯男并抛错，没有事务或失败回滚；因此同一帧留下节庆半初始化状态。
  - 同一暖进程中第二次进入仍在同一灯男路径失败；返回标题和重新载入没有恢复到一个可成功初始化年夜饭的资源状态。
  - 最终一次载入没有再进入酒馆或调用年夜饭命令，而是从 `20:20` 连续睡眠：`23:45` 完成一次原生保存，随后跨年到 `Y3 M1 D1 06:00` 再完成一次原生保存。
- Code/doc facts inspected:
  - 当前 `NpcTaskRenderController_Lightman.OnRenderNpc` 直接取得 `LightManFishingRope` 并立即访问其 `transform`、挂到 NPC Renderer 下，没有空值恢复或延迟重建。
  - 当前 `FestivalState.InitFestivalState` 顺序摆放节庆 NPC；任一 NPC 的渲染资源异常会使整条初始化命令退出，既没有预检，也没有撤销已经设置的参与者。
  - 当前 `Room.OnEnterRoom` 只有在 `FestivalState` 已存在时才抑制常规到达事件；它不能保护“尚未建立 FestivalState、正在执行 `start_festival`”的失败边界。
  - 从较早公开 build 到当前 `1.00.05`，官方增加了缺少普通 `NpcRenderer` 时的补取与部分房间事件抑制；灯男附属 `LightManFishingRope` 的初始化和节庆事务边界未获得对应保护。这个差异与“公告修过冲突，但该路径仍漏修”一致。
  - `npc_favorability@zenis4_1` 仅在周日 `10:00-14:59` 抵达泽尼瑟酒馆时启动 `zenis_favorability2`；玩家失败时的存档时间为 `20:20`，所以当晚不是泽尼瑟支线与年夜饭同时通过到达事件合法触发。
  - `DolocNoWeedsMod.dll` 只尝试 Patch `IDungeonResourceHost.GenNewDungeonResource` 与 `IVegetationHost.GenNewVegetation`，并仅在农场房间跳过生成；没有节庆、NPC、灯男、对话或房间到达补丁。
  - DTMAPI 该次运行只记录生命周期/保存通知，失败栈没有进入 DTMAPI；四个 DTMAPI Mod 文件夹中只有 NoWeeds 尝试加载，且其 `PatchAll` 因接口宿主解析异常失败。其余 DTMAPI Mod 未启用。
- Codex inference:
  - **已证实的直接故障**：原版 `start_festival evernight_newyear` 在初始化灯男时空引用，年夜饭因此只初始化一半；这直接造成 NPC 错位/重叠、泽尼瑟不可正常交互与材料提交失败。
  - **高置信因果前置**：同一进程稍早运行并中途出现 NPC 移动空引用的“酒馆帮工”剧情，随后只返回标题而未重启进程，留下了没有被读档完全重建的 NPC/渲染对象池状态；灯男的附属钓鱼绳资源最符合下一次节庆初始化所缺的对象。相同暖进程内两次同栈失败支持该判断。
  - **仍需对照的精确边界**：尚未证明暖进程残留是失败的必要条件，也未证明外部 Qiuzy 增强 Mod 完全无贡献。一次“完全退出后，以同一 `20:20` 备份冷启动进入酒馆”的隔离对照即可区分冷存档问题与暖生命周期问题。
  - 官方 `1.00.04` 所述“年夜饭期间意外触发支线”修复覆盖的是部分事件分发/普通 Renderer 恢复，不覆盖本次“先跑支线并暖读档，再创建节庆状态时灯男附属资源为空”的路径；因此这属于同一问题族的漏修，而不是公告中的修复毫无作用。
- Ownership:
  - 直接异常位于官方 `DolocTown` 的剧情命令、节庆状态和灯男原生渲染资源生命周期。
  - 若 DTMAPI 后续提供兼容修补，应作为有界的官方内容兼容 ProductNative/外部兼容补丁处理，不能把原始问题记为 DTMAPI Runtime、公共 API 或 Strict CodeMod 故障。
  - 外部 Qiuzy 功能 Mod 是待冷净对照排除的混杂变量；现有证据不足以给它定责，也不应因其存在而忽略已经捕获的官方空引用。
- Root-cause hypotheses:
  - 已证实：灯男 `OnRenderNpc` 空引用使 `FestivalState.InitFestivalState` 中途退出，且没有回滚。
  - 高可信：泽尼瑟剧情的异常 NPC 移动/隐藏/重定位与返回标题后的不完整资源清理，使暖进程中的灯男附属渲染资源处于不可重新初始化状态。
  - 待对照：冷进程从同一 `20:20` 存档是否成功；移除所有功能 Mod 后暖链是否仍复现。
- Rejected/unproven hypotheses:
  - “当晚 `20:20` 泽尼瑟支线又和年夜饭同时触发”：任务条件限定 `10:00-14:59`，存档与日志均不支持。
  - “当前文件保存了半初始化的 FestivalState，所以每次必坏”：最终两次保存发生在最后一次载入且没有进入节庆；当前存档没有本次年夜饭的进行中状态。
  - “所有存档字节已经损坏”：current 与 prev0..prev4 均能完成原版 AES 解密、JSON 解析及主要根区段读取；任务集合也跨代连续。
  - “需要使用 `.bak` 才能救回”：该 `.bak` 是 `2026-07-21` 的删除备份，不是本次滚动保存链。
  - “DTMAPI 直接导致异常”：失败栈和原生状态变更均不经过 DTMAPI；当前没有对应证据。
  - “功能 Mod 已被完全排除”：NoWeeds 可从代码边界排除直接作用，Qiuzy 增强 Mod 尚未完成净环境对照，不能扩大结论。
- Required downstream updates:
  - 本轮不实施修复，不创建 Update。
  - 若用户授权兼容修复，先以本 Review 为根因入口建立一个实现 Update；最小候选边界应包含灯男附属资源空值恢复、节庆初始化失败回滚/重试或进入节庆前的资源重建，不能只再次抑制房间到达事件。
  - 修复前需新建对应官方内容 Debug issue，记录冷/暖、带/不带功能 Mod 的最小矩阵及原版调用栈；只有实际游戏运行才追加 smoke 行。
- Acceptance checks:
  1. 只读保存并校验 current、prev1、prev2、prev4 的原始 hash；测试复制到与玩家 Steam AutoCloud 隔离的一次性存档夹具，绝不在玩家在线槽位上做回写恢复测试。
  2. 冷对照：完全退出游戏后，以 `prev1`（`Y2 M4 D28 20:20`）启动，直接进入酒馆，不睡觉、不保存；记录年夜饭是否完整初始化及是否出现灯男同栈异常。
  3. 暖链：以 `prev4`（`09:10`）在隔离夹具触发并完成 `zenis_favorability2`，不做原生保存而返回标题，重载并推进至 `20:20`，再进入酒馆；记录剧情移动异常、资源状态与节庆结果。
  4. 若冷对照也失败，在隔离夹具中停用所有功能 Mod 后重复最小冷对照；若只有暖链失败，则再做带/不带 Qiuzy 的一对暖链以归因混杂项。
  5. 任何补丁验收必须证明：年夜饭全部 NPC 到位；泽尼瑟可交材料；没有 `Lightman.OnRenderNpc`、Dialogue VM 或缺少参与 NPC 异常；返回标题后同一进程再次载入仍可重进；退出无残留进程。
- Blocker conditions:
  - 没有冷/暖隔离对照前，不能把“暖读档资源残留”从高置信推断提升为唯一根因，也不能公开宣称所有功能 Mod 无关。
  - 没有真实游戏日志和完整 NPC/交付验收前，不能把兼容补丁标为 solved/verified。
  - 禁止把玩家 live Steam AutoCloud 槽位通过测试后 `Copy-Item` 回写当作隔离策略；需要原生保存的暖链必须使用一次性、脱离云同步的夹具。

#### 2026-08-23 空引用对象与稀有触发条件细化

- 日志把 `NpcTaskRenderController_Lightman.OnRenderNpc` 的异常偏移记为 `0x00016`。当前 `1.00.05` DLL 的原始 IL 在该偏移依次为：读取 `_fishingRope`，随后调用其 `Component.transform`；灯男普通 `NpcRenderer.transform` 的访问要到 `0x002c` 才发生。因此本次空引用可以精确限定为 `EntitySystem.Next<LightManFishingRope>()` 没有返回一个仍有效的灯男钓鱼绳对象，不是灯男本人的普通 Renderer 为空。
- 同一进程稍早执行 `zenis_favorability2` 时，`21:00` 的 `hide_all_npcs_at_scene -> set_pos lightman` 成功。`set_pos` 的 `force=true` 路径会强制给灯男创建 Renderer 并调用同一个 `OnRenderNpc`；这证明本次进程启动时钓鱼绳 manager/prefab 原本可用，否定“该安装从启动起就缺少资源”。
- 正常灯男退场会经过 `NpcTaskRenderController_Lightman.OnUnRenderNpc`：先回收 `_fishingRope`，再把它从通用 `NpcRenderer` 子层级移回钓鱼绳专属容器。只有这条专用退场路径维护两套对象池之间的父子关系。
- 返回标题的原版 `DolocAPI.ReturnHome` 改走 `EntitySystem.Clear(force: true)`；`GameEntitySystem.ForceClear` 直接遍历各 manager 并调用 `RecycleAll()`，不会先调用每个 NPC 的 `TaskRenderController.OnUnRenderNpc`。如果灯男在返回标题边界仍处于渲染状态，通用 NPC Renderer 与仍挂在它下面的钓鱼绳会分别登记进各自对象池，却没有执行专用 reparent。之后通用 Renderer 的复用/回收可以使这个子对象失效，而钓鱼绳池仍保留其包装引用；下次灯男显形时 `Next<LightManFishingRope>()` 就可能交回无有效 Unity 对象的条目。
- 本次剧情恰好制造了这个稀有状态：它多次隐藏/强制重显 NPC，`21:00` 明确强制渲染灯男，`02:00` 又在玩家当前所在的 `city_多洛可商店街` 设置灯男站位，紧接着玩家返回标题；读档后年夜饭又保证必须重新渲染灯男。普通冷启动直接去参加年夜饭时，钓鱼绳池是新建的；普通场景离开若完整经过灯男专用 `OnUnRenderNpc`，钓鱼绳也已回到正确容器，所以绝大多数玩家不会触发。
- 这说明漏洞不是年夜饭每次都会发生，而是常驻代码中的条件性生命周期缺陷。年夜饭只是本现场“首次强制重新渲染灯男”的触发点；理论上任何“灯男仍渲染 -> 返回标题但不退出进程 -> 对象池发生复用 -> 再次渲染灯男”的链都可能更早触发。
- 证据等级：空对象身份、正常/强制清理代码差异和本次剧情顺序均已由 IL、源码与日志证实；“失效条目具体是在后续哪一次通用 Renderer 复用时被销毁”仍缺运行时 pool identity/parent 快照，属于高置信机制而非已逐帧观测事实。冷/暖复现时应记录 `LightManFishingRope` manager 的 active/cache 数、实例 Unity-null 状态、父对象 identity，以及灯男 Renderer identity，才能关闭最后这个微观缺口。

### Issue 2: 当前和滚动备份应如何判断

Original feedback:

- “这个存档目前也不知道是否有问题，是否需要读上次或上上次保存的备份存档。”

Screenshot/log transcription:

- 槽位 1 六代文件均属于同一原生滚动保存链：

| 文件 | 游戏内时间 | 场景 | 结论 |
| --- | --- | --- | --- |
| `prev4` | `Y2 M4 D28 09:10` | 农场 | 完整复现“支线 -> 暖读档 -> 年夜饭”所需的最早点 |
| `prev3` | `Y2 M4 D28 14:20` | 农场 | 支线白天时间点附近，但不是最小救档首选 |
| `prev2` | `Y2 M4 D28 17:20` | 农场 | 年夜饭前、时间更宽裕的恢复候选 |
| `prev1` | `Y2 M4 D28 20:20` | 农场 | 年夜饭前的直接恢复/冷对照首选 |
| `prev0` | `Y2 M4 D28 23:45` | 农场 | 已接近跨年，通常不适合作为年夜饭恢复点 |
| current | `Y3 M1 D1 06:00` | 农场 | 结构可读，已跨年并错过本年度年夜饭 |

- 单独的 `D:\下载\DTMAPI-logs寂静无声\doloc-save-1.data` 与支持包 `prev1` 字节完全相同，长度 `2458443`，SHA-256 为 `035EAF4A2AB9AE99C8A16265722284C01263E37ACCAEFCEBDC0223C27ED48C6D`。
- 关键滚动文件：
  - current SHA-256 `A9A007B30A5600680BB91FEB63453529CB6F2283CC4CBEED87E940324E3DD452`；
  - `prev0` SHA-256 `ADCB8F631AFDA3DA0D41605455B440589027C690A5F3138FCCDB8E636C53EBC5`；
  - `prev1` SHA-256 如上。
- 六代共同保留 `npc_favorability@zenis4_1`、`zenis_cup@0/@3` 等任务状态；只访问过 `zenis_favorability1`，未持久化 `zenis_favorability2` 的完成现场。这证明返回标题确实把未保存的泽尼瑟剧情进度回退了。

Review record:

- User-confirmed facts:
  - 玩家在异常后又继续睡过晚间，产生了跨年的当前存档。
- Screenshot/log observations:
  - current 是最后一次载入后两次正常原生保存的结果，并非在半初始化年夜饭现场直接保存。
  - current 已通过年末清理进入下一年；本年度年夜饭没有完成，也没有留下可继续交材料的活动现场。
- Code/doc facts inspected:
  - 原版保存轮转使 `prev0` 为 current 的直接上一代、`prev1` 为上上代；单独文件已为用户额外保留了 `prev1`。
  - `.bak` 的时间和用途与本次滚动链不符，不能把它当“上一次保存”。
- Codex inference:
  - current **不是全局损坏存档**：结构、主要根区段和任务链可读，也没有把半初始化 FestivalState 持久化。它的问题是剧情结果层面的——玩家已跨年，因而错过这一年度年夜饭。
  - 如果玩家只想继续普通进度、能接受今年年夜饭缺失，可以保留 current 继续；如果目标是补回这次年夜饭，恢复点应选 `prev1`，需要多一点准备时间则选 `prev2`。
  - `prev0` 太晚，`.bak` 太旧；两者都不是本次首选。`prev4` 只用于复现完整因果链，不是日常救档首选。
- Ownership:
  - 存档轮转和节庆跨年清理由官方游戏所有；DTMAPI 本轮只作只读审计。
- Root-cause hypotheses:
  - 已证实 current 的“异常”是错过年度内容，不是 JSON/AES/主要任务区段损坏。
- Rejected/unproven hypotheses:
  - “必须再找一份上上次备份”：用户单独保存的文件已经精确等于 `prev1`，无需另找。
  - “直接把 `.bak` 覆盖回去最安全”：其年代和语义均不匹配，风险反而更高。
- Required downstream updates:
  - 在完成冷对照或确定是否放弃本年度年夜饭前，保持 current、prev0..prev4 与单独 `prev1` 原样不动。
  - 任何真实恢复操作都应先对整个槽位目录做只读归档并记录 hash，再完全退出游戏和 Steam AutoCloud 写入窗口；本 Review 不授权或执行该恢复。
- Acceptance checks:
  1. 若选择继续 current：重启游戏后验证槽位可正常加载、普通任务/背包/地图可用；接受本年度年夜饭已错过。
  2. 若选择恢复年夜饭：先在隔离复制上用 `prev1` 做冷对照；成功后再由用户明确决定是否牺牲 current 之后的两次睡眠进度。
  3. 恢复后完成年夜饭、正常睡觉并由原生 `SaveGame` 保存，再次冷启动确认完成状态持久化。
- Blocker conditions:
  - 未确认玩家希望保留“跨年后的当前进度”还是“补回本年度年夜饭”前，不应擅自覆盖 live 槽位。
  - 若冷进程 `prev1` 仍同栈失败，应停止救档回写，先完成净 Mod 对照或兼容补丁。

## Cross-Issue Summary

- Confirmed user facts: 泽尼瑟酒馆帮工剧情先运行；同一进程返回标题并读档；晚间进入酒馆后年夜饭人物错位且无法交材料；之后又睡到下一年。
- Screenshot/log facts: 两次年夜饭都在官方灯男渲染初始化处同栈空引用；节庆只完成部分 NPC 布置；current 来自最后一次载入后的两次正常保存，没有保存半初始化现场。
- Code-path findings: 当前官方修复抑制了部分节庆期间的房间到达事件并补取普通 NpcRenderer，但没有保护灯男附属资源，也没有为 FestivalState 初始化提供失败回滚；这解释了公告“已修复”后仍存在的漏网路径。
- Risks: 暖进程残留虽为高置信因果前置，仍缺冷/暖唯一变量对照；外部 Qiuzy 增强 Mod 未完全排除。玩家 current 结构健康但已错过本年度内容，直接覆盖会丢失之后进度。
- Suggested implementation scope: 先用 `prev1`/`prev4` 隔离夹具完成冷/暖矩阵；若授权兼容修复，只处理灯男资源恢复和节庆初始化原子性/回滚，不扩展 DTMAPI 公共 API。
- Items that should not be carried forward: 不再把 `20:20` 故障描述成泽尼瑟支线与年夜饭“同时触发”；不把 current 称为全局坏档；不使用旧 `.bak`；不在玩家 live AutoCloud 槽位做试错回写；不在无净环境对照时宣称所有 Mod 已完全排除。

## Implementation Record Decision

- Create/update an implementation update record: no；本轮是只读根因与存档代际审查，没有实现、部署或存档变更。
- Additional debug/API/hook/smoke records required: 当前 no；没有运行游戏，不追加 smoke。若用户授权兼容实现，先创建官方内容 Debug issue 与一个实现 Update；不需要公共 API/Hook Map 变更，除非后续方案实际触碰这些权威边界。
- Suggested task titles:
  - `长夜节年夜饭灯男资源暖读档冷/暖隔离复现`
  - `长夜节年夜饭半初始化回滚兼容补丁`
  - `槽位 1 prev1 年夜饭恢复验收`
- Completion standard: 用隔离副本证明冷/暖及功能 Mod 边界；补丁后完整运行年夜饭、交付材料、返回标题重载、正常原生保存与冷启动，日志无灯男/Dialogue/NPC 缺失异常；最后再由用户明确选择是否把 `prev1` 作为 live 恢复点。

## 2026-09-07 Official 1.00.07 Static Follow-Up

- Owning capture Update: [正式版 public 1.00.07 全量逆向捕获与公告核对](../../../updates/2026/20260907-0006-public-10007-full-reverse-capture.md)。
- 1.00.07 对同一问题族补了触发顺序和入口门：未处理对话从 Queue 改为可选择的 List，带 `is_final` 的年夜饭/爆竹节点要等非 final 节点先处理；年夜饭入口另绑定泽尼瑟酒馆、18..23 时、4 月 28 日，爆竹节点只在东部远郊执行清场和动画。
- 好感任务图新增 3 条 4 月 28 日精确条件：澳柯玛 10 心和泽尼瑟 9 心通过 `_invert: true` 排除当天，泽尼瑟 4 心则无反转、只在当天成立。这个不对称配置按静态事实保留，不能改写成“所有支线当天统一禁用”。
- `CharacterRenderer.InternalWalkTo` 新增 transform-null 提前返回，属于长动画/NPC 生命周期的相关保护；但本 Review 已精确定位的 `NpcTaskRenderController_Lightman` 和 `FestivalState` 在 1.00.06 与 1.00.07 间逐字节相同，灯男钓鱼绳对象池和节庆初始化无回滚的 owner 没有获得直接修复。
- Static outcome: 新补丁直接收口公告中的“支线在年夜饭开始后触发”和“长动画跳过年夜饭”机制；它不能单凭静态差分证明本 Review 的暖读档灯男空引用/半初始化链已经消失。
- Remaining acceptance gap: 原冷/暖、带/不带功能 Mod 的隔离矩阵仍有效。没有实际进入年夜饭、交材料、返回标题重载或采集新日志，因此不关闭本 Review、不修改玩家存档、不新增 smoke 行。
