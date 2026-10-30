# 配置行为序列与战斗策略

当前配对为 Host / MonsterContent / 原生模块 / Authoring 0.1.27、Runtime / SDK 0.7.5，作者项目 v4、载荷 schema 2、绑定 `dtmapi.monsters/2` / 2.0.0。所有当前包统一声明 Host `[0.1.27,0.2.0)`；模板能力仍由实际目录逐项证明。

`configured-sequence` 顺序执行已有 MoveTo、原生 Attack 和定义内 skill。省略 combat 时执行一次；也可选择主动、反击或不主动策略。散射与冲刺沿普通 UseSkill 和共同取消边界，参数见各自指南。

## 配置与导出

在编辑器打开[完整样例](../tools/monster-editor/samples/configured-sequence.dtmonster.json)，导入匹配当前游戏版本的模块目录快照，选择“配置顺序行为”。编辑步骤、顺序和重复次数，然后保存项目、导出游戏包。导出包包含可重开项目和编译载荷。切换步骤动作会替换该步骤的动作参数，可以撤销。

样例使用 `ball_drone`，每实例执行“相对起点向右移动 1.25 → 原生 AtkNormal → 返回起点”，重复两轮；三个步骤各有 15 游戏秒预算。它还保留一条继承 AI 定义作对照。下面是定义中的 `behavior` 字段：

```json
{
  "mode": "configured-sequence",
  "sequence": {
    "repeatCount": 2,
    "steps": [
      { "id": "outbound", "action": "move-to", "offsetX": 1.25, "offsetY": 0, "maxTravel": 8, "maxGameSeconds": 15 },
      { "id": "fire", "action": "attack", "attackId": "AtkNormal", "maxGameSeconds": 15 },
      { "id": "return", "action": "move-to", "offsetX": 0, "offsetY": 0, "maxTravel": 8, "maxGameSeconds": 15 }
    ]
  }
}
```

移动偏移相对每次可见执行的起始位置，不是上一步的终点。使用原生寻路与到达容差；`maxTravel` 限制累计移动距离。攻击 ID 必须存在于所选模板的能力目录，攻击参数、目标、射程和冷却仍按原生动作处理。没有目标或暂时不在射程内时会等待，消耗本步骤预算。

至少配置一个步骤并执行一次；步骤数和重复次数不设平台上限。每步时间与路程限额由作者设定正数，移动偏移须为有限数值。步骤 ID 必须唯一。字段按动作严格校验，攻击步骤不能混入移动字段，移动步骤不能带攻击 ID。

SDK 导出自动声明 Host 依赖 `[0.1.27,0.2.0)`；Host 能力不满足时返回 `ExportHostIncompatible`。C# 作者使用 `MonsterDefinition.Sequence`、`MonsterSequence`、`MonsterSequenceStep` 构造同样的配置，并引用同一内部构建的共享契约；契约调整时同步消费者并重编译。

## 执行与生命周期

模块为每个实例创建独立序列状态；前一步完成后才进入下一步。每步的等待与执行共用预算：硬直期间尚未接纳动作的等待也计时，包括解除硬直但尚未恢复 AI 的固定步；耗尽后记为 `AdmissionTimedOut:HurtLocked`，恢复后不会补出招。此规则适用于一次、主动和反击序列；已接纳动作仍由原生动作时钟计时。游戏暂停时动作和等待预算都冻结。取消当前动作会令序列停止，动作失败或超时会令序列失败；不会跳过、重试失败动作或无限循环。

`MonsterInstanceSnapshot.Sequence` 返回状态、当前步骤、轮次、原因和动作句柄，可区分 `Running`、`Completed`、`Stopped`、`Interrupted` 与 `Failed`。动作快照的 `InterruptionReason` 是 `MonsterActionInterruptionReason`：成功清理后的 `TargetInvalidated`、`Hurt`、`AimUnavailable` 让当前有限序列以 `Interrupted` 结束，原因形如 `Interrupted:Hurt`；它不会重跑旧动作、跳到下一步或冒充完成。`None`、未知值、`ExternalDisplacement`、`NativeAttackStopped` 仍使序列 `Failed`。位置偏差或原生攻击停止本身不证明受击；代码/原生调用与清理故障保持真实失败责任。继承 AI 实例的此字段为空；C# 自定义行为自行决定下一步，战斗策略不会接管它。

现有行为上下文的 `Cancel` 只作用于所属实例的当前动作，已终结动作再取消返回 false，不追溯改写结果。省略 combat 的有限序列以 `Interrupted` 结束后，`CancelSequence` 也返回 false；要终止主动/反击策略的等待，使用 `CancelSequence`。

