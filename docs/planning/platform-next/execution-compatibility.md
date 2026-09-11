# 0.7.0 现有 Mod 与接入方式兼容执行包

- Lifecycle: `implemented`；PN-042.a–c 已完成：准确 0.6.1 差分、旧 marker 退化修正、r6/r2 与新旧作者组合及完整 Release 接受，结果归 [0012](../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)。以下保留原执行规格，未公开发布。
- Role: 0.7.0 首发兼容门的详细规格；与 [PN-041 SDK 迁移](execution-sdk-msbuild.md)共同交付，不增加第二套发布流程或证据账本。
- Scope: 以准确已发布 0.6.1、已保留的更早消费者及当前受支持接入方式为基线。队列归 [status](status.md)，候选选择归 [release-candidate](release-candidate.md)。

## 决定与承诺

用户允许推迟 0.7.0，但要求现有 Mod、接入方式正常。**玩家更新 Runtime 不应被要求重编、重打包、重签 receipt、改 manifest、重新登记 ID 或移动到另一加载目录。** 用户同时确认旧 SDK 从未公开发布，CSV 是已授权清退的第一方临时接口；本卡保护实际 Mod/Runtime 契约，不建立历史 SDK 用户支持或恢复 CSV。内部工程转换归 PN-041，旧工具 ZIP 只保留为准确证据。

“现有接入方式”沿 PROJECT：官方 `Local.*`、原生订阅快照证明的 `Workshop.*`、各自官方启停；Strict、Advanced 旧 receipt／Native V1／V2、省略 CodeModKind 的 legacy 通道；已有内容路径及 External BepInEx 共存。历史 `<game>/Mods` 已不属于当前发现承诺，不能借本卡重新开启；旧部署 journal 的 status/recover/withdraw 仍应可用。External 插件继续自行负责 Hook 和清理，DTMAPI 不承诺接管其热卸载。

兼容包含发现、准入、程序集绑定、实际代表行为及更新恢复，不能仅以 MemberRef 解析通过代替。准确证据能证明被测输入和规则，不能证明任意未知第三方反射、Hook 或数据副作用都不受影响。既有无 provider 的历史 ABI shell 不因本卡变成完整玩法承诺；基线中已知限制如实保留，不能把候选新增退化改称旧限制。

### CSV 删除的本次处理

`ITeleportDebugApi.ExportDestinationsCsv(IManifest)` 与 `TeleportCsvExportResult` 是用户确认已清退的第一方临时接口。[原删除授权](../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)及 [本次更正](../../updates/2026/20260910-0011-sdk-first-release-plan-correction.md)共同确认：**保持删除，不恢复 ABI、DTO、stub、实现或 UI，不构成 0.7.0 阻塞，也不等待 0.8 再删。**

ABI harness 的这一精确删除集合继续允许；不要因扩大公开基线检查而重新报为待修，也不要扩大到其他接口。新对照发现不属于该集合的真实支持退化，仍应修正。现有公共 API 的后续清退继续按 0.8 逐族规则。

## 顺序、归档和执行终点

| 步骤 | 与 PN-041 的关系 | 出口 |
| --- | --- | --- |
| PN-042.a 实际兼容基线 | 与 PN-041.a 准备；确认已发布 Runtime、现有包/入口与已授权 CSV 删除，不制造旧 SDK 用户 | 差异及已有证据映射；没有新退化则 b 无需实施 |
| PN-042.b 必要修正（条件性） | a 发现具名新增退化才执行，可与 PN-041.b–d 共进 | 最小修正/focused 通过；没有问题就直接进入 c，不为任务编号创造修改 |
| PN-042.c 最终组合验收 | 使用 PN-041.e 的准确 SDK，以及 b 后最终 Runtime；与 PN-041.f 合并游戏组合与最终完整 Release | 旧包／新包／来源／安装升级均有准确结论，才选定首发候选 |

