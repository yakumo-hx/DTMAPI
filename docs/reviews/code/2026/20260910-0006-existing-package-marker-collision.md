# PN-042：现有包 marker 文件名冲突

- Lifecycle: recorded
- Owner: [实施 Update 0012](../../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)。本记录只拥有新增根因和接受边界。
- Inputs: 准确已发布 Windows 0.6.1、Runtime r5、原位现有 Local/Workshop 包；来源和逐文件记录在 `artifacts/pn041/compatibility-evidence/`。

## 观察与根因

Steam 实测 `20260910-193305` 中 16 个现有 Mod 目录被 `package-marker-invalid: Unsupported package marker schemaVersion` 拒绝。游戏 smoke 的总体 PASS 只覆盖其具名运行门，不能消除这些发现失败。

用两个准确发行 Core 的真实 `ManifestReader` / `ManagedModClassifier` 对同一原样 `DTMAPI_Oil` 路径做非游戏差分：公开 0.6.1 接受为省略 kind 的历史原生兼容 CodeMod；r5 在 marker 检查拒绝。证据 `marker-published061-oil.json`、`marker-r5-oil.json`；原 DLL 未重编、未重签。最初“内容包”描述过宽：实际受影响集合含旧 CodeMod 和 ContentPack。

文件名 `Content/DTMAPI/dtmapi-package.json` 早于作者 SDK。现有安装脚本、Workshop builder、资产生成器及第三方包使用无 schema 的说明元数据；新共享 reader 把同名文件全部当作 SDK binding。这是新增读取退化，不是旧 SDK 客户端支持需求。

## 修正与限制

只识别已观察旧元数据的有界字段集合：uniqueId，以及 generatedBy/updatedAt/owner/packageKind/contentRoot 或第三方 uniqueId/version。字符串类型、ID/版本和固定字段值须一致；不赋予安装所有权、来源授权、Advanced 凭证或 Native 权限。现有身份、来源、Strict 闭包及 legacy 冷重启边界继续适用。

schemaVersion、SDK target/binding/authority、未知字段、重复字段或错误类型不能回退旧元数据。Dependency/Native 新格式继续先走原 verifier；旧 Advanced receipt 不进入这个分支。现有包不改文件、不重编、不补签。

Core、GameBridge、Doctor 共享同一修正。先用现有跨端 target matrix 验证旧格式正例及损坏新格式拒绝，再生成独立 Runtime 与两种玩家投影，重验真实原包发现/加载及 SDK/Mono 组合。旧 r5/r1 保留，不能沿用为修正后的最终候选。最终完整 Release 尚待新输入冻结；本 Review 不授权上传。

## 关闭证据

上述根因及修正边界保留。提交 `1002ae05` 的 Runtime r6 与导入同一载荷的多平台 r2 已通过准确安装/升级/恢复；16个原样旧包读取、跨端损坏新格式拒绝和 Steam `20260910-200318` 新旧组合通过。最终 `full-release-r5` 完整 Release exit0，SDK D6 的 CLI/IDE/CI 输出与已验游戏输入对齐。实施与 R-Compatibility.release 接受归 [Update0012](../../../updates/2026/20260910-0012-sdk-msbuild-first-release.md)及其证据索引；原 r5 失败不重标，候选未上传。
