# 下一批连续实施包：M3、首发准备与手柄实验

- Lifecycle: accepted-design
- Role: 原 M3/输入/发行切片的规格展开；本页不记进度/候选 hash。2026-09-10 起的当前连续任务以 [execution-sdk](execution-sdk.md)为准，本页仅供受影响修正查阅。领取与完成仅改 [status](status.md)及本卡 Update。
- Design: [包契约 P01–P07](../../architecture/platform-package-contracts.md)、[作者交付](../../architecture/platform-author-delivery.md)、[Runtime](../../architecture/platform-runtime-contracts.md)、[数据内容](../../architecture/platform-data-content.md)。
- Release: [版本路线](roadmap.md#可发布版本)。代码/能力/发布三个出口沿 [acceptance](acceptance.md)。

## 连续执行约定

同一任务、当前工作区继续；只读本卡、引用决策与发生变化的源/测试。PN-036 → PN-011/R3.shared → PN-010/R3.native → PN-022 → PN-023 → PN-033.a、PN-031.a → PN-037.a/b → PN-031.a 的受影响增量复核。两个 R3 节点在同任务完成，支持就继续，不因节点名称停工。

近期包含下列全部切片。真实依赖不满足时可交错完成无依赖片；无手柄只阻塞实体设备晋级，不阻塞实现、键盘导航实测或无设备验收。若首发包的提交/发布前提未齐，继续后面的内部工作；不绕过打包来源要求。最终留下本批统一验收材料，不自动上传。后续 M4 仍按原任务与 R4 实验证据进入，未细化的 native 实验不得凭本页扩成无限实现。

每张任务卡一个 Update，卡内切片与纠错继续原记录；无需每一编号子步骤再造审查、收据、备份、全量测试。现有选中测试图、SDK prepare、包 inventory、官方 Local 事务、runtime lock 与存档规范足够。具体 focus 使用 runner 中真实存在的名称，新用例注册到对应 suite。

## 当前收口顺序

本节是完成前面大部分切片后的接续规格，依据 [M3/输入/发行独立验收](../../reviews/code/2026/20260909-0009-platform-m3-input-release-acceptance.md)。完成状态由 status 和原 Update 拥有。依次完成下列四步；前三步无新长期契约争议就直接继续，不在每步后停等确认。A3 的验证入口修正由本次验收 Update 完成，实施任务复用其结果。

### 1. 补齐 PN-037.b 原生入口

接续 Update 0018。GameBridge 承接 D09 已选的标题 ButtonAction 适配，Bootstrap 提供文字与 OpenFromTitleButton 回调。匹配 native 输入为 build 25163613 的 HomePageUiState 私有 buttonActions、RenderTextMenu()、ButtonAction(Int32, Func<String>, Action)、HomePage.BuildNavigation()；先核对当前安装身份，变化时只重查这组接缝。

用户在原生入口首次验收后调整：并列入口为独立选项，默认关闭，保存后重启游戏生效即可，不要求热重载；独立图标、快捷键和面板内导航保留。配置界面按当前窗口分辨率动态缩放，不固定显示分辨率。后续验收分别覆盖默认关闭、启用后重启出现、禁用后重启撤回与不同宽高比下布局完整。

- 在原生 RenderTextMenu 前确保恰好一个平台动作，让其与其他原生行一起渲染并建立导航。优先插在唯一可识别的原生退出动作之前，保持退出仍为最后一项，因为当前 HomePageUiState 的 Cancel 调用 SelectLast；不得简单追加后改变 Escape 的原生含义。核对实际位置与 callback/index 的关系，不能改错其他动作。平台 action 保持可识别的对象身份；不要按显示文字/固定全局序号删别人的项。首次进入、Refresh、返回标题和语言变化均幂等。
- 原生类型和 Hook 留在 GameBridge；布局沿现有 NativeUiLayoutRepairService，模态/neutral 沿现有 NativeMenuInputAdapter。保留原图标和 open-only 热键，原生入口进入同一配置面板和事务；不增加第二套保存路径，不扩到游戏内打开。
- 关闭后恢复合理的原生选中项与已有导航设置；shutdown/owner 关闭撤自己的 Hook、动作、静态引用、UnityEvent 回调。形状不匹配时给一次可定位降级，避免破坏原生开始游戏/设置/官方 Mods/退出功能。
- 定向测试覆盖重复 Render/刷新、撤回与其他动作共存、首次确认不同时提交配置控件、关闭 neutral 与重新进入。接入既有 platform-controller focus；使用独立 authored fixture，不复制 native 实现。
- 真游戏的必要新增旅程：冷启动 → 仅方向/确认从原生标题选中并打开 Mod 配置 → 至少一项修改/取消或保存 → 关闭 → 同入口再开 → 原生开始游戏/设置仍正常。不可用鼠标、F8/F9、直接调用 OpenFromTitleButton 或反射 Invoke 冒充第一步。已有面板内 Tab/事务/H 控制台证据按未变输入复用；变化所及再验。自动输入要有原生选中对象与动作的正对照；无送达则留下该人工门，继续第 2 步和不依赖该门的打包/测试准备。

### 2. 补齐 PN-031.a 构建来源并冻结组合输入

接续 Update 0019；这是现有发行来源要求的完整实现，不是新治理系统。检查五 DLL、Catalog 选中 optional components 及其项目依赖的实际编译闭包。

- 通过 .NET 8/MSBuild 评估现有项目的 Compile、EmbeddedResource、ProjectReference 和仓库内导入，归一化到 repo 相对路径；处理新增/删除、外链源码、glob 增删和条件属性。至少覆盖 Shared、版本 props、target/BCL/policy/Catalog 资源、BepInExStubs 与 Compatibility 的产品链接。生成目录与外部锁定依赖不当作待提交源码；不维护逐版本的第二份文件清单。
- 将同一检查接到 builder 的正常构建和 SkipBuild 路径，沿已有输出/来源身份核对；旧 DLL 不得借 clean HEAD 变成“当前提交构建”。可选择从已提交输入重建并复用现有摘要比较，优先复用现有 provenance helper。没有可复用的输出证据时，明确拒绝 SkipBuild 并提示执行真实 build，不假写 BuildCommit。
- 在隔离 Git fixture 中分别留下仅 Shared、版本 props、embedded JSON、外链产品源的修改/新增/删除，验证在最终包替换前拒绝且原包保留。clean 输入可构建；只有 Wiki/计划文档改动仍可构建；已提交新输入搭配旧 output 必须拒绝。一次针对真实构建源的再现足够，无需为每个版本增加源码结构断言。
- 依已有授权只提交可归属的平台实现/测试/构建输入，保留无关工作树改动，禁止 `git add -A` 把 wiki-maintenance 一并认领。先列具体归属再提交；局部混合文件不能安全归属时保留该精确阻碍并继续其他片。不要把“工作区有改动”笼统当成外部批准缺失。
- Runtime 若生成新的内部字节，使用新的候选身份和可用 0.6.X 版本；冻结 SDK 0.6.5/API 0.6.4 和既有故障证据不覆盖。工具版、API target、native generator 的版本各守现有意义，不因 UI 修复任意改写 native 准入工具身份。

### 3. 收口 PN-023 并产生完整 0.7.0 target

接续 Update 0015。在最后内部组合上核对上述修复影响及原 M3 组合：新 Advanced Provider、Strict Consumer、shared/private 库与资源、独立 Control、旧 helper/implementer 和旧 receipt Advanced。同一进程确认共享返回/类型身份与 owner 清理；依赖/provider/native 故障既有证据可复用到未变边界，变化处增加最小实机复验。

R3 综合复盘只确认 P01–P07 组合和来源、作者闭环、兼容边界；结论支持后同任务继续。用已提交输入生成新的完整公开 0.7.0 target/source recipe，再生成与之匹配的新 SDK；历史 M2 同号快照不覆盖，planned/available/published 仍分开。API target 的 Source/版本/包描述/模板/required services、Runtime/file 版本与 Catalog source 投影保持一致，assembly compatibility 仍为原值。

对新 target 实际跑 frozen source 精确重建/缓存篡改、普通 SDK 仓库外 new/build/pack/Doctor/CLI-IDE 相同输入、旧 0.5.5 DLL 和 schema 1/2 reader 抽验；真实 Mono 冷启核对新版本字节的会话/共享调用/Advanced/配置入口。不以 0.6.4 的 PASS 自动代替改版本后的 Runtime。PN-023 的出口是已证明 M3 + 新 target，发行包出口继续第 4 步，不互相循环等待。

### 4. PN-031.a 最终包、维护演练与交回

- 用同一 source commit、Runtime/API/SDK/样例/许可/符号形成可审阅的 0.7.0 候选。按 skill 选择 Windows Runtime 包 lane，实际执行 `test-runtime-workshop-installer-061.ps1 -PackageRoot <准确候选>`；完整 Release 使用 `tools/scripts/test.ps1 -Configuration Release`，清除 focus，不用 PostRuntimeInstaller 或数段 focused 结果拼 PASS。首次全量运行发现问题即在原卡修复；无需先把本次所有无变化 focus 重跑一次。
- 已发布 0.6.1 → 候选 → 故障恢复/撤回使用既有 installer 事务；隔离 fake-game 承担破坏性故障，真实游戏只验证冷退出后的正常安装/升级/恢复与新注入。用原配置/产品与 enablement 正对照，确认卸载/升级不删除 Mod 数据；实际共享操作沿锁与 product validation。
- 准备固定最终组合的 ISSUE-010 至少一小时连续标题 idle → 读档 → 正常退出。只读现有 issue/protocol，用最后一次 Runtime 字节和列明 Mods；记录连续时长、实际加载/生命周期与崩溃/GC证据。期间可做无共享状态的文档检查，不能替换候选或并行占用游戏。失败由真实原因决定最小修复/重验，不临时改短门限。
- 更新 candidate notes 的能力边界、准确命令与支持报告；公开描述分别写按钮绑定实验与方向导航实验、真实键盘证明和设备待测，仍不承诺 SaveData/GameContent/断点调试或新的多平台实机支持。发布双发行渠道若要同版，须另对从同一接受 payload 生成的 sibling 做现行结构/生命周期检查；本机不能晋级 Steam Deck/Proton/CrossOver 的注入证明。
- 全部门完成后统一交回实际包、来源、验收与剩余外部项。没有上传授权不触碰 live upload，不主动把 PN-037.c 设备等待当作停止内部工作的理由。若仍有必要人工输入/归属等阻碍，先完成其余独立步骤，明确唯一未过门；不进入尚未细化的 M4 生产实现。

**本批之后的架构复盘。** 用户验收本候选后，在 GPT-6 进行 R4a（原生存档身份/提交窗口）与 R4b（选定表/图像的传播与隔离）实验设计；据实验证据再展开 PN-024/012、PN-013/025。版本路线、0.8 逐族清退与完整长期能力图不因本次收口改变。

## PN-036：内部版本整理和候选归位

**输入：** 当前版本 props、Catalog 的 mutable source 投影、target-catalog、compatibility/0.5.5 和未发布 M2 0.7.0 快照、PN-021 隔离构建器、Runtime/SDK/Doctor 组合语料。发布事实仍由 Catalog 拥有。

**决定：** 本次新内部 Runtime/API target 使用 0.6.2，后续需要新的候选契约时递增 0.6.X；0.7.0 留给完成 M3 的第一份公开候选，0.8.0 留给清退线。SDK 工具与 API target 本来独立；为清除此次未发布 0.2/0.3 命名歧义，下一份工具包也使用新的 0.6.2 标签，不复用旧 ZIP 的身份。assembly compatibility 仍为现有 0.5.3.0；session/schema 编号不因版本整理改动。

1. 先做只读清单：哪些属于已发布 0.6.1 / SDK 的冻结 0.5.5，哪些只是 M2/反射内部快照。原 M2 compatibility/0.7.0 的全部原始字节与 source-build recipe 保留到一个明确的 internal-snapshots 历史位置；保留目录相对布局和摘要。历史 Update/Review 不改结论，只追加简短 successor 路由（必要时机械修正已移动链接）。原反射 0.8 候选保持历史证据，不变成正式清退版本。
2. 从当前源产生全新 0.6.2 target（M2 + 已验证反射），给新 contract/hash/source recipe。普通 SDK 明确区分“可构建的内部候选”和“已经发布”；公开 0.7.0 保持 planned。不得把原 0.7.0 bytes 偷换为更大的 API，不能让历史快照进入新 SDK 包而冒充现行 target。
3. 同步可变 Runtime/file/tool 版本、minimum、模板默认、required-service 错误、候选构建脚本和 Catalog source 投影。Since/Minimum 指向确切新内部 target，并在作者说明区分首次内部可用与首次公开版本。保留原冻结 target/SDK/marker reader；不全仓替换 0.7/0.8 文本，不改已发布身份摘要。
4. 旧内部测试工程若指向退回 planned 的 target，给显式换 target、重新编译/打包的步骤；原 zip、DLL 和验证证据不改。恢复旧内部 SDK 时仍可用其原始离线快照重放历史，不强迫新 Runtime 兼容所有未发布候选编号。

**验收：** version projection、catalog/source metadata、target matrix、SDK prepare/包检查、未来 planned 拒绝、旧 0.5.5 精确 hash 不变。仓库外正常 CLI new/build/pack 可调用 M2 + reflection，旧 target 不能越界调用；SDK source/payload parity。一次受控 Mono 合成抽验覆盖新目标加载/会话、M2 调度一个闭环、反射 owner 关闭和原 retained 两 DLL；版本改写产生新字节，不能冒称原 runner 直接覆盖。未变的 M2 故障矩阵复用，PN-023 再做完整生态组合。

**出口：** 实际源码/工具回到 0.6.X，公开发布基线仍为原版本；可以继续构建内部候选，不占用 0.8 清退线。此卡属于新版本决策，不把 PN-007 的历史完成改成“没做过”。

## PN-011：依赖格式、加载计划和共享 CLR 类型

**直接前置：** PN-036；M2 owner/服务已成立。**落点：** Authoring.Contracts/src/Shared、SDK Validator/ManagedAssemblyInspector/Packager、Core Manifesting/loader/registry/owner coordinator、InstallDoctor。开始只搜对应加载入口，不重读整个平台。

1. 实现 P01–P03 的版本化模型、严格 reader 与规范化投影。SDK 从 PE 生成清单；Runtime/Doctor 验证同一模型和真实包字节。加入固定 SemVer 语料：边界包含/排除、beta 与正式版、numeric/lexical prerelease、build metadata、格式错/重复、旧 MinimumVersion 原行为。
2. 实现纯数据依赖/冲突 planner：宿主保留集、entry/shared/private roles、传递闭包、SCC required 环、optional 排序、同名同/异 bytes、同名异版本、当前驻留冲突。planner 结果含所有受影响 owner、依赖链和修复建议；用 Entry/静态初始化计数器证明拒绝发生在加载执行前。
3. loader 仅消费预检计划；registry 精确 Type，provider 成功后才启动 required consumer。实现必需依赖逆序关闭/owner retry；optional 不会因为 provider 失败被误关闭。原有 source selection/enablement 和旧 reader 不迁移。
4. SDK 先支持显式本地 managedReferences，供三个独立工程编译，不用仓库 ProjectReference。独立 Contract DLL 由发布的工具链/SDK library recipe 生成；Provider/Consumer 各自依赖同字节 contract。另有独立 Control，证明失败闭包之外仍可运行。

**必要故障语料：** 删除库、库篡改、包清单漏 transitive、宿主 DLL 改文件名、Strict 间接 native 引用、无许可材料、环、provider Entry 抛错、provider 关闭/consumer 缓存 API、optional provider 缺失、先驻留后磁盘换新。不得只证明 JSON 生成成功。

**真实出口：** 实际 Mono 中 Contract Type/Assembly ReferenceEquals、Provider → Consumer 返回值、两个 owner 及无关 Control 可观测；至少同 identity 异 bytes、同 simple name 异版本两种真实冷启拒绝和 provider 失败/关闭边界。普通用户包不需维护者补 DLL。

**R3.shared：** 给出新格式示例、固定三工程/Control、绑定与拒绝日志，确认 P01–P04；若 Mono 只能做到更窄范围，给确切诊断/限制，禁止临时换成“先到先得”或通用动态代理。结论支持就继续，格式暂是内部候选。

## PN-010：任意第三方自助 Advanced

**直接前置：** PN-011/R3.shared。**落点：** AdvancedCompilationReferences/AdvancedReferenceAssets、Shared marker/classification、Core classifier/loader、Doctor；原生实现仍在作者 Mod。

1. 在仓库外选全新、非 DTMAPI 前缀的合法 ID；用实际安装生成本地引用。先试通用直接引用；遇到 Mono/facade 冲突实施 P05 的 metadata reference surface，不沿用逐产品 stubs。缓存/编译没有维护者私有输入。
2. 实现 author schema 3、NativeContractVersion=1 和 provenance 图；Core/Doctor 复用验证。先覆盖三种入口（新开放/旧 receipt/legacy）与失败不降级，再接生成器和 pack。PROJECT 仅同步该已实施准入边界。
3. 只读实例查询 + 一个可恢复、准确 Harmony owner 的观察 Hook：Entry 安装、故意部分安装失败、关闭撤自身 Hook、另一作者仍有效。动态可选成员缺失给真实降级；核心声明缺失在 Entry 前拒绝。不为验收增加保存、玩法修改或分发官方二进制。
4. 游戏 build/hash 漂移与真实签名不匹配分开验证。模拟数据语料用于 parser，真实旧/当前引用用于 native Mono；没有可用第二 build 时保留那个实机子项未测，不写跨游戏版本保证。

**出口/R3.native：** 仓库外作者公开 new/build/pack/install-local/重启/withdraw 全流程；包排除官方/平台 DLL，引用来源可核对；同场旧 Advanced/Strict/legacy 正确分类。只重开 P05 中被事实推翻的部分，不能因新 ID 去改第一方 Catalog 或签名白名单。

## PN-022：多项目、库、资源与 CI

**直接前置：** PN-011；PN-010 可先做，技术上不依赖其游戏 Hook。**落点：** BuildPlan/ProjectBuildInputs/Builder/TemplateCreator/Packager、author schema 与 templates。

1. 将单工程 BuildPlan 扩成确定性的项目 DAG；按 P06 支持受控 ProjectReference 与 library 产物。所有工程约束在编译前检验，CLI/IDE 用同一后端；未支持 XML 节点继续报错，不静默丢弃。
2. 实现显式嵌入资源、复制资源、既有生成源码文件输入，锁定逻辑名称/目标路径/摘要。旧 sourceDirectory 非 src 工程迁移和原件保全继续过；DLL/PDB/resource/manifest 结果等价，重复 pack 确定性。
3. 明确 restore 子命令、显式源与锁定包闭包，禁止 build 暗中联网。选一个有可分发许可证的真实小型纯 managed 库，验证传递库、空缓存、断网、hash 变化、TFM/原生资产/不支持生成器负例。依赖来源凭据不入包/报告。
4. 交付无游戏 DLL 的公共 CI recipe：准备 SDK → 校验锁 → 显式 restore（若选择）→ build → pack/check → 作者逻辑测试。带游戏的自有 runner 单独执行 Mono recipe，不要求公开 CI 秘密下载游戏文件。

**出口：** 完全仓库外 solution 的 CLI/IDE/CI 共享有效输入，Mono 真调用 private library 并读取嵌入/包内资源。工程失败不会留下成功 marker/替换旧部署。这个范围完成才关闭 PN-022；不把未实现的 restore 悄悄挪出 M3。

## PN-023：M3 合成验收和首个公开 target 候选

**直接前置：** PN-010、PN-022、PN-021 与 PN-036 新字节验证。

固定一份 0.6.X 内部 Runtime/SDK 组合。全新 Advanced Provider + Strict Consumer + shared Contract/private library + Control + frozen 旧 consumer/implementer + 旧 receipt Advanced，全部公开 SDK 产物。执行一次 E01/E02/E04 主旅程：构建/资源/安装/真实启用选择/Mono 共享调用及 reflection → 故意依赖/provider/native 失败与 Control 存活 → 修复/退出/更新/重启 → 撤回。保留来源、磁盘/驻留状态、返回值和 owner 清理。

使用匹配候选验证官方启用路径，不能仅写 fixture 的 mod_infos 后声称官方 UI 操作通过。此前 M2 标题输入框不足以证明 Gameplay typing-focus：选真正游戏中可输入文字的原生/平台控件，对照焦点进出后的 Gameplay binding；若当前游戏没有合适入口，如实收窄，不伪造因果测试。

R3 综合收口只验证格式组合与作者采用，无新决定则直接继续。M3 成立后生成新的完整 API 0.7.0 候选（M2 + reflection + 本页格式/工程能力），不重用旧 M2 同号 hash。API target 与包格式各自版本化，最终公开物料由 PN-031.a 封装；未完成首发验收仍不 published。

**停止条件：** 必需共享/native 能力仍靠人工 ID/stub/补 DLL，或者实际 Mono 不能满足契约时回对应卡修复；不能靠把验收列表删短来宣称 M3 完成。

## PN-033.a：0.7 系列的弃用准备

**直接前置：** PN-020；可在无游戏输入时提前，不等待 M6。**范围：** 现行 Deprecated/Frozen/Disabled/Retired 家族，尤其 lamp、blocked custom entities 和已迁出 ProductNative 的旧桥接接口。

1. 使用当前 API matrix 的行与 retained 二进制 corpus；分清实现移出/禁用、provider 消失、接口/DTO 物理删除、存储 reader 删除。扫描当前第一方源码、retained DLL MemberRef 和可取得的第三方包，记录检查范围及未知，禁止“没找到=没人用”。
2. 给每族明确迁移方式：新公开能力、独立产品替代，或不再提供该能力。独立产品不等价于旧可编程 API，要说明行为损失；保留需要的薄 ABI 壳，不为了 0.8 标号先删稳定 helper。
3. 非 error Obsolete + owner/feature 去重 Runtime 提醒 + Doctor 离线已知 MemberRef 诊断 + 可运行迁移/替代示例。对每帧调用提醒有界，不把所有旧 target Mod 标成不兼容。
4. 准备 0.7.0 发布说明中的候选清单、最早 0.8.0 和待填的实际公告日期/移除日期；发布之前不是已经公告。为 PN-033.b 建立已发布旧字节、替代编译项目和回退 recipe。无作者反馈案例必须可演练。

**出口：** 0.7 候选中旧 DLL 继续按契约运行并收到准确诊断；具体 0.8 移除不在本片执行。清单与日期只在现行 API matrix/发布说明拥有，不建第二实时弃用登记表。

## PN-031.a：首发候选和维护基础

**直接前置：** PN-023、PN-033.a；随候选能力交付 PN-035 所需作者材料。

1. 明确 0.7.0 首发范围：M1–M3 作者生态、实际支持的调试/OS/game build、仍 Experimental 的服务、无 SaveData/通用 GameContent 承诺。核对 SDK payload、Runtime、工具 reports 和 release notes，不把不同候选拼在一个版本中。
2. 沿现有 release 脚本产出可审阅 Runtime/SDK/样例/许可/符号与检查结果。需要已提交输入时，仅提交本任务可归属的改动，保留其他工作树改动；混合归属不能确定时给确切待提交差异，不绕过来源 gate 或伪造 sourceCommit。没有上传授权不操作 live upload。
3. 锁内清洁安装/0.6.1 → 候选升级/失败恢复/撤回，区分产品保留数据与包替换。Runtime 包边界触发时才走现行 installer matrix/相关 skill；普通 Mod 测试不额外跑 Runtime installer 审计。
4. 补公开作者材料的真实命令、共享/冲突修复、Advanced 引用和冷更新说明；支持报告能定位 owner/来源/版本且不包含 session secret。不重写 Wiki。
5. 首发对实际承诺执行 E08 的对应兼容/性能项：保存现有原 DLL corpus，原发布回退；固定组合做可重复启动/空闲/循环、至少一小时标题 idle 后进档的已知 GC 风险路径。沿既有 ISSUE-010 协议，不因“只是 0.7.0”省掉命名风险门；无需把未来内容/实体性能套件提前做完。

**出口：** 可发布候选、未解问题/限制、发布说明和明确授权待办。发布动作最后交用户，内部实施可继续 PN-037；若后续修复进入同一候选，只重验变化所影响的门。若仍不能形成真实包，保持 implemented/具体 gate pending，不能用源码 metadata 输出充作 package PASS。

## PN-037.a：实验性控制器按键绑定

**直接前置：** PN-019；目标先放 0.7.1，可在首发验收中按独立性纳入 0.7.0，不降低 M3 门。**设计：** D09；它不是 PN-028 通用 UI 的前置。

1. 按当前游戏输入/native-owner 资料核对真正设备 API 和控制模式；复用唯一输入快照，不新增平台常驻 XInput 轮询驱动。无设备也能返回 unavailable 而不刷日志/分配。已有 JoystickButtonN 字符串是兼容 raw binding，不能武断别名为跨设备 A/B。
2. AddKeybindOption 支持录入数字按钮、显示/编辑/清除/恢复默认、多个替代绑定；capture 与 navigation 分离。若首片不能可靠取得轴/扳机，明确标未支持，不能把它们模拟成按钮并宣称全部支持。键盘旧串往返不变，坏/未知新串保留可修复诊断。
3. AutoFishing 消费原有 toggle 注册，配置入口消费自己的 action；两个真实消费者证明共享机制。Gameplay/标题/菜单 scope、长按只一次、冲突、neutral rearm、断开/重连、模态结束后的残留输入都按 D09。
4. 交付“无设备/实验性”状态和简短玩家验证步骤，禁止宣称已做实机手柄。测试内部设备快照、边沿/组合/序列化/owner 释放；实际游戏验证没有设备时键鼠原路径、配置可保存/取消/重启保留。只启动现有受控 runner，不伪造设备成功。

**出口：** 代码及无设备/键鼠回归 verified；实体控制器路径 pending-player，公开功能仍 Experimental。此待办不阻止 PN-037.b 和发行准备，未声明的物理按键映射不当作支持表。

## PN-037.b：配置菜单的方向导航

**直接前置：** PN-037.a 输入适配和 config menu 行模型；不需要 SaveData/GameContent。

1. 先用键盘方向键建立焦点图：原生标题/暂停菜单能进入“Mod 配置”，页列表/页面/控件/保存取消都有可达路径和高亮；隐藏/禁用项跳过，滚动到可见区，动态刷新保留或确定回退焦点。
2. 方向键/手柄 D-pad 与左摇杆方向映射到 Navigate；Confirm/Cancel/Adjust 用菜单动作层，遵从游戏确认/返回约定。方向轴有 deadzone/hysteresis/首次移动/按住重复；内部初值可用 0.55/0.35 阈值、350ms 后每 100ms 重复，再用实际帧率/设备证据调整，不写进稳定公共契约。
3. 字符输入、滑块/数字/枚举/按钮、重绑录入分别处理焦点；编辑时左右不能同时切控件，捕获期间 Confirm 不得既绑定又激活按钮。返回只取消当前编辑或回上层；有未应用改动按既有事务确认，不静默保存。
4. 模态开关、native action 消费、owner/scene 关闭、语言/缩放/长列表、键鼠与手柄切换做回归。当前仅标题配置页若不能安全在游戏中复用，先保证暂停返回标题入口和明确“不支持游戏内打开”，不得因为添加热键就假称 gameplay config UI 完成；若可复用则在本片完成游戏内入口与焦点占用实测。

**出口：** 真实游戏中仅用键盘完整走一次进入→选择→修改→取消/保存→返回；无鼠标死角，无导致玩家移动/钓鱼 toggle 的泄漏。模拟轴只能证明导航算法，实体映射/Steam Input/热插拔由玩家子矩阵补。对外分别描述“按键绑定试验”和“方向导航试验”，不能用一个已通过项目覆盖另一个。

## 本批完成后交回什么

status 的本批卡和 Update、固定合成候选、R3 两个有界结论、首发 gate 与限制、PN-037 两片的代码/键鼠/设备三个结果。正常无需再次重审 M1/M2。若手柄反馈或发布授权尚未取得，留明确待办并停止本批；下次统一验收决定发 0.7.0、吸收修复，或进入已列明的 M4 实验与 0.7 后续任务。
