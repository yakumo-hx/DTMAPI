# 单动作图集（Experimental）

当前配对为 Host 0.1.11、MonsterContent / 原生模块 0.1.10、Runtime / SDK 0.7.5，作者项目 v4、载荷 schema 2、绑定 `dtmapi.monsters/2` / 2.0.0。所有当前包统一声明 Host `[0.1.11,0.2.0)`；模板能力仍由实际目录逐项证明。

`MonsterSkill.Animation` 为散射动作提供自有 PNG 图集。普通 C# 和编辑器配置共用执行器、动作句柄与取消边界。当前实例需要目录 `Actions.ActionAnimation=true`，同时满足散射的直线弹体和原生索敌条件。实际样例使用 `ball_drone`，目录中的其他模板仍需各自验证。

完整 Strict 样例见 [ModEntry](examples/monster-action-animation/ModEntry.cs)，编辑器可打开 [action-animation.dtmonster.json](../tools/monster-editor/samples/action-animation.dtmonster.json)。样例六帧为原创几何图案，由 [生成脚本](../tools/monster-editor/scripts/create-animation-sample.mjs)重建。

```csharp
skill.Animation = new MonsterActionAnimation {
    Png = atlasBytes,
    FrameWidth = 32, FrameHeight = 32,
    PivotX = .5f, PivotY = .5f, PixelsPerUnit = 16,
    WindupFrames = new List<int> { 0, 1, 2 },
    RecoveryFrames = new List<int> { 3, 4, 5 }
};
```

| 字段 | 规则 |
| --- | --- |
| Png | 有效 PNG，不设平台文件大小或像素尺寸预算。模块解码失败会撤回准备中的资源 |
| FrameWidth / FrameHeight | 正整数，必须整除图集宽高；从左到右、从上到下编号，左上帧为 0 |
| PivotX / PivotY | 0–1，统一从每帧左下角计算；不逐帧改变 |
| PixelsPerUnit | 正数，每个世界单位对应的像素数 |
| WindupFrames / RecoveryFrames | 各 至少 1 个有效网格索引，可重复、可重排；每段内等分时间 |

时长仍由散射的 `WindupGameSeconds` / `RecoveryGameSeconds` 决定，带动画时两者必须大于 0。前摇按动作 `Elapsed` 选帧；到达前摇阈值的固定步，先显示第一张 Recovery 帧，再创建本轮弹体。收招从实际发射时刻计时，到末帧后保持，直到收招与本轮弹体都结束。若一帧比游戏固定步短，画面可能跳过中间帧；不会因此补发弹体。

**发射时刻与命中时刻不同。** 第一张 Recovery 帧对应发射；弹体飞到目标并发生原生碰撞时才命中。动画没有伤害帧事件或第二套计时器，不会因为换帧再次产生伤害。原生固定游戏时间是唯一推进来源，暂停时帧、动作、冷却和弹体均不推进。

执行期间关闭该实例原生 Animator，并清除旧动画完成/伤害回调；既有换皮 LateUpdate 不覆盖动作图集。完成、取消、超时、受伤和实例退休恢复原 Sprite 与 Animator 启用状态。受伤在原生 `OnHurt` 当下结束动作；取消沿同一动作回收排队/在场弹体，旧回调不能晚命中。图集资源归定义 Binding，实例只借用；定义撤回或准备失败时释放。

图片尺寸、pivot 和 PPU 只改变显示，不修改身体 Collider 尺寸/偏移、Transform 或原生 FirePosition。身体和发射点继续使用模板已有几何；作者应让视觉与其一致。当前配置不接受动态身体、发射点、伤害事件或冲刺动画字段。未来增加这些能力时，需要重新检查冲刺冻结 AABB 和散射发射边界。

编辑器在散射技能下导入 PNG、编辑网格/pivot/PPU 及两段帧序列；缩略图用于核对顺序。保存/重开保留图集与参数，真实 C# 预检查核对目录和资源，SDK 游戏包包含可重开的作者项目。带图集的包与其他当前包统一要求 Host ≥0.1.11、<0.2.0，目录缺能力时明确拒绝，Host 不满足要求时导出返回 `ExportHostIncompatible`。版本要求随当前内部实现和现用消费者同步调整。

有限血量阶段见[阶段指南](MONSTER-HEALTH-PHASES.md)。
