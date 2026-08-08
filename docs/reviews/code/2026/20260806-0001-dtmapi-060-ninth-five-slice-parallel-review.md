# DTMAPI 0.6 第九次五切片并行审查

Date: 2026-08-06
Status: `recorded`
Audited commit: `308194a2`

Owning Update:

- [`20260802-0001`](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)

## Scope

按用户规则，在第八次审核后的五个独立功能/发布证据切片完成后，同时启动
三路只读审查：

1. MoreEquipment Product/Compatibility 冷恢复与 save-commit 语义；
2. GAME-SMOKE 输入、期限、隔离、恢复和 aggregate receipt；
3. ISSUE、Batch 6、smoke matrix、路线图与 evidence retention 治理。

审核期间没有安装 Runtime、启动游戏、修改官方 `MODS`、启用状态、玩家存档
或侧车。发现及其有界修复属于本审核周期，不计入下一组功能切片。

## Findings

本轮没有 P0、P1 或 P3，合并后有三项 P2。

### R1 — P2：outer aggregate receipt 未进入持久留存路径

ISSUE-021 和 Batch 6 把
`MOREEQUIPMENT-COLD-RECOVERY/20260806-r5/cold-recovery-acceptance.json`
作为三进程关联回执，但 schema 5 的 durable-root 类别不包含该目录。三个
GAME-SMOKE 子进程本身会保留，outer 的 phase order、共同 fixture 与 completed
关系却可能被后续证据清理遗漏。无需为单个邻接 gate 新建 receipt/schema
家族；completed aggregate 应复制到最终、已被文档精确引用的
`GAME-SMOKE/20260806-015320/cold-recovery-acceptance.json`，由既有
`gameSmokeArtifacts` 规则保护。

### R2 — P2：旧包精确恢复和 Runtime lock 终态超出 r5 receipt

三份 result 能证明候选 package preflight、官方 profile 恢复、进程退出和功能
门，但 r5 aggregate 在释放 Runtime lock 前即写为 completed，也没有保存候选
外部换回旧 `0.3.1-dtmapi` 的 postflight hash。会话结束后的只读现场检查不能
倒灌成 r5 原子回执。smoke matrix 与路线图应删除“旧包 byte-exact、lock free”
的正式证据措辞；若未来需要这项发布权威，应由拥有 package swap 的事务记录
pre/post source 与 lock release。

### R3 — P2：第二次 Enter 缺少可证明的 UI 因果令牌

runner 在第一次前台 Enter 后固定等待五秒；若没有看到 `SaveSaving`，原实现会
直接发送第二次 Enter，却没有证明睡眠菜单仍停留在同一确认状态。慢帧或日志
调度可能使第一次已被消费，第二次随后落到变化后的 UI。当前 r5 两个保存阶段
都只走一次输入，二次分支没有运行证据。最小安全合同是每个进程只发送一次
前台、无 fallback 的 Enter，并要求该 offset 后出现 `SaveSaving`；未出现就把
整次运行判为 non-acceptance，由下一独立进程重试，而不是在同一 UI 重放输入。

## No-Finding Boundary

Product/save 审核没有发现问题：archive/player/custom identity 仍精确，300 秒
只允许文档 clock 有界落后且与 Product 现有合同一致；journal scope 与 document
scope 仍完全相等。生产 cold recovery 在首次 native mutation 前注册 session，
不完整、失败或 outcome-unknown escrow 会 veto `SaveSaving`；既有物理 fixture
覆盖 backpack/mail mutation-then-throw 与不可读回读。r5 成功路径和这些故障
门足以支持 ISSUE-021 的 issue-specific `verified`，但不支持更广产品矩阵、
最终候选或 publication。

## Resolution Boundary

本 Review 只保存独立审查发现。runner 收敛、aggregate receipt 留存、文档措辞、
验证和提交由 owning Update `20260802-0001` 记录；R1--R3 的修复不形成新的
五切片周期。
