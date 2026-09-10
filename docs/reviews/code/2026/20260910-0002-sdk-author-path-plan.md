# SDK 正常作者路径：审计吸收与下一批设计

- Lifecycle: recorded
- Request: 用户暂停旧实施任务，要求结合 SMAPI/SDK 新审计及流程优化完善计划，再新建 Astra/high 实施任务。
- Inputs: 源 HEAD `652d39b6` 加当前未提交工作；准确 SDK r2；用户提供的 `E:/Python_project/SMAPIlearning/SMAPI_technical_study/reviews/2026-09-10-dtmapi-0.7.0/` 六份报告及其只读证据。
- Plan owner: [规划 Update](../../../updates/2026/20260910-0002-sdk-audit-execution-plan.md)；执行规格见 [SDK 执行包](../../../planning/platform-next/execution-sdk.md)。

## 决定与依据

接受报告对当前结构的更新：owner 服务、反射、依赖库、多工程和任意 ID 的 Advanced 已有实质实现；Compatibility 是按需执行的独立组件。保留当前程序集骨架，不把源码行数当性能缺陷，不再把已解决的 JSON/配置备份问题列作新前置。

本轮直接核对了 LockedPackageRestore、NativeMemberMetadata、NativeSignature、NativePackageContract、BuildPlan、ProjectBuildInputs、ProjectGraph，以及本地 SMAPI 的 smapi.targets / mod-package.md。SMAPI 在普通 MSBuild 上补引用、部署与调试设置；DTMAPI 采用规范化 Roslyn 计划。维持短期默认执行器，不把其当前白名单当作永远不能扩展的公共承诺。

| 审计项 | 本轮判断 | 执行归属 |
| --- | --- | --- |
| R01 普通委托误判 | 当前源仍使用 Native 位检测。枚举检查确认 Runtime=3、Native=1，旧式得到 true，CodeTypeMask 等值比较得到 false。外部普通委托/Newtonsoft 的 CLI 反例有效 | PN-038，首发阻断 |
| R02 Unity 泛型打包 | 源同时拒绝泛型 MethodRef、泛型声明类型及反射泛型签名；不是只改一个 throw。MethodSpec/TypeSpec、约束及尚未加载的作者类型也必须纳入设计 | PN-038，首发阻断；按 P08 新原生格式处理 |
| R03 工程输入限制 | 常量/XML 固定和仅 SDK library 的限制确认；它们不都来自 Unity。普通工程子集可以映射到同一 BuildPlan | PN-039，同批完成低风险选项与普通库；完整 MSBuild 后端另做有界原型 |
| R04 SDK 材料 | 接续原审查 A1/A2，包括错误 Advanced 命令。必须验新 ZIP，不再要求新 SDK 与 r2 的执行字节全部不变 | 原 PN-031.a；吸收前序实现后一次交付 |
| R05 NuGet 资产选择 | 当前整包目录拒绝和精确 TFM 限制确认。先解决 netstandard2.0 中可明确判定的无关资产；更低 TFM/facade、真实 ref/runtime 分离需要自己的闭包与 Mono 证明 | PN-040.a 同批；PN-040.b 中期，不阻塞 M4 |

外部报告的 restore/build/pack 及 IDE 同字节结果是外部审计证据，本轮没有重新执行这些工程或启动游戏。不能把未完整准备的 System.Reactive feed 当已复现 SDK 缺陷；也不能把固定 r2 的 PASS 当已包含当前工作树修正。

## 对上一轮决定的修正

[上一轮发布审查](20260910-0001-release-070-compatibility-acceptance.md)在其准确输入范围有效，但“只剩文档，Runtime 不变”的后续实施条件已不成立：新反例要求 SDK 执行代码及 Core 原生验证改变。原 r3/SDK r2 和旧证据保留，新候选按变更边界重建与实测。旧任务保持暂停，不再向它发送续做指令。

0.7.0 暂不放行完整交付。首发应至少解决 R01/R02/R04；已细化的 R03、R05.a 在同批连续完成后纳入最终候选，避免先做一套很快失效的文档封包。0.7.1 不重复已吸收的控制器实现，转向作者工程剩余覆盖与设备反馈。SaveData、ModContent/GameContent、Host/Pack 保持下一条主线，不等待完整 MSBuild/NuGet 生态。

## 关键架构选择

- P08：原生签名采用显式 V2，保留 V1、旧 receipt 和 legacy reader；只在当前目标的新作者路径生成 V2，不原地改变冻结载荷。由结构化定义/类型实参/约束共同描述泛型，不用字符串放行，也不在验证阶段加载作者 DLL 来 MakeGenericType/MakeGenericMethod。
- AD-11：普通 C# 选项由 csproj 的受支持属性拥有，进入同一个 BuildPlan；不要求作者再在 JSON 重填。普通辅助库不需要伪装成 Mod 或获得 UniqueID。未覆盖的工程输入必须指出真实项目及可用路径。
- AD-12：NuGet 的目标/资产选择和执行资产政策分开。保留锁定哈希、许可、宿主身份、实际 PE 闭包和离线构建；本批不放开全部 TFM、unsafe、生成器或自定义 targets。
- R-Generic 由新 Astra 任务基于确切语料复盘后连续推进；未来 R-AuthorBuild/R-Assets 与 R4a/R4b 只审对应实验，不重做全平台架构。

这些是未发布候选的技术选择，没有新增自动上传、遥测、兼容删除或任意代码热重载承诺。0.8 清退窗口和已经授权的 CSV 历史删除例外保持原解释。

## 流程校准

采用已验证的 [执行流程修正](../../../updates/2026/20260910-0001-execution-workflow-repairs.md)：先检查前提，失败用 focused 或具名阶段排错，最终代码冻结后完整 Release 一次；参数/内部错误不能算功能拒绝。标题测试可用 `-WaitForManualExit -SaveSlot 0`，实际泛型行为选择适合其声明的游戏上下文。

SDK 的 TemplateCreator 错误分类已经有未提交修正，必须保留并计入新 SDK 差异。模型切换/压缩不触发重新建包或重测。原一小时标题/读档证据按未变的帧、生命周期和保留根边界复用，不把新 Core 的短测写成一小时 PASS；若这些边界或 ISSUE-010 症状改变，再加相应长测。

实现记录按卡复用：PN-038、PN-039、PN-040.a 各在开工时维护自己的一个 Update；发行整合继续原 PN-031.a Update。candidate 页只放选用候选、未满足门和链接，status 只放队列，不复制命令/哈希/完成叙述。

Resolution 2026-09-10：R01/R02/R03/R04/R05.a 已由对应实施 Update 与[统一候选验收](../../../updates/2026/20260909-0019-platform-release-preparation.md)收口，准确 Runtime r5 / SDK r4 的完整 Release、外部作者和 Mono 通过；首次 r4 V2 owner 失败保留。R05.b、设备/新平台与 M4 实际实验仍按原边界，不把本次技术接受写成发布。
