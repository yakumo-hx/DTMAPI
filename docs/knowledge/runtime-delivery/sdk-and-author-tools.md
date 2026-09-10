# 作者工具与示例的交付边界

本文合并作者文档和 SDK 历史中的有效设计经验，不复制现行字段手册。当前入口查 [platform author delivery](../../architecture/platform-author-delivery.md) 和 [current-state](../../onboarding/current-state.md)；阅读快照记录见 [清单](../../archive/migrations/20260908-workspace.json)。

## 作者文档测试先验证作者实际会做的动作

[July 1 artist/author/prototype review](../../archive/updates/2026/20260701-0005-author-docs-oilfloater-review-closure.md) 实际解析 guide 的 11 个 JSON block 和 oilfloater 的十个 JSON，检查 48 PNG/两 WAV；这是作者指引的有效校验，不需要构建 Runtime 或启动游戏。prototype 的 0 权重、0 产量与未核实 document fields 需要明确意图，目录形状正确不能证明可正式发布。

[PNG direction clarification](../../archive/updates/2026/20260701-0004-author-docs-png-frame-direction.md) 通过实际 Hatch 包证明只有一套 44 帧，没有左右后缀；模板没请求的额外 PNG 通常不被使用，不能因文件多就报错。静态模板存在也不是每个模板都有 Hatch 同等级运行证明。

[sprite_size correction](../../archive/updates/2026/20260701-0006-author-docs-sprite-size-collider-guidance.md) 绑定 `23762374_public_C416D4`：`AnimalLevelData` 读取 sprite_size，`AnimalInfo.PostResolve` 生成 collider/emotion offset，`Animal.OnRender` 传给 renderer collider。DTMAPI PNG name mapping 不读取这个字段；PNG canvas、视觉 bbox、interaction core 和容量占用不是同一参数。旧 prototype 指引曾把 canvas 不一致当问题，后续已纠正为可能有意，应先保留模板尺寸再按实际手感调整。

具体当前 schema、模板范围与作者第一条测试路线只链接 [动物作者指南](../../../author-docs/content-packs/custom-animal-json-png-wav.md)。旧 prototype 的待发布修复归该产品需求，不因文档迁移自动创建新 Mod 或通用 validator。

## 官方 JSON 工具：验证器也会引入回归

2026-07-14 的 v0.6 审查以 native `23762374_public_C416D4` 为基线，发现精确 `$type`、交换商店表名小写、目录身份及导入再导出的数据保留问题。它只证明静态源码与表结构结论，并未运行游戏；远程 JSZip/FileSaver 和未转义 DOM 风险也不能只靠“源码可读”排除。经作者授权的 07-15 v0.7 分叉固定依赖并去掉远程 FileSaver，用合成浏览器测试覆盖字段、注入和文件保留；该授权未把第三方 HTML、素材或整个分叉自动变成仓库 MIT 内容。分叉原件与交付包仍属本地证据。

作物工具曾被建议同时生成种子/收获道具，但用户随后明确选定：作物项目只生成 `plant_tbseed.json`，相关道具由“新增道具”项目分别建立。早期审查的相反建议已被替代。第一条记录的表单编辑与其余未知记录原样保留是当时选择的边界，多记录可视化编辑属于新功能。

真实 Touhou Fumo ZIP 进一步揭露了合成夹具漏掉的导入回归：单层外包目录未展开；以 `Content` 第一段分组而非表所在目录；把导入路径当新建 ID 校验；重定位保留表与新生成表并存；商店季节数从 99 被重写成 1。原包的 `item_tbtiem.json` 拼写错误属于原 Mod，误拒路径和重复表属于分叉新引入问题。根因证据通过只去外包前缀、九个内部文件哈希完全不变来隔离，无需游戏或改写第三方原包。

这些已由 `docs/updates/2026/20260715-0007-official-json-tool-v06-repair-clone.md` 关闭：旧 r2 包应保留为失败证据，不能作为待修清单或正式候选复活。可迁移的测试原则是：导入路径与新建标识分别建模；已知近似表名显式阻断并建议、不静默重命名；按完整来源目录替换；未知内容的保留不能制造双份有效表；真实失败最小结构应转成不含第三方素材的合成回归。

来源：`docs/reviews/code/2026/20260714-0003-third-party-official-json-authoring-tool-audit.md`、同日 Update `0006`，07-15 Updates `0002`、`0006` 及 Review `0004`。

## SDK 0.1.0 的已实现边界

