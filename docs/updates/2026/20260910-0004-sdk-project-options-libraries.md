# 20260910-0004: 作者工程选项与普通辅助库

## Metadata

- Update ID: `20260910-0004`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime, docs`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: PN-039，[execution-sdk](../../planning/platform-next/execution-sdk.md)与 AD-11；沿既有 BuildPlan / ProjectGraph 实施。

## Summary

csproj 拥有常量和 XML 文档输出；普通 SDK-style netstandard2.0 Library 接入现有 DAG，无 Mod manifest、UniqueID 或专用库描述要求。

## Changed Files

- `ProjectCompilerSettings`、BuildPlan/emit：csproj 常量、条件、自追加及 XML 同次输出；输出发布失败回滚 DLL/PDB/XML，关闭文档时清理陈旧 XML。
- `OrdinaryLibraryInput`、ProjectGraph：无 Mod 身份的普通 netstandard2.0 库、身份/版本、Compile/Link、资源、传递工程/锁定包、父工程引用元数据；保留 SDK library。
- TemplateCreator 的 IDE/migrate 投影及作者文档/schema；保留用户已有 SDK001 错误分类修正。

## Validation

- passed：project-graph 的真实 Release/Debug 条件分支、常量去重/非法值、XML 当前成员、迁移 DLL/PDB 同字节、编译失败与锁定 XML 发布失败保全、关闭 XML。
- passed：普通库 + 传递普通库 + SDK shared library，实际 CLR 方法/嵌入资源/程序集版本；无 Mod 文件且原 csproj 不变；不支持 target 指向真实子项目。普通中间库传递到锁定包的在线/离线 restore/pack 与继承许可通过。
- 标准 MSBuild 对照发现默认常量缺 RELEASE 与较低 NETSTANDARD*_OR_GREATER；已按 .NET SDK 8.0.421 实际 csc 命令修正，框架/配置常量在作者替换后追加，新增真实 #if 回归通过。
- passed：修正后 SDK 默认全套 `20260910-020317-360-9e9685fe05b2`，包含现有 CLI/IDE、旧格式与 Advanced fixture；准确 ZIP 仓库外 CLI/IDE 的 DLL/PDB/XML 及换目录包同字节也已通过，原普通库标准 MSBuild 成功。
- passed：最终 r5 Mono 普通库/资源/shared contract 实际组合值 91、委托 42、资源 resource-ok、标准 Release 分支。

集中实机补证：最终 Runtime r5 / SDK r3 执行载荷在 GAME-SMOKE/20260910-102550 正常加载/退出，结果与保留失败见[统一候选证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-unified-070/README.md)。最终 SDK r4 仅指南与对应 inventory 变化，实际解压命令再次通过；最终完整 Release 从 FromStart 通过（exit 0，24.56 分钟），执行中无源/文档输入漂移。下方早期结果保留其当时时点。

## Evidence

新探针 `E:/Python_project/DTMAPI-sdk-probes/20260910-pn039/Standard` 的原普通工程标准构建成功，`standard-build.log` 保留完整有效编译参数。focused `20260910-020203-16888-7c96ce861219` 已通过并清理；最终产品证据归准确统一候选。

## Rollback Notes

仅回退本卡改动；保留 PN-038 和用户已有修改，无游戏存档写入。

## Follow-Up

本卡完成；完整 MSBuild、自定义 targets/生成器等范围留 PN-041，不阻塞 M4 有界实验。
