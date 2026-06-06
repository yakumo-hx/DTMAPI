# Manual QA / Requirement Review: Official Console-Informed Y Console Expansion and New Mods

- 时间：2026-06-06 12:53:18 +08:00
- 来源：用户新功能需求；上一轮本地反编译查询确认官方控制台命令系统；本轮无截图。
- 范围：记录需求、基于官方控制台提出 Y 键控制台优化方案、更新独立 goal 文件、输出短 `/goal`；不实现代码。
- 禁止事项：不启动 `/goal`；不实现 runtime/API/mod；不直接开放官方危险命令；不复制旧 DLKsmapi；不修改官方/Workshop 第三方内容文件。
- 审查记录：`docs/reviews/manual-qa/2026/20260606-0003-official-console-yconsole-new-mods-review.md`

## Official Console Baseline

代码/文档事实：

- 官方控制台由 `DevHelper` 持有 `GameConsole`，F1 打开条件是 `gameOuterConfig.enableGameConsole`。
- 外部配置从 `Application.persistentDataPath/config.json` 读取；控制台自己的设置文件是 `redsaw_console_settings.json`。
- 官方命令系统通过 `[Command(...)]` 注册，当前反编译版本约 490 个命令。
- 官方控制台打开时会 `DolocAPI.UserInput.DisableAllInput(includeGlobal: true)`，失焦时 `ResumeCurrentInput()`；这是 Y 控制台输入隔离应参考的关键路径。
- 官方命令已覆盖 Y 控制台多项底层能力：`genitem` / `obtain_item` / `try_place_in_backpack`、`set_weather`、`pass_day` / `pass_month` / `set_time_scale`、`add_tech_point`、`add_money`、`unlock_all_recipes`、`unlock_tech_tree`、`gen_monster`、`force_create_resource_at`、`go`。
- 官方也包含危险命令：`lua`、`load_game`、大量全解锁/清空/刷新/剧情脚本命令。它们不应原样暴露给玩家 UI。

Y 键控制台优化方案：

- 保留 Y 控制台作为玩家/Mod 作者可用的安全 UI，不用官方 `GameConsole` 直接替代。
- 新增或扩展 experimental bridge API 时，优先复用官方安全函数/同等内部 API，但必须做白名单、参数校验、日志、可失败原因和第三存档验证。
- 可以增加开发者隐藏页/命令桥，只列出官方命令清单和白名单执行；默认不显示，不允许执行 `lua`、任意 `load_game`、任意清存档/剧情污染类命令。
- Y 控制台 UI 应继续提供来源索引、搜索、图标、hover、本地化和安全按钮；官方控制台只作为底层事实和调试参考。
- 输入隔离必须对齐官方控制台：Y 控制台打开后键盘、鼠标、热键、工具使用、背包快捷键等都不得穿透到游戏。

## 问题 1：Y 键控制台新增官方控制台同类调试功能

原始反馈：

- 基于官方控制台给出现有 Y 键控制台优化方案。
- Y 控制台加入下一天、下一周、下一季节功能。
- 加入时间倍速功能；可参考坐在椅子上是 4 倍；提供 2x、4x、8x、16x。
- 可以获得技能点数，不同技能点单独按钮，点一次 +10。
- 一键解锁全部科技树，不消耗点数。
- 修改螺母金币数量，提供 +100、+1000、+10000、+100000。
- 所有作物瞬间成熟。
- 加入创造模式，存档内热开关；打开后所有固定配方的合成、制作、设备制作、烹饪不消耗材料；所有制作类设备运行不消耗时间，包括手动放入物品的加工类。
- 加入创造模式发电机：无消耗，产出 999999 电，复用游戏内最顶级发电机贴图，不可合成、不可出售，只能通过 Y 控制台调出。
- 左侧生成物品分类加入生物/怪物，左键当前位置召唤 1 只，右键召唤 10 只。
- 加入资源分类，左键当前位置最底层地面无条件生成 1 个单次资源，右键生成堆叠 10 个；不是资源刷新点；支持垃圾、矿物、植物三种；房间内和城镇使用无效。
- 地图传送补全所有车站、船，以及多洛可河谷、沼泽等小镇外地图。

审查记录：