07-15 Batch 3 已交付自包含 .NET 8 Windows x64 CLI；生成的游戏程序集仍是 `netstandard2.0`。当时 SDK 固定 Runtime 0.5.5、固定 Abstractions/编译器/NETStandard 引用，离线编译不读取玩家 DLL。确定性包覆盖跨根目录、时间和空 PATH/NuGet 环境；PE 产品版本关闭源码修订注入，避免只提交 Git 就改变交付字节。固定旧哈希属于该版身份，不能替代当前 SDK 目标目录。

`manifest.json` 拥有身份/版本/最低版本/依赖；`info.json` 为投影；作者元数据不是部署授权。Doctor 使用 PE 元数据，只读且不 `Assembly.Load`。SDK 0.1 的严格三段数字最低版本是作者工具输入约束，不能据此缩窄 Runtime 对旧 manifest 的兼容解析。部署、更新和撤回依赖包内收据与包外日志同时吻合，异常目录、额外文件及身份/哈希漂移拒绝操作；外部源覆盖状态也不是所有权收据。

Workshop 选择的 native `23762374_public_C416D4` owner 是 `ModManager.GetSubscribedMods` 与 `GetSubscribedModDirectory`，目录存在或旧 `mod_infos.json` 不足以证明订阅。早期 `Awake` 抓取过早得到 False/0，已改成 GameManager 初始化后的既有 FirstFrame 单次抓取；已有 ReloadMods Postfix 再抓取。四种源模式及失效路径的当前合同应回到 author-sdk 文档和 `docs/architecture/platform-author-delivery.md`，不维持第二份规则。

显式作者会话当时只允许 Audio 替换做完整预解析、载荷验证、提交前再哈希与 owner generation 原子切换；无效输入保留 last-good。CodeMod、官方 JSON、Custom Animals/PNG/AssetBundle 一律要求重启，原因分别是 Mono 程序集生命周期、原生表事务缺失、Unity/native 缓存及活实例未具备完整撤回 owner。无 descriptor 的玩家 Runtime 不创建监听、计时器、watcher 或文件轮询。启动首个空标题事件只有一次例外，收到解析请求后同次/后续回标题即关闭会话；不能扩成永久后台会话。

07-15 的 Author matrix PASS 与通用 TitleIdleResourceGrowth 的 Failed 可同时成立：测试主动替换 generation 违反后者“无变化”前提。应使用明确匹配的断言，保留该失败解释，不为了聚合绿灯重复游戏。已关闭的启动错误、未处理请求的启动尝试与真正有效的变更会话须区分；这些分钟级 smoke 不是 GC 闭环。

来源：`docs/reviews/code/2026/20260715-0008-batch3-author-sdk-source-reload-boundary-review.md`、`docs/updates/2026/20260715-0016-batch3-author-sdk-preview.md`。
## SDK 恢复状态与包引用的历史缺口

07-28/29 的多轮审查发现两类普通 green suite 没覆盖的组合：可 Base64 解码但语义无效的前态快照，以及产品 A 留 pending marker 后产品 B 合法修改全局 source-state、再被 A 的整文件恢复覆盖。修复复用现有 journal 和 game-root lock：移动或写回前完整验证快照语义；pending localInstall 构成持久写入屏障；读状态及匹配目标 recover 仍可用；recover 只覆盖可证明的精确前态或事务派生后态。未知漂移保留，不猜测多 pending 恢复顺序。

另一处外层误判把只读 status 的 `success=true` 当安装成功，但它也可能返回 `RecoveryRequired`。已修成只接受精确 `CommittedLocalDevelopment` 及有效 deployment/source tree digest，组合测试同时制造 crash 与丢失/损坏报告。只读对账可以消除不确定性，不能重放 mutation。检查应盯产品注册状态；基础 Runtime 收据可能合法刷新，不能误要求整个外层 receipt 字节不变。

这些修正曾只影响 SDK 或 installer/package，没有改变游戏 DLL 时，旧游戏/GC 证据无需重跑；新 player tree 则必须重新计 hash。07-29 Review 0003 已接受两项发布阻断，不再把 0001/0002 的 open 状态当现行任务。

