# 官方来源选择、提交与实际部署

现行来源和身份由 [PROJECT](../../../PROJECT.md)、Loader 及 [ISSUE-017](../../debug/issues/ISSUE-017-20260801-official-mod-ui-source-transaction.md)、[ISSUE-020](../../debug/issues/ISSUE-020-20260805-enabled-official-source-arbitration.md) 拥有。本页保留历史因果，不另列当前产品/版本表。

## 身份、启用、选择、运行分别证明

包存在、订阅存在、高版本、DTMAPI 扫描和官方页面可见，不互相代替。5 月初次 UI 检测曾漏 `Local.` 前缀；随后真实官方显示/切换才关闭视觉缺口。修改 mod_infos 证明 gating，不能单独证明玩家能在界面操作。已加载 DLL 停用后仍驻留，来源切换按重启边界处理。

6 月 28 日曾修复 disabled 高版本 Local 遮住 enabled Workshop；7 月 author/source-mode 重构又将 Workshop 权重置于启用状态前，Unit 甚至固化错误结果。8 月重新沿 native owner 收口：官方 Local MODS 与原生证明的 Workshop install roots 才是玩家来源；先 enabled，再唯一最高 native priority，平手/无唯一赢家明确指导单副本。版本或内部权重不覆盖官方禁用。`<game>/Mods` 是旧开发目录，不能恢复为普通玩家发现根。

同名产品失败先确认 selected path、版本、Entry 字节以及实际进程加载身份。选对 Local 后仍失败，可能只是 Local 本身旧；不能再改来源算法去掩盖部署问题。来源：[8 月仲裁手测与根因](../../reviews/manual-qa/2026/20260805-0002-enabled-official-source-arbitration.md)、[0.6 兼容根因](../../archive/reviews/code/2026/20260801-0003-doloctown-100-dtmapi-060-compatibility-root-cause.md)。

## 官方页面刷新不是持久提交

Register 的 ReloadMods 是预览，Hide 内另一次 ReloadMods 又早于 SaveModManager。绑定正确 manager、成功保存、完整 close callback，下一帧一次权威刷新才构成提交。debounce、吞 warning、忽略第一次，或“先写文件再 refresh”的单测，都绕开真实 first-close 时序。保存失败、错 manager、无 candidate、打断保持前一来源。

未来只比较启动计划并提示重启的设计，不等于目前事务 Hook 可以删除；也不授权热加载依赖重建。准确完成度取 ISSUE-017/020，八例仲裁是一次修复矩阵，未被加入每次 Release。8 月 30 日旧 Y-console 冷启动子条件已被后继发布/玩家确认替代，不能继续挂旧 24585411 尾项；更广来源生命周期仍由 ISSUE-020 管理。

## 临时验收与用户最终将用的包

Candidate11 按其隔离合同测完恢复旧目录；用户长期手测环境后来只持久部署了 MoreSaves，没有更新 YConsole，因此后者仍 ResolveTargets 失败。临时新候选通过不能证明还原后的旧环境已更新。交付应明确本次是否实际留下了用户将用的包，不为此再创造新的 receipt 体系。来源：[发布阻断与本地旧包](../../archive/reviews/manual-qa/2026/20260805-0003-release-blockers-moresaves-live-package.md)。

旧 SDK managed 部署命令因仍写历史 Mods 根而暂停，新写入不得借 LegacyOfficialLocalOnly 绕过；旧状态/recover/withdraw 的恢复能力与当前 authoring 是不同边界。来源状态、手工发布和订阅字节继续回原 owner，Runtime Workshop 自动更新不等于复制到游戏的 Runtime 已更新。具体安装所有权见 [安装器知识](installer-boundaries.md)，作者工具演进见 [SDK 知识](sdk-and-author-tools.md)。
