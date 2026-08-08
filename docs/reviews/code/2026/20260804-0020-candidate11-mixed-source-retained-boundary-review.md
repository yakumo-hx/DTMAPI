# Candidate11 九源码候选与两 retained 实物边界审查

## 记录信息

- 日期：`2026-08-04`
- 状态：`recorded`
- 性质：DTMAPI 0.6.0 Candidate11 source transaction root-cause review
- 审查 HEAD：`2adb2872a8726e892dd246de38665d5144eaa967`
- Source / implementation owner：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- 历史事务边界：[Batch 5 收尾与 Batch 6 Phase 0 验收审计](20260720-0003-batch5-closeout-and-batch6-phase0-acceptance-audit.md)

本 Review 记录最终 11-Mod `Local11` 事务在运行前暴露的 artifact-boundary 漂移。它不授权 MoreEquipmentSlots `1.0.0`、不改变 Manbo 的普通 retained 激活边界，也不把历史 Candidate11 PASS 冒充当前 0.6 候选结果。

## 1. 触发事实

当前 0.6 路线的 11 个公开 Mod 不是 11 个源码重建包，而是：

- 九个由现行 Author SDK policy 生成的 ProductNative 源码候选；
- exact retained MoreEquipmentSlots `0.3.1-dtmapi`；
- exact retained Manbo `0.1.0-dtmapi`。

`build-release-workshop-packages.ps1` 已正确只选择九个 ProductNative，并在调用 builder 前排除 MoreEquipment、Manbo、StrongPlantingGun 与 Mine。`run-game-smoke.ps1` 的 `Published11` artifact preflight 也已按九项 `CurrentPublished`、两项 `Retained` 分流。

但 `candidate11-source-transaction.ps1` 的 `Get-Candidate11CatalogSelection` 仍把 11 行一律解释为 source candidate：

- 要求每行存在 `sourceVersion`；
- 要求 `manifest.Version == sourceVersion`；
- 要求 `info.version == sourceVersion`；
- 不读取 `releaseStop` 的九项源码选择或两项 retained identity；
- 不把 retained package tree 与 Catalog 的 file/byte/tree authority 比较。

这条旧假设适用于 2026-07-19 的历史 11-source Candidate11，但不适用于当前 0.6 路线。

## 2. 两个具体冲突

### MoreEquipmentSlots

Catalog 当前事实为：

- `sourceVersion=1.0.0`；
- `publishedVersion=0.3.1-dtmapi`；
- `releaseStop.explicitlyExcludedExistingWorkshopUpdates` 精确保留 `0.3.1-dtmapi`；
- retained tree：`9` files / `539,565` bytes / `E0854CEE94969D98B916A3F6085FD03773C67BCD35C7BC83DC2894F8156E0CA6`。

Branch B 已禁止自动发布 1.0。若为了满足旧 Candidate11 preflight 而生成或塞入 MoreEquipment `1.0.0`，会直接越过用户尚未决定的 save/recovery 扩面门；若诚实放入 retained `0.3.1-dtmapi`，旧 preflight 又会以 source version mismatch 拒绝。

### Manbo

Catalog 当前保留 `manifest.Version=0.1.0-dtmapi`、minimum `0.5.2-alpha`，retained tree 为 `7` files / `229,384` bytes / `23A3209E75788B68041F4E1EECFE81550F87D6579893272AF67711B2C0BFC40E`。实际 exact Workshop `info.json` 的 Workshop metadata version 为 `1.0.0`，不等于 Mod manifest 版本。

因此 retained tree 不能沿用 source package 的 `manifest.Version == info.version` 假设。它必须核对 Catalog-published manifest identity，同时由 exact retained tree 冻结 `info.json` 的真实字节。

## 3. 根因分类

这是发布事务工具的 selection projection 过期，不是产品包损坏：

1. 历史 Candidate11 以 Catalog 全部 `sourceVersion` 作为候选来源；
2. 0.6 路线后来明确把 mutation set 收窄为九个 Advanced ProductNative，并把 MoreEquipment/Manbo 留在 retained lane；
3. release builder、Published11 preflight 已同步，Local11 source transaction 尚未同步；
4. 现有 synthetic test 的 11 个 fixture 又全部使用同一 source version，因而无法反证混合边界。

首次把真实 0.6 候选与两棵 retained subscription tree 组装后，还暴露出两个同根因的宿主/格式投影遗漏：九个现行 Author SDK 包使用 schema 2 `CodeMod / Advanced / dtmapi-author-sdk-package-binding` marker，而两棵 exact retained 包仍保留没有 `packageKind` 的 legacy marker；旧 synthetic fixture 伪造的统一 `workshop-mod` marker 两边都不真实。其次，retained tree 与 Runtime assembly-set 摘要曾依赖 `Sort-Object`；PowerShell 7 与 Windows PowerShell 5.1 对真实混合大小写路径或有序字典的排序并不一致，使同一字节在 5.1 下得到不同 retained/Runtime 摘要。前者会错误拒绝合法候选，后者会让事务回执不可跨宿主复现。