- 用户确认事实：这是新功能需求，不是上一轮 bug 回归；重点是把官方控制台能力转成更安全的 Y 控制台 UI。
- 截图/日志观察：无截图。
- 代码/文档事实：
  - 官方已有时间命令 `pass_day`、`pass_month`、`pass_time_to_target_hour` 和全局 `set_time_scale`。
  - 官方已有 `add_tech_point`、`add_money`、`unlock_tech_tree`、`unlock_all_recipes`、`set_crop_level`、`gen_monster`、`gen_monster_at`、`force_create_resource_at`、`go`。
  - 现有 DTMAPI Y 控制台已有物品、天气、传送、下一时段、存这里、移速等 debug API，但没有完整的官方命令桥、创造模式、技能点按钮、资源/怪物生成分类。
- Codex 推断：
  - 下一天/周/季节应走官方时间推进或等价 API，不能只改日期字段；必须验证作物、机械、天气、NPC/日程状态是否正常推进。
  - 时间倍速与玩家移速不同，应参考官方 `set_time_scale` 或座椅加速路径，但需要避免影响 UI/物理/战斗异常。
  - “技能点”不能简单等同科技点；实现前必须列出所有点数系统和 UI 名称，再每类单独按钮 +10。
  - 创造模式不是单个命令，属于 DTMAPI GameBridge 级别能力：材料消耗、配方检查、烹饪、设备制作、加工时间、电力/燃料消耗都可能走不同路径。
  - 创造模式发电机适合做 official-local 内容物品 + DTMAPI GameBridge 电力集成；只能由 Y 控制台给予/生成。
  - 生物/怪物/资源生成可以参考官方命令，但必须限制位置、场景、数量和资源类型，避免污染刷新点。
  - 传送扩展应从官方站点、船、mark point、路径数据构建白名单，并保留 CSV 导出给用户审定名称。
- 反证/未证实：未确认所有技能点系统名称；未确认座椅 4x 的具体实现路径；未确认所有船/站点表名；未确认所有固定配方和加工设备的消耗路径。
- 归属：DTMAPI GameBridge、Bootstrap Y Console UI、experimental debug/creative APIs、official-local content for creative generator。
- 需要更新：独立 goal 文件、debug docs、hook map、API matrix、smoke matrix、update record。
- 验收点：第三存档逐项验证时间推进、时间倍速、点数/金币、科技树、作物成熟、创造模式制作/设备加工、创造发电机、怪物/资源生成、跨地图传送；每项有日志和截图/录屏证据。
- blocker 判定：如果创造模式无法覆盖关键固定配方/设备路径，或时间推进会破坏存档状态，必须留下 blocker，不得标 complete。

## 问题 2：Zoom 大视野 Mod

原始反馈：

- 新增 `zoom` 大视野 mod。
- 键盘 `+` / `-` 可以放大缩小屏幕范围。
- 最小是正常视野，最大支持显示 400%，能看到半个农场。

审查记录：

- 用户确认事实：这是新独立 mod 需求。
- 截图/日志观察：无截图。
- 代码/文档事实：本轮未深入检查相机类；需要实现 Codex 查 `Camera`、像素完美、UI scale、房间边界和官方缩放/视差路径。
- Codex 推断：应修改游戏相机视野/orthographic size 或相机控制器参数，而不是缩放整个 UI；UI、鼠标坐标、交互射线和像素风格必须保持正常。
- 反证/未证实：未确认 400% 是面积 4x、视野宽高 4x，还是用户感知的最大缩放级别；实现时应以“最大可看到约半个农场且不破 UI”为验收。
- 归属：新 official-local DTMAPI Zoom mod + GameBridge camera API。
- 需要更新：独立 goal 文件、API matrix、hook map、debug/smoke evidence。
- 验收点：第三存档农场内按 `+`/`-` 平滑调整；最小恢复原版；最大视野明显扩大，UI 和点击不漂移。
- blocker 判定：如果官方相机/像素渲染不允许稳定大视野，应报告最大安全比例。

## 问题 3：箱子定位器增强 Mod

原始反馈：

- 设备消耗材料时可识别全农场，包括所有建筑物房间内任何安装了“箱子定位器”的储物设备。
- 游戏默认只能识别房间内安装箱子定位器的。

审查记录：

