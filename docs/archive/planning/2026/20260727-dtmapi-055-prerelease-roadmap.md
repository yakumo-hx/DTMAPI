# DTMAPI 0.5.5 发布前路线图

> 日期：2026-07-27  
> 状态：`implemented / selected-candidate-accepted / publication-staging-open`；2026-07-30 已暂停新版 MoreEquipmentSlots 与通用受保护存储 API
> 当前授权边界：使用 owning Update 冻结的既有 30 文件候选；完整 Release、候选包审计及真实旧 Workshop Manbo/MoreEquipmentSlots 兼容目标均已有证据。
> 当前硬停止：不从当前 HEAD 重建 Runtime，不重复完整 Release，不上传 Steam，也不启动发布后的订阅包复核。
> 生命周期记录：[20260727-0001](../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)

## 1. 发布目标与边界

本轮只准备 DTMAPI Runtime `0.5.5`。Runtime 先发布；现有 Advanced 产品随后按独立波次更新为 `1.0.0`。产品波次不反向阻塞 Runtime 候选冻结，但 Runtime 必须继续兼容尚未更新的现有订阅产品。

本轮不顺带实现：

- Content Host G7；
- CustomAnimals 或 AudioReplacement 的下一版本基座化；
- AnimalPack、Oil、Mine 的后续产品工作；
- ShellCrab 发布；
- DebugConsole 视觉/UX 重写；
- BGM。

冻结候选的完整 Release 和候选目录玩家包审计已经完成。实际 Steam
上传目录的同步与复核、Steam 上传和上传后订阅实物复核仍是独立发布阶段；
只有新的明确授权才改动上传目录或开始 Steam 上传。

## 2. 已冻结基线

- Batch 0–6、G2 synthetic fixture 与十一个 Advanced/ProductNative 产品已经完成各自的有界验收；MoreEquipmentSlots 已实现但 1.0.0 验收与发布延期。
- 0.5.5 不再继续拆产品，不新增第十三个 Advanced 产品，也不开放通用 Advanced 作者路线。
- 冻结的公开 ID、WorkshopID、公共 ABI、manifest 身份、配置键和序列化键不得在发布前命名审查中改动。
- 历史里程碑收据不进入默认全量门；验证按本次变更风险选择聚焦入口。
- 旧订阅包是兼容基线，不因当前源码已经重构就被重编替代。

## 3. 当前实施顺序

### 步骤 1：收掉两个 P2 真相漂移

> 完成状态（2026-07-28，独立复核接受）：源码、配置、诊断、smoke 投影和 Unit 已移除旧 bucket 调度真相；AutoHarvest 当前合同已改为 `ApiDemandSample` / `NeverPublish` / 零真实消费者，历史 Phase 0 receipt 仍可复现。独立复核没有 P0/P1；两项文档措辞 P2 已在接受前修正。

1. 清理已经没有调用者的 `UpdateGameBridgeFeatures` / bucketed-update 旧路径，使配置、Doctor、状态和诊断只报告当前真实调度模型。
2. 把当前 Phase 0 机器合同中的 AutoHarvest 修正为 `ApiDemandSample` / `NeverPublish` 研究样本，与 Catalog 一致；不改写历史基线，不删除冻结公共 API。

验收：

- 旧 bucket 路径没有生产调用者、配置入口或误导性健康状态；
- Catalog 与当前 Phase 0 合同对 AutoHarvest 的身份、发布资格和 owner 分类一致；
- 只运行受影响的 source/unit/contract 聚焦门，不启动游戏，不运行完整 Release。

### 步骤 2：一次有界的内部命名与可读性审查

> 完成状态（2026-07-28）：清单只包含一个 Core internal CLR/file name、一个 Compatibility adapter 文件名和一处 broker-proxy owner 注释。公共 ABI、MemberRef、产品/Workshop/manifest 身份、配置文件/键、DataContract 名称、sidecar、receipt、诊断 ID 和行为均未改变。

只审查 private/internal 类型、成员、文件和注释，优先处理会影响 0.5.5 维护与故障定位的误导命名。禁止借机修改：

- public 类型或 MemberRef；
- provider/Mod/Workshop 身份；
- manifest 字段语义；
- 配置键、存档 sidecar、事务收据或序列化键；
- 已冻结行为。

每个实际改动按所属组件运行 build 与聚焦 Unit；没有 Hook、生命周期或玩家行为变化时不启动游戏。

