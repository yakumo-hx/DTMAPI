# 平台数据、内容与游戏领域架构

- Lifecycle: `accepted-design`；本文中的新能力尚未形成当前公开承诺。
- Role: [平台主架构](platform-next.md)下的数据、内容、资源与领域能力设计；实施顺序、完成状态分别归[路线](../planning/platform-next/roadmap.md)和[任务状态](../planning/platform-next/status.md)。
- Scope: M2 的生命周期与基础 IO、M4 的数据内容平台、M5 的领域能力；M1 作者闭环和 M3 开放原生与协作提供分发及实施基础，M6 持续维护这些承诺。
- Current contract: [PROJECT](../../PROJECT.md)和[公共 API 矩阵](../api/public-api-matrix.md)仍拥有当前身份、物理归属、保存语义及 API 状态。本文接受未来行为，不将现有 Experimental/Frozen 外壳升级为已实现能力。
- Companion: [能力地图](../planning/platform-next/capability-map.md)、[产品验收](../planning/platform-next/acceptance.md)。代表性 API 形状用于约束设计，名称及精确签名尚未冻结；进入公开 target 前必须补齐契约测试和 SDK 交付。
- Method: 本文接受保存回退、作者归属、兼容、错误隔离和准确能力声明等产品不变量；同档存储、候选编辑、Host 语法、UI 组件树及实体持久模型须先通过[方法验证](../planning/platform-next/method-validation.md)的对应比较，再冻结新公共契约。内部方案不因写入本文件就成为作者必须适应的长期规则。

## 从当前事实出发

| 已复核入口 | 当前事实 | 规划含义 |
| --- | --- | --- |
| `src/DTMAPI.Abstractions/Helpers.cs`：`IContentQueryHelper` | 来源、文本与物品索引查询，没有游戏资产编辑/创建/失效接口 | 保留为查询；新 ModContent 与 GameContent 分开交付 |
| `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`：`ContentQueryService` | 已有候选快照、generation、last-good、owner 移除机制 | 可复用事务思想及当前通用 generation 机制，不能把索引更新当作原生游戏内容更新 |
| `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`：`LoadMods` 的 ContentPack 分支 | 非代码包可进入 loaded/owner 集合；未验证某个通用 Host 接受了其 schema 并产生游戏效果 | 区分发现、依赖接受、Host 接受、内容激活；现有 loaded 不证明通用 ContentPack 产品成立 |
| `src/DTMAPI.Abstractions/Manifest.cs`、`src/DTMAPI.Core/Manifesting/ManifestModels.cs` | 当前公开 manifest 没有通用 ContentPackFor/Host 绑定 | 绑定与新 writer/reader/Doctor 必须同批实现，不能靠目录约定暗中增加协议 |
| `src/DTMAPI.GameBridge.DolocTown/AuthorSessionReloadBridge.cs`：`ReloadSelectedAuthorContent` | 会话 reload 仅允许已审查的 Audio-only 所有权范围；custom-animals 和其他内容拒绝 | 通用热刷新仍需建立；新能力不能沿用旧命令名称暗示全类型刷新 |
| `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs` | 自定义实体登记与状态存在，创建返回 `runtime-creation-blocked` | 登记通过不代表动物、怪物、攻击或无人机已经可生成、保存、恢复 |
| `src/DTMAPI.Abstractions/Helpers.cs`：`IUiHelper` | 只能打开平台页面与导出日志 | 不是作者可创建菜单/HUD 的 UI 平台 |
| `src/DTMAPI.Core/Services/TranslationService.cs` | owner 内翻译目录缓存，当前语言动态解析，缺项回退 | 保留，补参数替换、语言变化通知、明确刷新能力及语言 QA；不是重写翻译基础 |
| `src/DTMAPI.Core/Services/ConfigService.cs` | JSON 原子替换、损坏备份与迁移回调已有实现；手写格式化与备份失败处理有已核实缺陷 | PN-014 先修现有正确性；配置不等于通用 Data，迁移版本与多次读取幂等性由 PN-019 建新契约 |

当前 Doloc Town 原生事实已对本地 `references/doloc-town/reverse/builds/24456188_test_E861E0/decompiled/Assembly-CSharp` 做符号及方法体复核：

- `DolocTown.Config/ModManager.cs` 的 `UpdateCache`、`LoadWithMods` 拥有官方启用来源、配置合并及 sprite 选择；`DolocTown.Config/DolocConfig.cs` 的 `Loader` 在生成表前使用该合并结果。
- `DolocAssetCache.cs` 的 `GetAsset<TAsset>` / `CheckAsset<TAsset>` 对 Sprite 查询先走官方 Mod 覆盖，然后才读原生缓存。
- `DolocTown.GameData/DataPersistenceManager.cs` 的 `SaveGame` 在底层文件保存成功后才运行 `AfterSaveData`，外层 `DolocAPI.SaveGame` 随后还有 UI 操作。它们失败时磁盘可能已提交；后续 SaveData 必须验证该窗口。当前公共保存边界仍按 PROJECT 定义。
- 当时对 `ArchiveDataHandle`、`ExtraArchiveData` 的检查没有找到已证明可用的通用 Mod 字典，不能由这个局部结果推导必须使用 sidecar。后续 `25163613_public_604898` 仍存在 `cityData.dialogueManager.variableStorage` 的同档命名空间字符串候选；它不是官方 Mod 专用扩展协议，其保留性、对话初始化、容量与冲突行为尚待 V-Save 实测。8 月相关方案因 MoreEquipment 停用退物的产品取舍暂缓，不构成该存储方式的技术否决。

