# 包依赖与共享契约 DLL

0.7.0 是首次公开 SDK 候选，尚未发布。作者工程统一使用 schema 4 与标准 MSBuild；当前 API 目标生成 manifest `DependencyContractVersion=1`。旧二进制、包格式与安装恢复继续由各自 reader 检查。

`manifest.json` 的 Dependencies 是 Mod 身份依赖权威。新项必须包含 UniqueID、Required、VersionRange；VersionRange 包含 minimumInclusive、可选 maximumExclusive、必填 includePrerelease。完整 SemVer 2 区间下界包含、上界排除；false 排除所有 prerelease，build metadata 不参与优先级。不要在新项中混写 MinimumVersion 或 IsRequired。Runtime 最低版本和游戏版本继续使用原比较规则。

在 Mod csproj 中引用普通契约库：

```xml
<ItemGroup>
  <ProjectReference Include="../Contract/Contract.csproj"
                    DtmApiRole="shared-contract" DtmApiDistribution="self-authored" />
</ItemGroup>
```
role 只接受 shared-contract 或 private-managed。DLL 文件名必须与实际 AssemblyName 相同，实际 TargetFramework 必须是 netstandard2.0。所有传递库均须显式携带；第三方库使用 licensed-third-party 并包含非空许可文件。声明不代表许可已经获得。游戏、Unity、BepInEx、Harmony 和平台提供的 DLL 不得复制进 Mod。BCL 接受集合来自冻结 NETStandard.Library 2.0.3 的 113 个实际引用 DLL 身份，记录在 bcl-reference-identities.json；第 114 个引用目录文件是 XML，不是 DLL。没有 System.* 前缀通行规则。

库进入 lib/shared 或 lib/private，许可文件进入 licenses。SDK 从实际 PE 生成 dtmapi-dependencies.json，并把它、manifest、DLL、许可和其他发布文件绑定进原 dtmapi-package.json 的 inventory。不要手改生成文件；内容变更后重新 build/pack 并增加 Mod 版本。不同源码/版本标签不能允许同 CLR 身份对应不同字节。

每个包单独安装时都应有完整库闭包。Provider 与 Consumer 分别引用并携带同一份 Contract DLL；接口、枚举和 DTO 放在该库中，避免原生类型、可变静态状态和初始化副作用。公开 GetApi<T> 使用准确 CLR Type；同名接口文字相同不构成类型相同。

Runtime 在 Entry 前检查全部选中包及其传递 AssemblyRef，再生成加载顺序。同 simple name、完整 identity 和 SHA 相同的库绑定一次；任意 identity/字节冲突会拒绝所有参与包及必需消费者，无关 Mod 保持可运行。private-managed 不提供 AppDomain 隔离。入口程序集不能重名。已驻留来源或原始字节无法证明时要求重启，磁盘覆盖不能更新已经加载的程序集。

required provider 缺失、版本不符或 Entry 失败时，consumer 不进入 Entry。provider 关闭时先关闭必需 consumers；consumer 清理失败保留 provider，下一次沿现有清理路径重试。optional provider 不可用时 consumer 可降级；但 optional Mod 声明不能免除真实 DLL 引用闭包。Provider 返回的对象仍由作者负责 owner guard；平台不会撤销别人缓存的普通对象或生成自动代理。

保留原始包及其 hash。旧 DLL 兼容性不由新编译器重建证明；不要仅改 marker、DLL 名或 MinimumDTMApiVersion 绕过校验。本格式提供完整性与兼容性预检，不是 CLR 安全沙箱。

## 普通契约库

在 SDK 开发环境中用 `dotnet new classlib -f netstandard2.0` 创建库。程序集版本由该库的 csproj 拥有；Provider 和 Consumer 分别引用同一库，并在各自包中携带其完整闭包。标准 restore/build 选择实际资产，pack 按 CLR identity、hash 和依赖检查最终运行库。生成器、不同 ref/lib、许可元数据和离线恢复见[工程与恢复](PROJECTS-AND-RESTORE.md)。
Generated shapes are described by `schemas/dtmapi-dependencies.schema.json` and `schemas/dtmapi-package-v3.schema.json`; hashes, intervals and cross-file consistency are validated by the tools. The installation-owned `.dtmapi-author-receipt.json` is outside immutable package payload inventory and cannot contain an executable.
