# DTMAPI Author SDK 0.7.0：本次设计指导的包内依据

审阅日期：2026-09-10。

对象：`DTMAPI-Author-SDK-0.7.0-win-x64.zip`

SHA-256：`78086042d6dc7a7aae4aa9d3bbad50a135734981033e753e1b25e25833c35eb7`

## 阅读边界

本记录仅摘录本轮实际解包读取的模板、构建配置、schema 相关说明与随包作者文档。未运行 Windows CLI、MSBuild、Doctor 或目标游戏 Mono，未重新执行用户粘贴报告中的编译对照实验。随包文档记载的测试结果仍是文档陈述，不是本轮独立复现。

包的 sdkVersion 是 0.7.0，随包文档标为未发布候选；没有把同名旧源码审计 ZIP 当成本次更新版实现。SHA-256 仅用于标识本次文件，不等于官方签名或独立可信证明。

已对本记录涉及的 6 个、列入包内 release inventory 的文件核对其自述 SHA-256；该检查仅证明文件与随包 inventory 一致，不证明运行行为。

这些摘录用于支撑设计建议，不是新增项目治理文件或开发者需要维护的清单。行号均从包内原文件第一行开始。

## S1 本次包身份、当前作者支持说明

### `README.md`，L3–L23

```text
L3: 当前 0.7.0 候选的任意作者 Advanced 工作流见 [本机原生引用](NATIVE-REFERENCES.md)。新项目使用 NativeContractVersion=2；旧 V1 与 receipt 包保留各自校验路径。
L4: 
L5: `dtmapi-author` is the single authoring authority for SDK `0.7.0`. Complete M3 API `0.7.0` is the default and requires Runtime 0.7.0; retained targets keep their own catalog ranges. Available means buildable from the exact committed source recipe, not published: release acceptance is still in progress and the published Runtime remains `0.6.1`. The Windows x64 ZIP is self-contained on .NET 8; generated game-loaded CodeMods remain `netstandard2.0`.
L6: 
L7: Internal `0.6.3` adds [package dependencies and shared DLLs](PACKAGE-DEPENDENCIES.md). New projects select author schema 3; older projects require explicit migration.
L8: 
L9: ## API target selection
L10: 
L11: [target-catalog.json](target-catalog.json) defines the default and available API targets, each payload's immutable contract, and its supported package Runtime range. `new --api-target 0.5.5` selects the retained target explicitly; omission uses `0.7.0`. Available means buildable, not published. `build` and `pack` use the project's `targetDtmApiVersion`; they do not infer it from the installed game or Runtime version. Schema-1 projects keep their original `targetRuntimeVersion=0.5.5` field without automatic rewriting.
L12: 
L13: The retained internal `0.6.2` contract combines the Experimental [platform services](PLATFORM-SERVICES.md) and [reflection](REFLECTION.md). Save-bound data remains outside the current API. Earlier unpublished M2 and reflection candidates remain repository history and are not distributed SDK targets. Old hashes and packages are never relabeled.
L14: 
L15: New packages must declare a minimum Runtime within the selected target's catalog range; promising a version below the compiled API target is now an error. Readers retain the lower-minimum behavior of previously issued SDK 0.1.0 / API 0.5.5 Strict and ContentPack packages, including existing receipt recovery; Advanced policy requirements remain enforced. This does not alter frozen payload bytes. An explicit `--compatibility-root` or `DTMAPI_AUTHOR_COMPAT_ROOT` is authoritative: missing, mismatched or modified payloads fail instead of silently falling back to another directory. The executable embeds the catalog and original compatibility contract; editing the distributed JSON cannot authorize another target.
L16: 
L17: `session prepare --api-target <version>` requests that same available API target while negotiating the session protocol independently. Existing Advanced reference policies remain bound to their original target and receipts.
L18: 
L19: Use **Strict CodeMod** for public DTMAPI/BCL and supported managed dependencies. Any valid author ID can use the current **Advanced CodeMod** native-contract path with explicit local game references. The older receipt path remains identity-specific; copying another product's policy or receipt fails closed. Neither path permits redistributing game/platform DLLs.
L20: 
L21: Strict project assembly references and their actual PE dependency closure reject host dependencies with `SDK160`. Source names bind through Roslyn against the frozen references: comments, strings and same-named user types are valid; unresolved host types produce compiler diagnostics. Undeclared inputs and unsupported targets/options produce `SDK180`; undeclared bundled DLL inputs produce `SDK161`. Schema 3 supports declared libraries, resources, locked packages and `managedReferences`, subject to complete package closure validation. See [projects and restore](PROJECTS-AND-RESTORE.md) for the supported grammar. Native V1/V2 use schema 3 and generated local provenance; hand-editing only the Runtime manifest does not authorize native references.
L22: 
L23: `DTMAPI.Abstractions` is a public assembly, not a promise that every public type is Stable. Before adopting a surface, check both stability and disposition in the bundled [API status](API-STATUS.md), generated from the repository's authoritative matrix. Diagnostic, Frozen, Disabled, DTMAPI-internal, and Proposed contracts are not ordinary new-mod dependencies. See [migration](MIGRATION.md), including the historical CSV removal exception.
```