- 用户确认事实：这是新独立功能需求，目标是扩大官方箱子定位器的材料查找范围。
- 截图/日志观察：无截图。
- 代码/文档事实：本轮未深入检查箱子定位器或制作设备材料查询路径；需要实现 Codex 查官方设备材料消费、储物设备、房间/农场容器索引。
- Codex 推断：应尽量扩展“材料查询/扣除”路径，而不是复制物品到当前房间；必须维护真实扣除顺序和失败原因。
- 反证/未证实：未确认官方是否已有全农场索引或仅当前 room 查询；未确认建筑物房间是否属于同一个 farm data 容器。
- 归属：新 official-local DTMAPI Chest Locator Enhancer mod + GameBridge inventory/storage API。
- 需要更新：独立 goal 文件、API matrix、hook map、smoke matrix。
- 验收点：第三存档把材料放在农场不同建筑内带定位器的箱子中，当前设备制作/加工能识别并真实扣除；无定位器箱子不参与。
- blocker 判定：如果官方材料扣除无法安全跨房间事务化，必须停止并报告避免复制/丢失物品。

## 问题 4：更强的种植枪 Mod

原始反馈：

- 现在种植枪只能放一个东西，种子或薄膜或肥料。
- 拓展为三个 UI：可放种子、肥料、薄膜。
- 使用时有几种东西就一次放几种。
- 允许 `[` / `]` 调节范围；按 4x4、5x5、6x6 递增，最大 15x15。
- 遵循官方放置逻辑：空盆才种，薄膜破损才替换，数量不够按官方种植枪逻辑与放置顺序种植所有。

审查记录：

- 用户确认事实：这是新独立功能需求，目标是强化现有种植枪交互。
- 截图/日志观察：无截图。
- 代码/文档事实：本轮未深入检查种植枪类；需要实现 Codex 查官方种植枪物品、槽位 UI、范围扫描、种植/施肥/覆膜逻辑。
- Codex 推断：强种植枪应复用官方单格/单盆操作判断，循环执行时只扩大目标集合和槽位来源，不应绕过空盆、破膜、肥料限制。
- 反证/未证实：未确认官方 4x4 最大范围、槽位存储位置、薄膜破损判断字段。
- 归属：新 official-local DTMAPI Strong Planting Gun mod + GameBridge farming/planting-gun API。
- 需要更新：独立 goal 文件、API matrix、hook map、smoke matrix。
- 验收点：第三存档 4x4 到 15x15 调节可见；种子/肥料/薄膜三槽可用；材料不足时按官方顺序部分完成并不吞物品；非法盆位不误操作。
- blocker 判定：如果官方种植枪 UI/存储无法安全扩展三槽，应保留 blocker，不得用外部背包扫描伪装完成。

## Problem Grouping

- UI：Y 控制台新分区、创造模式开关、技能/金币按钮、生成分类、传送列表、Zoom 热键、种植枪三槽 UI。
- API/GameBridge：官方命令白名单桥、时间推进/倍速、作物成熟、创造模式材料/耗时 bypass、怪物/资源生成、跨房间箱子索引、相机控制、种植枪范围/槽位。
- 官方/工坊兼容：不得接管官方控制台或第三方内容；只读索引与白名单复用官方函数。
- 测试/证据：所有功能必须第三存档验证；时间和创造模式尤其需要 before/after 状态日志。

## Boundary Constraints

必须做：

- 把官方控制台作为底层事实/参考，不作为玩家直接暴露的无限命令行。
- 新增功能要在 Y 控制台中有清晰中文/英文 UI。
- 每个会改存档的按钮都必须写日志、显示结果、可失败原因。
- 创造模式必须可热开关，退出或关闭后不遗留永久免费制作状态。
- 新 mod 使用 official-local DTMAPI 包装，走现有 DTMAPI 架构和官方启用状态。

禁止做：

- 不开放 `lua`、任意 `load_game`、清空/全刷新/剧情污染命令给普通 Y 控制台。
- 不直接改第三方/Workshop 内容文件。
- 不用全局硬编码传送点替代官方表扫描和 CSV 审定。
- 不绕过官方物品扣除造成复制或丢物。

可选做：

- 增加隐藏开发者命令清单页，仅列官方命令和 DTMAPI 白名单映射。
- 对创造模式提供醒目的状态提示，防止用户忘记开关。

blocker 判定：

- 时间推进、创造模式、跨房间材料扣除、资源/怪物生成、种植枪三槽任一存在存档污染或无法验证真实官方路径时，目标不得标 complete。