本卡是 PN-041 首发批次的兼容验证支线，共用一份实施 Update/月度行和最终验证，不为每个样本/空修正步骤另建记录。原 SDK/Runtime r5、多平台 r1 和旧失败留作证据。a 有问题则最小修正，无问题直接 c；内部修正继续执行，只有真实长期承诺冲突、必要外部条件最终阻塞，或本批详细任务结束才交回。

## PN-042.a：准确基线、渠道差异与实际支持范围

**输入：** PROJECT/current-state、[原 0.7 审查](../../reviews/code/2026/20260910-0001-release-070-compatibility-acceptance.md)、Catalog/subscription manifest 的实际发布来源、0.6.1 准确 Runtime、已有保留包、当前 Runtime r5／SDK r5／多平台 r1。代码重点为 `ManagedModClassification`、`ManifestReader`、包 verifier、兼容 broker/host、`ExperimentalGameBridge`、ABI harness 和 SDK deployment/session reader。

1. 从 Catalog 的已观察发布来源及已有私有保留档案解析 0.6.1 的实际 Abstractions、Core、GameBridge/Compatibility、安装器和产品输入，校验记录中的 hash/identity。目录、同名版本或当前构建不能冒充已发布字节；不为此触碰 live upload。区分准确当前已发布产品、冻结历史消费者、自控新 fixture、未获得的外部样本。现有 retained ABI 默认覆盖更早 0.5.2 基线和历史消费者，继续保留，但不能把它重命名为完整 0.6.1 升级证明。
2. 比较准确 0.6.1 与候选的公共 surface、assembly identity、manifest/marker/Native/receipt 选择、来源与官方启停、依赖和最低版本行为。每项差异注明其是既有边界、SDK 作者重编变化还是 Runtime 新变化；按下表 C01–C10 建立场景和已有证据映射，不要求重跑一切历史场景。
3. 核对 CSV 的精确删除集合与现有测试允许项，保留它们；不新增 CSV 调用/导出恢复实验。为其他仍支持接口复用准确旧 consumer/implementer 和真实产品原 DLL，测试解析、实例化及实际调用；缺样本时明确自控 fixture 身份，不把它称为公开产品。
4. 区分已发布 Mod 包/玩家状态与未发布作者工具：包/ABI/receipt/Native 与实际部署恢复要保全，旧 SDK 的任意 author schema、CLI/session 调用和所有历史工程不成为新增首发门。现成 reader 若不影响迁移可原样保留；裁剪未使用内部工具前核对实际消费者，不牵连已部署包或恢复数据。
5. 对真实新增差异做最小差分，区分发现、准入、绑定或行为；没有新退化就复用合格证据直接进入 c。需要真实游戏的判断留 c，不能将损坏新格式退回 legacy 或关闭来源校验来放行。

**a 出口：** 准确基线/样本边界、CSV 已授权删除集合、其余新增退化与责任层及必要 b 修改；允许结论为无新增修正。c 仍使用准确最终产物。原本不支持或已授权退役的能力按事实排除，不能扩大成恢复所有旧玩法/内部工具的任务。

## PN-042.b：兼容修正与候选整合

**前置：** a 的差异和方法结论。**落点：** 只修改已证明受影响的 Runtime／SDK reader、兼容适配、API surface、来源状态或安装代码，不以本卡重构所有协调类。