### `author-sdk-release.json`，L1–L23

```text
L1: {
L2:   "schemaVersion": 1,
L3:   "sdkVersion": "0.7.0",
L4:   "targetRuntimeVersion": "0.7.0",
L5:   "defaultTarget": "0.7.0",
L6:   "targetCatalogSha256": "4b8c24b7da5482f8103566a051fec74095425802ce72b9c3e68584d2fcfc8e11",
L7:   "availableTargets": [
L8:     "0.5.5",
L9:     "0.6.2",
L10:     "0.6.3",
L11:     "0.6.4",
L12:     "0.6.5",
L13:     "0.7.0"
L14:   ],
L15:   "buildDotNetSdkVersion": "8.0.421",
L16:   "runtimeIdentifier": "win-x64",
L17:   "packageKind": "self-contained-portable-author-sdk",
L18:   "pathMap": "/_/DTMAPI",
L19:   "deterministicZip": {
L20:     "entryOrder": "ordinal-relative-path",
L21:     "entryTimestampUtc": "2000-01-01T00:00:00Z",
L22:     "compression": "store"
L23:   },
```

## S2 构建模板接管标准目标；旧 props 关闭标准还原

### `templates/codemod/__UNIQUE_ID__.csproj.template`，L1–L31

```text
L1: <Project>
L2:   <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
L3:   <PropertyGroup>
L4:     <AssemblyName>{{ASSEMBLY_NAME_XML}}</AssemblyName>
L5:     <RootNamespace>{{ROOT_NAMESPACE}}</RootNamespace>
L6:   </PropertyGroup>
L7:   <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)\{{PAYLOAD_PATH_XML}}\DTMAPI.Author.props"
L8:           Condition="Exists('$(DTMAPI_AUTHOR_SDK_ROOT)\{{PAYLOAD_PATH_XML}}\DTMAPI.Author.props')" />
L9:   <PropertyGroup>
L10:     <DtmApiUnifiedBuild>1</DtmApiUnifiedBuild>
L11:     <LangVersion>12.0</LangVersion>
L12:     <CheckForOverflowUnderflow>true</CheckForOverflowUnderflow>
L13:     <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
L14:     <DefineConstants Condition="'$(Configuration)' == 'Debug'">DEBUG;TRACE</DefineConstants>
L15:     <DefineConstants Condition="'$(Configuration)' != 'Debug'">TRACE</DefineConstants>
L16:   </PropertyGroup>
L17:   <ItemGroup>
L18:     <Compile Include="{{SOURCE_GLOB_XML}}" />
L19:   </ItemGroup>
L20:   <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
L21:   <Target Name="RequireDtmApiAuthorSdk" BeforeTargets="PrepareForBuild"
L22:           Condition="!Exists('$(DTMAPI_AUTHOR_SDK_ROOT)\{{PAYLOAD_PATH_XML}}\DTMAPI.Author.props')">
L23:     <Error Text="Set DTMAPI_AUTHOR_SDK_ROOT to the extracted DTMAPI Author SDK, or build with dtmapi-author build." />
L24:   </Target>
L25:   <Target Name="Build" DependsOnTargets="RequireDtmApiAuthorSdk" Condition="'$(DesignTimeBuild)' != 'true'">
L26:     <Error Condition="!Exists('$(DTMAPI_AUTHOR_SDK_ROOT)\dtmapi-author.exe')" Text="Set DTMAPI_AUTHOR_SDK_ROOT to the extracted self-contained SDK." />
L27:     <Exec Command="&quot;$(DTMAPI_AUTHOR_SDK_ROOT)\dtmapi-author.exe&quot; build &quot;$(MSBuildProjectDirectory)&quot; --configuration &quot;$(Configuration)&quot; --compatibility-root &quot;$(DTMAPI_AUTHOR_SDK_ROOT)&quot;" />
L28:   </Target>
L29:   <Target Name="Rebuild" DependsOnTargets="Build" />
L30:   <Target Name="Restore" />
L31: </Project>
```

### `compatibility/0.7.0/DTMAPI.Author.props`，L1–L24

```text
L1: <Project>
L2:   <PropertyGroup>
L3:     <TargetFramework>netstandard2.0</TargetFramework>
L4:     <LangVersion>latest</LangVersion>
L5:     <Nullable>enable</Nullable>
L6:     <ImplicitUsings>disable</ImplicitUsings>
L7:     <Deterministic>true</Deterministic>
L8:     <RestoreProjectStyle>None</RestoreProjectStyle>
L9:     <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
L10:     <NoStdLib>true</NoStdLib>
L11:     <NoConfig>true</NoConfig>
L12:     <DtmApiAuthorCompatibilityRoot>$(MSBuildThisFileDirectory)</DtmApiAuthorCompatibilityRoot>
L13:     <DtmApiAuthorReferenceDirectory>$(DtmApiAuthorCompatibilityRoot)ref\netstandard2.0</DtmApiAuthorReferenceDirectory>
L14:   </PropertyGroup>
L15:   <ItemGroup>
L16:     <Reference Include="$(DtmApiAuthorReferenceDirectory)\*.dll">
L17:       <Private>false</Private>
L18:     </Reference>
L19:     <Reference Include="DTMAPI.Abstractions">
L20:       <HintPath>$(DtmApiAuthorCompatibilityRoot)DTMAPI.Abstractions.dll</HintPath>
L21:       <Private>false</Private>
L22:     </Reference>
L23:   </ItemGroup>
L24: </Project>
```

