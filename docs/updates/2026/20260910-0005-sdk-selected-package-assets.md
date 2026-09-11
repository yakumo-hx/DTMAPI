# 20260910-0005: 按所选 NuGet 资产判定支持范围

## Metadata

- Update ID: `20260910-0005`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime, docs`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: PN-040.a，[execution-sdk](../../planning/platform-next/execution-sdk.md)与 AD-12；复用 NuGet.Frameworks/Packaging 的选择模型。

## Summary

在现行 netstandard2.0 执行边界内先选择资产，修正无关 TFM/占位文件引起的整包误拒。当前适用但未支持的原生、build/generator、RID 或分离 ref/lib 仍明确拒绝。

## Changed Files

- `PackageAssetSelection` 使用 NuGet.Frameworks/Packaging 分组及 nearest framework；LockedPackageRestore 在纯托管判定前记录选择、排除和未支持项，验证所选依赖组与 lock。
- 多 TFM、占位、同/异 ref-lib、当前 build/generator、ref-only 的真实 ZIP 布局用例，以及作者范围说明/schema。

## Validation

- passed：不兼容 net8 lib/ref/build/buildTransitive/RID、nearest 占位、ref/lib 同字节正例 restore/offline/build/pack；包仅含选中执行库。当前 build/generator、ref-only、不同 ref/lib 通过公开命令按 SDK701 具体类别拒绝并保留资产报告。
- passed：原方法元数据、hash/许可/闭包/宿主约束语料；SDK 默认全套 `20260910-020317-360-9e9685fe05b2` 通过。
- passed：仓库外多 TFM 真实 nupkg 经准确 ZIP restore/offline/build/pack，最终 r5 Mono 只调用选中 netstandard2.0 委托库并得到 42。

集中实机补证：最终 Runtime r5 / SDK r3 执行载荷在 GAME-SMOKE/20260910-102550 正常加载/退出，结果与保留失败见[统一候选证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-unified-070/README.md)。最终 SDK r4 仅指南与对应 inventory 变化，实际解压命令再次通过；最终完整 Release 从 FromStart 通过（exit 0，24.56 分钟），执行中无源/文档输入漂移。下方早期结果保留其当时时点。

## Evidence

既有外部审计只读保留；本卡新语料使用新测试目录。

## Rollback Notes

仅回退本卡选择逻辑和测试；不放宽 PE、宿主身份、锁或 TFM，不删除旧包/研究证据。

## Follow-Up

本卡及 PN-031.a 统一候选验收完成；两条 M4 实验输入已准备。低 TFM 与不同字节 ref/lib 属于 PN-040.b，R05 整体未完成。
