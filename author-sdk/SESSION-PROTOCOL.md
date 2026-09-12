# 显式作者会话

当前实现使用 descriptor/envelope schema 2 和 session protocol 1.0。本页说明会话实现契约；实际发行状态见[仓库发布页面](https://github.com/yakumo-hx/DTMAPI/releases)。编译 API target、SDK 版本、已安装 Runtime 版本和会话协议是不同的值，不能相互代替。

## 准备会话与 hello

`session prepare --game-root <path>` 写入一次性启动描述符和受保护的客户端凭据，不启动游戏，也不预设 Host 版本。两个文件都绑定规范化游戏根目录、GUID-N 格式的 session ID、派生管道名、随机 token，以及十分钟的 UTC 有效期。Windows 使用受保护的当前用户 ACL，Unix 使用 0600 权限；这项文件权限实现本身不代表其他平台的完整作者流程已验收。

schema 2 增加 `protocolMajor`、`minimumMinor`、`maximumMinor`、`apiTarget`、`minimumRuntimeVersion`、`requiredCapabilities` 和 `optionalCapabilities`。当前 writer 请求协议 1.0、已有冻结编译 target、会话契约的最低 Runtime、必需能力 `get-source-snapshot/1` 和可选能力 `reload-content/1`。离线描述符没有 `hostVersion` 或旧 `runtimeVersion` 字段。描述符在启动时只被原子消费一次；没有描述符就不建立 listener、watcher 或轮询循环。

Host 校验凭据、路径和有效期，再选择双方协议范围交集中的最高 minor。major 不同、minor 无交集、未满足最低 Runtime 或必需能力未知时，拒绝描述符。未知可选能力会报告出来，但不会启用。首个线路请求必须是经过认证的 `hello`，并携带与描述符相同的能力及版本提议。响应包含选定的 major/minor、实际 `hostVersion`、`apiTarget`、`acceptedCapabilities` 和 `unsupportedOptionalCapabilities`。

每次调用 CLI 都使用新的 request ID 和原始提议重新执行 hello。Host 返回相同协商结果，不能通过更改提议重新协商已有会话。首次 hello 成功后，SDK 原子替换受保护客户端凭据，在其中保存 `negotiated` 结果；不会重新建立启动描述符。后续 CLI 调用及响应必须与该结果一致，包括实际 Host 身份和能力列表。

## 业务消息与生命周期

实现还支持可选 `execute-command/1`，仅由 `session prepare --commands true` 请求。`session command --command-line <text>` 使用已有会话和选定 owner。schema-2 请求携带长度受限的 `commandLine`；Host 复用现有 pending-request/去重生命周期，由唯一的 Runtime scheduler 执行同步 handler。完成结果保留相同 request ID 和 owner。到达 deadline 或会话关闭时，取消尚未开始的工作；已开始的工作继续完成，不承诺回滚。此能力与公共 API target 是否可用相互独立，见[平台服务语义](PLATFORM-SERVICES.md)。

schema 2 请求携带版本及能力提议字段，以及 `gameRoot`、`session`、`token`、`requestId` 和 `operation`。业务请求还绑定选定的 `hostVersion`/`protocolMinor`、`uniqueId`、`selectedRoot` 和 `expectedTreeSha256`。响应绑定 schema、协议、Host、根目录、会话、请求、操作及 owner。只有协商接受的能力才授权对应操作。hello 由传输层处理，业务操作进入有界 Runtime 线程队列。重复 request ID 会被拒绝，包括使用旧 ID 重试 hello。

`snapshot` 读取当前选定来源。`reload` 校验选定根目录、当前文件树、活动身份和包元数据，只刷新支持的非代码内容。DLL 改变、格式未知或 manifest/包身份变化都需要重启。包的编译 target 由已有 SDK 包契约独立校验，不以 Host 发行版本代替。

未知可选 JSON 字段会被忽略，重复关键字段会被拒绝。描述符及请求大小上限仍为 32 KiB，响应为 64 KiB；每个能力列表最多 32 项，防重放窗口保存 4,096 个 request ID。现有 pending/并发上限及 Runtime deadline 继续生效。会话关闭会撤销接受状态，清空排队工作、防重放状态和 Host 凭据，并关闭 listener。token 值不进入报告。

## 兼容与诊断

schema 1 只接受已知旧 `runtimeVersion` 值 `0.5.5`，没有 hello。即使 Host 较新，其请求及响应的 `runtime` 字段仍保留这一会话的准确线路值；实际 Runtime 版本另在状态值 `hostVersion` 中显示。旧响应 envelope 不增加新的顶层字段，使旧 SDK 的严格响应 reader 继续兼容。

新 SDK 命令不会静默写入或降级为 schema 1。已有旧凭据可被 clear，或在到期后替换；新命令会要求作者先 clear/prepare 再使用 schema 2。这不会迁移工程的编译 target。

| 状态 | 含义与处理 |
| --- | --- |
| `host-unavailable` | 没有可用 listener，或连接失败。启动游戏并检查日志和版本。Windows 连接超时时会通过本机管道 API 区分命名管道是否缺失。 |
| `handshake-timeout` | 连接/hello 未在 deadline 前完成；不能由此推断 Host 版本或必须升级。 |
| `upgrade-required` | 经过身份核验的 hello 响应明确报告协议、能力或最低版本不兼容。 |
| `pipe-response-identity-mismatch` / `pipe-response-negotiation-mismatch` | 响应身份或已确定的协商结果改变，业务请求不能判为成功。 |
| `restart-required` | 经过认证的业务结果要求重启游戏，不代表热重载成功。 |

仅凭 listener 缺失，无法区分“旧 Host 消费或拒绝 schema 2 后未建 listener”和“Host 尚未启动”。这两种情况都保持 unavailable/timeout 诊断，不能推断为必须升级。

线路格式见[描述符 schema](schemas/author-session-descriptor.schema.json)及 [JSONL schema](schemas/author-session-wire.schema.json)。仓库定向验证使用 `DTMAPI.AuthorSdk.Tests` 的 `DTMAPI_AUTHOR_SDK_TEST_FOCUS=platform-session-handshake`，以及 `DTMAPI.UnitTests` 的 `DTMAPI_UNIT_TEST_FOCUS=platform-session-core`；它们也会在默认测试集合中运行。可选 `DTMAPI_AUTHOR_LEGACY_EXE` 提供已构建旧 SDK，补充真实旧客户端/Host/旧响应校验器测试；缺少时会明确输出。fixture 和凭据留在 `DtmApiTestSession` 内。Unity Mono 作者流程验收仍是独立的平台集成检查。
