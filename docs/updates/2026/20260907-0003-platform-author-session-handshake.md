# PN-002: SDK/Core author session handshake

## Metadata

- Update ID: `20260907-0003`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户要求 PN-001 验收后继续 PN-002；内部实现和必要测试已授权，保留其他未提交工作。
- Review: [平台基线 Review](../../reviews/code/2026/20260907-0001-platform-next-baseline.md)。
- Task: [PN-002](../../planning/platform-next/tasks.md)，按 [A04 已接受协议](../../architecture/platform-next.md)实施。

## Known facts and scope

原始 SDK prepare 使用冻结编译 target 作为 descriptor/runtime wire 值；Core 按实际 Runtime 版本精确比较，后续请求和响应也有同类绑定。先用真实 prepare→Core reader 测试复现：旧 target 对照接受，当前 Host 返回 `descriptor-runtime-mismatch`。已排除只放宽 descriptor、修改冻结 API payload 或把全部常量改成未来发布版本的方案。保留一次性消费、时效、token、pipe 身份、有界队列和重放拒绝；没有 descriptor 时不创建 listener。按已接受的 A04 实施，没有重新审查整仓架构。

本项用空 game root 和真实 SDK/Core/pipe 组合测试验收托管协议；不改 Hook、游戏保存、SDK target 载荷或共享游戏部署。真实 Unity Mono 作者流程仍由 PN-008 验收。

## Changed Files

- `src/DTMAPI.Authoring.Contracts/AuthorContracts.cs`、`src/DTMAPI.AuthorSdk/AuthorSessionService.cs`：独立的 session 契约、schema 2 prepare、认证 hello、协议/能力与真实 Host 校验、保护凭据中的固定协商结果；新 CLI 不降级，明确区分不可达、超时与有身份依据的升级诊断。
- `src/DTMAPI.Core/Runtime/AuthorSessionContracts.cs`、`AuthorSessionDescriptorStore.cs`、`AuthorSessionHost.cs`、`AuthorSessionWire.cs`、`DtmApiRuntime.cs`：schema 1/2 reader、最高共同 minor、required/optional capability 处理、Host 快照与身份绑定；hello 使用同一重放窗口，业务仍进入既有 Runtime 队列；关闭时撤销凭据，超大响应保留协商 envelope。
- `src/Shared/AuthorSessionJson.cs`、Core/AuthorSdk 两个 `.csproj`：共享 BCL JSON 结构校验，拒绝重复字段和缺失身份/offer 字段，忽略未知可选字段；不增加 Runtime DLL 或改变游戏程序集 target。
- `src/DTMAPI.GameBridge.DolocTown/AuthorSessionReloadBridge.cs`：包 marker 使用既有 SDK/target 契约，独立于实际 Host release；继续要求代码、未知格式与身份变更重启。
- `tests/DTMAPI.AuthorSdk.Tests/PlatformSessionHandshakeTests.cs`、`Program.cs`：真实 prepare→Core reader/pipe/Host→SDK validator 组合矩阵；故障 server 只补边界；`platform-session-handshake` 与默认 suite 调用同一组函数。可选旧可执行文件入口在本次实际执行。
- `tests/DTMAPI.UnitTests/Program.cs`：`platform-session-core` 复用默认路径中的七项会话生命周期/来源/内容回归；旧 fixture 使用固定 legacy wire identity，包 marker 使用编译 target 权威。
- `author-sdk/schemas/author-session-descriptor.schema.json`、`author-session-wire.schema.json`、`author-sdk/SESSION-PROTOCOL.md`、`README.md`、`tools/scripts/check-author-sdk-release.ps1`：保留旧 schema，补新协议形状、兼容与诊断说明；协议文档随 SDK 打包并纳入现有包检查。
- 本 Update、九月台账及平台 README/tasks/status/架构头部：本项交接和生命周期，任务状态仍只由 status 持有。

## Result

编译 target、SDK release、Host release 和 session protocol 已分离。schema 2 离线文件不声明 Host，hello 返回并绑定真实版本、共同协议和明确能力；后续 CLI 重试不能改变已协商身份。schema 1 只接受已知 `0.5.5` wire alias，request/response 保持同一精确值，真实 Host 通过旧客户端能解析的 status value 单独报告。