上述 build 是本次调查证据，不是所有未来游戏 build 的兼容承诺。每项原生实施必须从当前本地 build 重新验证它依赖的少数符号。参考字节、反编译方法体及官方资产不进入分发物。

## 分层与依赖

```text
M1 外部作者能够真实构建、安装、诊断和更新
  -> M2 owner + runtime/save/scene generation + 主线程 + 基础 IO
  -> M3 Advanced 和跨 Mod 契约（需要原生引擎或 Host 协作时使用）
  -> M4 Data / ModContent / GameContent / ContentPack Host
  -> M5 窄领域查询与事务、UI/HUD、内容家族、持久实体
  -> M6 游戏升级、兼容迁移、故障恢复与长期支持
```

这是能力关系，不是全串行限制。纯文件 ModContent、全局数据和翻译可在 M2 后独立交付；不持久化的图像编辑不等待 SaveData。Host 绑定只依赖其包输入、依赖和 owner 基础，具体编辑 Host 才依赖 GameContent。原生已经完整保存的内容家族不必再接 Core SaveData；需要额外状态或 Host 自己重建实例时才增加该依赖，但所有持久家族都必须完成真实保存、缺失与升级验收。通用反射可以辅助内部适配或 Advanced 作者，不是 Data、内容管线或生命周期的前提。

| 责任 | 物理所有者 | 对作者的边界 |
| --- | --- | --- |
| owner 隔离、路径、序列化、版本化快照、所选后端的提交协调、内容诊断 | Core，复用已有通用机制；journal/编辑排序只在选中路径需要时加入 | DTMAPI 自有 DTO、结果及可选服务 |
| 当前游戏存档身份、真实保存结果、表加载/asset 读取与场景对象传播 | GameBridge 的已验证共享适配 | 不暴露原生对象、字段名或 Harmony patch |
| 特定内容 schema、校验与家族创建引擎 | 可选 Content Host | 独立版本化 schema；Host 依赖平台基础服务 |
| 单产品 AI、经济规则、画面风格、玩法平衡与专用补丁 | 该 Advanced 产品 | 不因需要反射就升格到 mandatory GameBridge |
| UI 交互状态、资源与焦点归属 | 平台 UI 服务；优先复用已有菜单和原生窗口适配，组件模型经 V-UI 决定 | DTMAPI 自有窄契约及资源句柄，避免 raw Unity |

## D01：数据分成三类，不让安装目录承担用户状态

1. **配置**：作者选项、按键、语言偏好；沿用 Config，独立于游戏保存即时提交。
2. **全局作者数据**：不属于某一局的记录/缓存；由 Core 按 owner 与 key 隔离，保存在 Runtime 用户数据目录，包更新/撤回不删除。默认本机数据，不承诺 Steam Cloud。
3. **随存档提交的数据**：当前存档的玩法值；Core 维护 Working/Committed，GameBridge 只提供真实身份与提交适配。不能用全局数据或 Config 绕过游戏回档语义。

近期配置修复只保留序列化器生成的有效 JSON 和现有原子替换；删除有误的 Prettyish 后处理，不换 JSON 技术栈。损坏恢复必须先成功保全原文件；读取权限/IO 异常不当作坏 JSON，备份失败不覆盖原始字节。现有 ReadConfig 的迁移 Action 仍按旧调用语义执行；PN-019 的按版本迁移是新可选能力，不暗改为全局只运行一次。

Mod 自带 JSON/图像是只读输入，不在安装目录写回设置。包内使用相对路径；拒绝越界路径、设备路径、大小写冲突及链接逃逸。文件 API 的边界是可靠性与可移植性，不是 CLR 代码沙箱。

最小公开形状是 owner 绑定的 `ReadGlobal<T>(key)` / `WriteGlobal<T>(key, value)` 与 `ReadSave<T>(key)` / `WriteSave<T>(key, value)`；删除采用独立动作，避免 null 是否表示有效值的歧义。读取返回 `Found` / `Missing` / `Corrupt` / `UnsupportedSchema` 等可分辨结果；预期环境不足不伪装成不存在。编程错误（无效 key、失效 owner、错误线程）按契约失败。新接口走可选服务，不给旧 `IDtmHelper` 增加必需成员。

存储 envelope 记录平台格式、owner、key、作者数据 schema 与载荷摘要。序列化只处理数据模型，不写 CLR 类型名、不启用任意类型实例化。公共接口不暴露目录布局，布局以后可迁移。大小与并发上限先作为可诊断、可测试的内部资源策略，不承诺无限大 JSON。

迁移逐版注册，迁移候选通过验证后一次发布；异常保留原字节和旧 Committed。未知新 schema 禁止旧 Runtime/Mod 用默认值覆盖，损坏内容进入明确的只读错误状态，恢复采用显式动作。全局数据和配置可有各自的缺省策略，但 SaveData 损坏绝不自动清空后保存。

## D02：SaveData 先验证同档存储，再选择必要的提交机制

已确定的目标是 owner/key/schema 隔离、原生成功后才提交、不保存即回退、错误可诊断及未知数据保全。新通用 SaveData 不预选 sidecar，也不预先承诺永久公共 SaveIdentity。按 V-Save / PN-024 有界核对正式扩展，再优先实测现有同档字符串容器；不适用时按证据和成本比较受限序列化适配与外部 sidecar，不要求先把每个方案实现一遍。受限适配也必须证明缺 Runtime 时的原生加载和重存保留，不因 JSON 可添加字段就视为成立。

