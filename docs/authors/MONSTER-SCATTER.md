# 散射技能（Experimental）

散射是定义内的自定义技能。普通 C# 作者在 `MonsterDefinition.Skills` 中声明 `MonsterSkill`，以 `IMonsterBehaviorContext.UseSkill("fan", 10)` 执行；配置序列使用 `action: "skill"` 和 `skillId: "fan"`。两者共用接纳、动作预算、取消和实例生命周期。

```csharp
definition.Skills.Add(new MonsterSkill {
    Id = "fan", Kind = "scatter",
    Scatter = new MonsterScatter {
        ProjectileAttackId = "AtkNormal", BulletCount = 5,
        SpreadDegrees = 60, BulletSpeed = 6, BulletLifetimeGameSeconds = 2,
        Damage = 10, Range = 10, WindupGameSeconds = .5f,
        RecoveryGameSeconds = .3f, CooldownGameSeconds = 2
    }
});
```

`Id` 在同一怪物定义内唯一；定义接纳后冻结全部嵌套参数。技能限于 `code-provider` / `configured-sequence`，不能附加到原生继承模式。当前支持 `scatter` 和[冲刺](MONSTER-DASH.md)。未知类型和字段会拒绝，不静默降级。

| 参数 | 含义与范围 |
| --- | --- |
| ProjectileAttackId | 模板已有的直线弹体来源；目录 `Actions.ScatterAttackIds` 必须明确列出。仅有 AttackIds 不足以证明支持 |
| BulletCount | 至少 1 发，同时发射 |
| SpreadDegrees | 两端之间的总夹角，非负°，各发等角分布；单发始终沿中心方向 |
| BulletSpeed | 正数 世界单位/游戏秒 |
| BulletLifetimeGameSeconds | 正数 游戏秒；飞行路程由速度和寿命决定。碰撞可提前回收 |
| Damage | 非负 原始原生攻击值，无暴击；实际扣血仍由原生防御/无敌决定。0 不调用伤害入口 |
| Range | 正数；施放和发射前使用原生视线/距离检查。不是弹体飞行距离；飞行距离由速度和寿命限制 |
| WindupGameSeconds | 非负；从成功接纳开始，下一次可执行固定步发射，最多有一个固定步的量化误差 |
| RecoveryGameSeconds | 非负；从实际发射开始。动作要等收招和该轮全部弹体结束才 Completed |
| CooldownGameSeconds | 正数；按实例和技能 ID 独立，自成功接纳开始，取消不返还；暂停不推进 |

目标来自模板原生视野。成功接纳锁定目标身份，发射前重查当前房间、目标身份、活动状态和射程/攻击通道；同一目标的位置可更新。丢失/换目标或射程/射线失效会 `Interrupted / TargetInvalidated`，不会向旧坐标补发；零瞄准方向为 `AimUnavailable`，明确受击锁或已支持动作动画的受击回调为 `Hurt`。原因由动作快照的 `InterruptionReason` 返回，仅在清理成功后发布。发射后是固定方向直线弹道，不追踪目标；隐藏、死亡、离房或 Owner 结束沿共同回收撤销执行，新显示代次的冷却重新开始。

三种正常中断结束当前有限序列，不重放旧动作或跳步；active 等完整轮间隔后另起新轮，retaliate 冷却后等待新有效 hit。未知原因和真实执行/清理故障不重新等待，显式取消优先；完整规则见[序列指南](MONSTER-SEQUENCES.md)。

散射借用模板已取得的原生弹体管理器、外观、碰撞和发射点，不修改模板/原生攻击参数，也不新建全局管理器。默认前摇和收招保持模板待机表现；可选[单动作图集](MONSTER-ACTION-ANIMATION.md)，由现有前摇/收招时长选帧，发射步骤显示第一张收招帧。所有伤害来自原生弹体碰撞，每发回收后拒绝重复伤害回调。挂点编辑沿后续队列；已有有限血量阶段见[阶段指南](MONSTER-HEALTH-PHASES.md)。

`UseSkill` 返回同一 `MonsterActionHandle`。技能完成前保持忙状态；`Cancel(handle)` 取消排队和在场的所属弹体，退休回调不能再伤害。Completed 表示该轮已无弹体，因此之后取消返回 false。`MaxGameSeconds` 包含前摇、收招及弹体等待；配置步骤还包含接纳等待，不足时 TimedOut 并清理，不当作成功完成。`SkillCoolingDown`、暂时无目标或超射程使用已有有限等待；缺技能、缺直线弹体来源、无原生视野等为明确失败。