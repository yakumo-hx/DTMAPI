# 20260910-0003: 正常委托与原生泛型交付

## Metadata

- Update ID: `20260910-0003`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime, docs`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: PN-038；[已接受设计](../../reviews/code/2026/20260910-0002-sdk-author-path-plan.md)与 [execution-sdk](../../planning/platform-next/execution-sdk.md)，沿 P08 实施，不另开全平台 Review。

## Summary

修正锁定包的合法 Runtime 委托误拒；实现 SDK、Doctor、Runtime 一致的原生泛型 V2，并保留 V1/receipt/legacy。源码和 focused 验证先行，最终统一候选已补齐 Mono、完整 Release 和 SDK ZIP 证明。

## Changed Files

- LockedPackageRestore 与 Tooling.Metadata/ManagedMethodMetadata：CodeTypeMask 分类，合法普通/泛型委托形状，包/版本/资产/成员/拒绝类别诊断。
- NativeGenericSignature / NativeGenericMetadata：有界结构化定义、约束和类型/方法实参；磁盘元数据提取与宿主开放定义反射比较。Core 既有 PE reader 读取实际 TypeDef/NestedClass，包中作者类型实参按实际 DLL 核对，不加载作者程序集。
- NativePackageContract、marker、SDK build/pack、Doctor 和 Runtime 分类：显式 V2 分支，V1 保留；当前新 Advanced 选择 V2，旧 target 保持 V1。build 前置可表达性与源/宿主变化检查；schema 同步。
- 当前已有 TemplateCreator/测试分类和执行流程改动归 20260910-0001，不作为本 Update 新修复。

## Validation

- PASS：R01 原代码在新委托 fixture 的 restore 复现 SDK701；修正后 `platform-project-graph`（含 public CLI 普通/泛型/ref-out 委托、离线恢复/build/pack）通过。P/Invoke、Unmanaged、InternalCall、Native、伪 Runtime、混合标志、非法委托形状和非 IL-only 的真实变异包均核对 restore / exit 1 / SDK701 / 资产与成员类别。
- PASS：仓库外准确 Newtonsoft.Json 13.0.3、原 NuGet lock 与许可，经当前源码 CLI restore→offline restore→build→pack；实际 DeserializeObject 调用已编译，最终 Mono 确认私带版本因驻留冲突在 Entry 前拒绝；Advanced 宿主 JSON 实际操作得到 42，准确示例已随 SDK 交付。
- PASS：`platform-native-contract`，早期 focused session `20260910-013454-19844-3d7691466ab4`。旧 V1/receipt 分支原语料和新 V2 作者类型实参、泛型宿主/嵌套/开放方法、数组/ref/out、确定性 pack、约束/成员/参数位置/实参身份/实际类型不存在、marker/未知/重复字段拒绝；预检计数器与程序集列表证明未进入作者加载/初始化。下文记录 owner 修复后的 focused 与最终完整运行。
- PASS：真实 build 25163613 冻结 Unity CoreModule 输入，仓库外 AddComponent<作者 MonoBehaviour>、GetComponent<作者组件>、GetComponent<Transform> 与非泛型对照已通过公开 CLI pack。保留首次 mscorlib 2.0 表示失配的失败原因；加入准确 mscorlib 2.0 identity 映射后通过，不放开任意 System 前缀。
- PASS：vararg 在 build 的成员/PDB 源位置被 SDK202 拒绝，并保留原 DLL。一次新增测试误要求 `src/` 前缀（该工程 PDB 用 SourceDirectory 相对路径）已修正断言；不计为功能缺陷或负例 PASS。
- PASS：最终 r5 Mono 委托、泛型/作者 Component 与非泛型同对象、Control、旧 reader/原 retained ABI 共存；作者 GameObject 清理及正常退出通过。

### R-Generic 有界复盘

源码/固定语料支持继续 PN-039：V2 为显式 reader，不原地重解释 V1；约束及位置进入规范键，宿主身份来源和 inventory 绑定保留；闭合参数只读结构及包 PE，未调用 MakeGenericType/MakeGenericMethod 或提前加载作者代码。Core 未新增 Cecil/强制游戏 DLL，Abstractions 冻结载荷未动。

V2 不支持 function pointer/calli、vararg、custom modifier 和非 vector 的 rank-one/特殊有界数组；与受支持泛型分别诊断。该检查不是恶意 IL 沙箱。实例 use 与宿主定义的实际 Mono/JIT、mscorlib 2.0→Mono 表示及资源清理已在最终 r5 补证，准确包交付检查通过；接受范围限实际 Windows build。

集中实机补证：最终 Runtime r5 / SDK r3 执行载荷在 GAME-SMOKE/20260910-102550 正常加载/退出，结果与保留失败见[统一候选证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-unified-070/README.md)。最终 SDK r4 仅指南与对应 inventory 变化，实际解压命令再次通过；最终完整 Release 从 FromStart 通过（exit 0，24.56 分钟），执行中无源/文档输入漂移。下方早期结果保留其当时时点。

R-Generic 补正：初次有界复盘漏掉最终 AdvancedHarmonySupervisor 仍只识别 V1 的 owner 分支，r4 实机因此在 Entry 前失败。该遗漏已按精确 v1/v2 provenance 修复，并增加真实 classifier→supervision→loader→Entry→close 回归（20260910-021913-3448-63727784613d）；r5 实机通过。首次失败不抹除，也不拿 classifier 单测代替最终加载证明。

## Evidence

- 外部审计只读输入：`E:/Python_project/SMAPIlearning/SMAPI_technical_study/reviews/2026-09-10-dtmapi-0.7.0/02-sdk-author-experience.md` 及 `_evidence/sdk-probes/nuget-Delegates-restore.json`。其 restore 返回 exit 1 / SDK701，36 个 Newtonsoft 委托方法误判的原结果保留；修复后准确 13.0.3 restore/offline/build/pack 已重验通过。
- 新探针及测试使用独立目录，不覆盖该研究包或旧候选。
- 仓库外早期源码探针：`E:/Python_project/DTMAPI-sdk-probes/20260910-pn038/` 的 json-restore/json-offline/json-build/json-pack 与 unity-new/unity-build/unity-pack；最终 SDK ZIP 已在 `20260910-unified` 和 `20260910-unified-final` 独立验证，旧源码探针日志不冒充最终 ZIP。

## Rollback Notes

仅回退本 Update 对应源码/测试和记录；保留冻结 API 载荷、旧候选与其他未提交工作。不触及玩家存档。

## Follow-Up

本卡有界交付已完成。未支持特殊签名、Strict JSON 驻留冲突及新游戏版本仍按 SDK 说明处理；M4 实验另行执行，不扩展本卡。