08-04 的 SDK202 是夹具来源错误，不是 policy 过严：把当前 `24456188` DLL 放进临时游戏却标 `23762374`，精确检查应失败。未改的产品仍可保留旧 build policy，经 Drift 激活与“在新游戏重编”是两回事。旧 policy 的构建测试使用私有冻结 `23762374_public_C416D4` raw bytes，或明确给出 exact input unavailable；不得伪造 build、批量重签或把官方 DLL 打进 SDK/Git。基础 Strict 的 Git-only 离线目标与 Advanced 的显式合法原生引用前提须分开。后续工程必须把所选引用目录传到底层 builder，不能中途回退当前安装游戏。

来源：07-28 Review `0004`、07-29 Reviews `0001`、`0002`、`0003`；08-04 Review `0005`。后续实施 owner 为 `docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`，本页仅保留根因。

## 现行作者交付目标只链接领域 owner

`docs/architecture/platform-author-delivery.md` 是 active 目标设计，不是已发布能力证明。它已接受统一规范化编译输入、CLI/IDE 同一构建语义、显式 Debug/Release 和符号身份、官方 Local 单事务、真实来源/磁盘/加载状态分离、独立 SDK target 与会话版本、复用 Doctor 和分层作者验收等目标。实现状态只读 `docs/planning/platform-next/status.md`，任务定义只读 tasks；本知识页不再复制目标清单。旧 SDK frozen 载荷不能原地改写，新 target/模板显式迁移；候选目标也不是发布承诺。
## 0.6 冻结记录已暂停旧 SDK 新部署入口

08-02 的大型路线最终在 08-09 冻结；它记录了 07-15 部署实现后来被暂停这一关键变化。公开 `deploy`、`update`、`install-local` 和 source local select 应先返回 SDK003，不读取包或改写游戏/状态。旧 `<game>/Mods` 只保留既有部署的 status、recover、withdraw/clear，不再是玩家发现源。旧 schema 1/2 日志可读取并恢复成 3，不等于作者现在可以新建部署。外层 Runtime installer 也不应继续重放旧产品部署事务。

冻结 SDK 0.1.0 的 08-06 ZIP 身份为 126,983,587 bytes、SHA256 `8CA3E7A350A8A0C5E9061C26C8B664F8C3239B3BAF9F346D3F12D7CB60766559`。该数字仅用于历史取证。公开 gate 必须用实际打包 CLI 的黑盒测试验证；只运行内部兼容引擎测试不能证明作者入口正确。后续 official Local 单事务归 [作者交付 owner](../../architecture/platform-author-delivery.md) 与 platform-next 状态，不能仅去掉 SDK003 就恢复部署。

SDK 编译 API target、Runtime 最低版本、当前 Runtime 上限和 Advanced 原生引用身份是独立轴。0.5.5 API target 不应误作最高 Runtime 上限；反过来，当前游戏 DLL 不能冒充历史 `23762374` policy 引用。构建使用明确的引用目录并传到底层，同一 policy 的私有夹具可在一次构建中复用；SkipBuild 不应重新建立夹具。固定 Abstractions 只取显式 override 或有界的已知冻结位置，不在任意临时目录、工作区或玩家 DLL 中猜选。

来源：`docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md` 全文及末尾冻结交接。该历史 Update 不再拥有新任务；本页较早将其称为“后续实施 owner”的句子仅指当时的实施去向。

## 选择真实工具链，不把 resolver 错误当源码失败

[7 月 10 日](../../archive/updates/2026/20260710-0007-dotnet8-toolchain-resolution.md) 本地 .tools/dotnet 可用，resolver 却见 PATH 任意 SDK 就提前返回。应按仓库 common 的 .NET 8 SDK/runtime 组合选择，不继承旧 Major roll-forward 捷径。首次五秒期限中止只是测试驱动超时；游戏 netstandard2.0 与桌面宿主也须分开。

7 月 28 日另一次外层输出丢失曾重放 mutation；既有 journal 应持有复合事务/精确前态，结果不明只读 reconcile，不能自动再 install。假游戏根可验证跨产品 state 和错产品反例；无需游戏。8 月 6 日实际 SDK ZIP 的四个暂停命令需要黑盒 SDK003、非零退出及 package/game/state 未变，内部 RunPublic PASS 不能防未来误打旧 Author DLL。

来源：[local-install 审查](../../archive/reviews/code/2026/20260728-0004-dtmapi-055-prerelease-route-completion-audit.md)、[打包 CLI 后继审查](../../archive/reviews/code/2026/20260806-0002-dtmapi-060-tenth-five-slice-parallel-review.md)。当前构建准备和图范围以脚本为准，本文不再同步 SDK 版本/路径表。
