# API 0.6.2 引入的平台服务

这些平台服务在内部 API 0.6.2 中合并了此前验证的 M2 与反射能力，仍为 Experimental。SDK 0.7.0 保留 0.6.2 target，默认 target 为 0.7.0；target 是否可选及对应 Runtime 范围以 [target catalog](target-catalog.json) 为准。显式选择 API 0.5.5 的工程保持原载荷，不能调用这些服务。请使用正常 SDK 构建及匹配的引用载荷，不要为了编译通过而引用已安装的 Runtime DLL。

## 获取服务与生命周期

在 `Entry` 中使用 `helper.GetRequiredService<T>("0.6.2", "0.6.2")` 获取必需服务，可选服务使用 `helper.GetOptionalService<T>()`。获取服务要求位于 Runtime 线程且 owner 处于活动状态。查找依据准确契约 Type，不按名称查找，也不会退回 Mod API registry。

`IDtmRuntimeContext.Snapshot` 是不可变快照，不包含原生对象，可在后台线程读取。`IsMainThread` 描述当前调用线程。快照先发布，再同步通知 `Subscribe` 订阅者。加载失败也会消耗一个 save epoch；房间切换会先使 world epoch 失效，再发布下一个就绪世界。`SaveLoaded` 仍是原有加载回调，不表示 `WorldReady`；这两种 epoch 都不是持久存档 ID。

## 调度与受管资源

`IDtmScheduler.Post`、`NextTick` 和 `Delay` 可从任意线程接收同步 Action。原生世界工作选择 `CurrentWorld`，绑定某一次读档尝试的工作选择 `CurrentSaveSession`，属于整个 owner 的工作选择 `ModOwner`。入队时捕获当前 epoch；范围不可用立即拒绝，范围变化会取消尚未开始的工作。`NextTick` 在当前 Core tick 之后开始，回调内提交的工作不会在同一次队列处理期间执行。Delay 和排队超时按单调经过时间计量；超时限制等待开始的时间，不限制回调执行时长。

每个 handle 只有一个终态结果。取消无法中断已运行的 Action。回调共享 Runtime 每帧预算，应尽量简短。完成 Task 的 continuation 异步运行，不会自动回到 Unity 线程；先完成后台工作，再用 Post 提交结果。不要使用 `async void`：SDK203 会识别直接传给 scheduler、context、command 和 language 回调的 async lambda/method group，但不会追踪存入变量的委托。

`IDtmOwnedResources.Register` 在 Runtime 线程上注册一个 `IDisposable` 并返回 token。`Unregister` 只解除注册，不 dispose；token 的 `Dispose` 请求清理。后台调用 Dispose 会等待 Runtime 处理队列。范围失效或 owner 关闭时，资源按注册顺序倒序 dispose，异常相互隔离，清理失败的资源保留以供重试。dispose 应可重复执行而不产生额外副作用。已关闭 owner/token 不会因同 ID Mod 再次加载而恢复活动。

## 命令

`IDtmCommands.Register("status", handler, scope, description, parameters, alias)` 注册规范名称 `UniqueID/status`。名称和别名比较不区分大小写；别名冲突会失败，不覆盖先前注册。`GetHelp` 返回只读快照。`Execute` 与其他工作使用同一 scheduler 和范围检查。handler 收到 request ID、owner ID、只读参数及 `WriteLine`。输出最多 32 行/4096 字符。解析器支持带引号参数、反斜杠转义和空引号参数；输入不是 shell 命令或脚本。

存在注册命令时，标题界面的 Status 页会显示命令输入框。用 `help` 查看名称；界面显示简短输出，完整输出写入日志。认证作者会话先通过 `--commands true` 准备，再使用 `session command <UniqueID> <selectedRoot> --game-root <path> --command-line "UniqueID/status"`。该选项协商可选 `execute-command/1`，仍需通过选定来源、文件树、owner 和 request-ID 检查；不能借会话执行另一个 owner 的命令。排队超时或会话关闭会取消未开始的工作，已经运行的工作不回滚。

## 带版本的配置

`IVersionedConfigHelper` 绑定 Runtime 线程和 owner。`Read<T>(schema, validate)` 对新配置返回 `Missing`；需要默认值时显式调用 `Write(defaults, schema, validate)`。通过 `RegisterMigration<T>(n, value => migratedValue)` 注册每个 `n → n+1` 步骤，版本从 1 开始。整个迁移链使用相同的可序列化模型类型，应保留迁移所需字段。

迁移先作用于候选值，校验后一次原子提交。校验返回 null/空字符串表示成功，错误字符串表示失败。校验接收隔离值，必须只做检查，校验中修改的值不会提交。读取已提交版本成功后，不会再次迁移。`Corrupt`、`UnsupportedSchema`、`MigrationRequired`、`MigrationFailed`、`ValidationFailed`、`AccessDenied` 和 `IoError` 都会保留现有字节，且与 `Missing` 不同；不能把它们当作写入默认值的许可。不会自动修复并覆盖损坏或未来版本数据。owner 关闭时释放迁移回调。

带版本配置位于 Runtime 配置区，与旧 `ReadConfig` 文件分开；采用它不会静默导入或改写旧文件。作者可显式读取旧模型、校验，并且只在版本化读取结果为 `Missing` 时写入新配置。旧 migration Actions 仍保留原来的每次读取行为。配置独立于玩法存档的提交/回滚语义。

## 输入与翻译

继续使用现有 owner 的 `Input` 注册、范围及抑制 API。`IDtmInputDiagnostics.Snapshot` 描述最近一次冻结的输入接收状态。`GetRegistrations` 只返回当前 owner 的不可变绑定描述；`FindConflicts` 返回可能重叠的绑定及 owner/name/scope，但不会决定谁优先。仅共享修饰键不算冲突；通用 Control/Shift/Alt 包含左右物理键。范围、平台/owner UI 焦点、抑制和物理输入回到中立状态后重新启用的规则，仍共同决定事件是否送达。控制器覆盖仅限实际支持并测试的后端路径，这些 DTO 不会增加设备驱动。

`IDtmTranslations.Get` 使用当前包的语言目录，沿用 locale → English → 显式 fallback/key 的查找顺序。参数写作 `{name}`，`{{` 和 `}}` 输出字面花括号。替换只执行一遍；缺失参数保留在文本中，并列入 `MissingParameters`，参数名区分大小写。使用 `SubscribeLanguageChanged` 订阅，在其同步 Runtime 线程回调中刷新可见作者 UI。订阅时没有初始通知。即使共享英文回退，区域语言变化也分别处理。dispose 订阅或关闭 owner 会解除订阅。读取也要求 Runtime 线程，因为选择 locale 可能读取原生语言状态。
