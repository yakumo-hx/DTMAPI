# 0.7.0 独立发布审查：兼容范围与作者交付

- Lifecycle: recorded
- Request: 用户询问 0.7.0 是否完全不影响现有 Mod、是否达到发布标准。
- Inputs: HEAD `652d39b6`；准确 Runtime r3 来源 `9fb6f018`，SDK r2，API recipe `a5d0e8c`。准确路径与摘要沿 [候选身份及原验收](../../../debug/evidence/GAME-SMOKE/20260909-platform-release-070/README.md)，不以同号旧 M2 包替代。
- Implementation owner: [PN-031.a](../../../updates/2026/20260909-0019-platform-release-preparation.md)。本 Review 记录审查时的事实与后续验收要求；修正结果归该 Update。

## 决定

**不能承诺“完全不影响所有现有 Mod”。Windows Runtime 的既定技术门通过；完整 Runtime/API/SDK 0.7.0 交付在下面的作者文档修正完成前暂不放行。** 这是有界的交付收尾，不重开 M3 架构或要求等待 M4、实体手柄和其他平台。

允许发布的口径应为：Windows Developer Preview；保留已验旧 helper/已知旧二进制的兼容性，新平台服务与控制器仍 Experimental，列明已授权的历史删除例外和未测范围。不能写“所有旧 Mod 零影响”“完整 SMAPI 等价”“手柄正式支持”或“全平台已验收”。

## 兼容判断

1. **存在真实的公共 ABI 删除例外。** 对已发布 0.6.1 的来源 `db5e518a6d7f` 与准确 r3 比较，Abstractions 的删除为 `ITeleportDebugApi.ExportDestinationsCsv(IManifest)` 和 `TeleportCsvExportResult`，包括该 DTO 的构造、属性及访问器。ABI harness 为这一族明确允许 20 条 surface 差异；PASS 不等于零删除。直接调用这些符号的旧二进制不具备兼容保证，可能发生类型/方法绑定失败；本轮没有伪称实际运行了这种外部消费者。
2. 该例外由 [20260831-0001](../../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)明确授权，引用了旧 Y 控制台无剩余用户的既有决定。它早于本轮平台路线，不是新批准提前执行 0.8 清退；无需擅自恢复或扩大删除。其余家族的 0.8 公告、逐族保留期与替代验证要求不变。
3. 独立 ABI 复验通过：11 份保留历史产品 DLL 的 463 个 Abstractions MemberRef，以及 4 份外部样本的 23 个相关 MemberRef 均解析；保留 AutoFishing setter 绑定通过，未发现该门之外的新非授权删除。**这些是冻结历史样本，不是 11 个产品的最新发布版本全部在 Unity 内重测。** 脚本基线 Abstractions 为 0.5.2，不能把它改称对发布 0.6.1 的完整行为比较；0.6.1→r3 的删除判断另由源码差异支持。
4. 原 r3 实机证据包含未重编译 helper consumer/implementer、组合 Mod、shared Type/Assembly、Advanced、独立 Control 与 owner 关闭。它支持这些实际路径，不证明任意第三方 Hook、反射私有实现、存档操作或每个历史玩法行为都不变。
5. 新依赖格式会拒绝入口重名、程序集 identity/字节冲突及来源不可证明的驻留，代码更新需冷重启。这是已说明的保守加载边界；本轮未将未复现的 mixed-loader 猜测记成缺陷，也未把 ABI 解析通过升级为混装永不受影响。

## 发布前修正项

### A1 / P2：准确 SDK ZIP 的版本、默认 target 与作者阅读路径不一致

在准确 SDK r2 ZIP 内实际读到：

- README 前部已称 SDK/default API 0.7.0，但 API target selection 段仍写省略 target 使用 0.6.4；真实 target-catalog 默认是 0.7.0。
- MIGRATION 仍称当前 SDK 0.6.5 / API 0.6.4；PACKAGE-DEPENDENCIES 称公开 0.7.0 planned；PROJECTS-AND-RESTORE 把 CLI 当前版本写成 0.6.5。保留历史组件 0.6.4 的说明本身不应被全局替换。
- 作者判断稳定性/弃用必须访问的 `../docs/api/public-api-matrix.md` 等链接指向 ZIP 外的仓库路径，准确 ZIP 未携带这些目标。仓库本身链接通过不代表解压 SDK 可读。

影响：作者无法只按所交付材料可靠判断当前 target、可用能力和迁移边界。包的 hash/编译门不能证明这项作者体验已成立。修正当前使用指引与必需契约入口；可以用构建期投影保持单一文档权威，不复制 Wiki/历史研究，不另建 API 状态注册表。内部/历史 target 与冻结组件版本须保留准确归属。

### A2 / P2：首发兼容声明需要明确 CSV 历史删除例外

[候选说明](../../../planning/platform-next/release-candidate.md)的弃用行写“无物理删除”，迁移材料没有交代上述 CSV 例外。若只指 PN-033.a 没有新增删除，应明确写出该范围，不能让玩家/作者把它理解成相对 0.6.1 完全无删除。

