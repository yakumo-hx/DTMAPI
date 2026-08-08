# MoreSaves 1.00 旧槽位存档迁移复核

- Status: `implemented / focused source-unit-sdk-package validation passed / corrected player revalidation pending`
- Date: `2026-08-04`
- Source: 用户对当前存档列表、MoreSaves 实际 UI 表现和 1.00 迁移时序的复核
- Scope: MoreSaves ProductNative、Doloc Town 1.00 索引 6–11 旧存档族、NoNativeSave 当前文件族解析
- Related issue: [ISSUE-019](../../../debug/issues/ISSUE-019-20260804-moresaves-100-legacy-slot-migration.md)
- Owning update: [20260802-0001](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Supersedes: [20260804-0010 AutoFishing fifth-save fixture review](../../code/2026/20260804-0010-autofishing-fifth-save-fixture-availability-review.md)

## 问题 1：MoreSaves 已扩成 12 格，但后六格在 1.00 显示为空

用户反馈：

- MoreSaves 确实把 `GameManager.archiveFileCount` 从 6 改为 12，官方存档 UI 的后六格已经渲染。
- 1.00 当前文件名为 `doloc-save-{index}.data`；前六档已经是 `doloc-save-0.data` 至 `doloc-save-5.data`，所以能显示。
- 原有后六档仍是 `ea-playtest-doloc-archive-6.data` 至 `ea-playtest-doloc-archive-11.data`，因此官方 UI 查询 `doloc-save-6.data` 至 `doloc-save-11.data` 时把它们视为空槽。

审查记录：

- 用户确认事实与只读目录清单一致：旧索引 6–11 的 current 文件仍完整存在；部分槽位还保留旧 `-prev.data` / `-bak.data` 成员。没有读取、解密、移动或改写这些玩家存档字节。
- 当前反编译事实：`GameManager.archiveFileCount` 初始为 6，`archiveFileNameFormat` 为 `doloc-save-{0}.data`；`GameManager.Awake` 在 DTMAPI 延迟启动前构造 `LocalSave`。
- 当前反编译事实：`LocalSave.Validate()` 先写入 `convert_data_100=true`，再按当时的 `dataFileCount` 循环；原生映射为旧 current → 新 current、旧 `-prev` → 新 `.prev0`、旧 `-bak` → 新 `.bak`，且只在目标不存在时移动。
- 根因：原生一次性转换发生时槽位数仍为 6，只处理 0–5；MoreSaves 到 Entry 才把数量设为 12，因此无法让已完成且已打 flag 的原生循环回头处理 6–11。这是 MoreSaves 在 1.00 的 ProductNative 迁移缺口，不是官方 UI 分页或槽位渲染故障。
- 归属：索引 6–11 只因 MoreSaves 才存在，迁移属于该产品的 ProductNative 启动适配；不得把单产品文件状态机放进 mandatory Runtime 或 SharedNative GameBridge。
- 验收：启用 MoreSaves 时，在把槽位数暴露为 12 之前，对 6–11 的旧 current/prev/bak 完整文件族做一次启动期迁移；没有旧文件必须是成功空操作；完成后继续由官方 `GetAllArchiveInfos`、加载、保存、复制和删除逻辑拥有全部行为。

## 问题 2：AutoFishing “第五档缺失”是旧文件名硬编码造成的假阻断

用户反馈：

- 第五档 `doloc-save-4.data` 实际存在，第三档 `doloc-save-2.data` 也存在。
- `tools/scripts/common.ps1` 的存档族 helper 硬编码旧 `ea-playtest-doloc-archive-{index}.data`，从而错误报告第五档缺失。

审查记录：

- 代码事实：`Get-DtmApiCurrentSaveArchiveFamilySnapshot` 用旧名构造 current 路径，但其 `.prevN/.bak` 集合比较逻辑本身符合 1.00 的备份族形状。
- 失败矩阵 `20260804-100952-70e21c42` 仍证明没有启动游戏、没有触碰存档且部署被清理；它只证明错误路径的 preflight 失败，不能证明真实 UI #5/index 4 缺失。
- [20260804-0010](../../code/2026/20260804-0010-autofishing-fifth-save-fixture-availability-review.md) 已标为 `superseded`，其局部 fixture blocker 被撤销。
- 验收：helper 默认解析 `doloc-save-{index}.data`，比较同一 current 的 `.prevN/.bak`；旧 `-prev.data/-bak.data` 仍不得冒充当前家族，也不得进入 NoNativeSave 不变性证明。

## 问题 3：迁移必须保护内部索引和完整备份族，不能直接批量改名

用户约束：

- 1.00 同时改变备份族与内部索引校验，暂时不得直接改名玩家文件。
- 产品设计只做轻量启动迁移：不增加冲突页面、逐槽停用或玩家恢复流程；无旧存档不报错。

审查记录：

- 当前原生事实：`LocalSave.FixArchiveIndex(int,string,out string)` 会解密/解析存档，并同时检查顶层与 `baseData.archiveIndex`。它返回需要改写时，不能把单纯文件名匹配当成可安全移动的充分证据。
- 实施边界：同步预检 6–11 所有现存源与目标成员；通过反射只读调用原生索引校验，任何 mismatch/解析失败都必须在第一次移动前 fail-closed。产品不自行重写加密存档内容。
- 每一族只允许旧 current → 新 current、旧 prev → 新 `.prev0`、旧 bak → 新 `.bak`；目标已存在时不覆盖、不删除源文件，并记录诊断。迁移使用同卷 `File.Move`，逐项核对移动前后的 length/SHA-256。
- 中途故障不创建第二套恢复协议：已移动成员由“源缺失、目标存在”表示，未移动成员由“源存在、目标缺失”表示；下次启动重新全量预检后可幂等续完。迁移未完成时保持原生 6 槽，不让部分结果暴露给 UI。
- 验收必须在受管临时测试会话或明确 `ArchiveMutation` 的 disposable save fixture 中完成；不得用玩家 Steam AutoCloud 存档做迁移试验，也不得以事后复制恢复冒充隔离。

## 排除项

- 不新增显示名、任意槽位数量、滚动/分页、冲突选择 UI、逐槽禁用或通用存档迁移平台。
- 不修改 `flags.json`，不重跑或伪造官方 `convert_data_100` flag。
- 不把 MoreSaves 管理的存档安装到 `BepInEx/plugins`，不增加 Harmony Patch。
- 不删除任何额外存档或旧文件；产品禁用后仍只恢复原生槽位数 6。

## 验收结果（2026-08-04）

1. 对“后六格为空”的产品缺口，SDK 正式路径构建的 MoreSaves `1.0.1`
   包 `EF1E15EB9A85B39667AAC281A88BEC432436713954DC27D2CD37517CD44F5A7C`
   已在 Runtime `7ab6d567396b` 上完成 disposable `ArchiveMutation` 验收。
   `GAME-SMOKE/20260804-212423` 对 18 个 native-valid 的 6–11
   current/prev/bak 成员先验证后全部移动，官方 UI 的 #7–#12 均显示内容，
   并通过官方路径加载 index 6；`GAME-SMOKE/20260804-212602` 冷启动时以
   `legacySources=0 / moved=0` 幂等通过，再显示相同六格并加载 index 11。
2. 对 AutoFishing 假阻断，正式 runner 已按 `doloc-save-{index}.data`
   找到当前 1.00 文件族；本轮没有再把旧名当 current，也没有对 live
   Steam AutoCloud 文件执行迁移或恢复写回。
3. 对内部索引保护，`GAME-SMOKE/20260804-212108` 使用玩家真实
   `ea-playtest-doloc-archive-10-bak.data` 的隔离副本时，由原生
   `FixArchiveIndex` 证明它不属于 target index 10。产品在第一次移动前
   拒绝整个族，保留 18 个旧名、零新目标、native 6 和 owner cleanup；
   这正是获准的 fail-closed 行为。随后使用每个索引自身已验证 current
   字节构造完整三成员族，才形成上述两次 PASS。

三次进程都先证明 `LocalSave.cloudDirPath` 指向隔离 SAVE 根；两次 PASS
均通过 official UI/evidence、`SaveLoaded`、process/fatal、profile/source/QA
清理。live 旧源的 length/SHA-256 前后不变，没有 routine byte backup 或
archive writeback；测试夹具已删除，MoreSaves 最终为 `AbsentNoJournal`，
Player Doctor 为五个 Runtime artifact、零 error、零 warning，共享锁已释放。

2026-08-04 证据更正：上一段沿用了原 `18` 行回执的整体结论，但第五轮
并行审核发现它只有 `16` 个唯一 `SourceName`，把两个 absent identity 错写成
重复 current 名。同一 evidence root 的[修正回执](../../../debug/evidence/GAME-SMOKE/20260804-212602/live-legacy-source-state-correction.json)
现以 18 个唯一名称给出准确口径：16 个实际存在的 live 旧源
length/SHA-256 前后不变，index-10 `prev` 与 index-11 `bak` 前后都不存在；
仍未创建 routine byte backup，也未执行 archive writeback。这条追加更正取代
上一段对原回执形状的解释，但不改写原观察或 disposable 迁移 PASS。

产品对“完整且内部索引正确的旧族”的发布验收已关闭。玩家真实
`ea-playtest-doloc-archive-10-bak.data` 仍原样保留，但其内部索引不符；
在“不改写存档、不加恢复 UI”的既定设计下，它需要用户另行决定丢弃、
替换或专门恢复，不能通过放宽产品校验自动迁移。这是局部玩家数据决策，
不重新打开 MoreSaves 0.6 实现门。

## 2026-08-05 用户修订：问题 1 的当前 0.6.0 边界

用户反馈（保持本轮编号顺序）：

1. 第十槽当前存档没有问题，异常的是旧备份
   `ea-playtest-doloc-archive-10-bak.data`。MoreSaves 不应因为一个不会被
   普通列表、加载或保存流程读取的旧备份内部编号不符而阻断全部 6–11 槽。

截图文字转录：

- `ISaveSlotsApi` 先不处理：0.6.0 不删除或修改兼容服务，现有所有权互斥
  保持不变，也不重新设计兼容 API。
- 本轮只把 MoreSaves 迁移器收敛为官方语义的启动期零内容移动；配置菜单、
  热卸载、750 ms 重试和更长期生命周期简化移入后续路线图。
- 对每个旧角色文件：源不存在则跳过；目标已存在则不覆盖并保留旧源；否则
  `File.Move`。移动抛错时本次不公布 12 槽，下一次启动按剩余旧文件继续。
- 不再需要 SHA、内部编号校验、整族预检或回滚 journal；同目录移动完成后只
  验证“源已不存在、目标已存在”。

审查分析：

- 当前反编译代码已经给出官方转换的实际语义：循环中的旧 current、prev、
  bak 分别映射到 `GetDataFullPath(index)`、`.prev0`、`.bak`，并只在目标不存在
  时移动。该转换按文件角色与名称工作，不先解密存档，也不调用
  `FixArchiveIndex`。
- 因此，上一版“每个 current/prev/bak 都必须证明内部编号属于文件名槽位”的
  策略是 DTMAPI 额外增加的完整族策略，不是官方 1.00 文件迁移语义。用户明确
  撤回这项额外策略；先前问题 3、旧保护门和三次旧 player fixture 仍是历史
  证据，但不再定义当前 0.6.0 迁移行为。
- 当前实现边界是：MoreSaves 获取原有 ProductNative owner 后，通过原生
  `GetDataFullPath` 确认唯一 SAVE 根和精确 `doloc-save-{0}.data` 格式；在发布
  12 槽前逐文件处理 6–11 的旧 current/prev/bak 名称。它不读取、解密、散列、
  修复或改写文件内容，不修改 `convert_data_100`，也不在 `LocalSave` 构造前
  打 Harmony。
- 目标已存在不是失败：目标和旧源都保留，迁移继续。移动失败或移动后没有
  形成“源缺失、目标存在”时，本次激活保持 6 槽并释放 owner；已完成的改名
  自然表现为源缺失，下次启动可继续，无需 flag、journal 或整族回滚。
- 异常 index-10 bak 按官方角色迁移后成为 `doloc-save-10.data.bak`。它不会被
  普通存档列表或加载路径选作 current；原生 `DeleteGame(10)` 会在用 current
  替换 `.bak` 前先删除已有 `.bak`，所以它不再阻断第十槽当前存档。
- 数据分类为启动期官方文件名角色迁移（`ArchiveMutation`），不是会在原生
  `SaveGame` 前推进的普通 gameplay sidecar 状态。自动测试必须使用受管临时
  夹具；本轮不把玩家 Steam AutoCloud 根用于迁移验证。

当前验收点：

- 无旧文件或 SAVE 根尚不存在时成功且不创建目录；
- 6–11 的每个现存旧角色文件可独立迁移，任意内容或内部编号都不被读取；
- 已有目标永不覆盖，旧源保留且不阻断其余文件；
- 中途失败不公布 12 槽，已完成移动不回滚，下一启动幂等续完；
- 原生格式或 `GetDataFullPath` 的正式文件名/SAVE 根不符合预期时拒绝运行；
- `ISaveSlotsApi` 兼容服务、双向所有权互斥和 6/12 发布/恢复生命周期保持不变。

本追加段取代本记录“问题 3”作为当前 0.6.0 实现边界；旧段落与证据不删除，
以保留决策演变。配置菜单、热卸载和通用兼容 API 设计明确排除在本轮之外。

### 聚焦验证结果

- `MoreSavesArchiveMigration` 已从 `559` 行收敛为 `260` 行；生产源码不再含
  `FixArchiveIndex`、内容读取、SHA、fingerprint 或 rollback 实现。
- MoreSaves focused Unit 通过，覆盖无源/缺根 no-op、18 个角色独立移动、
  index-10 异常 bak 的不透明内容移动、目标存在继续、中途失败后续跑、移动后
  报错、后置条件失败、格式/文件名/单根拒绝，以及迁移失败不公布 12 槽并释放
  owner。
- PowerShell 7 与 Windows PowerShell 5.1 均通过 MoreSaves source/policy/compat
  聚焦门；Catalog/保护契约双宿主通过。正式 Author SDK 生成 `1.0.1` 包
  `3021E7340C30BC71727301E61FE02D0DC7E7070782740AD7EBF6A760C853F571`，
  entry DLL 为
  `90EBC85A2B8154F8A73D3C62DCFCCA06FC5338D90581E80EB5DEDDB9C20C0EC2`。
- 本轮没有安装或启动游戏，没有读写 Steam subscription、官方 `MODS`、玩家
  SAVE 或 sidecar，也没有获取 Runtime lock。旧 2026-08-04 disposable player
  PASS 不适用于新字节；按用户“完成必要测试后暂停”的边界，玩家进程复验、
  完整 Release 与最终候选重建均留待恢复路线图后执行。
