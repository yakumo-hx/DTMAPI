# 0.7.0 SDK 功能验收与分发审查

- Lifecycle: recorded
- Scope: 用户本轮要求以 SDK 功能为主，确定作者工具上传方式，检查多平台玩家安装器是否已有本轮产物。
- Inputs: 源码 `53285790`，Runtime r5、SDK r4；实现任务已完成。原始验证及材料修正由 [本轮 Update](../../../updates/2026/20260910-0007-sdk-acceptance-distribution.md)拥有。

## 结论

接受 PN-038/039/040.a 在已声明 Windows 作者范围内的功能实现。独立解压 SDK 的普通工程、委托/JSON 恢复、编译常量/XML、CLI/IDE 一致性、旧 target、真实 Unity 泛型/作者类型打包与 Doctor 均有新复核。已有准确 r5 Mono 证明实际调用和 owner 关闭，本轮没有重新启动游戏或把元数据检查算成新的 Mono PASS。

仍有两项交付问题；它们不推翻上述 SDK 功能，但不能写成三种分发均已准备完成。

| 项目 | 判断与处理 |
| --- | --- |
| SDK 材料仍混入旧规则（P2） | 准确 r4 README 的包树把 native 描述画在 Content/DTMAPI，实际 marker 指向根目录；后文还把全部 Advanced 限为 registry、声称包只有入口 DLL，与自助 Native V2/依赖库矛盾。修说明并生成独立新 SDK，比较全部执行/引用字节；不重开 Runtime 实现 |
| 0.7.0 多平台候选缺失（P1，若承诺同步多平台发行） | dist 仍为 0.6.1。PowerShell 构建器接受条件及 C# installer host 都硬绑定 0.6.1；把 r5 输入前置检查会被准确拒绝。不能只重标 version 或复制 DLL。多平台候选须沿 PN-031.multi 补齐；Windows 技术接受不等于多平台 0.7.0 可发 |

## SDK 能力与限制

普通 netstandard2.0 辅助库无需 Mod 身份，配置常量与 XML 真正参与输出；明确不支持的自定义 Target 返回实际子工程错误，不会冒充 build 成功。NuGet 方法标志已区分 Runtime 委托与 Native，真实 Newtonsoft 13.0.3 恢复/离线重放/打包通过；私带 Json 与游戏驻留库的冲突仍是独立运行限制，宿主引用示例的创建/构建/打包和既有 Mono 证据成立。

Native V2 实际提取 MethodSpec/TypeSpec 并由共享结构比较开放定义；旧 V1/receipt 路径保留。新独立探针额外覆盖 GetComponents 的 List 泛型重载，只有 SDK/包层结论；实际 Mono 范围仍以原 r5 探针为准。更宽 TFM、不同 ref/lib、任意 MSBuild/生成器及未支持签名语法仍按文档限制，不称为与 SMAPI 完全等价。

测试选择遵循当前简化流程：重新验准确 ZIP 和代表性外部输入，复核原完整 Release 的准确 SHA/来源；没有为审查再次运行整套发行矩阵，也没有重复一小时标题测试。

## 发布决定

SDK 使用项目 GitHub Releases 的独立 ZIP + SHA256 附件，面向 Mod 作者；两个 Workshop Runtime 包面向玩家，保持各自安装边界。SDK win-x64 是开发工具平台，不是普通玩家安装包；多平台 Runtime 安装器也不等于 Linux SDK。具体步骤归 [SDK 发布流程](../../../workflows/author-sdk-release.md)。本轮没有上传或建立公开发布事实。

多平台后续保留已发布 0.6.1 的冻结来源和订阅记录，新增准确候选来源模式及 host 读取/升级验收，不能靠改掉旧 hash 常量削弱来源。候选可在公开上传前建立；候选来源不能伪造 Steam 已发布 manifest。Windows 与 WSL fake-game 通过只证明安装器，Steam Deck/CrossOver 的新 Runtime 注入仍须分别说明。

## 材料修正结果

[本轮 Update](../../../updates/2026/20260910-0007-sdk-acceptance-distribution.md)已纠正 README 和仓库入口，生成独立 SDK r5 并完成实际 ZIP/作者复核；只有指南及其 inventory 项变化，原执行/契约与 Mono 证据可有界复用。多平台缺口仍由 PN-031.multi 待实施，本次没有为了验收扩大到该安装器改造。