先用私有命名空间载荷验证 `cityData.dialogueManager.variableStorage`：两 owner 不碰撞；原生对话初始化和正常运行不覆盖载荷；卸掉 Mod/Runtime 后原生加载、保存、再装回仍保留；字符串类型、容量和未知 schema 不破坏游戏；官方复制、备份回退及新建同槽得到正确数据。当前参考中的 `CityArchiveData`、`DialogueManager` 均序列化相应字段，`DialogueVariableStorage` 支持带 `$` 前缀的 string；`DialogueRunner` 的 Yarn `SetProgram` 后保留性仍未实测。该候选曾见 [ISSUE-028](../debug/issues/ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md)，历史记录不替代当前实验。它通过才能进入同档方案设计；若任一成立条件失败，保存准确反例后比较下一方案，不能转成作者必须避开对话系统或永不卸载 Runtime 的隐含要求，也不能直接升级为全局序列化器 patch。

公共 save 上下文先只要求本次 SaveSession/generation 与明确可用性，避免将槽号、玩家名、日期或推断的存档谱系公开为永久 ID。同档方案优先让数据随原生复制、导入和回退自然移动；只有独立存储/外部引用确实需要稳定身份时，才验证其产生、复制和恢复规则并决定是否公开。对 sidecar，槽位与路径仍不能独自证明永久身份；关联无法证明时不得写入或猜测恢复。

已有产品 sidecar、reader、恢复及停用退物语义保留，不在新通用后端选择时批量迁移或删除。若后续迁移某产品，另做准确旧数据、缺 Mod、回退和恰好一次验收；本节不改变 PROJECT 或旧保存回调的现有承诺。

| 边界 | 确定行为 |
| --- | --- |
| 进档 | 从该原生存档内载荷或已可靠关联的外部载荷建立 Committed/Working；旧 save/scene 句柄全部失效 |
| `WriteSave` | 在调用边界验证并序列化为不可变 Working 快照；不执行原生保存，不把值标为 Committed；不保留作者可在背后修改的 DTO 引用 |
| 准备原生保存 | 固定此次各 owner 候选并保证它们进入本次正确的提交边界；同档方案纳入该次原生序列化，sidecar 才要求预先持久化可校验 journal；不在此阶段首次运行作者序列化回调 |
| 已证明原生未提交 | 候选不提升；允许本次运行继续保留 Working 以便玩家明确重试；返回标题/退出时丢弃未提交 Working |
| 已证明原生磁盘提交 | 同档载荷已随该次原生文件提交，即使随后回调/外层 UI 失败也保持已提交，外层异常单列；独立载荷仅提升与该次事实匹配的候选。之后新写入属于下一 Working revision |
| 原生磁盘结果未知，或已提交后独立载荷尚未完成提升 | 保留候选与关联证据，进入可诊断待恢复；不能凭外层 bool/异常删候选或自动重放 gameplay。已证明同档提交但外层失败不属于这一状态 |
| 原生提交后的中断 | 同档方案冷加载时读取已随原生提交的载荷，不再另做一次玩法提交；sidecar 方案用精确身份、提交证据和候选摘要恢复提升，证据不足只报告待处理，不重放任意玩法 |
| 没有发生原生提交就返回标题/关闭/崩溃 | 重新进入时只见上一次 Committed；未提交 journal 不得成为自动保存；原生提交中断另按上述证据恢复 |
| owner 停用/卸载 | 停止新写入、释放句柄、保留已提交数据；不得借停用把 Working 转为 Committed；孤儿恢复走独立管理事务 |

旧保存回调、required prepare、native 与 commit/abort 的先后由 [RT-07 保存协调](platform-runtime-contracts.md)拥有。旧 SaveSaving 先更新 Working，再冻结本次候选；冻结后的写入（含 SaveSaved）属于下一 revision。重入保存明确拒绝，不能替换在途候选。PN-024/012 应验证这个时序，不以事件订阅顺序偶然正确作为保证。

实验分别观察准备结果、原生磁盘结果、外层调用结果，以及存在独立 participant 时的提交结果，具体时序见 RT-07。必须覆盖文件替换后 AfterSaveData/外层 UI 异常以及进程中断，不能仅用 outer SaveGamePostfix 的成功返回当作所有持久状态的唯一证据。同档方案应证明载荷与原生同次序列化/切换即可，不为复用旧实验模型再制造独立 journal 和提交阶段；sidecar 方案仍须完整证明身份、witness、journal 和冷恢复。

**保存准备失败必须在技术实验中解决。** 同档载荷无法正确进入本次序列化，或选中的 sidecar durable candidate 写入失败时，不能继续报告“保存成功”却丢弃 Mod 的本次数据。推荐按正式保存失败路径返回可重试错误并向玩家说明；GameBridge 必须证明不会破坏睡眠/仅保存/其他正式入口。同档方案要求载荷与原生一起提交，不要求在原生保存之前另持久化一份；sidecar 如果只能观察成功却无法事先保证候选可靠落盘，则不能进入公开写入能力。

Data 只保证受管理的键值随官方保存提交，不自动让任意原生物品、金钱或世界操作具有事务性。物品先消耗、随后 `WriteSave` 失败的业务仍可能丢物品。需要跨原生状态的一致性时，必须用相应领域事务服务或产品自己的完整事务，不以“使用了 Data helper”代替设计。

复制/删除/恢复规则：

