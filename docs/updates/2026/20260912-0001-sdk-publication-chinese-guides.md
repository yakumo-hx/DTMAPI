# 20260912-0001: SDK 首次公开交付与随包中文指南

## Metadata

- Update ID: `20260912-0001`
- Date: `2026-09-12`
- Lifecycle Status: `in-progress`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求把 SDK 交付到仓库并更新未发布说明，随后明确由主任务核对准确包和路径，原 Astra low 任务只负责中文。技术基准为 [D7 验收](../../debug/evidence/GAME-SMOKE/20260910-sdk-msbuild-070/repair-d7/independent-acceptance.md)，发布沿 [SDK 流程](../../workflows/author-sdk-release.md)。

## Summary

核对首次公开 SDK 的准确输入，补齐随包中文使用说明，以 GitHub Release 独立附件交付 Windows x64 作者工具。SDK 源码已在 0.7.0 主线；玩家 Runtime 与 SDK 分开分发。地图研究及其他工作区改动不纳入本次交付。

D7 的完整 ZIP 与原验收完全一致。进一步核对发现它早于 [BOM 兼容修复](20260911-0009-runtime-070-package-marker-bom.md)，随包 Doctor 尚未包含该已提交修复；最终 SDK 需纳入准确补丁，不能把它描述为仅换中文文档的 D7。

## Changed Files

- `author-sdk/`：随包中文使用说明；必要时从 API 状态唯一来源重新生成投影。
- 根 README、当前候选及状态入口：实际发布确认后更新 SDK 下载与状态。
- SDK 独立发行包、清单及 SHA-256：主任务核对与发布，不提交工具链二进制到 Git 源码历史。

## Validation

- PASS：实时认证查询 GitHub Releases 为空，未将本地候选称为已发布。
- PASS：D7 ZIP 长度、SHA-256 与侧车和原验收一致；6,177 个条目中清单所列 6,176 项长度/哈希逐一通过。
- PASS：隔离校验输入下执行现有 `check-author-sdk-release.ps1`，包含确定性 ZIP 字节重放、全部兼容载荷、链接、许可及禁止载荷检查。README 使用准确 D7 原文，避免把后来的仓库导航当作旧包输入。
- PASS：以公开主线 `73aaffce` 在独立检出构建，306 项原 D7 输入逐项比较；按原哈希恢复检出行尾后，编译逻辑差异只有已提交的 `AuthorPackageMarker.cs` BOM 补丁，地图变更未进入输入。tracked builder 和技术 ZIP 检查通过。
- PASS：最终 Doctor 直接复用现有 `PlatformPackageTargetMatrix` 的 83 项断言，全部通过。D7 对照的 14 个合法旧/schema 1/2 BOM 例及空文件例失败，保留原结果；这与新 schema 4 的依赖 marker 路径是不同检查。
- PASS：除 Doctor 外，两份构建辅助 DLL 的类型/字段/方法签名及 IL 相同，ProductVersion 中的构建提交标识从 `cc7044a7` 更新到 `73aaffce`；其他非文档载荷逐字节保持 D7。
- PASS：9 份随包指南完成中文校对，25 个包内链接/锚点通过；最终 ZIP 执行完整 release checker 与确定性重放通过，保持 6,177 个文件。API 状态的自动投影及许可正文未改。
- PASS：在仓库外的新中文/空格路径，以最终 SDK 和空 NuGet 缓存完成 new、离线 restore、Release pack 与只读 Doctor，输出 ZIP 与报告 SHA 一致。首轮探针在清单尚未生成时并行启动，按预期被 SDK204 拒绝；待最终包完成后重做成功，未修改校验逻辑。
- 观察：新 schema 4 依赖 marker 的普通/BOM 正例通过，重复 BOM/非法 schema 拒绝；仅 BOM 的损坏 marker 在原 D7 和本次包均返回 SDK999/exit 3，保留原拒绝行为，不声称本次修改了这条独立 reader。
- not-run：公开附件上传、重新下载及从该下载包执行最小作者命令。
- 既有公开源码 CI 的 Core 用例 `RuntimeApiRegistrationIsAtomicAndDynamicProviderReserved` 仍失败；本次不将它记为已修复或全仓 CI 通过。

## Evidence

- `tmp/sdk-release-root-20260912/d7-audit.json`：原包身份、逐文件结果、随包文档及校验输入哈希。
- 同目录 `d7-release-check.log`：准确 D7 包检查 PASS。
- `artifacts/pn041/sdk-msbuild-distribution-d7/DTMAPI-Author-SDK-0.7.0-win-x64.zip`：保留原始验收包，不覆盖。
- [最终 SDK 交付核对](../../debug/evidence/AUTHOR-SDK/20260912-first-publication/README.md)：最终包、差异及验证范围。

## Rollback Notes

文案可按本条提交回退，原 D7 与历史验收保持原字节。公开附件不得原地替换字节；后续技术修订需使用新的工具版本和附件身份。本次不触碰共享游戏、官方 MODS、live upload 或玩家存档。

## Follow-Up

完成准确技术包、中文指南和最小验证后发布，下载核对通过后记录最终链接与提交。
