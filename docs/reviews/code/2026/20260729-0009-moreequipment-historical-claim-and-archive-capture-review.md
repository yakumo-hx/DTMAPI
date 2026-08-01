# 20260729-0009: MoreEquipmentSlots Historical Claim 与 Archive Capture 独立审查

Status: `recorded / P0=0 P1=3 P2=0 / correction required`

## Scope

本轮是用户授权的第 2 轮子智能体独立只读审查。审查基线为
`cdad0e7d`，覆盖：

- durable `pending -> completed` claim 的跨进程、崩溃和重启边界；
- pre-schema global、deterministic archive、live Product 与
  `.previous` Product 的精确 authority 组合；
- 生产 Hook、resident Compatibility Host 和 SaveSaved 终态；
- Update、Batch 6 合同与产品 README 的事实归属；
- 用户已列问题之外的新增 P0/P1/P2。

审查没有修改项目文件、启动游戏、安装 Runtime、修改玩家存档或运行完整
Release。

## Result

```text
P0 = 0
P1 = 3
P2 = 0
MoreEquipmentSlots = implemented / acceptance-open
```

第 1 轮指出的迟到 claim 崩溃窗口和 claim-deletion TOCTOU 已由 durable
completed tombstone 关闭；生产 Hook/Host 单分发边界未发现新问题。本轮仍发现
三个发布前 authority 缺口。

## Findings

### P1: 历史终态缺失 claim 时不会回填 durable completed barrier

旧候选可能留下：

```text
exact stamped Product + exact deterministic archive + global absent + claim absent
```

当前 scope 会直接接受 Product。若一个旧进程已在 claim 发布前暂停，它随后仍可
发布绑定另一 scope 的 claim。新协议不能只保护新完成的迁移，还必须把该历史终态
回填为精确 completed tombstone，再允许 Product 返回。

修正门：回填前证明 exact Product scope/source stamp、exact archive 和 global
absent；create-if-absent 后不得覆盖并发出现的不同 scope claim；真实第二进程应在
回填完成后无法到达发布后崩溃针。

### P1: global 预检哈希与归档/删除之间仍有 byte-authority TOCTOU

原路径先读取 global 哈希，随后：

- 若 deterministic archive 已存在，直接删除当前 global；
- 若 archive 不存在，直接把当前 global 移到旧哈希命名的 archive。

另一写入者可在两步之间替换 global。第一条会静默删除新字节；第二条会把新字节
错误发布到旧哈希路径。

修正门：先用同卷原子 rename 捕获当前 global，再对捕获文件计算哈希；不匹配时
恢复或保留唯一 quarantine，绝不能删除新 global，也不能把新字节放到旧哈希
archive。

### P1: completed claim 校验错误拒绝合法 `.previous` Product authority

completed 路径仅调用 live-only 读取器，因此 live 缺失或可恢复损坏、但带精确
scope/revision/stamp 的 `.previous` 可作为既有 Product authority 时仍被拒绝。

修正门：复用 Product store 已有 live/previous 资格判定；wrong scope、future、
无效 previous 继续 fail-closed。

## Confirmed Boundaries

- `pending/completed` schema-1 claim 兼容、完成后 tombstone 保留、completed
  发布后崩溃重启和生产 SaveLoaded/SaveSaved 单分发成立。
- Compatibility Host 的 resident/cold 路由没有出现新的 owner、demand 或
  生命周期问题。
- 本 Review 不改变 C0/U1/U3/U4、玩家迁移、claim crash/resume 和完整 Release
  的开放状态。

## Resolution Link

后续实现、验证与生命周期只记录在 Update `20260723-0008`；本 Review 保持为
本轮审查时的发现与边界快照。