- 官方复制得到独立目标及正确的原提交 owner 数据，不共享可写状态；同档方案不额外要求公开一套谱系 ID。独立存储方案才维护目标身份与数据复制事务。
- 官方删除、失败删除和槽位重用不得使新档继承旧 owner 数据。独立存储按已验证删除事务和保留策略处理外部载荷，不能把这个机制强加给只随原生文件存在的载荷。
- 外部复制/导入、原生迁移、current/prev/bak 回退分别测试；同档载荷应随对应原生字节恢复。外部载荷无法可靠判断关联时要求显式映射，不猜测“相同玩家名即同一档”。
- 旧/新 Mod schema 升降级、Runtime 回滚不得覆盖未知数据。显式恢复只恢复匹配官方提交的数据；恢复工具不能把玩家已经放弃的白天操作带回来。
- 不从同档存储推导所有 Steam Cloud/跨设备路径都已验证。独立存储的导出/导入必须携带必要身份映射和原生提交描述；仅复制 sidecar 不算可迁移存档。

SaveData 首次进入验收至少包括：同一游戏进程连续两个存档、官方复制/删除/槽位重用、普通睡眠保存、另一原生保存入口、不保存回滚、准备失败、原生未写的失败、写入后回调/外层 UI 失败、原生提交前后进程中断、未知 schema、旧备份恢复、owner/Runtime 缺失后重存。对同档容器增加对话初始化、两 owner、容量与冲突；选用 sidecar 再完整执行候选落盘、提升失败、损坏 journal、提交关联与冷恢复矩阵。后续切片按 E05/产品流程选择改变的行为，复用未变的入口证明。故障/中断使用与实时 Steam AutoCloud 隔离的可处置 fixture；普通保存沿 PROJECT 的专用槽规则。每种窗口确认准确原生字节、相关 Mod 载荷、玩家可见值和下次加载值，不靠事后覆盖玩家存档伪造成功。

## D03：ModContent 是只读包资源，资源生命周期先于素材种类

第一层提供 owner 根下的文本、JSON、字节及资源存在查询；第二层增加解码图像与供 UI/内容适配使用的图像句柄。资源 key 使用 owner 命名空间，规范路径分隔符，SDK 检查跨平台大小写冲突。不能通过一个“全局相对路径”读到另一个包。

只读包输入和图像消费可独立交付，不依赖通用 GameContent 编辑器。先复用现有文件服务、解码与已证明的 native 资源接口；V-Content/V-UI 的两个真实消费者决定需要哪些新句柄和共享缓存。资源释放、越界防护和迟到结果失效是必需行为，但不为尚无消费者的素材类型先建设万能资产系统。

新缓存键至少包含 owner、规范相对路径和 generation。两个独立包各有 `data/settings.json` 时必须读回自己的不同内容；当前 ContentQuery 的全局相对路径查找保留旧语义，不在新 own-file 服务中复用它猜归属，也不顺手破坏旧 ABI。

Core 保留字节/数据快照及缓存元数据，GameBridge/渲染宿主创建与释放 Unity 资源。公共图像数据可以是 DTMAPI 自有不可变像素描述，运行时资源以 `IImageHandle : IDisposable` 等不透明句柄表达；不把 `Texture2D`、`Sprite` 或原生 Addressables handle 放入 Abstractions。

资源句柄绑定 owner 与内容 generation，save/scene 资源还绑定各自 generation。异步读取完成后回到约定主线程并再次检查 owner/generation；取消或迟到的结果释放资源，不激活已退出场景的对象。调用方释放和 owner 清理幂等；先采用简单 owner 持有/释放，只有实际共享/跨代消费者需要时才采用引用计数或租约及依赖释放顺序。不能提前 Destroy 仍有效使用的纹理，也不为假想共享先建资源图。

缓存失效不等于旧句柄消失。已发布句柄维持其租约，新的请求得到新 generation；是否将新图像传播到正在显示的对象由消费适配决定。磁盘变更可以返回“下次请求生效”或“需场景重载”，不能直接报告所有画面已刷新。

验收用两个独立包读取同名不同图像，检查 namespace 隔离、非法路径、坏 JSON/图像、大小写冲突、owner 清理、异步迟到、反复刷新与标题往返的资源数量。Mono 中实际展示图像并验证旧对象与新请求的行为；仅成功读出文件或解析 PNG 不证明 UI 资源能力。

## D04：GameContent 先比较官方内容与薄适配，再增加必要的编辑机制

第一版只选一个低风险数据表字段和一种静态图像用途完成完整链路。优先非持久化的展示文字/图标，禁止把 item ID、行为类型、背包容量或实体创建混入第一个演示。GameBridge 必须找到实际请求、表构建、缓存、引用者与重新传播边界；能读取最终值不等于可以写入。

官方系统继续拥有官方 Mod 的选择、顺序与合并。V-Content 在已有 R4b 接缝和 stone 文本/自制图标样本上比较：A 直接官方 JSON/PNG；B 沿用官方 schema 和加载语义，只补来源、校验与生命周期适配；C 对 A/B 无法满足的真实组合/动态修改需求，建立受限编辑阶段。记录作者步骤、额外语法、重复配置、冷启/撤回、实际传播及成本，而不是只比较代码量。满足目标的 A/B 不必为经过 DTMAPI 而再写一套补丁 DSL；选择 C 时才在官方最终结果后、原生消费前接入，不能另做官方 JSON 合并器。不同资产可选不同方式，不强求万能 Hook。

候选编辑 API 可使用 `AssetKey`、带 schema/generation 的 `AssetInfo`、owner 绑定注册和失效结果，但精确签名、优先级、回调及 schema 待 V-Content/R4b 选择实际需要的机制后冻结。无论采用哪条路径，都区分已应用、拒绝、下次加载、场景重载或重启生效；只有完成声明的传播范围才报告已应用。官方直接内容继续按官方支持范围描述，不借薄适配声称获得未实现的回滚/热刷新。原生表类型、Unity 对象和 Harmony 参数留在内部。

若选中 C 的受管理编辑机制，以下是其待原型验证的内部组合方案。失败不泄漏半修改是该编辑能力的产品门；具体优先级与注册语义在原型后冻结，不成为 A/B 官方内容的新规则：