隐藏、离房或撤销会走已有动作回收路径。隐藏后重新显现会创建新执行，从新执行起点重新开始；旧上下文、实例句柄和动作令牌失效。序列进度不跨存档保存。

序列可同时声明[血量阶段](MONSTER-HEALTH-PHASES.md)。原生攻击/移动参与交接需要目录 `phasedNativeActions`；阶段倍率不修改它们的原生参数。阶段立即取消的 `PhaseChanged` 是独立终态，下一固定步重试当前步骤并保留原冷却，不停止整个序列。配置始终使用同一组步骤，按阶段选择不同招式可用[普通代码行为](examples/monster-native-phases/ModEntry.cs)。



## 主动攻击触发与再次执行

在“模板与身份 → 行为触发”选择“主动攻击：原生目标触发”，设置无目标检查间隔和轮次间隔。[主动攻击样例](../tools/monster-editor/samples/active-sequence.dtmonster.json)使用同一 `ball_drone` 的 `AtkNormal`，一轮执行一次有限序列。序列自身的 `repeatCount` 属于一轮内部的重复次数，不是战斗轮次上限。

```json
"combat": {
  "mode": "active",
  "checkIntervalGameSeconds": 0.5,
  "repeatIntervalGameSeconds": 2
}
```

`combat` 与 `sequence` 同为 `behavior` 的字段，只能用于 `configured-sequence`。两个间隔均为 正数 游戏秒；未知策略和字段会被拒绝。SDK 为主动策略包生成 Host `[0.1.27,0.2.0)` 依赖，旧 Host 无法导出该包。

| 输入或事件 | 行为 |
| --- | --- |
| 没有有效原生目标 | `WaitingTarget`，按检查间隔查询；不创建空轮次、不调用 Attack/MoveTo、不消耗步骤预算 |
| 原生视野锁定当前房间的活动目标 | 新建有限序列，记录本轮位置起点及目标身份，进入 `Running`；原生警戒过程、攻击射程和冷却仍有效 |
| 全部步骤及有限重复完成 | `WaitingInterval`，完整等待轮次间隔后重新查询目标；不保留旧目标作为下一轮的启动凭证 |
| 成功清理后的 `TargetInvalidated`、`Hurt` 或 `AimUnavailable` 动作中断 | 当前轮 `Interrupted`，策略 `WaitingInterval`；完整间隔后重新查询目标，从新轮第一步和新位置起点开始 |
| 执行中目标丢失或身份改变 | 在下次原生 AI 固定更新的动作推进前取消当前轮，记 `TargetLost` / `TargetChanged`，等待轮次间隔后重检；目标只移动位置不会重启 |
| 步骤接纳超时、原生动作超时/行程耗尽/失败、未获允许的中断或查询/清理故障 | `Failed`，保留原因；不自动重试失败轮次 |
| 显式取消序列，或取消当前动作 | `Stopped`；即使同一帧发生目标丢失也不重启。取消先关闭决策，再沿原动作路径清理当前动作；此前已正常完成的动作资源沿原生到期 |
| 暂停 | 检查间隔、轮次间隔及步骤预算均冻结，不按墙钟补跑 |
| 隐藏、离房、Owner 退出 | 沿已有执行代次失效/清理；重新显示是新执行，旧上下文、句柄和动作令牌不能启动它 |

每个实例只保留当前轮和一个等待计时器，一次原生固定更新最多开始一轮。目标变化不会直接改写正在执行的攻击目标，也不跳过序列剩余步骤。

正常中断的间隔从 runner 实际观察到中断的固定步开始，该步不扣等待时间；硬直跳过 AI 观察时不补算墙钟。取消、故障、超时或失效句柄优先于同时发生的目标变化和正常中断。`PhaseChanged` 保留专用当前步骤重接纳语义，不按正常中断结束本轮；技能冷却不返还。

C# 定义直接设置 `MonsterDefinition.Combat = new MonsterCombatPolicy { ... }`，无需临时 JSON。`GetInstance(handle).Combat` 返回策略状态、轮次、剩余等待游戏秒、原因和目标句柄；`Sequence` 返回当前或最近一轮的有限序列快照，首轮未开始时为空。等待下一轮期间 `Sequence.Completed` 或 `Sequence.Interrupted` 均不等于策略结束。

