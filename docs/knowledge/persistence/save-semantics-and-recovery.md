# 保存、回滚与恢复的证据边界

现行规范仅由 [PROJECT](../../../PROJECT.md#游戏存档提交语义规范)及 [产品验证流程](../../workflows/product-change-validation.md)拥有。本页保存历史推理和不能丢失的失败类型。

## 玩法变化与所有者恢复

装备、耐久和背包属于同一原生保存提交的玩法状态。Working、未提交 candidate、Committed 与 owner/orphan recovery 需要不同语义，不能让普通未保存操作借恢复 journal 成为永久进度。

原生放置可能溢出到邮件而返回 false，也可能改完数据才抛异常。准确立即观测应区分背包、邮件、确无变化和结果不明。结果不明不得重试、不得用未来无关同物品计数变化补认；完整记录与隔离需在动作前建立。

## 恢复验收为何有冷进程

[ISSUE-021](../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md) 曾拒绝“一次 NoNativeSave 中看见恢复日志，随后删除 seed”的假闭环。其特定事务由准备并正常原生保存、独立冷进程恢复再保存、第三进程确认终态不重放组成。一个 fixture 沿三个阶段复用；失败保留，最终磁盘未变与进程退出证明完成后整体清理。

保留的准备失败包括旧 Runtime manifest、过度严格的背包前提、精确时钟 QA 假阴性及游戏未消费的一次 SendInput；它们不能拼接成最终 PASS。这套三进程规则属于改变恢复事务的验收，不是普通 UI 或无保存游戏测试的默认前提。

20260724-0007 还指出空→空 oracle 的局限：执行伤盾、破盾、替换后把所有临时物品清掉，只能证明没有提交新变化，不能证明已有 committed 盾的耐久和唯一物品能够回滚。后续验收先正常保存非空盾，再分别验证未保存伤害、破损和替换后的冷载恢复。外层保存目录环境变量也需在成功和失败出口恢复，避免污染同一 PowerShell 会话的下一次任务。

## 观察失败不能变成零

20260730-0012 至 0016 从 QA 顶层邮件链深入到嵌套附件，再发现独立生产 reader 同类漏洞。可枚举不等于合法集合（string 也实现 IEnumerable）；缺 Id、接受状态、reward 类型、item/count 不能靠默认值解释为零。测试应让真实对象图通过生产使用的 reader，不能只向后续校验器注入一个整数。

更严格 reader 只是第一步：原生调用后观察失败必须隔离结果不明，不能把已发邮件视为普通失败并保留可再次发放的侧车。输入扣料也要验证精确 -1；空/全 missing 的指纹相等不是 native commit 证据；cold journal 的粗略 count 增加无法证明此事务；延迟重新读取时玩家可能已收信、消耗或独立获得同物品。历史五项 P1 后来被用户延期，延期不是技术关闭，后续 Branch B 又在明确授权下重开。

## 原生版本变化的实际触发

20260804-0007 比较旧/新 build，邮件和背包方法未改，但 native archive 家族已转为 current/prevN/bak 并改变备份轮转；因此只修 attack 第五参数不能证明旧 Host 的保存事务。无关 Mod owner 不应触发 EquipmentSlots 全局 Hook release：20260804-0017 中 AutoFishing 专项已成功，通用清理却因无关 EquipmentSlots 的旧 target 失败。正确修复是无资源 owner 的 no-op，保留真实 owner 的失败门，不能豁免通用 cleanup。

## 用户纠正了测试恢复的证据含义

[20260724-0001](../../archive/reviews/manual-qa/2026/20260724-0001-moreequipment-unsaved-save-commit-regression.md) 两项问题依次为：①盾值/装备 prepared intent 提前越过原生提交点；②无保存测试不应靠备份写回制造回档。第一项是既有不保存退出基线的回归，不能降成每击 I/O 的性能取舍；正常玩家常经睡眠保存，但技术边界是 native SaveGame 成功，其他明确保存入口也算。

原 runner 曾先复制、退出后无条件写回，还把 config 中 sidecar 一起恢复，可能掩盖意外存档与提前 sidecar 提交。历史统计为 908 份备份、约 1.38 GiB；只记录恢复后 hash 不能证明此前未变。Steam 在退出附近先扫描、runner 后写回的顺序证明存在竞争窗口，但不证明另一次云冲突由它造成。共享锁和故意改过的安装/配置恢复仍有独立用途。

该历史 P1 后已由 Working/Committed、聚焦故障注入、两次 NoNativeSave 和两次隔离提交/恢复关闭，三次失败仍保留为 non-acceptance；明确没有完整 Release/GC 梯度/长测。当前可处置槽的授权例外与普通测试执行方法仍以 PROJECT 和产品工作流为准，不把历史要求一律隔离重新升级成现行门。

## 早期提前落盘已经证明了哪一层

[6 月 6 日手测](../../archive/reviews/manual-qa/2026/20260606-0002-028-manual-qa-followup-review.md) 已定位复制帽子：先扣内存背包、立即写 owner-global JSON，玩家未 native save，重读原生背包回旧状态而侧车保留新物品。0.2.9 改 dirty、SaveGame 后 flush、读档/标题丢未提交状态；当时 sidecar timestamp 只在原生保存后变化。这关闭提前写盘，仍未证明按档隔离、Product 缺席恢复、满包或删档新建，因为该版仍用 owner-global 文件。

8 月真实升级手测由玩家主动完成原生保存，后续只恢复部署/上传/启用，不能把已经提交的玩家进展覆盖回测试前。与 NoNativeSave 的检查边界应分别表达。来源：[旧 Runtime 升级手测](../../archive/reviews/manual-qa/2026/20260801-0002-old-runtime-newmods-upgrade-compatibility.md)。
