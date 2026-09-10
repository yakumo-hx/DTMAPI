# 20260910-0012: SDK 首次公开交付的标准 MSBuild 与现有 Mod 兼容

## Metadata

- Update ID: `20260910-0012`
- Date: `2026-09-10`
- Lifecycle Status: `in-progress`
- Validation Level: `not-run`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户指定在当前工作区连续执行 PN-041.a–f / PN-042.a–c；最新决定归 [0011](20260910-0011-sdk-first-release-plan-correction.md)，详细规格归 [SDK](../../planning/platform-next/execution-sdk-msbuild.md)及[兼容](../../planning/platform-next/execution-compatibility.md)。

## Summary

0.7.0 是 SDK 首次公开交付。本批贯通标准 MSBuild 单后端、普通工程/生成器/资源/恢复、实际 IDE/CI、准确封包/Doctor、完整离线 SDK 和真实 Mono 作者回路，并用原样旧 Mod 与准确 0.6.1 基线检查兼容。临时 CSV 接口保持精确删除；不开发通用旧 SDK 迁移器，不建立任意历史客户端支持矩阵。

## Changed Files

- 待实施：SDK build 薄集成、CLI、analyzer、实际内部工程/模板/测试、打包与发行输入。

## Validation

- PN-041.a：已定位原 CodeModBuilder / BuildPlan 直接 Compilation 及模板 Build/Restore 覆盖；开始引用准备/标准 Csc 薄集成。
- PN-042.a：准确 0.6.1 源已由 PN-031.multi 留存在独立证据目录；待公共 surface/原样旧包/来源映射。
- 其余 focused / 实际 IDE / 外部 SDK / Mono / 最终一次完整 Release：not-run。

## Evidence

- 控制组：SDK r5、Runtime r5、多平台 r1 均保留原字节；安装器证据由 [0008](20260910-0008-multiplatform-070-candidate.md)拥有。
- 当前无 Runtime 修改，不重建 Runtime 或两个玩家投影；只有真实受改字节/边界才使相应旧证据失效。

## Rollback Notes

保留无关工作树改动、历史 SDK ZIP、冻结引用及旧包。仅转换当前真实使用的内部工程。未上传或修改实际公开 0.6.1 的 Catalog/订阅事实。

## Follow-Up

按 PN-041.a→f 连续实施，PN-042.b 仅在发现真实新增退化时执行；最终合并 B/C 的准确候选验收后统一交回。