1. 首先取得官方基础结果，然后选 DTMAPI replacement，再依序运行 edits。
2. replacement 只有唯一最高优先者时可接管；最高优先级并列即冲突，保留官方基础并定位所有冲突 owner，不随机选先加载者。
3. 编辑顺序为依赖拓扑约束、有限编辑优先级、稳定 owner ID、owner 内注册顺序。依赖优先于显式编辑优先级；非法循环拒绝注册并诊断。
4. 每个编辑在隔离候选上执行，正常返回且 schema 校验通过才提交到下一阶段。失败编辑全部丢弃，后续编辑从最后成功候选继续；不会看见一半修改。结构性无法隔离的资产不得声称支持这种编辑模式。
5. 对声明式补丁记录字段/区域写集。同字段写入允许按确定顺序覆盖，但可查看来源与结果；可声明 expected-old-value/冲突拒绝来保护假设。任意 C# callback 的诊断不虚构精确字段写集。
6. 递归请求同一资产、依赖资产循环、请求过程中失效、刷新后又有新 generation 均须有界处理。未完成旧候选不能覆盖新 publication。
7. 所有编辑成功前不改变原生共享表或存活对象。失败维持上一有效 generation；首次加载失败保留可用的官方基础结果。

第 4 项只适用于宣称支持失败隔离的编辑方式，不能以浅引用备份宣称通用回滚。原型比较数据深副本、窄字段补丁或已证明的 copy-on-write；不要强克隆任意原生对象图。所选编辑方式必须让编辑者先改嵌套集合或图像再抛错，检查其他持有者和后续编辑均未见半修改，并测量复制/解码/纹理分配与 lease 释放。成本不成立就收窄编辑粒度、保留官方路径或只读，不降低保证后仍保留同样宣传。

只在承诺运行期失效的资产上维护所需的派生资源/消费者关系，批处理重复请求，不预建全游戏依赖图。受管编辑的 Core 负责候选与 publication；GameBridge 为每个受支持领域声明下次读取、就地重绑定、重建显示对象、场景重载或只能重启。原生重载足够时先复用它并证明影响范围，禁止用全局清缓存掩盖未查清的消费者。

验收至少两个独立作者与原生官方内容控制组：对照官方顺序、同名内容与错误诊断、语言切换、已打开/新打开界面、场景重建、启停/撤回及资源回收。选用受管编辑时再完整执行不同字段组合、同字段覆盖与来源报告、replacement 并列冲突、坏编辑后的好编辑、深层抛错/last-good 和官方内容共存矩阵。必须看到真实游戏字段/画面和原生读取一致；索引报告、反射读取或单张截图不能替代传播承诺。V-Content 不重做原有接缝调查，只在既有输入上补方法对照与选中方案缺失的证明。

## D05：ContentPack 绑定一个 Host，Core 只管理共性

一个通用 pack 显式绑定一个 Host，内容 schema 由 Host 自己拥有。V-Host 先以独立 Host 和两 Pack 证明依赖、资源、失败归属与游戏效果，再冻结 Host ID/兼容版本和 manifest 字段。pack 可以依赖其他包，但不得隐式匹配“目录里碰巧存在的任意解析器”。绑定通过现有 SDK/Runtime/Doctor reader 同批交付，不手写第二套 manifest 解释器；旧包没有绑定时继续既有受限语义，已有官方内容不必迁移成新 Host Pack。

加载顺序为：来源及启用检查 → manifest/Host 依赖解析 → Host CodeMod Entry 注册能力 → pack schema 验证与候选生成 → 到达相应生命周期后由 Host 激活内容。Host 未安装、版本不足、Entry 失败、未注册处理器或 pack 未启用时，pack 都不能显示“内容已加载”。不能仅在 ModRegistry 增加一行就算成功。

Host 获得其 pack 的只读 manifest、资源服务、翻译与稳定 owner 身份。Host 的回调可以代表 pack 注册资源，但归属记录必须同时保留 Host 和 pack，不能全部记在 Host 名下。单 pack 失败不阻止同 Host 的其他合法 pack；pack 清理释放自身资源，Host 清理关闭全部依赖 pack 的执行权。pack 不包含可执行 DLL；需要代码时使用 CodeMod。

通用 Host 绑定只依赖已成立的依赖/owner 与只读包资源，使用原生引擎时增加 Advanced；只有具体 Host 需要编辑或额外保存时，才增加相应 GameContent/SaveData 依赖。首个 Host 先比较复用官方 schema/原生能力的薄 Host 与确有需求的小型补丁 Host，不预定以 DSL 证明平台成熟。增改删、条件、表达式属于具体 Host 的可选语义，不能让 Core Host 协议等待完整编辑器，也不另建保存引擎。

官方 `ModInfo` 会扫描 `Content` 下的 JSON/PNG，Host 未接受的包若直接进入这个扫描范围，可能在 DTMAPI 标为 inactive 时已经生效。V-Host 必须验证包布局/原生消费接缝能阻止缺 Host、错误版本或停用包提前生效和双重加载；只隔离 Core 状态不够。直接官方内容控制组沿用原生机制，新 Host Pack 的受控资源由所选适配显式管理；不移动或重新解释现有合法包来迎合新协议。

验收用仓库外独立 Host 工程与两个 pack 工程，通过分发 SDK 打包并安装官方 Local 来源；至少一个 pack 同时包含翻译与图像。覆盖缺 Host/错版本/错 schema、pack 与 Host 独立启停、错误隔离、依赖顺序、升级后旧 schema 迁移、运行期刷新与需重启状态，并由作者无需改 Runtime/Catalog 完成全流程。