发布材料应同时说明：这项删除的精确符号、已有授权与旧 Y 控制台范围；已知保留样本通过不代表全量 Mod 的玩法验证；0.8 起的后续逐族清退条件不变。不得据此再删除任一成员。

## 本轮实际验证及证据复用

本轮未启动游戏、未安装/卸载、未修改 Mod/玩家数据，也未上传。独立检查记录在 `artifacts/review-070-20260910/`：

| 检查 | 结果与边界 |
| --- | --- |
| candidate-byte-verification.json | 准确 Runtime manifest、5 个必需 DLL、可选 Compatibility DLL、SDK r2 ZIP、完整 Release r9 日志及一小时原始日志 SHA 全部匹配原记录；DLL 长度匹配 |
| test-retained-release-abi.ps1，对准确 r3 Abstractions，NoBuild | PASS；retained-abi.json / retained-abi.log。复用已构建 harness，未用 PATH SDK；该工具明确 UnityMonoRuntimeValidation=False |
| check-author-sdk-release.ps1，对准确 SDK r2 ZIP | PASS；sdk-release-check.log。1020 文件/6 个 available target 的完整性和确定性检查，不证明说明文字或仓库外链接正确 |
| test-runtime-build-source.ps1 | PASS；runtime-source-test.log。真实干净 fixture 构建、MSBuild 闭包、修改/新增/删除输入、旧输出与构建中漂移的拒绝均通过 |
| 已有完整 Release r9 | 原记录 FromStart、exit 0、49m30s，源与关键包字节未变，原日志 hash 独立核对；本轮没有重跑整套后宣称新 PASS |
| 已有准确 r3 Windows 实机及恢复 | 真实已发布 0.6.1 升级/撤回/恢复、组合与公开命令、至少 60m46s 标题后首次读档并退出通过；30 存档、5 sidecar、16 配置保护及恢复沿原证据复用 |

一小时标题后读档不是一小时实际玩法，更不是广义 ISSUE-010 的关闭依据。实体控制器/Steam Input、Linux/Deck/Proton/CrossOver 新候选注入仍未验证；这些限制已披露，不要求为当前 Windows 预览补成虚假的全平台 PASS。

## 连续修正与交回要求

沿原 PN-031.a Update，在同一实施任务、当前工作区完成 A1、A2，然后一次交回，不扩展 M4：

1. 修正当前作者材料，确保首次 new/build/pack、默认/旧 target、开放 Native V1、共享依赖、生命周期与 API 状态/迁移的必需阅读路径能从解压 SDK 使用。维护者历史资料若只适用于源码检出，应明确区分；不要求把整个仓库装进 SDK。
2. 保留 SDK r2 和所有 Runtime 字节。通过 tracked builder 生成独立目录中的新 SDK 候选，禁止覆盖原 ZIP、手改 ZIP 内清单或重标原 SHA。原 API source recipe、target payload、冻结 0.5.5 和 native generator identity 不变。
3. 对准确新 ZIP 跑包检查；在仓库外的空目录按最终说明运行默认 0.7.0 和显式 0.5.5 的最小 new/build/pack，并检查必需文档链接。比较新旧 SDK 中所有非文档执行/契约文件，证明 CLI、依赖、模板、API/ref 字节未变。文档/inventory 的预期差异单列；若执行字节变化，说明原因并补对应最小作者回归。
4. 更新 candidate identity、候选说明、原 Update 与队列，不篡改旧 r2 同字节/完整 Release 的历史结果。Runtime 若未变，复用其 installer、Mono、恢复和一小时证据；文档修正本身不重开全量 Release 或一小时游戏门。治理检查通过后交回准确产物、差异与修正结果供独立收口。

完成以上修正且无新行为差异，可放行 **Windows 0.7.0 Developer Preview 的技术交付**。发布动作仍需真实发布指令，Catalog 的已发布 0.6.1 身份不能提前改变。

## 后续反例与执行替代（2026-09-10）

用户随后提交准确 SDK r2 的独立普通委托、Unity 泛型和工程选择反例，并暂停原任务；[新审计采纳](20260910-0002-sdk-author-path-plan.md)及 [execution-sdk](../../../planning/platform-next/execution-sdk.md)取代本页“仅文档收尾”的后续步骤。以上审查保留其固定输入的事实，不据此给新 SDK/Core 代码免验；新 Runtime/SDK 必须按变化补证，整体 0.7.0 仍暂不放行。

Resolution 2026-09-10：原候选结果保留；SDK 必需材料、当前版本/default target、CSV 历史例外及后续 SDK 实现增量已在[原 PN-031.a 的新候选验收](../../../updates/2026/20260909-0019-platform-release-preparation.md)完成。选用 Runtime r5 / SDK r4，Windows Developer Preview 技术门通过，未发布；不外推所有旧 Mod、实体设备或新平台。
