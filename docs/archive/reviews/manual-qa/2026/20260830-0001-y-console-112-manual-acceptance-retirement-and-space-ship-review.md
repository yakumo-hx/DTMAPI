# Y 键控制台 1.1.2 手测接收、0.3.1 退役授权与旧城守护者静态审查

## Review Header

- Date: `2026-08-30`
- Status: `recorded`
- Scope: Y 键控制台动物/无限燃料玩家手测接收，旧 `0.3.1-dtmapi` Compatibility 约束解除，`space_ship` / 旧城守护者原生生成链静态根因，以及后续机械拆分边界
- Source: 用户对前次审查结论的逐项批注和本轮明确要求
- User constraint: 先更新文档；旧城守护者先做详细代码 Review，不启动游戏、不先尝试修复；随后只实现无限燃料普通 `999` 堆叠，并允许对 `DebugConsoleUi` 做行为不变的机械拆分
- Owning Update: [20260830-0003-y-console-112-stackable-fuel-and-maintenance](../../../updates/2026/20260830-0003-y-console-112-stackable-fuel-and-maintenance.md)
- Prior records:
  - [Y 键控制台语义目录、ProductNative 召唤与技术债复核](20260812-0001-y-console-roadmap-native-owner-review.md)
  - [Y 键控制台 1.1.0 玩家反馈：布局与原生操作根因复核](20260813-0001-y-console-110-player-feedback-layout-native-actions.md)
  - [Y 键控制台 1.1.0 旧城守护者熔断与发布限制复核](20260813-0003-y-console-space-ship-release-limitation.md)
  - [DTMAPI 0.6.0 九家族真实消费者与 Breaking Removal 审查](../../code/2026/20260804-0019-dtmapi-060-nine-family-consumer-and-removal-review.md)

本 Review 按用户本轮反馈顺序保存事实和分析。玩家确认只记为手工验收，
不改写为 `GAME-SMOKE`、自动化存档证据或新的 Steam 发布授权。

## 1. 旧 Y 键控制台 0.3.1 已无使用者，兼容保留限制解除

### 用户确认

- 用户确认旧 `0.3.1-dtmapi` Y 键控制台已经没有任何使用者。
- 用户明确允许解除围绕该旧版本建立的保留限制。

### 当前代码与权威边界

- 旧包的 provider、UI 和七组动作执行器目前仍物理存在于可选
  `DTMAPI.GameBridge.DolocTown.Compatibility` 路径；Catalog 中的
  `retainedArtifact` 仍是过去发布实物的审计事实。
- 当前公开 `1.1.1` ProductNative DLL不调用八个旧 provider 接口，但仍直接复用
  `28` 个 Debug DTO/enum。解除旧二进制消费者约束，不等于这些仍被当前产品
  引用的数据类型已经可以同时删除。
- 2026-08-04 Review 对 DebugConsole 作出的 `KEEP / NO-GO` 是当日消费者审计
  结论；本轮用户事实只取代其“必须为 0.3.1 使用者继续保留”的未来决策前提，
  不改写当日扫描结果。

### 决策

- 精确旧 `0.3.1` provider/UI/action Compatibility 路径的退役限制已解除。
- 本轮不顺带删除这些 ABI、Host、broker 或 DTO。实际删除应由一个独立、可回滚的
  breaking cleanup Update 完成，并同时更新 public API matrix、Compatibility
  构建接线、Loader/Doctor 提示和现存 ABI 测试。
- 该后续清理无需再以“可能还有 0.3.1 用户”为阻塞理由；但必须先把当前产品仍用的
  `28` 个 DTO/enum 内部化或明确保留，不能由“旧接口无人使用”推导出批量删除。

## 2. 动物保存、重载和隐藏产物可用，原计划矩阵按手测通过接收

### 用户确认

- 控制台生成的动物可以完成正常保存和重载。
- Ready 动物可以获得隐藏产物。
- 用户允许把先前 Update 中等待的动物 disposable save/reload/hidden-yield
  项标记为手测通过。

### 记录边界