## D06：游戏领域先复用原生操作，新增抽象由真实缺口决定

领域 API 不从现有 Frozen 大接口继续加方法。V-Domain 先用两个真实作者需求找到共同 native owner，比较原生既有查询/操作的薄适配与平台协调；先交付小型快照，再交付有实际结果的必要操作。原生单次调用已满足需求时不另造事务引擎；仅跨资源/系统的一致性缺口需要领域专用 preflight、提交或补偿，且必须证明它们适用的边界。Native 调用存在不自动证明原子性，GameBridge 只吸收已证明的共性，玩法规则留在产品。

V-UI 用两个独立作者的状态 HUD 和交互面板，比较复用现有配置菜单、原生窗口/组件的薄适配与新的声明式组件树。先确定 owner/焦点/关闭/资源行为，验证作者代码、布局、翻译、共存和维护成本，再冻结组件 API；不先建布局 DSL、通用编辑器或新导航框架。该方法对照不撤销 D09 已验收的配置菜单输入路径。

| 能力家族 | 推荐方向 | 关键进入条件与产品验收 |
| --- | --- | --- |
| 世界/房间/玩家状态 | 只读快照、明确 ready 状态、generation 绑定的瞬时句柄；时间变化/房间变化是语义事件 | 普通进档、换场景、加载失败、返回标题、两档连续访问；旧句柄不能操作新场景；不公开一个随时可写的巨大 World 对象 |
| 物品/背包/经济 | 先查询 ID/数量/可用性，优先原生操作与结果核验；有跨边界缺口才建立有限事务 | 真实堆叠、背包满、部分可用、同时变动、消耗失败、保存回滚；一次返回成功必须对应实际 native 数量结果 |
| 农业/机器/效果 | 将一次性操作与持续调度分离；产品规则留在产品，真实共享事务才归 GameBridge | 不从某一个自动化产品概括全领域；冷启停、实际生产、资源消耗、刷新、保存失败与两消费者冲突全部验证 |
| UI/HUD | 先复用已验菜单和原生显示；V-UI 选择最少必要的文本/图像/交互抽象 | 两作者面板共存、键鼠/手柄导航、缩放/分辨率/长翻译、模态释放、异常关闭、标题清理；设备按实证声明，不复制 SMAPI 的 MonoGame 渲染参数 |
| 输入 | 稳定 owner/scope/按键重绑/模态接收，再设计游戏动作拦截 | 现有 `Suppress` 仅隐藏 DTMAPI 当前帧状态，不能改成未验证的全原生拦截；若新增原生动作取消，必须覆盖原生 UI、移动/交互消费者及多 Mod 顺序 |
| 翻译/配置 | 保留现有服务；增加参数替换、语言变更、可验证迁移和可选刷新 | 空/坏 catalog、区域回退、动态语言切换、配置编辑取消/保存/回滚、重绑冲突；显示层真实验收后再提升稳定级别 |
| 音频 | 先保留现有 Audio replacement 的事件/回调限制；后来增加 owner 音频播放租约 | 原生音量/暂停/场景/回调语义明确，两个 replacement 冲突可解释，无遗留播放；3D emitter 和原生回调另验收，不能从 2D 替换外推 |
| 新物品/配方/商店条目 | 首选官方 schema 与内容管线；行为扩展由产品/Host 拥有 | 稳定命名空间、ID 冲突、翻译/图标、消费/制作/交易、重进与保存缺包行为全部成立 |
| 动物/怪物/攻击/无人机 | 每个家族独立 Host/adapter；优先已有原生家族的有限变体 | 实例创建、房间/AI 管理、渲染、死亡/捕获/销毁、native 保存与恢复、缺 Host/缺定义策略齐备；登记成功和调试 Spawn 成功不算 |
| NPC/任务/地图/地牢 | 保留完整能力方向，先用已有原生模板扩展，再评估新行为/新场景 | 对话/日程/交易/任务依赖、地图边界与传送/资源刷新/实体保存，分别做家族进入审查；不能把所有系统包装成一个 CustomEntity 服务 |
| 新载具/多无人机/原创宠物 | 产品实验优先，证明原生 owner 或自有实体引擎后再决定平台化 | 单例与场景限制、输入/相机/保存需要逐项突破；没有稳定原生入口时允许产品自有实现，但不得假装通用平台已经支持 |

## D07：持久实体必须包含缺失与升级行为

V-Entity 对一个实际家族比较：A 官方 schema＋原生创建/保存；B 沿用 A，仅为原生缺失的状态加受控载荷；C Host 自有实体创建/保存模型。先选已有原生家族的有限变体，不从 Frozen CustomEntity 外壳反推必须建设 C。原生已经保存的实例不在 sidecar 再保存一份并重复生成；额外实例 ID、序列化格式、重建协议须有明确需要并通过缺包/升级实验后才冻结。

选择 A 不以 Core SaveData 为前置，但必须证明官方 ID、创建/销毁、原生保存和缺失恢复足以支撑该家族。B/C 需要新增随档载荷时依赖已接受的 D02；Host 拥有其新增 schema、必要的稳定类型/实例 ID、状态及引用，并负责原生映射、创建顺序、迁移和房间/AI/资源清理。运行时对象指针、Unity instance ID、场景数组索引不能充当需要跨保存恢复的永久 ID。没有一条路径满足安全缺失与恢复行为时，交付有界实验结果，不公开持久家族。

