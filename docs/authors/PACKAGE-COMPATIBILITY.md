# 包兼容与诊断

CodeMod 使用统一加载路径。Strict/Advanced、NativeContractVersion、DependencyContractVersion 与旧 SDK 证明不再决定准入等级。Core 根据官方来源、原生启用状态、最低 Runtime、实际代码与依赖、Owner 生命周期决定可用性。

新版展示与产品版本来自 info.json，旧包仍读 manifest.Version。未来未知 ManifestVersion 报格式不支持；最低 Runtime 太新单独报告。缺失展示字段不伪造版本。

Harmony 常规定时全局审计已移除。检查器不可用表示未检查；不会因此批量停用产品。Entry/模块激活中实际观察到的 Owner 冲突仍按对象处理；清理只撤销该 Owner 的补丁，无法证实完整清理时保留重启提示。
