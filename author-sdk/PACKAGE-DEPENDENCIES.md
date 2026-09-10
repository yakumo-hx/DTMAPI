# 包依赖与共享契约 DLL

内部 SDK/API/Runtime 0.6.3 首次提供依赖格式 V1，尚未公开发布。默认 new 使用 author schema 3、manifest `DependencyContractVersion=1`；显式旧 target 保留旧 schema/reader。0.6.2 的原始载荷与工具 ZIP 继续保留。公开 0.7.0 仍 planned。

`manifest.json` 的 Dependencies 是 Mod 身份依赖权威。新项必须包含 UniqueID、Required、VersionRange；VersionRange 包含 minimumInclusive、可选 maximumExclusive、必填 includePrerelease。完整 SemVer 2 区间下界包含、上界排除；false 排除所有 prerelease，build metadata 不参与优先级。不要在新项中混写 MinimumVersion 或 IsRequired。Runtime 最低版本和游戏版本继续使用原比较规则。

示例作者输入（在 SDK 生成的 dtmapi.author.json 内添加；不会原样复制本地路径到运行包）：

```json
"managedReferences": [
  {
    "path": "../Contract/bin/Release/netstandard2.0/Example.Contract.dll",
    "role": "shared-contract",
    "distribution": "self-authored",
    "licenseFiles": []
  }
]
```

role 只接受 shared-contract 或 private-managed。DLL 文件名必须与实际 AssemblyName 相同，实际 TargetFramework 必须是 netstandard2.0。所有传递库均须显式携带；第三方库使用 licensed-third-party 并包含非空许可文件。声明不代表许可已经获得。游戏、Unity、BepInEx、Harmony 和平台提供的 DLL 不得复制进 Mod。BCL 接受集合来自冻结 NETStandard.Library 2.0.3 的 113 个实际引用 DLL 身份，记录在 bcl-reference-identities.json；第 114 个引用目录文件是 XML，不是 DLL。没有 System.* 前缀通行规则。

库进入 lib/shared 或 lib/private，许可文件进入 licenses。SDK 从实际 PE 生成 dtmapi-dependencies.json，并把它、manifest、DLL、许可和其他发布文件绑定进原 dtmapi-package.json 的 inventory。不要手改生成文件；内容变更后重新 build/pack 并增加 Mod 版本。不同源码/版本标签不能允许同 CLR 身份对应不同字节。

每个包单独安装时都应有完整库闭包。Provider 与 Consumer 分别引用并携带同一份 Contract DLL；接口、枚举和 DTO 放在该库中，避免原生类型、可变静态状态和初始化副作用。公开 GetApi<T> 使用准确 CLR Type；同名接口文字相同不构成类型相同。

Runtime 在 Entry 前检查全部选中包及其传递 AssemblyRef，再生成加载顺序。同 simple name、完整 identity 和 SHA 相同的库绑定一次；任意 identity/字节冲突会拒绝所有参与包及必需消费者，无关 Mod 保持可运行。private-managed 不提供 AppDomain 隔离。入口程序集不能重名。已驻留来源或原始字节无法证明时要求重启，磁盘覆盖不能更新已经加载的程序集。

required provider 缺失、版本不符或 Entry 失败时，consumer 不进入 Entry。provider 关闭时先关闭必需 consumers；consumer 清理失败保留 provider，下一次沿现有清理路径重试。optional provider 不可用时 consumer 可降级；但 optional Mod 声明不能免除真实 DLL 引用闭包。Provider 返回的对象仍由作者负责 owner guard；平台不会撤销别人缓存的普通对象或生成自动代理。

迁移旧工程时，先保留原工程与包，使用新 SDK/new 生成 schema 3 工程，再迁入源文件、显式 managedReferences 和新区间。需要沿用旧依赖解释时继续选旧 schema/target；不要仅改 marker、DLL 名或 MinimumDTMApiVersion 来绕过载荷边界。本格式是完整性与兼容性预检，不是 CLR 安全沙箱。

## Build a separate contract library

The PN-011 recipe uses the .NET 8.0.421 C# compiler and the selected SDK payload's `ref` directory. It does not reference a repository project or game DLL. Keep the contract to interfaces, enums and DTOs; avoid mutable static state and initialization side effects.

```powershell
# Set these to your installed .NET 8 toolchain and extracted author SDK.
$dotnetRoot = 'C:/tools/dotnet-8'
$authorSdkRoot = 'C:/tools/DTMAPI-Author-SDK-0.6.3-win-x64'
$compiler = Join-Path $dotnetRoot 'sdk/8.0.421/Roslyn/bincore/csc.dll'
$references = @(Get-ChildItem "$authorSdkRoot/compatibility/0.6.3/ref" -Recurse -Filter '*.dll' |
    ForEach-Object { '-r:' + $_.FullName })
& "$dotnetRoot/dotnet.exe" $compiler -nologo -target:library -deterministic -nostdlib `
    -out:Pine.Contract.dll @references Contract.cs
if ($LASTEXITCODE -ne 0) { throw 'Contract compilation failed' }
```

`Contract.cs` declares `[assembly: System.Runtime.Versioning.TargetFramework(".NETStandard,Version=v2.0")]` and an explicit `[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]`. Provider and Consumer each point `managedReferences` at the exact same resulting DLL. Each packed Mod contains its own complete copy; Runtime binds identical copies once. The distributed BCL identity list is `bcl-reference-identities.json`.

Generated shapes are described by `schemas/dtmapi-dependencies.schema.json` and `schemas/dtmapi-package-v3.schema.json`; hashes, intervals and cross-file consistency are validated by the tools. The installation-owned `.dtmapi-author-receipt.json` is outside immutable package payload inventory and cannot contain an executable.
