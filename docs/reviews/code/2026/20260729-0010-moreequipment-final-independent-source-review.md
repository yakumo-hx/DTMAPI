# 20260729-0010: MoreEquipmentSlots 最终独立 Source Review

Status: `recorded / P0=0 P1=4 P2=1 / correction required; no round-4 review`

## Scope

本轮是用户允许的第 3 轮、也是最后一轮子智能体独立只读审查。基线与实际执行
HEAD 为 `df1b8dc474483907885c9c82d2f803b230df404c`，工作树 clean。审查覆盖：

- historical Product/archive/no-claim 回填及不同 source revision；
- pending/completed claim 的新旧进程混合竞争；
- same-volume global capture、archive 和崩溃恢复；
- pre-schema 与 identity-bearing global flat 的终态；
- 生产 Hook、resident/cold Compatibility Host、eligible `.previous`；
- Update、Batch 6 合同、产品 README 和提交/包 provenance。

审查没有修改文件、启动游戏、安装 Runtime、修改玩家存档或运行完整 Release。

## Result

```text
P0 = 0
P1 = 4
P2 = 1
MoreEquipmentSlots = implemented / acceptance-open
```

四个聚焦入口均通过，但没有覆盖下列四个 authority 顺序。

## Findings

### P1-1: completed barrier 不是 game-root 单赢家

per-hash claim 路径为 `<sourceSha256>.json`。两个历史候选可分别留下
`Product A + archive S1 + no claim` 与
`Product B + archive S2 + no claim`。升级后两边因 source hash 不同而互相忽略，
各自在不同路径回填 completed。无身份 global 的同一 game-root 最终出现两个存档
authority。

修正门：增加与 source hash 无关、create-if-absent 的 game-root winner/index；
它精确绑定 scope 与 source，另一 winner 永不覆盖。补不同 hash 历史终态的顺序及
真实双进程竞争，loser 字节保留 fail-closed。

### P1-2: pending 到 completed 会覆盖迟到的不同 scope claim

完成路径读取并验证旧 pending 后保存 `claimExists=true`，在开放 fault window 后仅
凭旧布尔值执行 `File.Replace`。旧 pre-durable 进程可先删除原 claim，迟到 scope B
再发布 pending B；当前 scope A 随后把 B 静默替换成 completed A。

修正门：不得依据陈旧预检布尔值 Replace。完成必须以跨进程 singleton/CAS 或原子
捕获证明当前 owner；不同 scope/source/state 的原字节必须保留并 fail-closed。

### P1-3: capture rename 后崩溃没有重启入口

global 被 rename 到随机 `.migration-capture-*` 后，如果进程在 archive 发布前
退出，`catch` 不会执行。重启只识别 global 或 deterministic archive，因此
`Product + pending + exact capture` 会永久卡住。

修正门：capture 必须可枚举并绑定 expected hash；只有 exact
Product/pending/backup/capture 组合可以恢复到 deterministic archive。mismatch、
多个不同 capture、replacement global 均保留并 fail-closed。

### P1-4: `GlobalFlat` 未证明最终 global absent

`PreSchemaGlobal` 完成后会复核 global/archive/Product，但 identity-bearing
`GlobalFlat` 在 `after-global-archive` 重建 global 后直接返回成功。当前会话可同时
存在 Product 与 active global。

修正门：所有 global source kind 返回前都重新证明 active global absent 与 exact
archive；重建时两份字节均保留并 fail-closed。覆盖 exact/different replacement
及 existing exact/different archive。

### P2-1: 文档并发结论超前

Update 与产品 README 宣称 historical backfill 不会覆盖不同并发 claim，但
P1-1/P1-2 尚未关闭。Batch 6 总体 `implemented/acceptance-open`、提交 provenance
与 replacement package 开放状态正确。

## Confirmed Boundaries

- 生产 Hook 仅经 feature fanout 分发一次 EquipmentSlots SaveLoaded；
  SaveSaving/SaveSaved 各一条生产 persistence 路径，resident backend session 可达
  terminal。
- same-source late child、state-less pending、completed publish crash/restart 和
  global completion revalidation 的直接矩阵成立。
- `TryLoadValidated` 对 missing/corrupt live 的 eligible previous 回落成立；
  future/wrong-scope/revision-regressed live 与 invalid/wrong-stamp previous
  fail-closed。
- resident Host recovery、lone `.json.previous` demand 和 loaded Product owner
  suppression未发现新增缺陷。

## Executed Checks

```text
HEAD = df1b8dc474483907885c9c82d2f803b230df404c
targeted Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
```

本 Review 只拥有独立发现与确认边界。修正生命周期继续由 Update
`20260723-0008` 所有；由于这是用户允许的最后一轮子智能体审查，后续修正必须以
实现方聚焦验证和诚实的剩余开放状态收口，不得声称另有第 4 轮独立 acceptance。