当前 Runtime/API/SDK 版本、冻结 payload、Product Catalog 和订阅发布记录均未改。新包是本地验证候选，尚未发布。公共 Abstractions API 和 ABI 没有增加成员。

## Validation

- 仓库工具链 Release build 通过，Core/GameBridge 继续产出 `netstandard2.0`，SDK/测试使用 .NET 8。
- `platform-session-handshake` 先红后绿，最终退出 0。覆盖真实新 SDK/Core 往返、旧 target 对照、minor 交集、未来 major/不交 minor/最低 Runtime/required 能力拒绝、optional 未知能力、一次消费、过期、错误 session/token/root/pipe、重复 JSON、必需字段、重放、Host/请求/响应身份与协商结果固定。
- 最终 focus 还覆盖 hello 前拒绝业务、同 owner 并发、队列上限、Runtime 超时、关闭后的 pending/in-flight/replay/凭据清理、超大请求及大响应回退。Windows ACL 与报告 token 脱敏检查通过。
- 旧 Host 拒绝 descriptor 后无 listener、新 Host 尚未启动和 silent Host 分别执行；没有把无响应推断成需要升级。身份核验后的明确协议错误返回 `upgrade-required`。
- 使用 PN-001 时、会话改动前构建的真实 SDK 可执行文件生成 schema 1，并通过当前 Core 完成 snapshot 和原客户端严格 response 校验；不是用新版客户端模拟旧 reader。
- 既有 Core session focus 通过。完整 SDK suite 通过，`advancedReferenceFixture=executed`；最终补充队列/大响应测试后重跑同一 handshake focus。最终完整主 Unit 退出 0、`DTMAPI.UnitTests: OK`，其 cleanup-pending 会话由现有工具预览并确认解锁后清理。
- 两份 JSON schema 的 legacy/descriptor/hello/response 正例通过 PowerShell `Test-Json`；缺 offer 或 Host 字段的负例被拒绝。
- `build-author-sdk.ps1 -Configuration Release -OutputRoot <本次独立路径>` 连同实际 ZIP 的 `check-author-sdk-release.ps1` 通过：冻结编译载荷与确定性 ZIP 校验保持有效，协议文档已装包。
- 文档治理（7,816 项）与 diff 检查通过。对开始时记录的 71 个已有修改文件核对 SHA-256，除本路线/台账的 5 个预期编辑外，其余 66 个原字节不变；没有重置或清理其他工作。没有运行完整 `test.ps1` Release 套件或游戏/Mono/安装/Hook smoke；不将托管测试推导成实机验收。

## Evidence

- `tmp/platform-next/PN-002/session-red.log`：原版本错配复现与旧 target 对照。
- `tmp/platform-next/PN-002/build.log`、`build-final.log`、`handshake-final.log`、`core-focus-second.log`、`sdk-full.log`、`unit-full-final.log`。
- `tmp/platform-next/PN-002/handshake.log`、`handshake-second.log`：实现过程中发现旧 JSON 反序列化不会执行字段初值，以及重复响应字段误归类为一般 IO 错误；同一实现中修正，最终矩阵验证通过。
- `tmp/platform-next/PN-002/schema-validation.log`、`sdk-package-final.log`、`sdk-artifact-root.txt`；最后一个文件只路由本次非发布产物。
- `tmp/platform-next/PN-002/cleanup-preview.json`、`cleanup-applied.json`。凭据和 C# fixture 均位于 `DtmApiTestSession`，SDK 包检查的进程 TEMP/TMP 指向本次仓库内独立产物目录。
- `tmp/platform-next/PN-002/docs-review.log`、`docs-final.log`、`diff-check.log`、`preservation-check.json`：文档与其他未提交工作保留检查。

## Rollback Notes

只回退本项新 schema 写入、协商实现、包 target 投影与相应测试/文档；保留旧 reader/恢复状态和凭据检查。没有共享游戏部署、Author 来源配置或存档资产需要恢复。工作区其他未提交修改不纳入回退。

## Follow-Up

PN-002 的托管组合验收、包检查和文档收口已通过；任务标为 done、PN-003 解锁为 ready。完整作者/Mono 验收和多 target 交付仍分别由 PN-008、PN-003 承担，不提前声明已实现。