### 步骤 3：Author SDK 安装事务尾项

> 完成状态（2026-07-28，同轮独立复核接受）：前三次独立复核依次否决了外层双 owner、仅同进程恢复/损坏报告重放，以及 journal 终态写后误报。外层现只调用一次 `install-local`，模糊报告只用只读 `install-local-status` 对账；Author SDK 以 deployment journal schema 3 持久化 `localInstall` 前态和 owned paths。对 `588cbcf6` 的续审又发现，损坏但可反序列化的 schema-3 marker 仍可能被误称为可重试；`f384d233` 已让 `HasRetryableLocalInstallAuthority` 完整验证 phase、路径、previous/next、hash 和 snapshot Base64，并为结构损坏/不可读 marker 增加 `authority-unknown-fail-closed` Unit。随 SDK 发布的 journal JSON Schema/README 已同步到 schema 3，并由制品 parity 门与仍为 schema 2 的 receipt/marker 明确区分；`add0dba3` 修正 Windows PowerShell 严格模式下的 nullable-schema 解析。新 SDK 的 Release/Unit、双宿主发布检查和双宿主真实 Zoom 本地安装事务矩阵通过；同轮独立复核为 `P0=0 / P1=0 / P2=0`，接受 Step 3。

把安装顺序调整为：先完成 Catalog preflight，再由 Author SDK 在单一锁和单一事务 owner 下完成不可变输入校验、source preflight、deploy/update 与 source selection。任何失败都不得把旧 managed destination 或其他产品的 source selection 留在错误状态。

验收：

- PowerShell 5.1 与仓库标准 PowerShell 主机均通过；
- source 缺失、损坏、版本不符和中途失败都保持旧目标可用；
- install/check/status/uninstall 的既有所有权语义不变；
- 只跑安装事务聚焦矩阵，不运行完整 Release。

### 步骤 4：RC 截止点与兼容清单

> 完成状态（2026-07-28）：公开元数据权威集合为精确 DTMAPI 查询加三项历史兼容身份，共 22 项；四个查询均已分页到空页，宽泛查询的 37 项 union 明确只是发现噪声。第二页新增的 `3749143385` 是既有审查已确认的纯 JSON 官方内容 Mod，不是 DTMAPI 消费者。订阅缓存现有 44 个目录，retained ABI 仍是 11 个第一方加 4 个外部消费者；同一 gate 现会按精确 hash/reference version 解析四个外部 DLL 的全部 23 个 DTMAPI MemberRef。Runtime `0.5.2-alpha` 已从可变 Steam 缓存固化为源码/发行树外的私有不可分发 archive，含 46 文件 payload、内部 `SHA256SUMS` 和独立 archive SHA 校验入口。Manbo 的 7 文件订阅树与后续第三存档 `NoNativeSave` 方案已经冻结；当前玩家 `MODS` 中另有不同字节的 stale Local 副本，步骤 6 必须先隔离并证明 Workshop 来源，不能把本地副本的成功当兼容证据。本步骤没有启动游戏或改动订阅/玩家目录。

1. 刷新截止日可公开取得的 Workshop DTMAPI 消费者、外部 DLL 和订阅目录清单。
2. 将新增的真实消费者加入既有 retained ABI 矩阵；不新建第二套 ABI 或收据体系。
3. 冻结本轮发布名单、版本投影、回滚包和候选来源。
4. 冻结旧订阅 Manbo 的精确制品、hash 和验收方案，而不是用当前源码重编包替代；绑定最终候选的实机验收在步骤 6 执行。

Manbo 最小兼容门：

- 输入为 Workshop `3746319981` 的订阅实物 `Yuuka.DTMAPI.ManboCardboardAudio` `0.1.0-dtmapi`；
- DLL/tree hash 与 retained/subscription 清单一致；运行前隔离当前 stale Local duplicate，并证明只有精确 Workshop 来源被选中；
- Runtime `0.5.5` 接受其最低版本 `0.5.2-alpha`；
- `IAudioReplacementApi` 解析成功，`Entry` 和 WAV 注册成功；
- 没有 `MissingMethodException`、`TypeLoadException`、`FileLoadException`、provider/manifest/Loader/Fatal 错误；
- 返回标题和退出清理通过；
- 使用第三存档、`NoNativeSave`，先证明相关存档 archive 未变，再进行任何非存档测试资产恢复；
- “运行不报错”不要求触发真实纸箱音效。

