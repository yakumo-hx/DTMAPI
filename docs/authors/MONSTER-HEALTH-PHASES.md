# 有限血量阶段与 Boss 条（Experimental）

`MonsterDefinition.HealthPhases` / `behavior.healthPhases` 为可选阶段计划，`MonsterDisplay.BossBar` / `overrides.display.bossBar` 控制房间内屏幕顶部 Boss 条。

## 健康与遭遇

基础阶段 index 0，倍率均为 1；后续阶段不设数量预算，阈值严格递减且在 (0,1)。原生 `Monster.currentHealth / proto.Health` 是唯一健康来源，HP 小于等于阈值即跨越。一次扣血跨多阈值直接进入最深阶段，中间阈值一并消耗，不依次出招。回血不回退、不重新武装阈值；待切换的阶段也不因回血撤销。HP 到 0/死亡优先，不进入新阶段。

状态跟随原生逻辑 Monster，显隐、Controller 池复用和同会话离房再入不重置。同一逻辑对象与待切换阶段保留，隐藏结束旧动作，再显示前完成待切换。原生刷新或再次 Spawn 创建新的逻辑 Monster 才是新遭遇，从基础阶段开始并核对实际初始 HP。标题、换档、删除和 Owner 退出沿已有清理结束状态；不额外保存逐只 Boss。

## 动作与倍率

`transition` 必填，可选 `CancelCurrent`（立即结束为 `PhaseChanged`、回收所属弹体）或 `FinishCurrent`（等当前动作自然结束）。受伤、超时、显式取消、死亡和退出仍可提前结束动作；等待策略不授予霸体。切换只影响随后接纳的动作，正在执行的动作保留启动时的参数快照。多个待切换阈值合并为最深阶段。

配置序列遇到 `PhaseChanged`，下一固定步重试当前步骤，保留已有技能冷却；它不是显式取消，不能复活已停止/失败的序列。代码行为可通过动作终态和 `GetSelf().HealthPhase` 查询做同样决定。没有额外伤害事件、阶段入场招式或第二套时钟。

阶段倍率每次由定义的基础参数计算，替换上一阶段倍率：`damageMultiplier` 作用于散射/冲刺原始伤害；`speedMultiplier` 只作用于冲刺速度，不改变弹速、距离、前摇/收招/冷却或 Animator。原生 Attack / MoveTo 可以参与同一阶段交接，但不使用模块技能倍率；目录需有 `actions.phasedNativeActions=true`，旧目录报 `PhasedNativeActionsUnsupported` 并保留步骤。倍率须为正数，不再设置最终伤害、速度或时长预算。代码行为可按阶段选择下一招；配置序列仍使用同一组步骤。

`inherit-native` 仍与阶段互斥。保留原招式时选配置或代码行为作为唯一决策者，通过受控动作调用原生攻击。变形虫取消空中跳跃后回到经地图与身体检查的地面位置，无安全点则失败清理，不产生落地攻击效果。`FinishCurrent` 等待原生动作 manager 的终态；已发射的原生弹体可能继续存活，不等同模块散射的整批弹体等待。

## 显示

Boss 条读取当前逻辑对象的真实 HP/最大 HP，与 API 共用健康访问器；阶段文字读取同一个 HealthPhase 状态。待切换时显示当前阶段与待切换阶段。房间内活动 Boss 在屏幕顶部按当前绑定顺序排列，离屏仍显示，隐藏/死亡解绑；小血条保留其相机可见性规则。UI 不推进阶段或战斗。

## 作者入口

普通 C# 在已有受控技能定义上设置：

```csharp
definition.HealthPhases = new MonsterHealthPhases {
    Transition = MonsterPhaseTransition.FinishCurrent,
    Stages = new List<MonsterHealthPhase> {
        new MonsterHealthPhase {
            Id = "enraged", AtOrBelow = .5f,
            SpeedMultiplier = 1.5f, DamageMultiplier = 2
        }
    }
};
definition.Display.BossBar = true;
```

`Index` 为已应用阶段（0 是基础），`ReachedIndex` 为已跨越最深阈值，`PendingId` 表示等待应用的阶段，`Revision` 只在真正提交阶段时递增。一次跨过多个阈值只增加一次 Revision，回血不会改变这些值。没有阶段计划时 `HealthPhase` 为 null，仍可单独开启 Boss 条。