## S3 重复声明、项目/资源边界与普通库子集

### `PROJECTS-AND-RESTORE.md`，L9–L27

```text
L9: Create auxiliary projects with `dtmapi-author new library ../Logic --id Author.Logic --api-target 0.6.4`. A library has `dtmapi.library.json`, an ordinary SDK-delegating `Author.Logic.csproj`, and sources. It has no Mod manifest, Entry or standalone Mod ZIP; `build` produces its DLL and portable PDB. The default role is `private-managed`; explicitly select `--role shared-contract` for an intentionally shared CLR contract. Private libraries are not isolated AppDomains.
L10: 
L11: The Mod and all libraries use the same API target. Declare exact `.csproj` references in the Mod's `dtmapi.author.json` or the library's descriptor:
L12: 
L13: ```json
L14: "build": {
L15:   "workspaceRoot": "..",
L16:   "projectReferences": ["../Logic/Author.Logic.csproj"],
L17:   "embeddedResources": [{"path":"assets/help.txt","logicalName":"Author.Mod.Help"}],
L18:   "contentFiles": [{"path":"assets/data.txt","targetPath":"Content/Author.Mod/data.txt"}],
L19:   "generatedSourceFiles": ["generated/Constants.cs"]
L20: }
L21: ```
L22: 
L23: The root project defines the whole graph's workspace boundary. References can be SDK libraries or supported ordinary libraries inside that boundary. Reparse points, cycles, conflicting assembly names, inherited Directory.Build/Packages files and graphs over 32 projects are rejected before compilation. SDK library sources/resources stay beneath their own directory. Generated source files must already exist; the SDK never executes a generator. Missing or duplicate inputs are errors.
L24: 
L25: Embedded names and content targets are explicit and case-insensitively unique. Resources are limited to 16 MiB each. Content targets stay beneath `Content/` and outside the SDK-owned `Content/DTMAPI/` directory. Content is copied into build output and the Mod package; embedded resources are included in the owning DLL and accessed by their declared logical names. All source/resource bytes, transitive project inputs and references participate in the build identity. Libraries enter the existing dependency inventory with their role and exact CLR references; official/platform DLLs never become distributable project libraries.
L26: 
L27: CLI build, pack and the generated IDE Build target use the same backend. For an SDK project, an IDE ProjectReference, EmbeddedResource/LogicalName, Content/TargetPath/CopyToOutputDirectory=PreserveNewest, generated Compile item or exact PackageReference must match the JSON declaration. Undeclared items, unsupported conditions, analyzers, modified build targets and arbitrary imports are errors. Build each Mod entry project; shared auxiliary projects are compiled by its DAG. Frozen target props are unchanged.
```

### `PROJECTS-AND-RESTORE.md`，L29–L53

```text
L29: ## Constants and XML documentation
L30: 
L31: Declare these directly in the csproj; no duplicate JSON declaration is needed:
L32: 
L33: ```xml
L34: <PropertyGroup>
L35:   <DefineConstants>$(DefineConstants);AUTHOR_FEATURE</DefineConstants>
L36:   <GenerateDocumentationFile>true</GenerateDocumentationFile>
L37: </PropertyGroup>
L38: <PropertyGroup Condition="'$(Configuration)' == 'Release'">
L39:   <DefineConstants>$(DefineConstants);AUTHOR_RELEASE</DefineConstants>
L40: </PropertyGroup>
L41: ```
L42: 
L43: Literal symbols, self-append, and Debug/Release equality or inequality conditions on these properties are supported. Symbols are checked, deduplicated and sorted. Undeclared SDK projects retain DEBUG/TRACE in Debug, TRACE in Release, C# 12, checked arithmetic and nullable enabled. XML is emitted with the same DLL/PDB, reported by path/hash and packaged beside its assembly. A failed emit or publication preserves previous companion outputs; a successful build with documentation disabled removes its stale XML. `migrate-build` preserves these author options.
L44: 
L45: ## Ordinary auxiliary libraries
L46: 
L47: A referenced `Microsoft.NET.Sdk` project with one `netstandard2.0` Library target needs no Mod manifest, UniqueID or `dtmapi.library.json`. Its csproj is read without rewriting. AssemblyName and Version/AssemblyVersion/FileVersion belong to that library. Default C# semantics follow the supported .NET 8 SDK subset: C# 7.3, unchecked, nullable disabled, and SDK configuration/framework constants appended after author DefineConstants. Explicit 7.3/12.0, nullable enable/disable and overflow checking are supported. These defaults differ from the Mod template deliberately.
L48: 
L49: Default Compile glob excludes bin/obj and tooling directories. Literal Compile Include/Remove and Link can reference source inside the declared workspace; external-to-library sources require a relative Link. Transitive ProjectReference and raw EmbeddedResource/LogicalName are supported. resx generation, multi-targeting, custom targets/imports, generators and other unsupported semantics name the actual child project in the error. Build such a library with standard MSBuild and declare its output through `managedReferences` instead.
L50: 
L51: Ordinary libraries default to self-authored/private-managed. The parent can declare `build.projectReferenceMetadata` keyed by the existing ProjectReference path, with `role` (`private-managed` or `shared-contract`), `distribution` and `licenseFiles`; paths and values follow [the build schema](schemas/dtmapi-build.schema.json). Existing string references remain valid. This is package metadata and does not turn a library into a Mod.
L52: 
L53: Ordinary PackageReference versions and `packages.lock.json` remain owned by the child. Restore sources and licenses for the full package closure come from the parent's `build.restore`; its `packages` may be empty when it only supplies referenced libraries. The lock replay target remains the dependency-only netstandard2.0 format described below; an implicit NETStandard.Library entry from a normal standalone restore is not a distributable dependency. Keep the ordinary project's standard build and the bounded author replay inputs explicit.
```