这是步骤 6 在最终候选上执行的一次小型兼容验收，不是 L0–L5、长测或完整 Release。步骤 4 只冻结输入和方案，不提前产生“最终候选已通过”的结论。

### 步骤 5：构建并冻结 0.5.5 候选

> 完成状态（2026-07-28）：首轮候选的独立复核发现 private rollback override 可用“精确仓库根”或外部路径中的 junction 绕过非分发边界，步骤因此重开。修正后，仓库同路径/子路径和任一现存 reparse 祖先都会在读写前被拒绝，PS7/PS5.1 四项负向测试通过；候选已从新的 clean source commit `3f5cb3268f3e786a13594fae44e542c0c9d2657e` 全量重建。Runtime/API 为 `0.5.5` / file version `0.5.5.0` / compatibility assembly identity `0.5.3.0`；后续波次的十个现有 PublicWorkshop Advanced 产品源码、发布文本和候选包均投影为 `1.0.0`，Manbo 保持旧订阅 `0.1.0-dtmapi`。Runtime Workshop 目录、五个 mandatory 程序集、dormant-shipped Compatibility Host、Player Doctor、Author SDK 和十个独立产品 ZIP 已重建并记录 hash。该轮包内 ABI 检查为零删除、11 个第一方加 4 个外部 retained 消费者、外部 MemberRef `23/23`；Runtime 与十个产品包的 QA/test/negative/fixture 排除扫描通过。同一审查轮次又纠正了首轮程序集 hash 记录，逐值复核为 `P0=0 / P1=0 / P2=0`，当时的 Step 5 已接受。完成度审计后的精确外部消费者准入改变了 Runtime，现行替代候选见步骤 7 当前状态和 owning Update；十个产品 ZIP 未变。未运行完整 Release、未上传 Steam。

在干净、可追溯的准确 HEAD 上：

1. 统一 Runtime/API 版本为 `0.5.5`；
2. 统一将准备后续发布的现有 Advanced 产品投影为 `1.0.0`，但本轮不上传产品；
3. 重建 Runtime、Bootstrap、Core、GameBridge、可选 Compatibility Host、Author SDK 与选定产品候选；
4. 生成 Catalog、ABI、包/hash、Doctor、安装和回滚所需的既有产物；
5. 证明玩家包不包含 QA、测试 Mod、负向 fixture 或未授权产品。

候选一旦进入最终聚焦验收，除修复验收发现的问题外不再加入重构。

### 步骤 6：最终候选的聚焦发布前门

> 完成状态（2026-07-28，原候选独立复核接受）：步骤 5 当时冻结的五个 mandatory Runtime DLL、ActionSpeed `431627...2666F` 和 AutoFishing `B39F98...4B6A0` 通过了聚焦门。第三存档 no-demand 在 300 帧预热后完成 10,000 帧，optional demand/updater/Hook 安装/文件与反射工作均为零增量；旧订阅 Manbo `3746319981` 的 7 文件树和 DLL 精确匹配，隔离 stale Local 副本后唯一从 Workshop 加载，Entry、WAV 注册与 ready、标题/退出及 `NoNativeSave` 均通过。active-GC `prerelease-055-candidate-20260728-r14` 完成 ActionSpeed 8 个选定阶段及 AutoFishing L1/L3/L4/L5；12 次 smoke 都在清理前证明玩家 archive 和 committed sidecar 未变、没有归档写回且进程退出。ActionSpeed 的 resource snapshot-build counter 在每阶段均为 `35 -> 35`；AutoFishing 四阶段的 owner/input/event/API/demand root 与 Runtime record 均保持不变。产品树、部署 journal、Author source-state 和临时隔离标记均精确恢复，既有 ActionSpeed 42 项、AutoFishing 25 项 recovery ledger 未被候选收养或删除。首轮复核指出外层产品/journal lease 只能处理同进程异常；现已把同一 lease state 提升为逐移动前落盘的 schema 2，并提供只接受本 worktree 遗留 Runtime lock 的独立 `-RecoverOnly`，进程式测试覆盖原目标、原 journal、候选发布和四个恢复移动后的中断。r14 原 `stage.json` 仍如实保留旧校验器的 `600` 秒字段；`auto-fishing/final-validator-reevaluation.json` 只读绑定未改动的 raw/stage hash，并用最终 `190` 秒上限重算通过，不把新解释伪装成原 receipt。同轮复核确认修正后 `P0=0 / P1=0 / P2=0`，接受提交 `8fb40036`。完成度审计改变 Runtime 后，外部消费者、Manbo、no-demand r4 和 active-GC r15 已全部针对 `c7e1ec2f3697` 重跑通过。该门未运行完整 Release、历史全 ladder、强制 GC 或长时 soak；Unity/Mono live allocation counter 仍不可用，因此结论只支持既定“显著降低 Unity GC 压力、一定程度降低闪退概率”文案与现行候选一致，不表示全部 GC/闪退问题已解决。详细非验收尝试由同一 prerelease Update 记录。