`IMonsterBehaviorContext.GetTarget()` 查询同一原生目标边界：成功且值为 null 表示当前没有目标，失效上下文返回诊断。模板没有原生 `IMonsterSight` 组件时，目标查询与 `Attack` 返回 `TargetingUnsupported`，配置主动策略终止为 Failed；它与暂时没有目标不同。能力目录的索敌前提会标明这一限制，原生继承和单独受控移动仍按各自条件使用。某些模板依靠原版 AI 私有仇恨字段出招，不能据此推导它支持配置索敌。`MonsterTargetHandle` 只用于同一实例可见执行内的身份比较，不含原生对象，也不能保存为跨房间/存档目标。API 没有指定任意目标或改变原生视野的入口。

`IMonsterContentApi.CancelSequence(handle)` 可在无目标等待、轮次间隔或动作执行期间取消本 Owner 的配置序列。首次终止返回 true；仍有效实例的单次序列已停止/完成/中断/失败，或战斗策略已停止/失败时返回 false；失效或其他 Owner 的句柄返回 `StaleHandle`，非配置行为返回 `SequenceUnavailable`。本次执行没有自动恢复开关。



## 反击与不主动

在“何时执行”选择相应策略。[反击与不主动样例](../tools/monster-editor/samples/retaliation.dtmonster.json)包含受击后执行一次 `AtkNormal` 的定义和没有序列的不主动定义。切换不主动保留已编辑步骤并暂停使用，切回其他策略或撤销可继续编辑；不主动也允许省略 `sequence`。

```csharp
definition.Combat = new MonsterCombatPolicy {
    Mode = "retaliate",
    CheckIntervalGameSeconds = 0.5f,
    RepeatIntervalGameSeconds = 2.5f,
    TargetWaitGameSeconds = 4
};
// 不主动：Mode = "passive"，Sequence 可为 null。
```

JSON 对应 `behavior.combat.mode = "retaliate"`，可选 `targetWaitGameSeconds` 默认 5，允许 正数 游戏秒，仅反击使用自定值。它限制受击后等待原生目标的时间；取得目标后沿序列各步骤的独立预算执行。

| 输入或事件 | 反击行为 |
| --- | --- |
| 初次显示、仅看到玩家 | `WaitingHit`，不创建轮次，不发起策略目标查询或动作 |
| 原生 `Monster.Damage` 的任一重载正常返回，实际 HP 下降且 HP > 0、实例仍存活 | 进入 `WaitingTarget`；只记录一次触发，下一次可决策的原生固定步才检查目标 |
| 等待期限内取得有效原生目标 | 执行一轮有限序列；原生射程、冷却和硬直门保持 |
| 期限届满仍未取得目标 | `WaitingInterval / TargetWaitExpired`；丢弃此次触发，冷却后回 `WaitingHit` |
| 待目标、执行或轮次间隔中再次受击 | 不重启、不叠加、不排队、不刷新等待期限；伤害和受击表现仍按原生结算 |
| 一轮完成、目标丢失或改变，或成功清理后的三种正常动作中断 | 完成、取消或 `Interrupted` 结束当前轮，完整冷却后回 `WaitingHit`；需要冷却结束后的新有效受击，冷却结束该步之前的 hit 也不排队 |
| 显式取消、动作被取消、失败或超时 | 本次执行终止，新受击、旧触发和持续可见目标均不能重新启动 |
| 仅 `Hurt` 表现、`ManualDamage` 或直接修改 HP、没有扣血、致死伤害 | 不产生反击触发；致死优先沿共同回收 |

有效受击按**结算后的 HP 变化**判断，不按请求攻击数值、闪烁或返回 bool 判断：当前原生公式有最低 1 点伤害，因此请求 0 点攻击仍可能是有效受击。装饰器阻止命中且没有实际 Damage 结算时不会触发。两种 Damage 重载都没有攻击者字段，目标继续来自当前房间的有效原生 `LockedTarget`；本契约不提供攻击者追踪、任意指定目标或多阵营归因。

原生受击硬直跳过 AI 决策时，反击的等待期限和冷却仍由同一控制器固定步消费；游戏暂停不计时，不在硬直回调里启动动作。每个可见执行持有自己的策略对象，隐藏再入会新建 `WaitingHit` 或 `Passive` 执行；旧句柄、旧对象和旧受击观察不能操作新代次。

`passive` 始终返回 `Combat.State = Passive`、`Round = 0`、`Sequence = null`；看到目标与受击都不自动移动或攻击，正常伤害与死亡保持。它与省略 `combat` 的“显示后执行一次”有明确区别。`CancelSequence` 也可终止不主动实例的本次执行。
