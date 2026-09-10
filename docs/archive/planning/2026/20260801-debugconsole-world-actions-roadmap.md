# Y 键控制台世界动作优化路线

Date: 2026-08-01

Status: `superseded / frozen`

Superseded on: 2026-08-30

## Current handoff

本文冻结 2026-08-01 当时“发电机、怪物、资源均隐藏”的决策，不再作为当前
实现入口。后续事实由以下记录接管：

- [Y Console 1.1.0 Semantic UI and ProductNative Actions](../../updates/2026/20260812-0001-y-console-semantic-ui-productnative.md)：Monster 与 Animal 已进入可搜索、可分页目录，拥有显式数量、可用性、结果诊断和 ProductNative host 事务。
- [Y 键控制台 1.1.2 手测接收、0.3.1 退役授权与旧城守护者静态审查](../../reviews/manual-qa/2026/20260830-0001-y-console-112-manual-acceptance-retirement-and-space-ship-review.md)：动物保存/重载/隐藏产物与普通机器燃料行为已由用户手测接收；旧城守护者被定位为一个根加两个炮台的官方复合怪物，后续用户决策明确取消伪事务与共享熔断。
- [Y Console 1.1.2 stackable fuel and maintenance](../../updates/2026/20260830-0003-y-console-112-stackable-fuel-and-maintenance.md)：无限燃料使用现有物品浏览器并改为普通 `999` 堆叠，单体 UI 已做行为不变的机械拆分。
- [Y Console visible partial composite spawn](../../updates/2026/20260830-0004-y-console-visible-partial-composite-spawn.md)：`1.1.2` 候选已按可见部分成功语义修正旧城守护者，保留一倍/十倍生成，不增加确认，不回滚已生成实体，也不熔断后续请求。

当前真实边界是：

- Generator 快捷动作已经退出 UI；稳定 ID `dtmapi_creative_generator` 作为普通
  无限燃料物品存在于物品浏览器。
- Monster 与 Animal 已公开在当前 Y 控制台中；`1.1.2` 候选按 manager 调用前后
  集合差验证每个根，`space_ship` 的预期结果是一个主体加两个炮台，并显示请求根数、
  成功根数和实际新增实体数。
- Resource 生成仍无玩家 UI 接线，旧 backend 只为尚未清理的兼容代码保留。
- 旧 `0.3.1-dtmapi` 已由用户确认无使用者，其 Compatibility 保留限制已解除；
  物理删除仍由未来独立 breaking cleanup 执行。

## Remaining bounded work

### Resource UI

若重新开放 Resource，仍须先完成：具体对象搜索/分页、显式数量、当前场景可用性、
生成位置与范围、危险确认、调用前后新增集合验证、可见的部分成功，以及
no-save/normal-save 边界。不得从旧隐藏 backend 的存在推导为已完成产品设计。

### Old City Guardian（已由后续 Update 接管）

`1.1.2` 候选已以调用前后 `DM_monster.AllMonsters` 对象集合差验证
`space_ship + 2 × space_ship_bastion`。一倍请求实际新增 `3` 个实体，十倍请求
实际新增 `30` 个实体；两条路径均由 ProductNative `NoNativeSave` 运行验证。
旧的“完整回滚并熔断”建议已经被用户决策取代：当前批首次失败时停止，官方已经
加入 manager 的实体保留，后续玩家请求不受共享失败状态阻断。

### Maintenance

`DebugConsoleUi.cs` 已完成行为不变的 `partial` 机械拆分。nullable 标注和 0.3.1
Compatibility 物理退役仍是独立技术债，不能由文件移动或守护者修复推导为已完成。

## Historical boundary

本页 2026-08-01 的原始隐藏决定仍是审计材料：当时 UI 尚无完整对象选择、数量、
结果验证与保存边界，所以收起三个入口是正确的。它不能覆盖后来 1.1.0/1.1.1
已经实现、测试并发布的事实。