必须通过：

- Catalog/身份/版本投影；
- retained public ABI 与 provider；
- Doctor、Manager、安装状态和包/hash；
- 玩家包无 QA；
- mandatory Runtime 的产品代码零遗留；
- no-demand 时可选 Compatibility Host 保持 dormant；内容功能无 demand
  时不安装 Hook、不进入逐帧工作；mandatory GameBridge 仍允许完成空 feature
  注册和生命周期 refresh；
- PowerShell 5.1 解析与聚焦安装事务；
- 旧订阅 Manbo 兼容门；
- AutoFishing 与 ActionSpeed 当前候选的 active-gameplay GC 聚焦门。

GC 门仍分别测试两个玩法域，不预设它们争夺同一个 Animator：

- AutoFishing：Fishing Ready/Cast/Pull；
- ActionSpeed：Tool/Interact/Eat/Continuous-use；
- 关注 `1x`、启用但不加速、常用加速、高倍率、禁用恢复和标题循环；
- 比较单位动作与单位时间的短期分配、回调、遗留 owner/state 和高速状态切换压力。

不为该门默认重跑 L0–L5、完整 Release或长时 soak。对外结论固定为
[0.5.5 Workshop Update Copy](../../../releases/0.5.5-workshop-update-copy.md)
中的三语言精确文案，不在路线图中建立第二份可编辑副本。

该句不等于“解决所有 GC”或“消除闪退”。若最终候选的聚焦结果与这句结论冲突，停止发布准备并回到用户决策；不得静默保留一条证据不支持的文案。

### 步骤 7：形成临时发布前冻结点

> 当前状态（2026-07-28）：完成度审计所列 Author SDK 快照/prepare、
> 证据保留、三份外部消费者真实加载和文档真相缺口已完成实现修正。
> 由于精确外部消费者准入改变了 Core/Runtime 字节，Runtime 已在
> `c7e1ec2f3697` 重新冻结，Author SDK 已在 `ed85e11a` 重新冻结，十个产品
> ZIP 保持不变；外部消费者、Manbo、no-demand 和 active-GC 均已针对新
> Runtime 重跑通过。owning Update 仍为 `implemented`，候选仍为
> `provisional`；独立聚焦复核通过前不得写为
> `READY-FOR-FINAL-RELEASE`。

完成以下交付后先写入临时冻结状态，并将 owning Update 保持为 `implemented`：

- 精确 HEAD、clean tracked tree 与工具链；
- Runtime 0.5.5 候选及每个构建产物的 hash；
- 安装、卸载和失败回滚输入；
- Runtime 发布名单与后续产品波次名单；
- 三语言 Steam 文案和逐语言手工粘贴/回读清单；
- 聚焦门与 Manbo 实机证据链接；
- 已知风险、回滚点和明确延期项；
- 确认没有运行完整 Release、没有上传 Steam。

临时冻结后最多进行三轮相互独立的路线整体、代码和文档审查。任何修正都会使受影响的候选或证据失效并回到相应步骤。只有全部复核通过、修正完成且精确候选重新一致后，才可写为 `READY-FOR-FINAL-RELEASE` 并把 owning Update 升为 `verified`。这里的 `verified` 只表示本预发布路线通过，不表示完整 Release 或 Steam 发布已经完成。

三语言精确文案及唯一文字权威见
[0.5.5 Workshop Update Copy](../../../releases/0.5.5-workshop-update-copy.md)。

## 4. 后续发布波次

本轮次序替换 2026-07-13 决策记录中的旧波次顺序；旧记录保留为历史，不重写。

