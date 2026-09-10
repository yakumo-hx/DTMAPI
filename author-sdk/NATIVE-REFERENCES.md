# 本机原生引用与开放 Advanced（未发布候选 0.7.0）

任意合法作者 ID 可以创建 Advanced 工程，无需第一方 Catalog 注册。Mod 仍是官方 MODS 下的受管 CodeMod，目标框架固定为 netstandard2.0。

```powershell
dtmapi-author new codemod .\MyMod --id Pine.NativeObserver --name NativeObserver --author Pine --code-mod-kind Advanced --api-target 0.7.0 --game-root 'D:\Steam\steamapps\common\Doloc Town'
dtmapi-author build .\MyMod
dtmapi-author pack .\MyMod
```

创建命令默认引用该安装的 Assembly-CSharp、UnityEngine.CoreModule 和 0Harmony；可用 `--native-references` 提供以分号分隔的游戏相对路径。`--harmony-owner` 默认是 `<UniqueID>.Native`，必须等于 UniqueID 或以 `UniqueID.` 开头。工程中 `nativeReferences.gameRoot` 是明确的本机安装目录，包中不包含它。

当前 0.7.0 新项目同时声明 `CodeModKind: Advanced`、`NativeContractVersion: 2`、`DependencyContractVersion: 1`，作者工程使用 schema 3，最低 Runtime 为 0.7.0。旧目标继续生成 V1；旧包不自动升级格式。当前游戏引用 netstandard 2.1，与 Mod 的 netstandard2.0 直接编译冲突；SDK 因此使用通用 metadata reference surface。它保留类型/成员元数据，移除原方法体、资源和字段初始数据，仅在编译视图中适配 facade。缓存位于工程 `obj/dtmapi-native`，按原始程序集摘要与生成器版本区分；缓存字节改变会报错。禁止将这些引用或官方、平台 DLL 放入 content 或 managedReferences。

`dtmapi-native-build.json` 记录原始宿主 identity、长度、SHA-256、编译视图摘要、实际输出使用的宿主类型/成员及生成器输入摘要。依赖库也扫描同样的引用闭包。manifest、入口 DLL 和依赖清单先绑定到 native 描述，再由总文件清单绑定 native 描述；不要手写或修改生成物。

生成引用时实际读取的辅助元数据（例如旧游戏的 DOTween 枚举信息）记入 `referenceGeneration.metadataDependencies`，与显式引用一起参与缓存和生成器输入摘要。它们不会自动成为作者的编译/运行时引用，也不能借此打包官方 DLL。安装目录缺少此类依赖时，应恢复完整本机安装后重试。

动态反射和 Hook 必需成员需要在 `nativeReferences.requiredMembers` 补充完整签名；属性访问器以 method 记录，例如 `get_CurrentL10nId`。每项字段见 `schemas/dtmapi-native-build.schema.json` 的 member 定义。方法重载由完整声明类型、静态性、参数与返回类型区分。BCL facade 的可信身份归一为 `[bcl]`；游戏类型保留程序集完整 identity。V1 支持普通类型、嵌套类型、数组、指针与 by-ref；泛型成员/泛型声明类型、函数指针、自定义修饰符、varargs 和带特殊边界的数组会明确报 `native-signature-unsupported`，不会按名称猜测。尚未声明的可选动态查找由作者自行降级并记日志。

Runtime 在 Entry 前核对包绑定和必需宿主签名。必需签名缺失拒绝该 Mod；同 identity 的宿主字节变化但签名仍匹配会报告“未验证的游戏版本”。宿主 identity 变化或发现后替换文件要求重新验证/重启。Doctor 扫描安装目录时执行同样的宿主核对；单独扫描包只证明包绑定，不能证明游戏兼容。

V2 在上述 V1 保留路径之外支持常用泛型方法、泛型声明类型、嵌套/组合构造类型、作者自己的类型实参以及数组/ref/out 组合，例如 `GetComponent<Transform>()` 和 `GetComponent<AuthorComponent>()`。SDK 从实际输出扫描 MethodSpec、TypeSpec、定义及约束；build 完成前与 pack 使用同一可表达性检查。函数指针、varargs、自定义修饰符和特殊数组边界仍明确拒绝。有关结构化动态必需成员，见 [schema](schemas/dtmapi-native-build.schema.json) 的 `genericUse` 定义；V1 显式成员在 V2 项目中由 SDK 从宿主定义提升，不应手写包产物。

Runtime 比较开放泛型定义、约束与包内类型/引用身份，不在预检中构造闭合泛型或执行作者程序集。BCL 归一仅覆盖已知精确身份；同名未知程序集不因此获准。该检查不承诺验证任意恶意 IL，也不能替代目标游戏 Mono 的实际调用验收。

Harmony 实例使用工程指定的唯一 owner。在 Entry 安装观察 Hook，将 cleanup 注册到 owner 资源或实现 IDisposable 并调用该 Harmony 实例的 `UnpatchSelf()`。每次关闭只撤自身 owner，不调用全局 UnpatchAll。Runtime 保留已有的 Advanced owner 监督与失败回滚，但这不承诺恢复任意第三方原生副作用。只读观察不应改写参数、返回值或存档。

旧 receipt Advanced 继续使用旧策略 reader；旧 Strict 与省略 CodeModKind 的 legacy 继续使用各自 reader。新字段错误、未知版本或缺少绑定均拒绝，不会降级成 legacy。安装、撤回和更新仍走公开 install-local/session/withdraw 命令，加载过程序集后需要重启。

## 引用游戏已有的 JSON 库

Newtonsoft.Json 13.0.3 的合法委托元数据可通过 restore、离线 restore、build 和 pack。但当前游戏已驻留自己的 Newtonsoft.Json，Strict 包私带另一份会在 Entry 前报 `resident-conflict/restart-required`；重启本身不能消除两份不同载荷的冲突，不要靠改名或改签名绕过。需要游戏这份库时，可显式选择 Advanced 宿主引用：

```powershell
dtmapi-author new codemod .\HostJson --id Cedar.HostJson --name HostJson --author Cedar --code-mod-kind Advanced --api-target 0.7.0 --game-root 'D:\Steam\steamapps\common\Doloc Town' --native-references 'DolocTown_Data/Managed/Newtonsoft.Json.dll'
dtmapi-author build .\HostJson
dtmapi-author pack .\HostJson
```

将 game-root 换成自己完整的游戏安装目录；不要同时把 NuGet 版本作为 managedReferences 打包。作者代码可调用 `Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>("[20,22]")`，并使用返回的两个整数。当前 Windows 游戏 build 25163613 的 Mono 已验证该操作结果为 42；包绑定实际宿主身份与必需泛型签名，其他游戏版本仍需 Doctor 和实际调用验证。这不是所有 Strict JSON 依赖均可共存的承诺。