构建 11 个源码包、手改 retained manifest/info、放宽为“任意 11 个能加载的目录”或只看 entry DLL 都不是修复。它们会分别越过产品授权、改变冻结字节、丢失 exact tree provenance 或削弱事务的 fail-closed 目标。

## 4. 有界修复

`candidate11-source-transaction.ps1` 应复用既有 Catalog/release contract，而不是新建 selection authority：

1. 调用 `Get-DtmApiReleaseContractAdvancedProducts` 得到精确九个 source candidate Catalog ID；
2. 其余两个 PublicWorkshop 产品必须恰为 `more-equipment-slots` 与 `manbo-cardboard-audio`，并分类为 `RetainedPublished`；
3. `CurrentSourceCandidate` 继续核对 source version、source minimum 与 `info.version`；
4. `RetainedPublished` 核对 published manifest version/minimum，不强迫 `info.version` 等于 Mod 版本；
5. retained tree 必须按既有 `DTMAPI-Retained-SHA256SUMS-v1` 规范化并精确等于 Catalog 的 file count、bytes 与 tree SHA-256；
6. selection/preflight receipt 明确记录 artifact boundary、期望/实际 manifest/info 版本及 retained authority；
7. 九个 `CurrentSourceCandidate` 必须核对真实 SDK schema 2 marker、`CodeMod / Advanced`、entry/receipt 路径与 binding authority；两个 `RetainedPublished` 只核对冻结 legacy marker 的 owner/unique ID，全部其余 marker 字节仍由 exact tree authority 覆盖；
8. retained tree 继续逐字复用历史 `SortedDictionary(StringComparer.OrdinalIgnoreCase)` 规范，Runtime assembly-set 改用 `StringComparer.Ordinal`；两者均以固定摘要向量锁住跨宿主次序，`Published11` 游戏预检与 Candidate11 使用同一 retained 算法；
9. 保持一个 Runtime package、11 个产品目录、零额外目录、零 reparse/ADS、same-volume `Directory.Move`、候选不变、原树精确恢复和无残留进程合同。

不修改 Catalog schema，不创建 receipt family，不把 `currentPublishedArtifact` 用作 source candidate 期望，也不改变 Package Builder 的九产品集合。

## 5. 必须新增的反证

聚焦测试至少锁住：

- synthetic 事务真实包含 `9 × CurrentSourceCandidate + 2 × RetainedPublished`；
- retained manifest/info 版本不同仍因 exact tree 而通过；
- 把 MoreEquipment retained manifest 改成延期 source version 必须失败；
- retained tree 任一字节漂移必须在 staging 前失败；
- current source marker 降回 legacy `workshop-mod` 必须在 staging 前失败；九个 SDK marker 与两个 retained legacy marker 必须分别形成精确计数；
- 固定 retained 与 Runtime assembly-set 摘要向量必须在 PowerShell 7/Windows PowerShell 5.1 相同；
- tracked Catalog 当前也解析为九/二精确集合；
- 从 clean Runtime + 九产品 staging 与当前两棵 exact subscription tree 组装的真实 `Local11` 根必须在两宿主产生相同 retained tree、Runtime assembly-set 与 Runtime binding；
- 原有 smoke failure、post-smoke product drift、installed Runtime drift、unexpected directory、ADS、missing Runtime 和 exact restore 负例继续通过；
- PowerShell 7 执行与 Windows PowerShell 5.1 syntax gate 均通过。

## 6. 解决路由

本 Review 保持 `recorded`，不拥有实现状态、changed files 或 PASS。提交、聚焦验证、真实 mixed preflight、最终 Local11 游戏结果和任何后续纠正统一记录在 [DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)；第六轮并行审查发现的 SDK payload 绑定缺口按其独立 Review 的关闭条件处理。

## 7. 运行与发布边界

双宿主真实 preflight 只证明最终 `Local11` 事务能够诚实接收当前 artifact set；它不是游戏 PASS。clean Runtime + 九源码候选、两棵 retained 只读复制与 exact mixed preflight 已完成。后续仍须先完成本组五切片并行审查，再在安装与游戏操作前取得共享 Runtime lock，用 Steam 路径、第三存档、`NoNativeSave`、普通 no-QA `Local11` 运行 ISSUE-011 clean smoke；无论 PASS 或 fail-closed，都保留 exact product/runtime/restoration/process/crash-package 证据。

MoreEquipment Branch B 仍是局部产品阻断。若最终 11-Mod smoke 因其真实兼容路径失败，该失败不能通过移除产品、换成 1.0 或降低断言改绿；其余 ISSUE-011 与产品结果应按可分离证据继续记录。

## 8. 回滚

可回滚本次 projection 修正并让当前 Local11 preflight 明确恢复为阻断；不得回滚 releaseStop 的九产品选择、重新把 MoreEquipment 1.0 放入 builder，或把 retained exact tree 降为版本字符串检查。历史 2026-07-19 Candidate11 收据继续只拥有其当时 11-source 字节，不因本修正被改写。