| 波次 | 发布内容 | 约束 |
| --- | --- | --- |
| R0 | Runtime `0.5.5` | 最先发布；Manbo 与 MoreEquipmentSlots `0.3.1-dtmapi` 两个保留旧订阅输入均须兼容 |
| R1 | AutoFishing `1.0.0` | 单独发布与观察 |
| R3 | 其余八个现有 PublicWorkshop Advanced 产品 | 同一发布窗口、各自独立包和回滚 |
| 延期 | MoreEquipmentSlots `1.0.0` | 本轮不上传；Workshop `3744059735` 保持 `0.3.1-dtmapi` 并作为 R0 旧包兼容输入 |

R3 的八个产品为：

- ActionSpeed；
- OneActionComplete；
- FishBreedingAssistant；
- AnimalHusbandryProgress；
- MoreSaves；
- ChestLocatorEnhancer；
- Zoom；
- DebugConsole / Y 键控制台。

订阅人数只是本轮波次决策的产品依据；仓库当前没有可审计的订阅人数数据，不把它伪造成 Catalog 权威。Manbo 与 MoreEquipmentSlots 均保留旧订阅包参与 Runtime 兼容验证；MoreEquipmentSlots 不进入本轮 Advanced `1.0.0` 波次，也不由其他产品补占该波次。

通用受保护存储 API 同步暂停。0.5.5 不声明、不实现也不承诺
`IProtectedStorageApi`；现有冻结 `IEquipmentSlotsApi` 只服务精确旧 DLL，
不能被解释为通用存储能力已经准入。

## 5. 当前发布阶段

已经完成且不再重复：

1. owning Update 记录的精确冻结候选已通过一次最终、从头开始的完整
   Release；
2. 同一候选目录已通过 PowerShell 5.1/标准主机、空格/非 ASCII 临时路径、
   离线安装、check/status、uninstall 和 collect-logs 玩家包矩阵；
3. Manbo 在 `GAME-SMOKE/20260730-232836` 对精确冻结候选完整通过；真实
   Workshop MoreEquipmentSlots `0.3.1-dtmapi` 在 `20260730-225518`
   的目标字段通过。后者仅因最终清理临时根时 runner 自己仍占用 stderr
   而记为聚合 `Failed`；此前输入帧告警已恢复且未造成目标失败。因此该
   MES smoke 只记 `partial`，不把它改写成整轮 PASS。

仍需按顺序完成：

1. 获得明确授权后，把实际本地 Steam 上传目录同步为 owning Update 的冻结
   候选，只额外保留该目录已有的 `workshop.json`；当前目录仍是旧构建，
   还多一个 `0_probe`，不能上传；
2. 对同步后的实际上传目录只跑一次玩家包矩阵；无需再跑完整 Release；
3. 明确解除 Catalog 的全局 `releaseStop`，并取得 Runtime 上传授权；
4. 用户手工把三语言文案粘贴到 Steam 各语言页并逐页回读确认；
5. 上传 Runtime，重新下载订阅实物，核对 hash、布局、安装和最小运行
   证据，再开始 R1。

如果实际上传目录不再与已接受冻结候选逐字节一致，就必须把它作为新候选重新
冻结来源、长度、tree/hash 和回滚输入；不得沿用旧候选身份或既有完整
Release 结论。

## 6. 0.5.5 后下一版本

下一版本再设计和准入以下可选官方内容基座；这些决定不授权现在实现 G7：

- CustomAnimals 与 AudioReplacement 可继续由 DTMAPI 官方维护并随 Runtime 分发；
- 没有动物/音频内容需求时，不加载对应实现、不安装 Hook、不逐帧运行；
- 将来可进入按需加载的 G7 Content Host，而不是把领域桥复制回各个内容 Mod；
- 具体物种、概率、经济、产物和音频映射仍归 AnimalPack、Manbo 等内容产品。

AnimalPack 的下一版本目标：

- 合并 Hatch、Mole、Drecko、OilFloater，减少零散 Mod；
- 验证一个 Mod 内多物种的官方 JSON 写法；
- 形成可供其他作者复用的教程；
- 不把具体经济或物种策略提升为 DTMAPI 平台 API。

其他延期：

- ShellCrab：能力验证，明确不发布；
- Oil：官方 JSON 路线保留，稀有掉落与经济以后再定；
- Mine：静态内容继续官方 JSON，独立贴图、取消运行时 2x 与发布工作以后再做；
- BGM：继续作为独立阻断项。
