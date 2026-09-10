# Y 键控制台 1.1.0 旧城守护者熔断与发布限制复核

## Metadata

- Date: `2026-08-13`
- Status: `recorded`
- Source: player manual test and the exact `2026-08-13 07:00–07:06` DTMAPI/BepInEx/Unity logs
- Scope: durable manual-QA and release-limitation review; no monster implementation change
- User constraints: first inspect logs only; after diagnosis, prepare the three-language existing-Workshop upload directory and warn players not to save after risky teleport/boss actions
- Related Updates: [Y Console 1.1.0 Semantic UI and ProductNative Actions](../../../updates/2026/20260812-0001-y-console-semantic-ui-productnative.md); [Y Console 1.1.0 Three-Language Workshop Upload Preparation](../../../updates/2026/20260813-0001-y-console-110-workshop-upload-preparation.md)

## 问题 1：召唤旧城守护者后，其他怪物均不能继续召唤

### 原始反馈

- 用户确认本轮手测基本没有其他问题。
- 在第三存档中召唤一个“旧城守护者”后，控制台不能再召唤其他怪物。
- 用户先要求只检查日志；随后明确决定上传，并要求三语发布文案公开旧城守护者与 Boss 保存风险。
- 图片转写：无截图。

### 审查记录

- 用户确认事实：旧城守护者在场景中出现，之后其他怪物按钮不再生效。
- 日志观察：`07:04:44.813` 的 `space_ship` 请求记录 `requested=1 created=1`，说明原生创建已返回一个实体；随后后置条件报告 `Monster postcondition verification failed`，回滚未恢复 `exact monster count/containment`，并以 `rollback-failed-circuit-open` 为该存档打开硬熔断。之后 `drone_99`、`drone_ex`、`seed_carrier`、`fungus`、`battering_ram`、`bee` 等请求均为 `created=0 reason=batch-spawn-circuit-open`，没有再次进入原生创建。
- 对照事实：同一进程此前成功创建 `drone_ex`、`nian_head`、`nian_tail`、`scarecrow`、`space_ship_bastion` 和 `target_01`；因此不是整个怪物宿主不可用，也不是此前修复的 `Vector3 -> Vector2` 参数错误复发。
- 异常排查：同一时间窗没有 Unity、BepInEx 或游戏本体异常堆栈；唯一硬错误来自产品的数量/包含关系安全校验。
- Codex 推断：`space_ship` 很可能拥有特殊的宿主登记、关联实体或生命周期，违反当前“一次请求增加一个且返回实体直接包含于普通宿主集合”的验证假设。现有日志没有输出校验前后各集合数量和具体失败谓词，不能进一步断定是附属实体、实体替换还是特殊宿主登记。
- 已排除：不是后续每个怪物分别生成失败；它们被已打开的熔断器提前拒绝。不是 `space_ship_bastion` 的共同问题，该 ID 已独立成功。
- 归属：Y 控制台 ProductNative 特殊怪物验证/回滚兼容边界。
- 保存边界：本轮不声称 `space_ship` 或 Boss 可安全保存；发布文案明确提示玩家召唤 Boss 后尽量不要保存。该警告不等于证明已发生存档损坏，也不取代将来的 disposable 正常保存测试。
- 发布决策：本轮不放宽回滚熔断、不为特殊怪物增加未经验证的旁路。产品维持 Experimental，以明确玩家警告发布；未来修复必须先增加逐谓词和逐集合计数诊断，再用隔离存档验证特殊怪物创建、回滚和保存边界。

### 验收点

- 三语上传元数据均明确：不是所有怪物都能正常召唤；旧城守护者当前失败；Boss 召唤后不建议保存。
- 上传目录仍只包含 SDK 生成的 Y 控制台 1.1.0 精确包与原有 `workshop.json`，不手工编辑 DLL/receipt/manifest。
- 本轮不将旧城守护者标为已修复，不以发布授权扩大 ProductNative 原生策略。

## 2026-08-30 静态根因跟进

- 最新详细 Review 已把原先“特殊宿主登记或关联实体”的推断收敛为确定调用链：
  官方 `space_ship` 的 `MonsterDecoratorSpaceShip.OnMonsterLoaded` 会同步再次调用
  当前 Host，两次生成 `space_ship_bastion`。一次根请求因此给
  `DM_monster.AllMonsters` 带来 `1 + 2` 个实体。
- 当前产品只把反射返回的根加入 `created`，仍要求 manager 增量等于 `1`；后置
  条件必然失败。回滚只移除根且不会触发主实体死亡级联，两个炮台留存，所以精确
  计数恢复失败并打开本存档熔断。单独生成 `space_ship_bastion` 不产生子实体，
  因而可以成功。
- 策略 build `24456188` 与 public `24966367 / 1.00.06` 的相关装饰器、manager、
  controller 和 AI 文件字节一致，`GenerateMonster` / `RemoveMonster` 方法体也未
  变化；这不是近期官方代码漂移，而是官方复合怪物语义与产品单实体事务假设冲突。
- 本跟进没有启动游戏、没有修改怪物代码，也没有放宽安全断言。未来实现必须以
  manager 集合差取得完整实体闭包，验证一个根加两个炮台，并对子实体优先回滚；
  保存边界仍需修正后的隔离存档验收。
- 完整证据与后续门见
  [20260830-0001 Review](20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)。

## 2026-08-30 用户决策与实施取代

- 上一节提出的“完整回滚后再允许生成”仍是当时尚未裁定产品语义时的历史建议，
  不再是当前实现门。用户随后明确要求保留普通一倍和右键十倍、不要额外确认、取消
  自动回滚与共享熔断，并采用会保留且显示实际新增实体的部分成功语义。
- `1.1.2` 候选现以每次官方调用前后的 manager 对象集合差验证复合结果：
  `space_ship` 一倍为 `1` 个成功根/`3` 个实际实体，十倍为 `10` 个成功根/`30`
  个实际实体。当前批首次失败才停止，已加入实体不删除，下一次玩家请求仍可执行。
- `GAME-SMOKE/20260830-231652` 从隔离夹具的官方本地 `MODS` 根加载 ProductNative
  候选并通过上述一倍/十倍动作；玩家存档和已提交 sidecar 在清理前保持不变，未做
  常规字节备份或写回。当前事实与实现证据由
  [20260830-0004 Update](../../../updates/2026/20260830-0004-y-console-visible-partial-composite-spawn.md)
  持有；本文件标题中的 `1.1.0` 限制只保留为历史发布记录。
