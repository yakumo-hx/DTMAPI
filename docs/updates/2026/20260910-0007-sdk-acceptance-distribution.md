# 20260910-0007: SDK 独立验收与分发边界

## Metadata

- Update ID: `20260910-0007`
- Date: `2026-09-10`
- Lifecycle Status: `verified`
- Validation Level: `source, docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求以 SDK 功能为主验收、确定 SDK 上传方式并顺带检查多平台安装器；[独立审查](../../reviews/code/2026/20260910-0003-sdk-acceptance-distribution.md)。

## Summary

SDK r4 的独立作者功能复核通过，修正指南中的包路径与旧 Advanced 限制叙述；SDK 以 GitHub Release 独立附件交付，玩家安装包保持 Runtime 范围。发现现有多平台包/构建器/host 仍限 0.6.1，为 0.7.0 另列有界补齐任务。

## Changed Files

- author-sdk/README.md、仓库 README：纠正原生元数据位置、当前自助 Advanced/依赖库及最小 Strict 前提；与 legacy receipt 分清。
- author-sdk-release 工作流：作者 SDK 与两个玩家 Runtime 渠道、准确附件、下载验收及版本分工。
- Review、候选与任务路由：保留已有 Mono/完整 Release；指出多平台 0.7.0 尚缺。

## Validation

- 准确 SDK r4 包、确定性 ZIP 与必需链接/锚点检查通过；SHA 与原验收记录一致。
- 仓库外重新解压，执行创建、普通库标准构建、常量/XML、CLI/IDE/换目录同字节、旧 0.5.5、Newtonsoft 在线/离线恢复、构建/打包/Doctor 通过。
- 新作者 Unity/自有 Component/GetComponents List 泛型与宿主 JSON 经准确 SDK 创建、构建、打包和适用包 Doctor 通过。故意加入普通库 Target 后得到 build / exit 1 / SDK202 及准确子项目原因，随后恢复原输入。
- 新探针首次按 README 错误树定位 native 描述失败，产品 build/pack/Doctor 已成功；从实际 marker 读取根目录路径后完成检查。该脚本失败不记为功能负例。
- 多平台只读前置验证明确拒绝 r5：仍要求准确已发布 0.6.1 载荷；现有 dist manifest 和 C# host 同样限制 0.6.1。没有绕过门或操作 live upload。
- 指南修正已提交为 `9d6f0e6486f0062255d54c88602d5dfcab1f2742`。tracked builder 生成 SDK r5 并通过准确 ZIP 检查；重新解压的 1021 个文件仅 README.md 与 author-sdk-release.json 不同，910 个 DLL 和其余执行/契约文件全部相同。新解压目录实际 new/pack 成功，默认 target 0.7.0。
- 原完整 Release 日志 SHA 独立复核一致，选用 Runtime r5 的五个主 DLL 符合其原 manifest。没有本轮新游戏运行；原 Mono、升级及完整 Release 按执行/契约输入不变复用，不重标为新运行。
- 文档治理、相关本地链接和 git diff --check 通过，月度状态同步；新增证据引用使保留索引过期，按原生成器更新后检查。结束无游戏进程、Runtime 锁已释放。

## Evidence

本轮原始结果在 `artifacts/review-sdk-070-20260910/`；独立作者根为 `E:/Python_project/DTMAPI-sdk-probes/20260910-independent-sdk-r4/`，含 author-result.json、native-result.json 和具体 CLI JSON。候选身份与既有 Mono 由 [PN-031.a](20260909-0019-platform-release-preparation.md)及[原统一证据](../../debug/evidence/GAME-SMOKE/20260910-sdk-unified-070/README.md)拥有。

选用 SDK：`artifacts/pn031/sdk-070-candidate-r5/DTMAPI-Author-SDK-0.7.0-win-x64.zip`，229638813 bytes；SHA-256 `78086042d6dc7a7aae4aa9d3bbad50a135734981033e753e1b25e25833c35eb7`。同目录 `.zip.sha256` 为待发布配对附件。`sdk-r5-proof.json` 拥有完整对照，新作者目录为 `E:/Python_project/DTMAPI-sdk-probes/20260910-independent-sdk-r5/`。没有更换 Runtime，也没有覆盖原 SDK r4。

## Rollback Notes

本轮仅修说明和规划；SDK 比对候选不覆盖旧包。不修改 Runtime、官方 MODS、玩家存档或发布授权；读取真实宿主元数据时取得并释放了 Runtime 锁。

## Follow-Up

SDK 独立附件按工作流实际发布后再验证公开下载；多平台 0.7.0 候选沿新有界任务补齐，不能拿旧 0.6.1 包或 Windows 验收充数。