1. 仅修 a 的具名新增退化。CSV 删除集合保持原状，既不恢复也不扩大。没有新增退化时在同一 Update 记本步无需修改，直接进入 c，不制造空实现/新 Review。
2. 对其他真实回归分别修正。Native V1/V2 与旧 receipt 保留各自合法路径；legacy 省略 kind 不被新 SDK author schema 4 影响。旧包不要补新凭证；损坏新格式、保留 marker 被拆除、非法来源仍应拒绝。Strict 的直接/传递 native 和宿主驻留冲突仍为既有边界，不能为兼容删掉真正必要检查。
3. SDK 内部工程转换不投影成新 Runtime marker，不抬高旧 API target 最低版本。新 SDK 的实际 session/deployment 回路正常；已部署状态的必要 journal 恢复沿 PROJECT 保留，不等于承诺旧 SDK 客户端继续调用。普通 Build 不自动安装、启用或启动游戏。
4. 对官方 Local/Workshop 状态、重复来源、更新驻留、已有内容及外插件只修具名问题。现有内容包不能被强制转换为未来通用 Host 格式；External BepInEx 不属于受管 Owner。当前不支持的 `<game>/Mods` 发现不恢复，老 journal 也不变成来源授权。
5. 如果 Abstractions／共享 verifier／Runtime 字节变化，从真实源码生成新 Runtime 候选，并让 Windows 与多平台包导入同一准确载荷；原 r5/r1 留作基线。安装器 host 未变可复用对应源码/行为证据，仍须核对新包的完整性、来源和所需升级组合。SDK-only 项不触发无关 Runtime 重建。所有准确来源、构建和验收沿现行 release inventory 与 Update，不新增手填身份表。
6. 运行受改行为的 focused 检查及完整旧 surface/consumer 绑定比较，检查当前 API 不退化、冻结载荷原字节不变。必要的假游戏安装升级／撤回、reader 边界、来源差分可在此完成；不得把它们当成最终 Mono 行为。

**出口：** 所有具名修正 implemented，旧/新候选归属清楚。此时整体仍未 verified，也不授权上传；c 负责最终组合。

## PN-042.c：准确最终候选与现有入口验收

**前置：** b 后的准确 Runtime、PN-041.e 完整 SDK 候选及对应两种玩家投影。复用 PN-041.f 的外部作者工程和同一次最终冻结/Release，不另跑一套全量 build→Release。

| 编号 | 兼容承诺 | 必需输入与观察 |
| --- | --- | --- |
| C01 公开 ABI 与旧包 | 支持范围内的公开 0.6.1/现有消费者可绑定调用；精确排除已授权第一方 CSV 删除 | 准确 surface、原样旧包、仍支持 helper 的 consumer/implementer 实例化/调用。CSV 不恢复；新编译旧源码不替代原 DLL |
| C02 Strict 与依赖 | 旧 Strict／0.5.5、shared/private 与当前新包共存 | 固定旧最低版本/marker、真实当前 SDK 产物；跨 Mod shared type 调用与必要 identity/闭包拒绝；Control 存活 |
| C03 原生与 legacy | receipt、V1、V2、未声明 kind 的旧包继续按原通道运行 | 原包 provenance／旧凭证不重签；代表 native 调用、owner 管理与 legacy 的冷重启提示；未知新格式不降级 |
| C04 官方来源与启停 | 官方 Local 与已订阅 Workshop 的原有入口有效 | 当前实际官方启停、原生订阅/installed root、发现身份与实际 Entry；禁用/未订阅残留不加载。不得用目录模拟成功冒充真实 Workshop 来源 |
| C05 来源冲突与更新 | 同 ID 只选一份；磁盘更新不冒充驻留更新 | Local/Workshop 选择与作者覆盖/恢复玩家来源；旧包→新包安装、重启、新驻留 hash/行为；停用/撤回后状态与非存档资产恢复 |
| C06 现有内容 | 官方 JSON/PNG、混合 CodeMod＋Content、现有专用 Host/产品内容继续工作 | 固定原包与新 SDK 迁移/pack 对照内容字节/路径，真实显示/效果、坏包隔离及关闭/标题清理；不强制新 Host，也不冒充其能力成立 |
| C07 外插件共存 | External BepInEx 原位置/文件/自主加载不受安装或 SDK 迁移破坏 | 代表已有外插件与受管 Control 共存，保持只读诊断边界；升级/撤回前后文件 hash；不制造未知 Hook 热卸载承诺 |
| C08 首发作者入口与已有状态 | 新 SDK 完整开发回路正常，实际已有部署状态可恢复 | 当前 SDK session/install/status/update/withdraw/recover 与实际状态 reader；内部输入转换由 B08 验证，不建立旧 SDK 客户端/历史工程矩阵 |
| C09 安装投影 | 两种玩家包承载同一准确 Runtime，升级不改 Mod/配置/存档 | 0.6.1→最终候选安装、升级、撤回/恢复；Windows/多平台现行对应矩阵；新 DLL/manifest/provenance 对应。WSL/fake game 不宣称实机注入 |
| C10 生命周期与保护 | 旧/新作者路径按自身承诺退出/释放，玩家数据不受迁移影响 | 正常退出、owner 根清理、更新必须重启；NoNativeSave 的 archive/committed sidecar 对比和非存档测试资产恢复，准确产品及来源日志 |