缺 pack/Host/未知版本时默认保留原始状态并禁止破坏性重写，提供可诊断的缺失内容标记或阻止依赖该对象的危险操作。不得静默删除实体、把物品转成空气或反复生成替代物。具体家族若不能让游戏安全忽略缺失对象，应在加载前明确阻止该受影响存档并提示恢复依赖；不尝试“先加载再看会不会丢数据”。

首个家族的产品验收必须经过：新建、获得/放置、活动与跨场景、正常保存/重启、不保存回滚、消耗/捕获/销毁后保存、升级定义/schema、禁用或移除 pack/Host、恢复依赖及 ID 冲突。使用额外载荷时再执行其准确后端的失败窗口；sidecar 的原生成功/提升失败不能被省略，同档模型也不能以没有 sidecar 为由省掉提交前后冷加载。每一步核对恰好一份实例及其关联物品，不能把资源数量净变化作为零残留证明。

## D08：游戏版本适配与长期兼容属于平台交付

GameBridge 内部每个受支持能力记录所依赖的原生符号、当前验证 build、生命周期前提与必要后置条件，复用现有 Hook/feature 状态和诊断权威。不要按游戏版本复制完整 GameBridge，也不要仅用版本字符串推断能力可用。

新 build 到来先检测符号/签名与责任变化，然后做针对该能力的真实回归。结构匹配是筛查，玩家行为与保存矩阵是接受条件。某一可选图像/领域 adapter 不兼容时关闭该能力并解释原因，不让无关的日志/配置/作者安装链全部不可用；必需保存适配不可靠时不能继续暴露可写 SaveData。

对作者公开稳定能力 ID、版本、是否可用、缺失原因及恢复建议，不公开原生字段探测细节作为长期契约。公共 Stable API 需要旧作者 DLL 在新 Runtime 上实际加载与行为回归；语义破坏按兼容政策引入新契约，旧 contract 通过适配或明确弃用周期保留。Frozen 兼容壳的删除与新领域平台化是独立任务。

方法实验通过后，后续升级原则上增加经过验证的能力或修正对应 adapter，不因新的工具偏好重建整个平台。维护记录保留为何选薄适配或自有机制、被排除的准确反例和公共冻结点；只在实际反证影响已选边界时重开对应方法。0.7.0 不提前发布这些尚未验证的新公共面，亦不能借未来方法调整要求现有 Mod 改身份、目录、manifest 或接入方式。

## D09：现有配置菜单的控制器支持先独立交付

PN-037 是现有 Input/ConfigMenu 的产品能力，不等待 PN-028 的通用 UI/HUD 或 PN-012 SaveData。分开两个用户目标：可自定义控制器按钮来触发 AutoFishing/配置入口；用方向键、D-pad 或摇杆移动焦点并操作配置页面。能打开窗口不等于能不用鼠标完成配置。

本节已经形成的原生动作、标题入口、键鼠导航与实验性控制器边界保留。V-UI 复用其证据，只比较通用作者 UI 的新增需要，不重新要求已通过的同一输入路径从零证明；新设备/Steam Input 路径仍补对应实体体验证据。

责任分配：

| 边界 | 所有者与决定 |
| --- | --- |
| 硬件/原生动作 | GameBridge 的现有输入适配提供一次采样与 availability；设备/API/映射/游戏控制模式留内部，不在 Core 直接接 XInput 或加第二帧驱动 |
| 绑定语义 | Core 复用 DtmButton/DtmKeybindList、owner/scope、冲突和 neutral；旧字符串及 raw JoystickButtonN 兼容。不同设备的按钮编号不默认等价于 Xbox A/B |
| 配置交互 | ModConfigMenu 保持事务/行模型；Bootstrap/原生页面适配管理可见控件、焦点图、滚动和菜单 action。焦点/Navigation 内部模型不额外成为长期 Public API |
| 产品消费 | AutoFishing 只消费 toggle action，配置菜单消费自身入口；产品钓鱼策略不移入平台。两消费者证明可复用绑定机制 |
| 原生占用 | 菜单动作接在游戏实际菜单输入通道；Suppress 继续只代表现有 DTMAPI 帧遮蔽。模态禁止泄漏须有 native 证据，不能仅调用旧 Suppress 就声称阻断游戏 |

首片支持按键录入/替代组合、清除/恢复默认、冲突诊断与保存；键鼠设置不被自动覆盖。无手柄报告未连接，可编辑已存绑定；热插拔不删除配置，重新连接/焦点恢复必须先 neutral，不用旧 held state 触发一次新动作。扳机/轴在未证明映射前明确未支持，不能在稳定格式中猜设备编号。

导航优先消费游戏已有 Navigate/Confirm/Cancel 动作，保留游戏确认/返回习惯和 Steam Input 的实际输入路径。若必须转换模拟轴，deadzone、hysteresis 和 repeat 是内部可调实现；不要把所有轴值永久序列化为公共按键规范。原生动作不可取得时只提供已证明的窄 adapter，并记录缺失能力，不绕过未知原生菜单接缝。

焦点图覆盖入口、Mod 列表、页面、每种控件和应用/取消；跳过不可见/禁用项，切页/语言/缩放后有可见高亮。输入框编辑、数值调整、按键捕获和普通导航是不同模式。模态退栈先结束编辑再返回，选择/返回键不能同帧触发游戏移动、AutoFishing 或重复关窗。窗口/owner/场景关闭释放捕获并重置 neutral；不要修改不属于平台的其他菜单设置。