- 这关闭 `20260812-0001` 的玩家可见动物持久化跟进，不再要求为了同一已确认
  行为补跑一套 disposable 矩阵。
- 这是用户手工验收，不包含本轮可复查的 archive hash、cold-observer receipt 或
  自动 smoke，因此不会新增 smoke-matrix PASS 行。
- `IAnimalHost.CreateAnimal`、原生成熟状态和动态 `TbHusbandry` 阈值所有权不变；
  本轮不改动物代码、存档路径或 sidecar。

## 3. 无限燃料可以补满普通机器，但物品本身必须改为普通 999 堆叠

### 用户确认

- `dtmapi_creative_generator` 可以被兼容的普通机器接受，并把机器燃料补到该设备
  自身容量上限。
- 用户通过本轮批注明确允许把先前等待的兼容机器消耗、正常保存和重载矩阵
  标记为手测通过。
- 当前物品不可堆叠；用户要求按普通物品规则把单格上限改为 `999`。

### 静态事实

- 当前 `Content/item_tbitem.json` 的 `overlay` 为 `1`。
- 官方 `ItemInfo` 直接把 JSON `overlay` 映射为 `Overlay`；`Item.noOverlay` 在
  `Overlay == 1` 时为真；`ItemFactory`、`Item.TryCombine` 和 `LinearInventory`
  都以 `proto.Overlay` 作为创建、合并和背包容量上限。
- 因而本缺陷不是 UI 计数或给予逻辑：内容行明确把无限燃料声明成了单格一个。

### 实施边界

- 只把 `overlay` 从 `1` 改为 `999`，并增加聚焦结构断言。
- 保留稳定 ID、煤炭图标、普通 `ItemFunction`、不可出售/不可烹饪、
  `electric_energy=1000000000` 和九语言文案。
- 玩家手测已接受“机器可吃入并补到容量”以及该消耗状态的正常保存/重载；
  `999` 堆叠本身在本轮只做 source/unit/Author-package 验证，不伪造新的游戏手测。

## 4. 旧城守护者不是近期原生漂移，而是复合怪物与控制台单实体事务假设冲突

### 已复核的调用链

1. 控制台 `SpawnMonsterNative` 调用当前房间
   `IMonsterHost.GenerateMonster(MonsterProto, Vector2, true)`，每次返回后要求
   `DM_monster.AllMonsters == before + created.Count`。
2. 官方 `GenerateMonster` 先通过 `MonsterManager.CreateMonster` 把主实体加入
   `AllMonsters`，随后 `_RunMonster` 取得控制器并进入
   `MonsterController.Run`。
3. `MonsterController.Run` 在设置 `Monster.Controller` 后调用 prefab 上的
   `MonsterDecorator.OnMonsterLoaded`。
4. `space_ship` prefab 明确绑定两个炮台位置，并把 `bastionId` 配置为
   `space_ship_bastion`。
5. `MonsterDecoratorSpaceShip.OnMonsterLoaded` 先把主实体移动到房间中心上方，
   然后对两个炮台位置各调用一次 `_CreateBastion`；该方法再次调用
   `parent.Host.GenerateMonster(proto, transform.position)`。
6. 所以一次 `space_ship` 根请求的官方同步结果是三个 manager 实体：
   `space_ship + 2 × space_ship_bastion`，不是一个。

### 为什么现有日志精确表现为 `created=1` 后熔断

- 外层反射调用成功返回的是根 `space_ship`，控制台因此把根加入自己的
  `created` 列表并记录 `created=1`。
- 此时官方装饰器已经同步加入两个炮台，manager 计数增量是 `3`；控制台仍要求
  增量为 `1`，所以抛出 `Monster postcondition verification failed`。
- 现有回滚列表只含根实体。`IMonsterHost.RemoveMonster` 只移除并回收传入实体，
  不会执行主实体死亡路径；两个由装饰器生成的炮台不在回滚列表中，因而仍留在
  manager。最终计数为 `before + 2`，触发
  `exact monster count/containment was not restored` 和存档内批量生成熔断。
