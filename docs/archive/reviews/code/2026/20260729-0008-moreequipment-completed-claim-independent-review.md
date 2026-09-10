# 20260729-0008: MoreEquipmentSlots Completed Claim 独立审查

Status: `recorded / P0=0 P1=1 P2=1 / correction required`

## Scope

本轮是用户授权的第 1 轮子智能体独立只读审查。审查基线为
`d6de284bc230475941e072e1ba80c24c88ab5c6c`，重点覆盖：

- `9c0645bd` 的生产 SaveLoaded/title 单 owner 与跨进程 claim 修正；
- `af455c7d` 的真实第二进程和生产 Hook 聚焦测试；
- `d6de284b` 的 Update、Batch 6 合同和产品 README 事实归属；
- pre-schema global claim 从发布、Product/归档完成到重启恢复的额外崩溃窗口；
- Compatibility Host cold recovery、owner、demand 和 SaveSaved 终态。

审查只读；没有修改文件、启动游戏、安装 Runtime、修改玩家存档或运行完整
Release。

## Result

```text
P0 = 0
P1 = 1
P2 = 1
MoreEquipmentSlots = implemented / acceptance-open
```

生产 SaveLoaded/ReturnedToTitle 单分发、真实第二进程正常自撤路径、resident
backend recovery、上一轮四项直接修正以及 `d6de284b` 文档事实均成立。

## Findings

### P1: 迟到 claim 发布后、自撤销前崩溃仍会留下输家 authority

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:686`
- `tests/DTMAPI.UnitTests/MoreEquipmentSlotsProductTests.cs:1550`

确定顺序：

```text
B 通过最终 source-state 检查
→ A 发布 Product、归档 global、删除赢家 claim
→ B File.Move(temp, claim) 发布绑定 B scope 的迟到 claim
→ B 在 post-publication revalidation/self-withdraw 前崩溃
```

此时 A Product 和 archive 存在、B Product 不存在，但 B claim 会同时阻断 A
和 B。`af455c7d` 只证明 B 继续运行后能自撤，未证明这个进程终止窗口。

修正门：完成身份必须是不会留下 claim-deletion 空档的 durable
completed claim/tombstone，或重启必须能从 exact archive、唯一 stamped Product、
global 缺失和输家 Product 缺失安全修复；真实子进程必须在 publish 后故障点
退出并证明无需人工清理。

### P2: global absent 校验到 claim 删除之间仍有 TOCTOU

位置：

- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:958`
- `products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotDocumentStore.Migration.cs:969`

完成路径先检查 global 不存在，再验证 archive/Product，最后删除 claim。若另一
进程在校验后重建 global，本次仍删除唯一 claim 并返回成功。

修正门：完成步骤必须并入 durable completed-state 协议，不能以删除唯一 claim
表示完成；在完成校验后、状态转换前重建 global 时必须保留可恢复所有权且不得
报告终态。

## Confirmed Boundaries

- `SaveLoaded` 与 `ReturnedToTitle` 现在只走通用 feature fanout；
  `SaveSaving/SaveSaved` 保持专用事务边界。
- `moreequipment-product`、`moreequipment-cold-host` 在审查基线通过。
- claim early-return、pending-claim empty authority、archive/Product completion
  validation 和 resident backend recovery 未回归。
- Update、合同和 README 已把旧哈希与旧结论标成 superseded，产品仍为
  `implemented/acceptance-open`。

## Resolution Boundary

实现生命周期属于既有 Update `20260723-0008`。后续修正提交为
`6c5542ff`，覆盖提交为 `7adf2b7d`；本 Review 不吸收实现完成结论。第 2 轮
独立审查仍是 source reacceptance 门。