**标题入口选择。** 按匹配游戏的 HomePageUiState.buttonActions / RenderTextMenu 接入一个平台拥有的“Mod 配置”动作，让原生显式导航、显示和确认处理可达性。GameBridge 保持精确动作身份与 Hook 生命周期，Bootstrap 提供本地化文字和打开回调；刷新、语言变化、标题重建时不重复添加，关闭/撤回只移除自己的动作与回调。当前 Cancel 选择最后项，插入须保持原生退出仍是最后项及原回调索引正确。已有角落图标和 open-only 快捷键是额外入口，不能承担方向导航的唯一入口；原生入口失败时保留降级诊断，不宣称全程可达。此决定是内部标题适配，不建立通用公共菜单注册契约。对应 native 事实与返修边界见[独立验收 A1](../reviews/code/2026/20260909-0009-platform-m3-input-release-acceptance.md#a1--p2方向导航缺少原生标题入口)。

**实验与晋级。** 无设备时可完成纯逻辑输入、实际 Mono 无设备降级和键盘全流程导航；这些不证明真实控制器。公开材料分别标注绑定和导航为实验性，并给出设备型号/连接方式、OS、Runtime/游戏版本、Steam Input 开关、操作步骤与结果的简短反馈项，不要求玩家上传存档或全日志。

从实验转为“已验证”需要至少一份可复现的实体手柄正向完整旅程（重绑/保存/冷启、进入配置/遍历/调整/确认取消、断开重连与返回游戏），以及没有未解决的阻断问题；只晋级已证明的设备/输入路径。要宣称普遍控制器支持，至少增加一个不同映射或 Steam Input 路径的实测对照，并覆盖原生输入与 Steam Input，不能用两份相同转键盘配置当跨设备证明。玩家报告可由维护者按步骤/配置/证据核对，无须用户先购买设备。

API 的 Experimental、某设备已验证和产品对外支持是三个状态；不能由无人投诉、模拟注入成功或 M2 的 JoystickButton 注册晋级所有状态。反馈等待只影响相关支持声明；键鼠与其他里程碑继续。首次 0.7.0/0.7.1 的排期由路线拥有。

## SMAPI 参考与明确差异

本轮直接查看本地 `E:/Python_project/SMAPIlearning/SMAPI` 的 `IModContentHelper`、`IGameContentHelper`、`IDataHelper`、`IContentPack`、`Events/IContentEvents`、`ITranslationHelper`，以及 `SMAPI_version_study/SMAPI-3.0/src/SMAPI/IContentHelper.cs` 和当前仓库 `docs/release-notes-archived.md` 的 3.14 段落；仅采用语义经验，不复制实现。

- SMAPI 后来将 ModContent/GameContent 分开，并为加载冲突、编辑优先级、来源标注和资产变化增加事件。DTMAPI 应直接建立这几个区别，不先发布混合 helper 再留下兼容债。
- SMAPI 文档也区分清缓存与旧引用更新。DTMAPI 必须为每种 Unity 资产写清真实传播能力，不能把 invalidate 宣传成全游戏即时生效。
- SMAPI 的 prevAsset 引用备份不能证明任意原地嵌套变更可回滚；若 DTMAPI 提供更强隔离的编辑能力，必须独立证明正确性和资源成本。先比较官方内容能完成什么，不从 SMAPI 的编辑 API 推导 DTMAPI 必须复刻其全部机制。
- SMAPI SaveData 跟随游戏保存。DTMAPI 保留相同玩家语义，先验证现有同档容器和受限适配，不再由“尚未找到通用字典”直接推导外部 sidecar；每条后端分别证明缺 Runtime、复制/回退与提交行为。
- SMAPI 的 `IContentPack` / `ContentPackHelper.GetOwned()` 提供绑定、文件、翻译和包资源访问，不要求平台先拥有补丁 DSL。DTMAPI 的通用 Host 绑定与具体内容 Host 同样分开推进。
- SMAPI 直接暴露 MonoGame/Stardew 类型的做法不符合 DTMAPI 已选择的净化公共契约；DTMAPI 使用自有 DTO、资源句柄，Advanced 允许作者承担原生耦合。
- 历史说明中的跨平台路径、缓存与传播修复表明这些不是附带细节，必须进入首个可公开内容平台的验收。

## 架构复盘边界

Sol 可以继续已有基础 IO、全局数据及已接受生命周期工作；新后端、内容编辑、Host、领域/UI 和实体依[方法验证](../planning/platform-next/method-validation.md)执行有界原型、选择和实施。方法验证不是额外审批流程，不重读全仓，也不允许只证明方案能运行而省略较简单替代方案：

1. **V-Save / R4a**：先选同档容器、受限序列化适配或 sidecar，再验证其缺 Runtime、复制/删除/回退与实际提交；永久公共身份和独立 journal 只在所选方案需要时进入。
2. **V-Content / R4b**：比较官方内容、薄适配与受限编辑；选中编辑后完整证明候选隔离、真实传播与成本，再冻结 API 和推广资产家族。
3. **V-Host**：一个独立 Host 两 Pack 的绑定/错误/启停及原生不提前生效通过后冻结公共绑定，不等待通用 DSL。
4. **V-Domain / V-UI / V-Entity / R5**：验证原生机制的能力缺口，再决定操作协调、UI 组件和持久模型；在需要新增 schema/实例协议时完成缺失与迁移证明。
5. **稳定发布/弃用破坏点**：由真实两代作者工程和游戏回归证明兼容承诺；只有出现无法兼容的长期公共契约或产品方向冲突时才交给用户决定。

本次没有等待用户裁决的阻塞问题。方向保持单机、当前 Doloc Town/Unity Mono、官方来源、冷代码更新、只读包资源、owner 数据隔离和可选家族 Host；优先复用经实测的原生能力，仅对真实缺口建设新机制。多人同步、通用场景编辑器、跨游戏 API 和任意代码热卸载保留为独立产品方向，不自动纳入成熟度承诺。