- 单独生成 `space_ship_bastion` 会通过，是因为它的装饰器不会再创建子怪物；
  这也与 2026-08-13 玩家日志完全一致。

### 原生版本对照

- `MonsterDecoratorSpaceShip.cs`、`MonsterDecoratorSpaceShipBastion.cs`、
  `MonsterManager.cs`、`MonsterController.cs` 和 `MonsterAI_SpaceShip.cs` 在
  准入策略 build `24456188` 与当前 public `24966367 / 1.00.06` 间字节一致。
- `IMonsterHost.GenerateMonster` / `RemoveMonster` 相关方法体也未改变；该文件的
  可见版本差异只在 `RunMonsters` 遍历时先复制数组，与本问题无关。
- 因此静态结论是：官方一直把旧城守护者建模为复合怪物，控制台的
  “每个根请求精确新增一个 manager 实体”事务假设不兼容；不是 1.00.06 新增的
  原生签名漂移。

### 被排除的方向

- 不是 `Vector3 -> Vector2` 反射参数错误；调用已经返回并渲染根实体。
- 不是整个当前房间缺少 `IMonsterHost`；同会话其他怪物与单独炮台均成功。
- 不是后续怪物各自创建失败；它们在进入原生调用前已被产品熔断器拒绝。
- 不能通过放宽“数量不等也算成功”修复。这样会让真正的额外实体、漏注册或
  部分创建无法区分，也无法安全回滚。

### 后续修复门（本轮不实施）

- 以调用前后的 manager 集合差求出完整新增闭包，而不是只记录根返回值。
- 对 `space_ship` 明确验证一个根和两个 `space_ship_bastion`、共同 Host、有效
  Controller 和零未知新增；复合 Boss 的右键十个批量行为需单独限制或确认。
- 回滚必须覆盖完整新增闭包并在子实体优先的顺序恢复调用前集合；失败仍应熔断。
- 在实现前继续审查 BattleSystem、MonsterEnv group 和 recycle 的完整清理语义。
- 保存安全仍未知；只有代码修正和聚焦 source/unit 通过后，才可设计隔离存档测试。
  本轮按用户要求不启动游戏，也不把静态根因写成 runtime 修复。

## 5. `DebugConsoleUi` 允许机械拆分，但不得与行为修正混写

- 用户允许把接近四千行的 `DebugConsoleUi.cs` 做机械拆分。
- 本轮可把同一个 sealed class 转为多个 `partial` 文件，按生命周期、目录、世界动作
  和 Unity 工厂职责移动既有成员；不得顺便改变按钮顺序、输入时序、反射缓存、
  Rect、事件绑定或异常处理。
- 现有 nullable warning 是独立类型标注债。机械拆分可以降低单文件维护风险，
  但不能仅凭文件变短宣称这批 warning 已解决。

## 6. 旧路线图、API、Hook 和 Debug 状态需要同步当前事实

- 2026-08-01 world-actions Planning 中“怪物仍隐藏”已被 1.1.0 实现取代；
  Generator 已被物品浏览器内的无限燃料取代，只有 Resource UI 仍隐藏。
- Public API matrix 必须同时表达：公开产品已是 `1.1.1`、Monster/Animal UI 已上线、
  0.3.1 消费者保留限制已解除，但 28 个当前 DTO/enum 仍有真实消费者。
- DebugConsole Hook Map 中“公开仍为 1.1.0 / 本地 1.1.1”的描述已被 Steam
  manifest `6693520158465470410` 取代。
- ISSUE-015 的 canonical 状态已是 `verified`，issue ledger 不应继续写
  `mitigated`；ISSUE-020 继续作为通用 source-arbitration 问题存在，但不能再把
  旧 Y-console cold-start 当成未完成子门。

## Validation Classification

- Old City Guardian: detailed source/reverse review only; no game launch and no
  fix in this task.
- Animal and machine fuel behavior: user manual acceptance.
- Fuel stack `999` and UI split: source/unit/Author-package checks owned by the
  linked Update.
