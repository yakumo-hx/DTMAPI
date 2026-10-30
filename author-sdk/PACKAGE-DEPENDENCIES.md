# DLL 候选与依赖

扫描目录只记录候选路径。明确入口、CLR 实际请求、显式动态加载与可选模块激活决定实际使用的 DLL。未用 DLL 不读取 PE、不计算哈希、不参与整包冲突判断。延迟模块继续按需加载。

默认候选包括入口所在目录、包根 DLL、`lib/private`、`lib/shared`。可用 `Assemblies` 补充 `{ "Path": "extras/Library.dll", "Role": "private-managed" }`；角色还包括 `shared-contract`。它描述位置和共享意图，不证明全部文件可信或已加载。旧依赖清单仅提供可用的路径/角色提示，损坏提示不成为否决条件。

`Dependencies` 可以省略。每项 `UniqueID` 必需，`Required` 缺省 true，`VersionRange` 可省略；无版本范围只表达存在和排序。版本范围使用 `minimumInclusive`、`maximumExclusive`、`includePrerelease`。不伪造缺失的产品版本；有范围而无法比较时报告具体依赖问题。ContentPackFor 隐含必需 Host 依赖，不用重复写一行。

实际请求先由宿主常规绑定处理，Core 只补充请求者、本包及声明 provider 的位置。先过滤可见性：本包可访问 private；跨包只访问声明 provider 的共享程序集及 Entry，不会连带开放其全部私有库。多个 provider 都提供同名可见程序集时，该次请求报告歧义，不按依赖顺序选择。

在唯一选定包的可见候选内，按请求者同目录、Entry 同目录、包根、private 子树、shared 子树、其他声明路径选择第一层非空候选。规范化路径去重后仍有多个候选时，仅该次请求报错并列出路径；目录枚举顺序、版本和 SHA 不参与仲裁。缺少 RequestingAssembly 时沿实际调用 Assembly 的上下文解析；没有 Location 不拒载。未选候选与失败发现不占用进程绑定；实际方法缺失或不可替换实现冲突只影响相关对象。

记录实际 Assembly 对象；版本、Location、SHA 和 TFM 都不是运行资格。旧 DependencyContractVersion 不参与依赖解析或加载判断，无需修复或更新这个工具字段。

平台与原生 DLL 是宿主提供的编译引用，应设置 `Private=false`。自有私有库放 `lib/private`；声明依赖方能使用提供方入口程序集或 `lib/shared` 中实际请求的契约。
