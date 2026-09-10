# R3.shared — 依赖格式与共享 CLR 类型

- Date: `2026-09-09`
- Status: `accepted with explicit limits`
- Owner: [PN-011 Update](../../../updates/2026/20260909-0012-platform-package-dependencies.md)
- Decision: **GO PN-010**。接收 P01–P04 的内部 0.6.3 格式和保守单 AppDomain 绑定边界；不是公开发行或完整 M3 验收。

## 核验

检查 Shared 严格 reader/SemVer/PE inventory/planner、SDK managedReferences/pack/install-local、Core 预检 loader/registry/必需 owner 关闭，以及 Doctor 的实际包字节验证。author schema 3 与 manifest DependencyContractVersion=1 显式选择新模式；旧 reader 没有转换，旧 BOM manifest 回归通过。入口/依赖文件、许可证、完整 PE AssemblyRef 及 marker 哈希闭包从实际产物校验。公开接口没有新增必实现成员或第六个强制 Runtime DLL。

固定仓库外 Contract/Provider/Consumer/Control 使用普通 SDK 产物；同一契约既证明 CLR Type/Assembly 相等及返回 63，也以两个真实冷启动反例证明相同 identity 异 bytes、同名异版本均在执行前共同拒绝。最终静态计数正常各 1、两种冲突均 0；无关 Control 运行。正常退出 consumer 先于 provider；provider Entry 故意失败时 required consumer 不启动，optional consumer 可降级。证据、原失败与保护恢复集中在[最终记录](../../../debug/evidence/GAME-SMOKE/20260909-platform-pn011-final-runtime/README.md)。

Core 全部 154 入口通过。实际 Runtime 集成另验证 consumer 清理失败时 provider 保持可用，重试完成才关闭；对已驻留程序集进行磁盘包替换仍触发 restart-required。固定 SemVer/旧 MinimumVersion、环、optional 排序、缺库/篡改/传递缺失、Strict 间接 native、宿主改名、缺许可、重复字段和旧 target/Advanced reader 回归通过。SDK pack-build、official-local、session handshake、target compatibility 与 Catalog 检查通过。

## 修复与边界

实机发现 SDK 部署收据被当作未列出的 payload 拒绝；仅排除原部署元数据名，伪装 MZ 仍拒绝。Core 回归同时发现 M2 scheduler 空闲 LINQ 快照每帧分配；修复空闲分支后，空 Runtime 及已激活空闲 scheduler 各 10,000 帧零分配。这不构成活跃队列的全量性能预算。

GetApi<T> 保留准确类型的普通对象，不提供可撤销代理；provider 自行守卫缓存对象，private-managed 不意味着隔离。驻留磁盘更新负例在实际 .NET Runtime 测试中完成；Mono 证据覆盖共享、两个冷冲突、Entry 失败及关闭。没有通用热卸载/任意静态副作用恢复、跨游戏 build 或实体输入承诺。Doctor 检查被提供包的完整性，不能从任意离线目录集合推导实际启用/驻留状态。

新目标 0.6.3 和最终 SDK 保留精确摘要；原 0.5.5、0.6.2、M2 0.7 与反射 0.8 历史快照不替换。下一步依 P05 实施 NativeContractVersion=1 的任意作者 Advanced；不能把本 Review 当作 native 准入或发布授权。