- Steam subscription, official upload folder, local game package and saves are
  outside this task and receive no mutation authority.

## 7. 用户后续决策：取消生成伪事务与共享熔断，改为可见的部分成功

### 用户要求

- 修正旧城守护者；保留普通点击生成 `1` 个根怪物、右键生成 `10` 个根怪物，
  不为 `space_ship` 禁用十倍生成，也不增加额外确认，由玩家自己决定是否取用。
- 审查后取消图二所述的自动回滚和跨请求/全局熔断机制。
- 不再伪装“整批全成或全撤”的事务；逐个调用官方生成入口，首次失败即停止本批，
  已经成功或由官方调用实际加入 manager 的实体全部保留。
- 玩家必须能看到请求根数、成功根数和 manager 实际新增实体数；一次部分成功不能
  被显示成纯失败或虚假的全部撤销。
- 本轮比较可采用 `NoNativeSave`，不要求把测试结果写入玩家存档。

### 对原建议的复核与取代

- 本 Review 第 4 节“后续修复门”曾建议计算完整新增闭包、完整回滚并在回滚失败时
  熔断。该建议是在产品语义尚未由用户裁定时提出的保守候选方案；本节记录的用户
  决策明确取代其中的“限制十倍生成、自动回滚、回滚失败熔断”三项，不改写前述
  原生调用链和静态根因证据。
- `IMonsterHost.GenerateMonster` 不是控制台拥有的事务接口。官方 decorator 可在
  根调用内部同步再入同一 Host；控制台既没有官方事务 token，也没有一个能够证明
  装饰器、BattleSystem、回收池和所有子实体均恢复的撤销操作。
- 现实现只把返回的根实体加入 `created`，再用 `RemoveMonster(root)` 尝试撤销。
  对旧城守护者，这个操作不会覆盖两个炮台，因而“回滚”只是删除部分可见结果；
  随后的共享熔断又把一次误判扩散到所有怪物和动物请求。两者都不应继续作为安全
  语义保留。
- 因此新的安全边界是可观察、不可伪造：每次官方调用前后比较 manager 对象集合，
  验证本次新增集合；失败时停止继续调用，但不主动删除官方已经创建的实体，也不
  阻止下一次由玩家明确发起的请求。

### 实现验收语义

- 普通怪物每个成功根必须对应本次新增集合中的一个同 ID 实体，并具备当前 Host、
  Controller 和 manager 登记。
- `space_ship` 每个成功根必须精确观察到一个 `space_ship` 和两个
  `space_ship_bastion`；三个实体均属于当前 Host、具备 Controller、登记在
  `AllMonsters`，且本次集合没有未知新增。请求 `10` 个根时同样逐个应用该规则，
  不增加确认或特殊限流。
- `SpawnedCount` 继续表示通过后置条件的根请求数，不冒充 manager 实体总数；产品
  内部结果另带实际新增实体数，避免改变仍被当前产品使用的 public Debug DTO。
- 怪物和动物批量请求均在首次异常、空返回或后置条件失败时停止；已经通过验证的
  根数和调用留下的实际实体数必须写入状态与日志。没有任何后续请求级熔断。
- 自动验证至少覆盖旧城守护者 `1 -> 3`、十倍 `10 -> 30`、一次带真实副作用的
  中途失败，以及失败后的新请求仍可执行。运行验收如实施，必须分类为
  `NoNativeSave` 并遵守共享 Runtime lock 与原始存档字节不变门。

### 后续实现

- Owning Update:
  [20260830-0004-y-console-visible-partial-composite-spawn](../../../updates/2026/20260830-0004-y-console-visible-partial-composite-spawn.md)

## 8. 实施与运行结论

- ProductNative 已删除怪物/动物生成回滚列表和共享存档级熔断状态。每个根请求仍
  逐次调用官方 Host；本批首次异常、空返回或后置条件失败时停止，但不删除本次
  调用已经加入 manager 的实体，也不阻断下一次玩家明确请求。
