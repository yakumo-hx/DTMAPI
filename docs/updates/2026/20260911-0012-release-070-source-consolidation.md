# 20260911-0012: 收齐 0.7.0 源码记录并更新远端分支

## Metadata

- Update ID: `20260911-0012`
- Date: `2026-09-11`
- Lifecycle Status: `implemented`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求提交截至 0.7.0 发布的相关源码与记录，包括中文编码兼容，并完善远端提交；地图研究排除。随后在当前工作空间创建 Astra low 任务，使用中文技能优化远端说明，重点是 README。

## Summary

收齐已接受的标准 MSBuild SDK、工程转换、测试和流程修正，以明确的本地提交更新现有 `agent/dtmapi-0.7.0-source`。BOM 读取修复、Y 1.1.3 和准确订阅记录已在 `f3622199`，此次以其为父提交继续归集。根 README 先纠正发布状态，完整中文文案随后由新任务接续。

这是现有 0.7.0 源码分支的范围补齐。[公开交付设计](../../reviews/code/2026/20260911-0003-public-source-delivery-design.md)中的新导出器、目录重组、许可补件和干净源码验收仍按设计状态记录，不因本次分支更新标成已经实现。

## Changed Files

- Author SDK 的标准构建后端、分析器、schema4、模板、NuGet 资产处理和最终 IL 校验；收录相应删除、12 个产品工程转换及锁文件。
- 既有构建与测试入口、Runtime 构建范围修正和配套记录；各项验收状态保持其原 Update 的结论。
- 根 README、发布和 SDK 记录、公开交付设计与月表。Catalog 剩余差异仅为 JSON 排版，发布值与父提交逐项相同。
- 混合文件只收录本次内容；地图计划、源码、测试、Hook 图、smoke 行和月表行保留在工作区。地图实验增加的 native netstandard 2.1 签名归一、`NativeFacadeIdentityTests` 及测试入口调用也排除。Wiki、临时产物和本机资料保持原位。

## Validation

- PASS：文档治理、28 项发布路由、暂存 JSON 解析及 Catalog 语义比较。189 个文件进入暂存，五个混合文件只收本次内容；没有新增指向地图实验的文件链接。提交前后继续核对排除文件原字节。
- PASS：D7 完整 Release 输入清单中的 303 个非生成文件与当前工作区逐字节相同；另外三个差异分别是 README 发布事实、BOM 修复和地图签名归一。最后一项已从提交剔除；前两项按后续发布记录保留。工具链与生成物不计入这 303 项，不能据此声称所有当前文件或整份发行 ZIP 与 D7 相同。
- PASS：差异空白检查。两份 SDK 原始研究输入通过精确 `.gitattributes` 条目保留原字节和记录中的 SHA256；原文的行尾空格与 CRLF 依现有原始材料方式免检。六份现行文件只清除末尾空行，没有改写历史观察或实现。
- 复用 [SDK 首次交付](20260910-0012-sdk-msbuild-first-release.md)、[BOM 修复](20260911-0009-runtime-070-package-marker-bom.md)及 [发布确认](20260911-0011-runtime-y-hotfix-publication.md)的具名验收。这次归集不重编发行包，不用新提交号替换已发布包的构建来源。
- 远端写入前核对分支最新指向；快照从提交对象生成，不从工作树补文件，也不把内部开发父链推入远端。

## Evidence

- 本次范围与检查输出：`tmp/commit-070-20260911/`。
- 原 D7 完整 Release：`artifacts/pn041/full-release-repair-r2-result.json`；独立验收：`artifacts/pn041/independent-d7-acceptance/result.json`。
- 远端：[0.7.0 源码分支](https://github.com/yakumo-hx/DTMAPI/tree/agent/dtmapi-0.7.0-source)。

## Rollback Notes

按本次提交差异回退，不覆盖工作区中的地图或 Wiki 改动。现有预览标签继续保存原快照；玩家包、游戏目录和存档没有被本次操作修改。

## Follow-Up

完成本次提交、远端指向复核和本地保留内容核对后，创建用户指定的当前工作空间 Astra low 文案任务。正式公开树及进入 main 的源码验收继续沿上述设计推进。
