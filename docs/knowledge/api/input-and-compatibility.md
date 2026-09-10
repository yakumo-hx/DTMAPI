# 输入与兼容性定位知识

本文是历史阅读提炼；当前输入规则由 [输入生命周期规范](../../design/smapi-like-input-lifetime-contract.md) 拥有，Mod 身份与生命周期边界由 [PROJECT](../../../PROJECT.md) 拥有。逐篇处理见 [API 阅读清单](../../archive/migrations/20260908-workspace.json)。

## 输入问题如何定位

- 同帧输入抽样、事件和 query 应读取同一 frozen frame；overlay 在抽样后开闭，输入资格次帧生效。隔离输入不等于停止世界更新。
- 记录收到 Pressed 的精确 owner，之后因 UI、scope 或配置失去资格仍结清该 Release；owner 已卸载则丢弃回调，避免调用 dead code。
- 资格丢失后等待物理 neutral 再接收 Pressed，避免遮罩后积压按键。DTMAPI 的 Suppress 不承诺阻断 native/Unity/第三方直接输入。
- 长驻少量动作键与活动会话中的临时 query 应分开分析，避免为 query 新增永久 UpdateTicked。具体注册数量继续查输入规范和产品 owner。

## 兼容性解释中的常见混淆

- [Fishing 兼容地图](../../hook-map/focused/FishingAutomationCompatibility.md) 声明 dormant-shipped Host、首个有效冻结 ABI 调用激活及 exact AutoFishing owner 排斥。其 22 patch/21 methods 是冻结兼容库存，不能投影成当前产品或未来游戏基线。
- 同一个原生目标上的无关产品 owner 不等于 AutoFishing 冲突；兼容排斥须按确切产品身份处理。
- 已停用并清理 roots 与 Mono assembly unload 是不同事实。旧代码仍驻留时，更新后的行为验收依赖适用的 clean restart。
- [旧 DLK/SMAPI 迁移说明](../../archive/guides/2026/migrate-from-old-doloc-smapi.md) 的版本号、UI 路径属于旧发布语境；可保留的诊断线索是旧 Runtime 与新 Runtime 并存、手动 local MODS 残留、进程未重启。当前安装与删除行为必须查当前 installer owner；不得按旧文本批量删除 Steam 缓存、存档、配置或未获归属证明的包。

## 已读具体回归的启示

- [DebugConsoleInput](../../hook-map/focused/DebugConsoleInput.md) 以 `24456188_test_E861E0` 并比较 `23762374_public_C416D4` 定位三个输入 Prefix、四参 CostItemAt 和最终 MoveSpeed。Public `24788406` 玩家记录暴露 typed Y 在 UpdateTicked focus guard 之前执行；Hook 都安装成功不能证明文字输入隔离正确。
- 同页记录 movement 应乘最终 getter 结果且限当前 player，不能覆盖 native Buff 聚合器 MoveScaler；ordinary modal close 与 speed reset 是不同动作。
- warmed frame 不应重复实现未改变的 Hook topology；只在实际 demand 零/非零转换时安装或拆除，失败 tombstone 归精确 owner 恢复。
- 2026-08-30 用户释放旧 Y-console consumer 保留约束是历史决定，物理代码删除是否完成仍查当前产品/Compatibility owner，不能由此页代为宣称已经删除。

## 版本标签的历史约束

[June 版本 review](../../archive/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md) 记录当时 minimum-version 比较去掉 `-`/`+` 后缀、按数字比较，与严格 SemVer prerelease 排序不同；API 的稳定性仍由矩阵决定。它另有实际负证据：BepInEx plugin attribute 不接受 `0.5.0-alpha`，故 binary metadata 与可读 API label 当时分开。这里保留历史因果，不把 `0.5.0` 数值或该时点 loader 策略写成当前约定；当前版本与比较规则应查其源码 owner 和后续兼容 review。

[7 月 26 日 DebugConsole 迁移前审查](../../archive/reviews/api/2026/20260726-0002-debugconsole-admission-prerequisite-review.md) 固定 `24256979_test_7A1907`，先把等价行为与所有权移出 Bootstrap，再独立改界面。原生 `DisableAllInput` / `ResumeCurrentInput` 没有嵌套 owner token，盲目恢复会释放别人的输入锁；因此等价迁移保留已验证的三个 Prefix 和 Escape 两个干净帧 drain。物品/钱/科技等 Working 变更、明确调用 SaveGame 的提交与 movement/time-scale/creative 瞬时 lease 分开处理，清理不能替玩家提交或撤销已提交结果。Review 的旧 admission 阻塞、第三档/隔离矩阵不能覆盖后续实施记录和当前 [产品验证流程](../../workflows/product-change-validation.md)。

7 月边沿修正还证明：只看最终 down 会漏同帧短按，查询须按 ownerId+keybindId，typed release/chord 和同帧缓存需要真实三状态帧。Win32 fallback 可防按住重复，却不能恢复全部漏采样的松开再按。Core Suppress 仅清本帧 DTMAPI 逻辑输入/后续派发，不能撤销已执行 handler，或阻止 native/Unity 输入。该支线已有人工和短测接受，不作为永久开放缺陷；当前合同仍归上方输入 owner。来源：[边沿采样](../../archive/updates/2026/20260707-0005-hotkey-edge-sampling.md)、[人工后继](../../archive/reviews/manual-qa/2026/20260707-0002-hotkey-edge-followup-review.md)。
