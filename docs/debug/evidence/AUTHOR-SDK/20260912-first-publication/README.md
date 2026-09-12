# SDK 0.7.0 首次交付核对

Owner：[20260912-0001](../../../../updates/2026/20260912-0001-sdk-publication-chinese-guides.md)。2026-09-12 由主任务负责准确包、技术验证及发布，原 Astra low 任务仅负责中文文案。

## 准确包

| 项目 | 值 |
| --- | --- |
| 发行文件 | `DTMAPI-Author-SDK-0.7.0-win-x64.zip` 及同名 `.zip.sha256` |
| 本地最终路径 | `artifacts/sdk-release-070-20260912/final/DTMAPI-Author-SDK-0.7.0-win-x64.zip` |
| 长度 | 1,015,622,994 字节 |
| ZIP SHA-256 | `9f69f0cbe241f58c05457a8e6d252319a186967153823b3870c07e9d8511bee0` |
| inventory SHA-256 | `138ee32cea48ce6854ddf9108a21822b2810c3c1f3c689657c8decaf8c9561ac` |
| 文件数量 | 6,177（6,176 项载荷和 1 份 inventory） |
| 工具 / 宿主 | SDK 0.7.0；Windows x64；自包含 .NET 8 CLI 与 .NET SDK 8.0.421 |
| API | 默认 0.7.0；可选 0.5.5、0.6.2、0.6.3、0.6.4、0.6.5、0.7.0，各自 Runtime 范围由 target-catalog 维护 |
| 构建源码 | 公开主线 `73aaffce50d9105cfabd18e787b3f3160467b322`；最终中文文档单独汇入 |
| 公开状态 | 待附件上传及公开下载核对；不能把本地构建当作已发布 |

## 与 D7 的关系

原 D7 ZIP 保留在 `artifacts/pn041/sdk-msbuild-distribution-d7/`，SHA-256 为 `a801825fdf7565d24b7e13447e4e1ad96e138038f35355f8ec6bc3bdaad9f8a6`。本次重新核算全部载荷，并执行原包完整 checker 与确定性重放，均通过。

新技术包的编译逻辑变化仅是此前已提交的 UTF-8 BOM marker 修复。最终相对 D7 变化为 `DTMAPI.InstallDoctor.dll`、`analyzers/DTMAPI.Author.Analyzers.dll`、`build/DTMAPI.Author.Build.dll`、9 份中文使用指南和总清单；两份构建辅助程序集的类型、字段、方法签名及 IL 相同，ProductVersion 的源码提交标识更新。CLI、冻结 API/契约、工具链、模板、schema、引用及许可正文保持 D7 原字节。

最终 Doctor SHA-256：`67f5b3a5fb4e3cbbedfc30507cb92708399c0c9fe5a07af5b3c18e27da8916a9`。原 D7 Doctor 为 `aa0b5b61f1362ba7ad1f95fa6f4ea2c534e72c307410d8a5376320f32e6f31fc`。

## 验证

- 既有 package-target 矩阵直接引用准确最终 Doctor：83 项通过，保留 BOM、目标范围、身份、哈希及损坏包的正反例。原 D7 对照出现 14 个合法 BOM 拒绝及空文件异常；没有把旧 PASS 扩大到后来新增的矩阵。
- 标准 builder、原包/技术包/最终包的完整 release checker 与确定性 ZIP 重放通过；新包仅在同一原始技术包上应用明确中文指南，再用现有 inventory/ZIP 函数生成最终清单和附件。
- 最终 SDK 在仓库外新中文/空格路径、空缓存完成 new、离线 restore、Release pack 和 Doctor；SHA 与报告一致。
- 主要随包使用指南已中文化，自动生成的 API 状态参考仍保留原文；所有包内链接和锚点通过。未翻译或改写第三方许可证正文。
- 不重跑输入未变的 Mono/完整 Release；原 D7 接受范围仍为 Windows Developer Preview，不增加设备、任意依赖或断点调试承诺。

细项日志、原始失败及探针归 `tmp/sdk-release-root-20260912/`：`d7-audit.json`、`technical-build.log`、`technical-payload-delta.json`、`helper-il-comparison.json`、`doctor-matrix-technical/result.json`、`doctor-matrix-d7/result.json`、`final-package-check.log`、`final-author-probe-r2.log`。仅 BOM 的新依赖 marker 仍沿既有 SDK999/exit 3 路径拒绝；这是与本次旧 marker BOM 修复不同的边界。