- UI 和状态日志分别显示请求根数、通过后置条件的根数、manager 实际新增实体数；
  `SpawnDebugResult.SpawnedCount` 仍只表示成功根数，实际新增数由产品内部结果传递，
  因而 public Debug DTO ABI 没有改变。
- 聚焦 Unit 以物理 fake Host 覆盖普通成功、旧城守护者 `1 -> 3`、十倍
  `10 -> 30`、第四次调用在加入实体后抛错形成的真实部分成功，以及失败后新请求
  仍可执行。原生 trace 同时拒绝旧生成路径重新出现 `RemoveMonster`、rollback 或
  circuit token。
- `GAME-SMOKE/20260830-231652` 在第三存档的 AutoCloud 隔离夹具中，从夹具自己的
  `MODS/DTMAPI_YKeyConsole` 加载 entry SHA-256
  `2D7DA1DBF72512C359E2E7DED362F98F90E14BA169D4448671330682D32CA38E`；
  owner 为 `ProductNative`，Compatibility Host 保持 dormant。
- 同一次 `NoNativeSave` 运行验证 `space_ship` 一倍为
  `requestedRoots=1/succeededRoots=1/addedEntities=3`，十倍为
  `10/10/30`。运行整体、AdvancedDebug、QA 生命周期、进程退出均通过；玩家存档和
  已提交 sidecar 在清理前均未改变，没有常规字节备份、存档写回或恢复。
- 该运行按用户允许的“不保存比较”验证生成与清理边界，不冒充 Boss 正常保存后的
  持久化验收，也不产生 Steam 上传、订阅或公开 `1.1.1` 包变更授权。

## 9. 1.1.2 玩家观察：旧城守护者主体与后续测试存档

### 截图观察与原生结构核对

- 用户提供的游戏截图显示旧城区域上方有一块横跨大部分视野的深紫色长方形，周围
  可见机械主体、炮台和弹丸；用户询问该巨大矩形是否就是旧城守护者本体，并报告
  当前 `1.1.2` 手测未发现显著问题。
- 当前官方 reverse prefab `game_entity_monster_space_ship.prefab` 的根实体包含名为
  `船体` 的世界空间 Tilemap；其 tile 范围为 `106 x 11`，Grid 单元为
  `1.5 x 1.5`，两处炮台位置位于根坐标两侧。`MonsterDecoratorSpaceShip` 在根怪物
  加载后再创建两个 `space_ship_bastion` 子怪物。
- 因而可以高置信度确认：截图中的巨大深色矩形不是 Y 键控制台 UI，而是官方
  `space_ship` 根 prefab 的船体 Tilemap，也就是旧城守护者复合实体的主体部分；
  两个炮台是 decorator 追加并登记的子怪物。该结论来自 prefab/代码结构和截图
  几何的对应，不是对截图像素或运行对象做过注入式检查。

### 玩家验收边界

- 记录为：当前上传目录中的本地 `1.1.2` 候选，旧城守护者可见生成与当前游玩表现
  经玩家手测未发现显著问题。结合已有自动运行的 `1 -> 3`、`10 -> 30` 证据，
  当前复合生成修正不再作为本轮阻塞项。
- 本次玩家观察没有覆盖 Boss 保存后重载、跨场景长期存活、精确性能数值或 Steam
  已发布包，因此不把这些未执行项目写成通过，也不新增 `GAME-SMOKE` 行。

### 后续 Y 键控制台 fixture

- 用户指定：后续所有 Y 键控制台游戏测试统一使用游戏 UI 第十存档，即 runner
  `-SaveSlot 10`、原生 archive 索引 `9`，以绕开第三存档当前的饰品问题。
- 这只是领域权威 fixture 的迁移，不宣称第三存档损坏，也不改写此前第三存档
  smoke 的历史证据；其他领域继续遵守各自的存档规则。
- 未完成的运行/维护事项转入 owning Update
  [20260831-0006](../../../updates/2026/20260831-0006-y-console-runtime-lightweighting.md)
  的后续路线图，不阻塞上述玩家可见验收。