## S4 NuGet 还原与资产模型限制

### `PROJECTS-AND-RESTORE.md`，L55–L68

```text
L55: ## Explicit NuGet restore
L56: 
L57: Add `build.restore` to a Mod or library. Direct versions are exact. Every package, including transitive ones, has explicit local license files:
L58: 
L59: ```json
L60: "restore": {
L61:   "lockFile": "packages.lock.json",
L62:   "cacheDirectory": "obj/dtmapi-author/packages",
L63:   "sources": ["https://api.nuget.org/v3/index.json"],
L64:   "packages": {"Nett.Coma":"0.15.0"},
L65:   "licenseFiles": {
L66:     "Nett.Coma":["licenses/Nett-LICENSE.md"],
L67:     "Nett":["licenses/Nett-LICENSE.md"]
L68:   }
```

### `PROJECTS-AND-RESTORE.md`，L74–L88

```text
L74: Use a normal NuGet-generated `packages.lock.json`, containing one `.NETStandard,Version=v2.0` target and the complete package closure. To prepare or deliberately update it, use the installed .NET SDK in a separate dependency-only project:
L75: 
L76: ```xml
L77: <Project Sdk="Microsoft.NET.Sdk">
L78:   <PropertyGroup>
L79:     <TargetFramework>netstandard2.0</TargetFramework>
L80:     <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
L81:     <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
L82:     <RestoreSources>https://api.nuget.org/v3/index.json</RestoreSources>
L83:   </PropertyGroup>
L84:   <ItemGroup><PackageReference Include="Nett.Coma" Version="[0.15.0]" /></ItemGroup>
L85: </Project>
L86: ```
L87: 
L88: Run `dotnet restore --use-lock-file` on that project and review/copy the resulting lock into the author project. Implicit framework references are disabled only in this dependency-only lock recipe: the author compiler already uses its frozen BCL payload. Do not manually replace hash values or silently update dependency versions. NuGet owns dependency resolution and its [standard lock format](https://learn.microsoft.com/nuget/consume-packages/package-references-in-project-files#locking-dependencies); this SDK consumes that lock and does not introduce another resolver or lock format. Creating/updating a lock needs the .NET SDK; replaying it only needs the self-contained Author SDK.
```

### `PROJECTS-AND-RESTORE.md`，L97–L109

```text
L97: Restore walks referenced libraries too. Only explicit online restore accesses the declared sources. HTTPS sources cannot embed credentials, query strings or fragments; explicit local feed directories also work. Authenticated source providers are not supported in this slice. No credentials or source configuration are put into the Mod package.
L98: 
L99: The SDK verifies NuGet identity, content hash, locked/direct versions, actual nuspec dependency groups and transitive closure. Cached nupkgs are rechecked, and changed extracted assets are rejected. Offline empty-cache and mismatched-lock errors tell the author to run or repair explicit restore. Build/pack can materialize a missing local asset from an already verified nupkg, but never download it. A lock is an integrity input, not evidence that a package is trustworthy.
L100: 
L101: Restore first selects applicable NuGet framework groups and reports selected, excluded and unsupported paths. The execution group must contain pure managed, exact netstandard2.0 `lib` DLLs. Unrelated incompatible framework assets are excluded; applicable build/buildTransitive tasks, analyzers/generators, RID/native selection, tools/content and satellite groups remain unsupported. Separate ref/lib surfaces are accepted only when the selected DLL sets and bytes are identical. Ref-only packages, differing reference/implementation bytes and lower-TFM execution libraries remain outside this model. These failures describe an unsupported asset model, not malicious content.
L102: 
L103: Normal runtime-implemented delegate methods, including generic and ref/out delegates, are accepted. P/Invoke, Unmanaged, InternalCall, Native code types, non-delegate Runtime methods and non-IL-only assemblies are rejected with package/version, selected path and member diagnostics. Entry/lib/BCL closure still passes the ordinary package policy; a NuGet dependency cannot replace a reserved or already-resident host assembly. Successful restore of Newtonsoft.Json does not establish that a private copy can coexist with the game's resident copy; use the exact host reference through Advanced when appropriate.
L104: 
L105: ## CI and actual game tests
L106: 
L107: `ci/verify-project.ps1` validates, optionally restores, builds, packs and runs Doctor over the extracted artifact. Give it an extracted SDK already verified against your chosen ZIP hash. It accepts a separate explicit author logic-test script. A public runner needs no game files for Strict projects. It must not fetch private game binaries; an Advanced/native project needs its own licensed game runner and existing game-root settings.
L108: 
L109: Actual Mono loading, source selection, UI, owner cleanup and native compatibility are separate gates. No CI metadata or local CLR test substitutes for them. Keep old ZIPs before changing inputs and bump the Mod version when new bytes would replace an existing artifact. A failed graph does not create a new package or deploy over an existing installation.
```

## S5 开放 Advanced、编译引用视图与原生契约

### `NATIVE-REFERENCES.md`，L1–L29

```text
L1: # 本机原生引用与开放 Advanced（未发布候选 0.7.0）
L2: 
L3: 任意合法作者 ID 可以创建 Advanced 工程，无需第一方 Catalog 注册。Mod 仍是官方 MODS 下的受管 CodeMod，目标框架固定为 netstandard2.0。
L4: 
L5: ```powershell
L6: dtmapi-author new codemod .\MyMod --id Pine.NativeObserver --name NativeObserver --author Pine --code-mod-kind Advanced --api-target 0.7.0 --game-root 'D:\Steam\steamapps\common\Doloc Town'
L7: dtmapi-author build .\MyMod
L8: dtmapi-author pack .\MyMod
L9: ```
L10: 
L11: 创建命令默认引用该安装的 Assembly-CSharp、UnityEngine.CoreModule 和 0Harmony；可用 `--native-references` 提供以分号分隔的游戏相对路径。`--harmony-owner` 默认是 `<UniqueID>.Native`，必须等于 UniqueID 或以 `UniqueID.` 开头。工程中 `nativeReferences.gameRoot` 是明确的本机安装目录，包中不包含它。
L12: 
L13: 当前 0.7.0 新项目同时声明 `CodeModKind: Advanced`、`NativeContractVersion: 2`、`DependencyContractVersion: 1`，作者工程使用 schema 3，最低 Runtime 为 0.7.0。旧目标继续生成 V1；旧包不自动升级格式。当前游戏引用 netstandard 2.1，与 Mod 的 netstandard2.0 直接编译冲突；SDK 因此使用通用 metadata reference surface。它保留类型/成员元数据，移除原方法体、资源和字段初始数据，仅在编译视图中适配 facade。缓存位于工程 `obj/dtmapi-native`，按原始程序集摘要与生成器版本区分；缓存字节改变会报错。禁止将这些引用或官方、平台 DLL 放入 content 或 managedReferences。
L14: 
L15: `dtmapi-native-build.json` 记录原始宿主 identity、长度、SHA-256、编译视图摘要、实际输出使用的宿主类型/成员及生成器输入摘要。依赖库也扫描同样的引用闭包。manifest、入口 DLL 和依赖清单先绑定到 native 描述，再由总文件清单绑定 native 描述；不要手写或修改生成物。
L16: 
L17: 生成引用时实际读取的辅助元数据（例如旧游戏的 DOTween 枚举信息）记入 `referenceGeneration.metadataDependencies`，与显式引用一起参与缓存和生成器输入摘要。它们不会自动成为作者的编译/运行时引用，也不能借此打包官方 DLL。安装目录缺少此类依赖时，应恢复完整本机安装后重试。
L18: 
L19: 动态反射和 Hook 必需成员需要在 `nativeReferences.requiredMembers` 补充完整签名；属性访问器以 method 记录，例如 `get_CurrentL10nId`。每项字段见 `schemas/dtmapi-native-build.schema.json` 的 member 定义。方法重载由完整声明类型、静态性、参数与返回类型区分。BCL facade 的可信身份归一为 `[bcl]`；游戏类型保留程序集完整 identity。V1 支持普通类型、嵌套类型、数组、指针与 by-ref；泛型成员/泛型声明类型、函数指针、自定义修饰符、varargs 和带特殊边界的数组会明确报 `native-signature-unsupported`，不会按名称猜测。尚未声明的可选动态查找由作者自行降级并记日志。
L20: 
L21: Runtime 在 Entry 前核对包绑定和必需宿主签名。必需签名缺失拒绝该 Mod；同 identity 的宿主字节变化但签名仍匹配会报告“未验证的游戏版本”。宿主 identity 变化或发现后替换文件要求重新验证/重启。Doctor 扫描安装目录时执行同样的宿主核对；单独扫描包只证明包绑定，不能证明游戏兼容。
L22: 
L23: V2 在上述 V1 保留路径之外支持常用泛型方法、泛型声明类型、嵌套/组合构造类型、作者自己的类型实参以及数组/ref/out 组合，例如 `GetComponent<Transform>()` 和 `GetComponent<AuthorComponent>()`。SDK 从实际输出扫描 MethodSpec、TypeSpec、定义及约束；build 完成前与 pack 使用同一可表达性检查。函数指针、varargs、自定义修饰符和特殊数组边界仍明确拒绝。有关结构化动态必需成员，见 [schema](schemas/dtmapi-native-build.schema.json) 的 `genericUse` 定义；V1 显式成员在 V2 项目中由 SDK 从宿主定义提升，不应手写包产物。
L24: 
L25: Runtime 比较开放泛型定义、约束与包内类型/引用身份，不在预检中构造闭合泛型或执行作者程序集。BCL 归一仅覆盖已知精确身份；同名未知程序集不因此获准。该检查不承诺验证任意恶意 IL，也不能替代目标游戏 Mono 的实际调用验收。
L26: 
L27: Harmony 实例使用工程指定的唯一 owner。在 Entry 安装观察 Hook，将 cleanup 注册到 owner 资源或实现 IDisposable 并调用该 Harmony 实例的 `UnpatchSelf()`。每次关闭只撤自身 owner，不调用全局 UnpatchAll。Runtime 保留已有的 Advanced owner 监督与失败回滚，但这不承诺恢复任意第三方原生副作用。只读观察不应改写参数、返回值或存档。
L28: 
L29: 旧 receipt Advanced 继续使用旧策略 reader；旧 Strict 与省略 CodeModKind 的 legacy 继续使用各自 reader。新字段错误、未知版本或缺少绑定均拒绝，不会降级成 legacy。安装、撤回和更新仍走公开 install-local/session/withdraw 命令，加载过程序集后需要重启。
```

### `NATIVE-REFERENCES.md`，L31–L41

```text
L31: ## 引用游戏已有的 JSON 库
L32: 
L33: Newtonsoft.Json 13.0.3 的合法委托元数据可通过 restore、离线 restore、build 和 pack。但当前游戏已驻留自己的 Newtonsoft.Json，Strict 包私带另一份会在 Entry 前报 `resident-conflict/restart-required`；重启本身不能消除两份不同载荷的冲突，不要靠改名或改签名绕过。需要游戏这份库时，可显式选择 Advanced 宿主引用：
L34: 
L35: ```powershell
L36: dtmapi-author new codemod .\HostJson --id Cedar.HostJson --name HostJson --author Cedar --code-mod-kind Advanced --api-target 0.7.0 --game-root 'D:\Steam\steamapps\common\Doloc Town' --native-references 'DolocTown_Data/Managed/Newtonsoft.Json.dll'
L37: dtmapi-author build .\HostJson
L38: dtmapi-author pack .\HostJson
L39: ```
L40: 
L41: 将 game-root 换成自己完整的游戏安装目录；不要同时把 NuGet 版本作为 managedReferences 打包。作者代码可调用 `Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>("[20,22]")`，并使用返回的两个整数。当前 Windows 游戏 build 25163613 的 Mono 已验证该操作结果为 42；包绑定实际宿主身份与必需泛型签名，其他游戏版本仍需 Doctor 和实际调用验证。这不是所有 Strict JSON 依赖均可共存的承诺。
```

## S6 依赖与包身份、共享程序集和生命周期边界

### `PACKAGE-DEPENDENCIES.md`，L20–L30

```text
L20: role 只接受 shared-contract 或 private-managed。DLL 文件名必须与实际 AssemblyName 相同，实际 TargetFramework 必须是 netstandard2.0。所有传递库均须显式携带；第三方库使用 licensed-third-party 并包含非空许可文件。声明不代表许可已经获得。游戏、Unity、BepInEx、Harmony 和平台提供的 DLL 不得复制进 Mod。BCL 接受集合来自冻结 NETStandard.Library 2.0.3 的 113 个实际引用 DLL 身份，记录在 bcl-reference-identities.json；第 114 个引用目录文件是 XML，不是 DLL。没有 System.* 前缀通行规则。
L21: 
L22: 库进入 lib/shared 或 lib/private，许可文件进入 licenses。SDK 从实际 PE 生成 dtmapi-dependencies.json，并把它、manifest、DLL、许可和其他发布文件绑定进原 dtmapi-package.json 的 inventory。不要手改生成文件；内容变更后重新 build/pack 并增加 Mod 版本。不同源码/版本标签不能允许同 CLR 身份对应不同字节。
L23: 
L24: 每个包单独安装时都应有完整库闭包。Provider 与 Consumer 分别引用并携带同一份 Contract DLL；接口、枚举和 DTO 放在该库中，避免原生类型、可变静态状态和初始化副作用。公开 GetApi<T> 使用准确 CLR Type；同名接口文字相同不构成类型相同。
L25: 
L26: Runtime 在 Entry 前检查全部选中包及其传递 AssemblyRef，再生成加载顺序。同 simple name、完整 identity 和 SHA 相同的库绑定一次；任意 identity/字节冲突会拒绝所有参与包及必需消费者，无关 Mod 保持可运行。private-managed 不提供 AppDomain 隔离。入口程序集不能重名。已驻留来源或原始字节无法证明时要求重启，磁盘覆盖不能更新已经加载的程序集。
L27: 
L28: required provider 缺失、版本不符或 Entry 失败时，consumer 不进入 Entry。provider 关闭时先关闭必需 consumers；consumer 清理失败保留 provider，下一次沿现有清理路径重试。optional provider 不可用时 consumer 可降级；但 optional Mod 声明不能免除真实 DLL 引用闭包。Provider 返回的对象仍由作者负责 owner guard；平台不会撤销别人缓存的普通对象或生成自动代理。
L29: 
L30: 迁移旧工程时，先保留原工程与包，使用新 SDK/new 生成 schema 3 工程，再迁入源文件、显式 managedReferences 和新区间。需要沿用旧依赖解释时继续选旧 schema/target；不要仅改 marker、DLL 名或 MinimumDTMApiVersion 来绕过载荷边界。本格式是完整性与兼容性预检，不是 CLR 安全沙箱。
```

### `README.md`，L55–L83

```text
L55: Add `--json` for a machine-readable report. A minimal Strict `build` compiles in-process against the SDK's hash-fixed compatibility payload without an installed `dotnet` or game directory; projects with package dependencies additionally need their verified restore inputs. Advanced uses the explicit local game root saved by `new --game-root` or supplied to the command. The current native-contract path records the exact host identities, paths and hashes and derives required signatures from the compiled output; legacy receipt projects retain their tracked policy/build restrictions. The CLI does not search ambient game installs or unrelated workspaces for native assemblies. Tested game compatibility and remaining limitations are described in [native references](NATIVE-REFERENCES.md).
L56: 
L57: `pack` already compiles a CodeMod once. Use `pack MyMod --build-output path/to/compiled --json` when the calling build also needs that DLL and PDB; do not precede it with a duplicate `build`. The report keeps `outputPath`/`sha256` for the ZIP and adds `values.buildOutputPath`, `buildOutputSha256`, `buildInputSha256` and `buildInputIdentity` for the actual compilation. ContentPack does not accept `--build-output`.
L58: 
L59: Repository product wrappers use `prepare-author-sdk.ps1` once, then reuse its explicit output through `-AuthorSdkRoot`. Preparation checks the SDK source/dependency graph, compiler, build properties and packaging inputs; a normal product-source change does not rebuild the SDK. `preflight-workspace.ps1 -Operation BuildProduct -CatalogId more-saves` reports missing setup and the separate compile-reference/test-game paths without creating output. Formal release verification still packs twice for determinism.
L60: 
L61: `pack` emits the official package shape:
L62: 
L63: ```text
L64: Package/
L65:   info.json
L66:   dtmapi-dependencies.json   # current dependency-contract packages
L67:   dtmapi-native-build.json   # current Advanced V1/V2 packages
L68:   Content/DTMAPI/
L69:     manifest.json
L70:     Author.Mod.dll       # CodeMod only
L71:     dtmapi-package.json  # metadata only; never an ownership receipt
L72:     ...content files
L73: ```
L74: 
L75: Generated dependency/native metadata paths are relative to the package root and are recorded in `dtmapi-package.json`. Legacy receipt packages retain their original layout; use the marker's actual path instead of moving generated files by hand.
L76: 
L77: `manifest.json` remains authoritative for Mod identity, Mod version, dependencies, and minimum Runtime. `dtmapi.author.json` is an SDK-only pre-1.0 build/publish input and does not replace the Runtime manifest. The package's `info.json` is generated from both inputs and must not be hand-maintained in the author project.
L78: 
L79: Current Advanced projects accept any valid author ID through the [native-contract workflow](NATIVE-REFERENCES.md); no first-party registry row is required. Their generated provenance binds the package, exact local host references, required signatures, `netstandard2.0`, Harmony owner and minimum Runtime. Supported private/shared managed libraries are described by the dependency inventory. Game, Unity, Harmony, BepInEx and DTMAPI platform DLLs are never bundled as author dependencies; generated metadata reference surfaces are compile-only inputs.
L80: 
L81: Legacy receipt-based Advanced projects keep their exact tracked policy and live authoring registry. Core and Doctor also retain the historical acceptance registry for already-issued receipts; those historical rows cannot authorize a new receipt. These identity-specific compatibility rules do not restrict the current self-service native-contract path.
L82: 
L83: The SDK has no upload, credential, force, or adopt operation. DTMAPI-managed Strict or Advanced CodeMods must never be created, built, packaged, or deployed under `BepInEx/plugins`; third-party plugins placed there are External BepInEx Plugins outside DTMAPI ownership.
```

## S7 安装、运行诊断及调试承诺边界

### `README.md`，L85–L93

```text
L85: ## Legacy receipt-bound deployment recovery
L86: 
L87: Official installation derives the profile root using the same `DTMAPI_DOLOC_PERSISTENT_ROOT` override and Windows LocalLow default as Runtime. SDK never edits `SAVE/mod_infos.json`: enable/disable the Mod in the game's official Mod UI. `install-local` requires expected ID, version and ZIP SHA-256; `deploy` creates a new owned package and `update` requires an existing exact receipt. `deployment-status` reports disk inventory separately from official enabled state; selected source and resident DLL need a live `session snapshot` and are otherwise unavailable. A disk update never proves that a process loaded it.
L88: 
L89: Official journal schema 4 uses the existing transaction/inventory fields, no compound historical `localInstall`, and binds its receipt to `OfficialLocal/<UniqueID>`. Readers enforce the current resolved official root, retained complete inventory and exact prior transaction. Unknown files, wrong roots, access failures and mismatched journals are preserved and rejected. Staging and recovery remain on the official MODS volume. Windows cold mutations hold the SDK operation lock and an exclusive game executable handle through commit or rollback, rejecting a running or concurrently starting game. Restart after changing code. Non-Windows mutation is currently unsupported; no platform support is inferred from netstandard2.0.
L90: 
L91: DTMAPI 0.6.0 no longer discovers the historical `game/Mods/<UniqueID>` development root. The current source candidate routes `deploy`, `update` and `install-local` to official profile `MODS/<UniqueID>`, recognized as `Local.<UniqueID>`. It accepts any valid author ID without Catalog registration. `source local select` remains paused with `SDK003` for the historical source. Do not use that directory as player-load or release evidence.
L92: 
L93: Existing receipt-bound deployments remain recoverable. `deployment-status` and `install-local-status` are read-only; `recover` restores an interrupted prepared transaction; `withdraw` moves an exactly verified committed package into retained same-volume recovery; and `source local clear` may remove a stale historical selection without moving package files. These compatibility commands do not make `game/Mods` a Runtime source.
```

### `README.md`，L123–L143

```text
L123: ## Explicit Runtime session
L124: 
L125: `session prepare` must run before the user starts the game. Current source writes a one-shot schema 2 `author-session.json` and a protected `author-session-client.json`, then negotiates protocol/capabilities through authenticated `hello`. The API compilation target is independent of the actual Host version returned by hello. The bounded schema 1 adapter retains the known old SDK wire value. See the [session protocol](SESSION-PROTOCOL.md) for fields, compatibility, lifecycle and unavailable/timeout diagnostics; published availability remains with the release records.
L126: 
L127: Runtime atomically consumes the startup descriptor once. The client credential remains available for explicit `snapshot` and `reload` requests until expiry or `session clear`. Expired matching state is cleared on the next prepare/request; malformed or mismatched state is preserved and requires explicit clear. Tokens are sent only inside authenticated JSONL pipe requests and are redacted from human/JSON reports, Runtime messages, and response values.
L128: 
L129: Each business request includes the exact UniqueID, absolute selected root, a fresh request ID, and the current `DTMAPI-FileTree-SHA256-v1` hash. Response protocol/Host/session/request/operation/owner identities must match the negotiated session. Transport failures report stable codes such as `host-unavailable`, `handshake-timeout`, `pipe-response-timeout`, and `pipe-response-identity-mismatch`; absence alone never implies an upgrade requirement. Runtime outcomes remain `ok`, `rejected`, `restart-required`, or `error`. A valid `restart-required` response carries an explicit warning and still requires a game restart.
L130: 
L131: The CLI never launches the game, installs a watcher, polls for Runtime startup, uploads DLL/native data, or sends a request without an unexpired protected token. Custom Animals, CodeMod DLLs, official-native JSON, and unknown formats remain restart-required; Runtime owns the only reviewed reload implementation.
L132: 
L133: ## Read-only Doctor
L134: 
L135: `dtmapi-author symbols <DLL> [--pdb <path>] --json` verifies the portable PDB identity against the DLL's CodeView GUID/stamp and reports both hashes, resident-comparable module MVID and source document names. Missing or mismatched symbols return SDK191. This proves a matching artifact pair, not debugger attach. Source paths in SDK-built stacks are relative to the configured source directory (normally `src`); keep the exact source/build report with the DLL and PDB. Debug packages include symbols; for Release use `--symbols true` when packing.
L136: 
L137: In the current Windows shipping Mono candidate, the tested Entry exception identified the exact `ModEntry.cs` throw line. The tested event exception identified its callback's closing line instead of the throw line, despite matching symbols. Use the owner, event name, error message and paired source together; exact event throw-line accuracy is not promised. Event errors are retained by `helper.Diagnostics.GetErrors()` and the existing `ExportLogs()` report; they are not necessarily repeated in the text log. Inspect the Errors page or export while that process is still running. Exported reports are local and can contain identifiable machine paths, historical logs and crash dumps with memory contents; inspect the ZIP before sharing. Reports are not automatically uploaded. No archive, complete Mod configuration or session credential file entries were found in the tested export; this is not a privacy guarantee for dump contents. Breakpoint attach is a separate host capability and is not promised by PDB generation.
L138: 
L139: Live `session snapshot` separates `officialEnabled`, `ownerActive`, `diskEntrySha256`, `diskEntryMvid` and `residentEntryMvid`. `residentMatchesDiskMvid` compares module identities, not a hash of Mono memory; an unavailable resident observation is reported explicitly. A failed Entry can leave a resident assembly even though its owner is inactive. Code updates and symbol replacement require a restart.
L140: 
L141: Packages include all current native upload localization fields in `info.json` so native discovery does not migrate the file and invalidate exact receipts. If a pre-fix candidate was already rewritten by the game, status/update correctly refuses the drift; do not force/adopt it. Preserve evidence and restore the exact known test asset only within its original authorization before recovery. Newly generated packages need no such repair.
L142: 
L143: `dtmapi-author doctor <path>` prints a human report; add `--json` for the Doctor report schema. Doctor is a `0.1.0` support library inside the single Author SDK CLI, not a second apphost. It uses `PEReader` and shared-read file streams, never `Assembly.Load`, and never moves, deletes, adopts, enables, disables, or executes inspected binaries.
```
