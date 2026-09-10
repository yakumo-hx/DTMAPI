# 20260907-0001：DTMAPI 下一阶段架构与工程交接计划

## Metadata

- Update ID: `20260907-0001`
- Date: `2026-09-07`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-run`
- Related Issue State: `none`
- Source: 用户要求接管 DTMAPI 下一阶段技术规划，重新验证源码/测试/SDK/API/Runtime/GameBridge，参考指定 SMAPI 当前与历史代码，留下可由 GPT-5.6 Sol 直接执行的架构、决策、路线、任务和状态资料；允许必要 build/tests，不开始大规模实现。
- Review: [本轮独立基线](../../reviews/code/2026/20260907-0001-platform-next-baseline.md)

## Scope

交付规划与验证基线。用户已接受的目标是开放、可维护的第三方作者平台；旧审核仅作为事实和回归参考。决定 A01–A11、编排 PN-001–014，第一阶段 PN-001–008 详细可接手。未修改生产 C#、SDK compatibility bytes、产品 Catalog、公共 API 签名和发布版本。

目标架构由[platform-next.md](../../architecture/platform-next.md)承担；任务执行状态由[status](../../planning/platform-next/status.md)承担。以后各任务新建自己的 implementation Update，不把本记录变成长流水账。

## Baseline

- Workspace: `E:/Python_project/DTMAPI`。
- Branch at capture: `codex/major-update-batch0-20260713`。
- HEAD at capture: `5440aed5`；完整 HEAD 和初始状态记录在 `reports/platform-next/20260907/baseline-head.txt`、`initial-status.txt`。
- 初始工作区已有 Wiki、玩家支持、Update/index 等未提交改动。本轮保留这些内容；月度台账只追加本轮行。
- 当前 Runtime 版本来源 `tools/release/dtmapi-runtime-version.props`；捕获值 release `0.6.1`、file `0.6.1.0`、assembly identity `0.5.3.0`。
- 构建/测试使用仓库 `Get-DotNetExe` 解析结果，详情 `reports/platform-next/20260907/dotnet-info.txt`。
- 本地 SMAPI 实际路径与快照、历史演变取舍记录在架构文档，未复制第三方实现。

## Changed Files

- `docs/reviews/code/2026/20260907-0001-platform-next-baseline.md`：当前证据、问题、保留价值与裁剪取舍。
- `docs/architecture/platform-next.md`：接受的目标架构、公共演进方式、协议/SDK/反射/开放原生/数据内容决定。
- `docs/planning/platform-next/README.md`：接手入口与可直接使用的继续提示。
- `docs/planning/platform-next/roadmap.md`：阶段、依赖、出口和远期方向。
- `docs/planning/platform-next/tasks.md`：详细任务卡与验收/回滚。
- `docs/planning/platform-next/status.md`：唯一执行队列；PN-001 ready，其余未开始。
- `AGENTS.md`、`PROJECT.md`、`docs/onboarding/current-state.md`、`docs/planning/README.md`、`docs/architecture/README.md`：加入下一阶段路线，区分已接受设计与当前运行事实。
- 本 Update 与 `docs/updates/INDEX-2026-09.md`。
- 忽略目录 `reports/platform-next/20260907/`：本轮日志、初始状态和一次性 session 组合探针源码；运行 fixture 使用 `DtmApiTestSession`，不接触真实游戏。

## Validation

| 检查 | 实际结果 | 证据 / 限制 |
| --- | --- | --- |
| `tools/scripts/build.ps1 -Configuration Release -SkipTests` | passed | `reports/platform-next/20260907/build-release.log`；不是游戏验证 |
| `DTMAPI.UnitTests` Release、无 focus | failed | `DTMAPI.UnitTests.log`；在 `PreviewVersionMetadataIsConsistent` 停止，后续用例未执行 |
| `DTMAPI.QaUnitTests` Release | passed | `DTMAPI.QaUnitTests.log`；QA 单元程序，不是游戏 smoke |
| `DTMAPI.InstallDoctor.Tests` Release | passed | `DTMAPI.InstallDoctor.Tests.log` |
| `DTMAPI.MultiPlatformInstaller.Tests` Release | passed | `DTMAPI.MultiPlatformInstaller.Tests.log` |
| `DTMAPI.AuthorSdk.Tests` Release | passed | `DTMAPI.AuthorSdk.Tests.log`；包含 `advancedReferenceFixture=executed` |
| 真实 SDK CLI → Core descriptor reader 组合探针 | reproduced defect | `session-probe.log`；当前 Runtime 拒绝、旧目标对照接受，非修复 PASS |
| 文档检查（改动前） | passed | `doc-governance-before.log`，7715 checks |
| 文档检查、相对链接与独立接手审读（改动后） | passed | `doc-governance-after.log`，7753 checks；`doc-links.log`，115 个本地链接、0 缺失；两项独立复核的接手阻塞已修正 |
| 真实 Unity Mono / 游戏启动 / 安装 / 存档 / 全 Release 包矩阵 | not-run | 本轮调查无需改变共享 Runtime；不新增 smoke 行 |

测试均以 build 后现有二进制运行；五套独立结果见 `reports/platform-next/20260907/test-results.csv`。未把四套通过结果宣称为完整测试基线通过。

本 Update 的 `verified` 只表示调查与工程计划交付验收完成，不表示当前全部源码测试通过或 PN 任务已经实施。独立接手复核核对了任务依赖、公开反射签名和异常、现有 owner 清理接缝、Advanced 分派及会话协议。发现的依赖不一致、生命周期接口假定和握手诊断歧义均在本轮文档内修正；无 listener / 超时不得自动判定需要升级的反例已加入 PN-002 验收。

### 主 Unit 失败定位

`tests/DTMAPI.UnitTests/Program.cs:2737` 的 `PreviewVersionMetadataIsConsistent` 同时验证源码版本关系和重复保存的发布字节快照。断言中的 Runtime tree/player payload digest 分别为 `8280dcfb...` / `c7335934...`，当前 Catalog 对应为 `846665a9...` / `b4ec6a44...`。其余已读版本/info 字段一致。

这是已观察到的测试/发布快照漂移，不能据此宣称 Runtime 行为损坏，也不能未经来源核对就改 Catalog 或机械替换哈希。PN-001 负责核实当前 release authority，拆开源码版本投影测试与冻结发布包验证，再跑完整主 Unit，暴露目前未执行的后续测试。

### Session 组合探针

使用已编译 SDK 的 `AuthorApplication.RunAsync(session prepare --game-root <fixture> --json)` 在受管理测试 session 中生成真实 descriptor，再调用生产使用的 `AuthorSessionDescriptorStore.ConsumeStartup`。不伪造 token、不运行游戏、不启动真实 named-pipe 服务；输出不包含 token。

```text
SDKTarget=0.5.5; ReaderRuntime=0.6.1; PrepareExit=0; Accepted=False; Code=descriptor-runtime-mismatch
SDKTarget=0.5.5; ReaderRuntime=0.5.5; PrepareExit=0; Accepted=True; Code=descriptor-accepted
```

第二项使用独立目录，避免一次性消费混淆。探针退出 0 表示成功复现上述正反对照；功能仍有缺陷。其源码在忽略目录保留供核查；PN-002 将该用例转成受跟踪的长期组合测试并扩展到实际 request/response。

## Evidence

本轮代码推理有具体文件/符号支持，见 Review。原始输出保留于 [本轮本地报告目录](../../../reports/platform-next/20260907)。完整行为与协议决定保存在架构文档，交接不依赖这些忽略日志仍然存在。

## Rollback Notes

本轮只有文档与忽略日志。撤回时仅撤销本 Update 列明的新增文档及路由增量，保留初始 dirty 工作与共享月度台账既有行。没有需要恢复的游戏、配置、存档、安装包或 compatibility payload。

## Follow-up

规划验收完成后切换到 Sol，从 PN-001 开始。随后 PN-002 修复会话，PN-003/004 打通 SDK target 与官方开发来源，PN-005–008 交付可使用的反射与完整作者闭环。后续开放 Native、共享契约、Data 和 Content 按路线推进；本轮不标任何实施任务完成。
