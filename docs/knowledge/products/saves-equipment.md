# 存档位与装备侧车：保存边界的历史教训

现行保存语义以 [PROJECT](../../../PROJECT.md) 为准，测试前提与结束条件查 [产品变更验证](../../workflows/product-change-validation.md)，当前实现查 MoreSaves 与 MoreEquipmentSlots 源码。以下是历史研究，不能把旧第三档保护流程或迁移全矩阵重新设成普通小改默认条件。

MoreSaves 的原生责任只有 `GameManager.archiveFileCount`，产品固定开 12/关 6，游戏仍负责发现、读写、删档、复制及官方面板。其健康态曾永久订阅 UpdateTicked，只为很少发生的 manager-late 重试，却令 Core 每帧生成事件参数；修正是仅 retry pending 保留需求。owner 已停用时 Core 会移除事件，不能在 Unit 手动调用 Update 后宣称真实 Loader 仍会自动重试；终止清理失败应保留可诊断的租约，由后续显式清理或重启处理。

MoreSaves 的冻结 ABI 曾出现未注册却 IsConfigured=true、冷禁用不归六、混合 owner 丢失各自状态的语义回归。签名/MemberRef 相同不能证明旧行为相同。原生 owner 未变化的这类修正已明确可用 focused source/Unit 关闭并复用原短 smoke。

MoreEquipmentSlots 的三个产品槽与旧 ABI `0..24` 是两个契约。产品持有 Working/Committed 槽、侧车/journal、克隆 UI；游戏持有装备函数聚合、背包、邮件与原生存档。更换物品涉及 incoming 扣除与 outgoing 返还，必须按一组逻辑事务判断；失败保留重试证据，不能清 journal 后丢失已扣 incoming。`TryPlaceInBackpack(item,true)` 在邮件成功时仍可能返回 false，必须区分背包成功、邮件成功、真正失败。冷恢复测试需要产品 serializer 输出的真实 schema，手写旧 ABI 夹具不能证明新产品恢复。

7 月审查最初把每次盾受击同步持久化当作 P2 性能问题，用户的“不保存就应回退”语义把它改判为 P1 正确性问题。最终历史修正明确 GameplayMutation 留在 Working，原生保存成功才提升 Committed；Owner/OrphanRecovery 是独立目的。旧 `PlayerSaveRestored=Passed` 只能证明事后恢复，不能当作未发生原生保存的证据。原生保存、跨 store 崩溃窗口和 disabled cold recovery 确实需要对应专项；普通无保存动作不自动触发这些范围。

装备清理还要恢复实际 native 聚合属性，资源计数为零不代表数值已刷新。额外盾路径要保留 faint guard、官方盾优先、正确伤害原因、钓鱼清理及 hit-state 尾部；这些原生行为不能由简单效果策略 Unit 代证。Hook 双序及部分失败应调用真实产品 installer，不能只算 bool 或搜源码 token 后声称物理 rollback 已通过。

来源：[MoreSaves 语义与重试纠正](../../archive/reviews/code/2026/20260723-0003-sixth-product-commit-range-audit.md)、[装备迁移前的事务前提](../../archive/reviews/code/2026/20260723-0010-eighth-product-admission-runtime-weight-and-api-reuse-audit.md)、[装备事务与冷恢复修正](../../archive/reviews/code/2026/20260724-0001-eighth-product-commit-range-audit.md)、[未保存语义推翻旧 P2 判断](../../archive/reviews/code/2026/20260724-0004-eighth-ninth-product-split-closeout-audit.md)。

MoreSaves 最初曾讨论 24/60 槽及滚动 UI，6 月 12 日用户改为固定总数 12、关回 6，明确不是 6+12；这覆盖之前扩页愿望。标题按钮也曾从文字改为恢复 compact icon，说明不能把更旧截图建议当今天 UI 验收标准。

来源：[早期 24 槽与 UI 建议](../../archive/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md)、[固定十二槽覆盖](../../archive/reviews/manual-qa/2026/20260612-0004-title-saveslots-autofishing-regression-review.md)。
