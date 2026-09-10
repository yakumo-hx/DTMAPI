# 存档槽迁移：保留的决策与失败证据

本页是历史知识提取，不拥有当前实现或测试规则。当前保存语义见 [PROJECT](../../../PROJECT.md)，MoreSaves 的现行问题状态见 [ISSUE-019](../../debug/issues/ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md)，产品身份与版本查 Catalog。

## 文件角色迁移与内容修复的边界

1.00 升级曾在原生槽数仍为 6 时完成一次性转换，随后 MoreSaves 才扩展至 12，导致旧额外槽 6–11 留在旧文件名家族。UI 有十二格不能证明额外存档已被原生发现；旧 smoke helper 的文件名也会使真实存在的槽被误判为缺失。

2026-08-04 最初实现要求完整文件家族、解密验证内部 index、哈希与回滚；真实第 10 槽 backup 的内部 index 不同导致整组被拒绝。2026-08-05 用户明确纠正为原生文件角色语义：current、prev、bak 各自独立转换；目的文件存在则保留两边并继续；缺失角色不创建；完成的移动自身就是可续跑状态。内容修复、内部 index 改写、额外 journal 不属于这次迁移。旧严门槛的失败与 PASS 保留为已被替换实现的证据，不能验证后来的轻量实现。

## 调试与验收教训

- 用户第二次看到旧槽空白时，实际选中的 Local 包仍为旧版本；源码修复成功不能证明游戏选中了新字节。先核对选择来源、版本和实际入口 DLL，再判断迁移失败。
- 两次隔离准备失败分别缺少 Local 包与隔离 SAVE 中的 `mod_infos.json`；需要同时准备官方来源字节与原生启用状态。
- 启用产品本身可能触发启动迁移。进入游戏后不主动保存不足以证明该轮无写入；普通测试和迁移测试应按实际行为分流。
- 后续修正以原生 UI 显示、额外槽真实加载、第二冷进程无剩余迁移、干净退出闭环。它证明的是固定十二槽角色迁移，不是通用存档编辑或任意分页能力。

完整时间线、源字节及各次失败保留在 ISSUE-019；本页不复制易变状态和证据哈希。

## ProductNative 提取的历史边界

20260723-0002 admission 认定固定 6/12 只有一个真实消费者，原生面板已经能处理十二格，不需要 Show Hook、额外分页或 SharedNative。旧 ABI 可在现有单一 Compatibility Host 延续，产品和兼容实现必须协调全局 archiveFileCount 的唯一写入者。Admission GO、前提修复、实现、运行验收与发布是分开的事实；默认加载减少也不等于下载包或总源码减少。旧 admission 中强制 HookProbe 等测试细节不覆盖 PROJECT 后来的按风险测试规则。

## 玩家基线与证据更正

[20260713-0001](../../archive/reviews/manual-qa/2026/20260713-0001-moresaves-long-term-player-baseline-review.md) 的七项人工反馈依次为：①7–12 槽创建/保存/回标题/再载入正常；②冷重启识别；③复制删除正常；④禁用回六格但不损坏额外档；⑤重新启用可找回；⑥12 与 16 槽长期可用；⑦18 槽单页叠放越屏。当前固定十二槽基线受到前五项保护；16 槽人工可用不等于任意数量 API 稳定，18 槽的已知失败是 UI 容量而非已证明的数组损坏。

[20260804-0001](../../archive/reviews/manual-qa/2026/20260804-0001-moresaves-100-legacy-slot-migration.md) 必须读到 8 月 5 日追加段：用户撤回“全族解密/散列/索引正确才迁移”的额外保护门，要求逐文件遵循原生名称角色移动。早期异常 bak 拒绝整族的 PASS 只能证明旧设计，不能继续阻断用户已经授权的新边界。一次 18 行回执实际仅 16 个唯一文件名，修正版明确 16 个存在、两个 absent；追加更正保留原证据，不把重复名称伪装成 18 份文件。

[20260805-0003](../../archive/reviews/manual-qa/2026/20260805-0003-release-blockers-moresaves-live-package.md) 的编号为：①SDK 测试锁死陈旧 README 句子；②确定性安装暂停检查发生在 Runtime 提交之后；③退休 Mods 根仍由规范/日志创建；④MoreSaves 实际加载旧 Local 包；⑤ChestLocator 玩家确认远端识别与扣料；后追加⑥Y 控制台同样未留下新 Local 包。此处最可复用的部署教训是：smoke 按合同恢复旧树后，正式手测交付仍需单独持久部署。旧包被正确选中并不是来源仲裁回归。后续 MoreSaves 日志证明 16 个角色移动和一个额外槽载入；用户之后确认 MoreSaves、DebugConsole、AutoFishing 正常，不应扩写成所有槽逐项自动化证据。

[准入前提](../../archive/updates/2026/20260723-0003-moresaves-admission-prerequisites.md) 只改装载前 PE/receipt 与单写入者基础设施，不切 live 路线，因此明确不需要游戏。[第六产品实现](../../archive/updates/2026/20260723-0004-moresaves-sixth-advanced-product.md) 则用一次官方面板、回标题重开和真实 owner deactivation；其中一次 QA 错把 post-title requirement 分型造成挂起，保留为 non-acceptance。后续兼容状态/诊断修正没有改变成功原生路径，沿用那次有效实机证据，明确不做完整 Release/GC/长测。常驻监听器改成仅 manager 缺失时 demand activate，也是减少默认开销的实际实例。

## Entry 写入是由实际旧文件触发的例外

MoreSaves 的 legacy role 迁移可能在 Entry 发生。只有实际启用该产品且 effective SAVE 有精确旧迁移候选，才需要 ArchiveMutation 前提；不能从这项例外给所有无保存游戏测试常规隔离。必要 redirect 应先于 runtime.Start/Entry，以 cloudDirPath 和真实 path helper 证明生效；Start 后 bridge 初始化失败仍可能留下活跃产品，redirect 保持到停机，不能在错误出口提前撤回。

1.00 native archive 家族是 current/prevN/bak；观察新增、删除、改写不能还用旧 -prev.data/-bak.data 名字清单。历史 18 行回执重复名称的纠正可只读重评原件，未变的迁移证据无需进游戏重造。来源：[第四组](../../archive/reviews/code/2026/20260804-0011-dtmapi-060-fourth-five-slice-parallel-review.md)、[第五组](../../archive/reviews/code/2026/20260804-0018-dtmapi-060-fifth-five-slice-parallel-review.md)。