1. 先将已有合格证据映射到以上矩阵。相同输入且相关实现未变的安装、UI/帧、长测可复用；注明原范围。Runtime 恢复 ABI、新作者 DLL/符号或受改来源路径不得由旧版本 PASS 替代。原标题一小时后首次读档不变成一小时玩法证据。
2. 尽量合并一次受控新旧包组合：PN-041.f 的新 Strict/Advanced/生成器/共享/符号，配合同一批固定旧包、旧 helper、旧 receipt/V1/legacy 及无关 Control。不同消费冲突/产品需单独场景时按真实原因切分，不为表格每行重复启动。持共享 Runtime 锁，使用官方来源和 Steam 启动，默认 NoNativeSave；只因构建迁移不制造原生保存事务。
3. C04 Workshop 正例必须有真实已订阅输入；没有样本时记缺口并先做独立项。不上传测试项伪造来源；结束恢复有意改变的配置、部署和来源选择。此卡无 CSV 导出测试。
4. 以实际变化选择 C09 场景，复用 PN-031.multi 现有旧版本/平台边界证据；新 Runtime 必须准确导入两投影后再放行。Steam Deck/Proton/CrossOver 未实际启动只限制该平台的注入结论，不因 Windows/WSL PASS 宣布新平台游戏已验。设备/平台支持范围与既有接入兼容分开。
5. 局部失败先修复并只重验具名行为。所有源码和输入冻结后，与 PN-041.f 共用一次 `tools/scripts/test.ps1 -Configuration Release` 完整运行；它已含 build，不先另跑全量 build。最终实际 ZIP/Runtime 若相关字节改变，补受影响作者/Mono/安装证据；不变则以来源/hash 复用。Stage/StartAt 诊断结果不拼成完整 PASS。
6. R-Compatibility.release 与 R-AuthorBuild.release 一并判定。更新原实施 Update、status、release-candidate、实际作者/发布材料；保留失败、基线与未测范围。详细任务及必要返修完成后统一交回，不上传、不提前修改 Catalog 的公开 0.6.1 事实。

## 首发停止条件与范围

以下新增退化阻塞 0.7.0：已支持旧包必须重编/重签/迁移；有效官方来源/启停失效；仍承诺 API 的绑定/行为退化；新 SDK 回路失效或内部转换破坏现用工程；升级/撤回破坏 Mod、配置、存档或必要恢复状态；真实验收被源码/假游戏替代。精确第一方 CSV 删除、未发布旧 SDK 的任意客户端/格式不在阻塞集合，不为它们增加修复任务。

未来 SaveData／通用内容 Host、任意 NuGet 家族、未承诺 IDE/实体手柄和外插件任意 Hook 的热卸载不属于本卡首发新增能力。对已知基线限制不虚构零风险承诺，对具名新回归也不以“Experimental”直接豁免。若无新增回归且 PN-041/042 所有必要门接受，可以按已验证范围选择 0.7.0；后续能力沿原路线继续。
