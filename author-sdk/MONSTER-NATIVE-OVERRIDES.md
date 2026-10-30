# 原版怪物字段覆盖（Experimental）

当前构建、安装和契约供给统一见[Runtime 模块指南](MONSTERS.md)。旧专项 Host 构建命令已清退；代码用普通工程构建，内容用离线编译器导出。

**0.7.5-review 范围：** 当前源码仍包含 DTMAPI 对原版怪物提供的额外 `Skin/skin` 覆盖层，下文仅说明现有行为，不再推荐新作者使用，也不承诺持续兼容。官方 `Content` 图片替换能力不变。`ProjectileTint/projectileTint` 是按原版发射者限定弹体颜色的独立能力，当前不支持新定义发射者。新定义 `MonsterAppearance.Skin`、显示大小、主体 Tint，以及原版数值、攻击、声音和统一掉落使用各自的现有入口。

给原版怪物普通换图，使用官方模组的 `Content/<原始Sprite帧名>.png`，例如 `Content/anim_monster_amoeba_jump_2.png`；不需要声明 DTMAPI Skin。未提供的帧保持原图，AI 及动画时序继续沿原生；同名帧的全部消费者共享替换结果。


当前使用 Runtime 0.7.5 内置模块；模板能力仍由实际目录逐项证明。

`IMonsterContentApi.RegisterOverride` 与项目 `nativeOverrides` 修改当前原生目标的字段读取。目标必须来自运行目录并声明 `nativeOverrideFields`；独立新怪仍使用 RegisterDefinition。原生合并表是有效基线，无法还原到各原生 Mod 的个别来源。

| 字段 | 接纳值 | 已有实例的生效点 |
| --- | --- | --- |
| `defense` | 整数 非负 | 下一次使用模板防御的原生伤害结算 |
| `hurtDuration` | 有限游戏秒 非负 | 下一次普通原生受击；进行中的计时保持原样 |
| `tint` | 可空 `#RRGGBB` | 登记/撤回时刷新已有原版实例；随后 Run 应用当前胜出颜色 |
| `skin`（不推荐新项目使用） | 当前源码：模板指纹、PNG、根Sprite帧映射，缺帧不显示、多余帧不消费 | 仅记载现有候选的原版实例映射行为；新项目改用官方 Content |
| `drops` | `inherit`、`append`、`replace`，池引用或内嵌条目，`minCount/maxCount` | 继承原池；各份追加分别执行；替换按官方加载与声明顺序，数量/资格由原生 GuaranteedManager 消费 |
| `projectileTint` | 可空 `#RRGGBB`；目标目录须声明支持 | 仅给指定原版怪物发射的租出弹体乘色；刷新已有受管弹体，回收/复用及撤回时恢复；不作用于新定义发射者 |
| `sounds.hurt` | 同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 下一次真实原版Hurt；胜者变化停止原覆盖声音，不主动重播 |
| `sounds.dead` | 同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 下一次真实原版死亡；普通回池后继续尾音，来源切换/撤回和房间停止清理 |
| `sounds.recoveryWarning` | 同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 年兽实际恢复警告读取胜者，普通状态退出保留尾音；真实104按准确活动代次停止 |
| `sounds.humming` | 同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 下一次原生Run开始；Loop省略继承实际事件（蜂有限、无人机循环），显式false/true覆盖。来源变更停止旧请求，不补播 |
| `nativeAttacks.<AttackId>.fireSound` | 逐槽同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 下一次原生发射投递读取胜者，散射/散投/动画发射按原生轮次；来源变化退休所属尾音。能力由该槽`sounds`独立声明，见[发射声音示例](examples/monster-fire-sounds/ModEntry.cs)及其Windows限定验收 |
| `nativeAttacks.<AttackId>.dashSound` | 逐槽同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 下一次实际冲撞开始读取胜者，MultiImpact逐段、ShadowImpact主体一次。预备音由readySound独立设置；与Ready/Fire分别裁决、撤回和清理，见[冲撞声音示例](examples/monster-dash-sounds/ModEntry.cs) |
| `nativeAttacks.<AttackId>.throwSound` | 逐槽同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 每个成功投放的地雷/定时炸弹读取当前胜者；零投放、分配失败和投放前取消不补播。技能爆炸声保持，见[投放/列队声音示例](examples/monster-attack-event-sounds/ModEntry.cs) |
| `nativeAttacks.<AttackId>.volleySound` | 逐槽同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 列队射击每轮开始读取当前胜者；与逐弹Fire独立SET、撤回和清理。保留原生零弹轮次事件，不改变作者弹数下界 |
| `nativeAttacks.<AttackId>.readySound` | 逐槽同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 六类原生准备接点读取当前胜者，MultiImpact逐段播放。普通取消/替换保留尾音；真实104同时停止该怪物当前活动代次的ready/recovery倒计时，Sting109不加入104集合。与Dash/Fire分别裁决；见[预备声音示例](examples/monster-ready-sounds/ModEntry.cs) |
| `nativeAttacks.<AttackId>.landSound` / `.countdownSound` / `.explosionSound` | Landmine/TimeBomb逐槽同 Owner 的声音句柄、VolumeDb；编辑器使用 assetId/volume | 技能落地、每个倒计时步骤、后继Boom分别读取当前胜者与真实发声位置。技能回池停止落地/倒计时，爆炸尾音允许自然结束；原版技能独立于发射者controller。与throwSound分别裁决；见[技能声音示例](examples/monster-placed-sounds/ModEntry.cs) |
| `nativeAttacks` | 目录逐槽、逐字段开放的有类型参数 | [A16 跳跃](examples/monster-amoeba-parameters/ModEntry.cs)、[A01 Shoot](examples/monster-shoot-parameters/ModEntry.cs)、[A02 MultiShoot](examples/monster-multishoot-parameters/ModEntry.cs)、[A03 SalvoShoot](examples/monster-salvo-parameters/ModEntry.cs)、[A04 ScatteringShoot](examples/monster-scattering-parameters/ModEntry.cs)、[A05 DragonShoot](examples/monster-dragon-parameters/ModEntry.cs)、[A06 ScatteringThrow](examples/monster-throw-parameters/ModEntry.cs)、[A07 BombingDive](examples/monster-dive-parameters/ModEntry.cs)、[A08 BatteringBullet](examples/monster-battering-bullet-parameters/ModEntry.cs)、[A17 Battering](examples/monster-battering-parameters/ModEntry.cs)、[A09 FungusShoot](examples/monster-fungus-parameters/ModEntry.cs)、[A12 Sting](examples/monster-sting-parameters/ModEntry.cs)、[A13 ChomperBite](examples/monster-chomper-bite-parameters/ModEntry.cs)、[A14 Impact](examples/monster-impact-parameters/ModEntry.cs)、[A11 BulletTrain](examples/monster-bullet-train-parameters/ModEntry.cs)、[A22 ShadowImpact](examples/monster-shadow-impact-parameters/ModEntry.cs)、[A20 ChomperMimicry](examples/monster-mimicry-parameters/ModEntry.cs)、[A21 ThunderPile](examples/monster-thunder-parameters/ModEntry.cs)分别说明读取时点；射击伤害/暴击在命中时热读，速度/寿命在发射入队时固定，连射次数在本轮抽取后固定 |

血量、身份、图鉴、几何和行为不是本段支持字段；原掉落替换已由统一 `drops` 支持。攻击参数只限 `actions.nativeAttacks` 明确声明 `nativeOverride=true` 的字段，不能从可继承推导。显式传入防御或受击时长的特殊原生重载仍使用调用方参数。数值按共同原生边界接纳，主体色、弹体色和掉落按字段判定，不要求该目标能够完整继承为新定义。原版 skin 的旧目录不是长期支持承诺；projectileTint 按实际目录独立接纳。

`nativeAttacks.AtkNormal.damage` 等叶字段各自选择获胜作者，覆盖一个槽的伤害不会遮住另一作者的冷却或跳速。`startOnCooldown=false` 是有效 SET，快照数值用 0/1、目录单位用 boolean，作者输入仍是布尔值。攻击实例的原始共享 proto 保留；未知 holder 替换，或共同 manager 的 `Attack` / `RegisterAttackBehaviour` 存在其他 Harmony Owner，会拒绝原版攻击参数登记。MultiShoot、FungusShoot、ChomperBite 的专用 `Invoke` 上存在其他补丁不再关闭所有攻击参数覆盖；这不承诺任意第三方行为改写的兼容，实际冲突按受影响能力定位。

每条覆盖有 owner 内唯一的活动 LocalId、TargetId 和至少一个字段，不设每 owner 条数预算。注册时冻结值。Runtime 在 Entry/Pack Activate 前固定所有作者的加载计划；Pack 使用自身 Owner 的 LoadIndex。每次成功登记（包括 WaitingNative）分配该 Owner 的 DeclarationOrder，非法整条拒绝且不占序号。按字段选择 `(LoadIndex, DeclarationOrder)` 最大的有效声明，后者只在同一 Owner 内比较。晚登记的低顺位作者不能越过高顺位作者。null / 未填写不参与，0 是有效 SET；没有 Priority、同级冲突回退、ADD/MULTIPLY 或作者可写的顺位。

lease / Owner 撤回后立即重新求值：胜出者退出则剩余声明按相同规则取胜，全部退出则使用此刻原生读取值。旧 lease 不会撤回同 ID 的新声明。原生代次与 readiness 刷新重新校验存量声明，不重新编号；计划外 Owner 返回 OwnerLoadOrderUnavailable，需重新启动建立计划。

模块不替换原生行、不回写快照，故外部对原生行的修改不会被撤回抹除；GetOverrides 报告请求来源、LoadIndex、DeclarationOrder、代次、当前基线、有效值和 WinnerId，字段状态为 Effective / Shadowed / Inactive。着色字段使用 `RequestedTint` / `EffectiveTint`，数值 `Requested` / `Baseline` / `Effective` 为 null；主体原色属于每个实例，不伪造统一颜色基线。
第三方直接替换同一原生 getter 的 Harmony 补丁超出此仲裁协议。注册、原生代次刷新、GetOverrides / GetCatalog 会检查这些补丁；检测到时关闭本段读取覆盖并报告 ExternalOverrideHookConflict，需协调/重启。不轮询未知补丁，不保证检查间隙或第三方完全替换 Hook 后的排序。

活体、隐藏后再入与随后创建的原版怪物遵循同一“下次读取”规则。覆盖不增减当前 HP，不重放伤害、不重建实例、不改变击杀/图鉴 key，不写存档。修改安装包、官方启停和冷启动沿受管来源/重启规则；启动按当前启用作者重新登记，运行时撤回不会成为跨启动的持久配置。原生表重载重新检查目标，新表缺目标时拒绝该注册而不是套用旧行。

主体着色仅作用于目录声明 `tint` 的共同模板根 SpriteRenderer，按捕获原色乘以当前胜出色；不叠乘其他作者的颜色，`#000000` 也是有效 SET。撤回、回收、退出代次时恢复本模块应用前的原色；同模板独立新定义、子部件、弹体、共享材质和贴图不受影响。原生 Hurt 的材质闪烁/抖动继续归原生。若外部直接写入不同 renderer.color，清理保留该外部值；本协议不协调任意直接写色系统，不宣称整个多部件身体统一换色。

```csharp
var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId);
// Registration holds module demand. RequestModule() is optional prewarming.
var lease = api.RegisterOverride(new MonsterNativeOverride {
    LocalId = "armored", TargetId = "bee",
    Defense = 12, HurtDuration = 0.35f
});
// GetOverrides() explains all active authors; lease.Value.Dispose() withdraws this one.
```

通过 Runtime 注册的 `IMonsterContentApi` 调用。保留 module demand 和覆盖 lease，并交给 `IDtmOwnedResources`；完整调用见[C# 样例](examples/monster-native-overrides/ModEntry.cs)；包含项目文件、锁文件和配套 Pack 的可搬运工程见[双作者指南](MONSTER-DUAL-AUTHOR.md)。

项目 v4 可使用 `monsters: []` 和显式覆盖集合：

```json
"nativeOverrides": [
  {"id":"armored","targetId":"bee","fields":{"defense":12,"hurtDuration":0.35}}
]
```

字段留空表示不参与此字段。至少保留一个新定义或一条覆盖。现用工程为项目 v4，由共用编译器生成 统一 content.json schema 1；Runtime 0.7.5 供给该能力。旧项目版本、旧 priority 与旧 payload 明确拒绝；编辑器保全原文供下载，不静默改写作者意图。

## 原版资源字段

原版 `Skin` 当前使用与新定义相同的 `MonsterSkin`，项目字段为 `skin: {assetId, templateFingerprint, frames}`；资源按整张映射仲裁，回收/撤回恢复仍由本模块持有的 Sprite，再释放资源。原版字段不推荐新项目使用；新定义仍可通过 `MonsterAppearance.Skin` 使用 PNG 和帧映射。

新定义目前在缺帧或图片外矩形时显示空图，不会自动回退到 Animator 当前原帧。多余帧不参与播放，尺寸由作者决定，pivot/PPU 继承；没有完整帧数、同尺寸或图片预算限制。

统一掉落继续使用 `Drops / drops`：继承、追加或替换。多个追加各自执行，替换按官方顺序取胜者；原版和新增定义共用该链路，详细字段、数量与保底语义见[当前入口](MONSTER-075-CURRENT.md)。

`ProjectileTint` 从原生攻击回调定位发射怪物，只给目标原版怪物租出的弹体乘色，回收/复用、覆盖撤回时恢复。目标的 `nativeOverrideFields` 必须包含 `projectileTint`；当前不适用于自定义怪物定义。它不全局修改共享弹体资源，不改变速度、轨迹、寿命或伤害。官方同名 PNG 替换没有等价的发射者选择语义。例如原版目标 `drone` 的声明可使用 `fields: {"projectileTint":"#4080FF"}`，仍须以实际目录确认支持。

`GetOverrides` 中掉落资源继续使用 `RequestedResource/EffectiveResource`，配合 `WinnerId` 区分来源；主体 Tint 和 ProjectileTint 继续使用 `RequestedTint/EffectiveTint`，资源/色字段没有伪造的数值基线。相关接点受第三方直接 Harmony 修改时，未知作者通过诊断拒绝对应字段，不能以本仲裁解释任意补丁排序。



target_01 的原版根伤害入口拒绝普通攻击，子靶使用加载/重置时读取的 Health / Defense；子靶破损不进入 Monster 掉落消费路径，因此掉落覆盖不是每次子靶破损奖励。scarecrow 没有攻击，但正常死亡仍消费普通奖励。新定义用法见[训练对象样例](examples/monster-native-training/ModEntry.cs)。

Landmine / TimeBomb 投放各九字段通过 `nativeAttacks` 覆盖，嵌套组为 `landmine` / `timeBomb`；冷却与出生标志沿既有仲裁，当前技能快照不随撤回改变。后坐不是前摇，定时炸弹允许递减冲量；完整范围见[投放参数指南](examples/monster-placed-parameters/ModEntry.cs)。

Call已接入同一叶字段仲裁：冷却、出生先冷却、前摇及 `call.minCount/maxCount/roomLimit`。热改和撤回保留当前前摇/冷却；完成时读有效数量及房间总数，已生成原版子体不会被覆盖作者认领。范围与示例见[Call双域说明](examples/monster-call-parameters/ModEntry.cs)。

`nian_tail` 的 RandomShoot 在目录逐字段开放原版覆盖，不要求该部件能独立继承。九字段包括冷却、出生冷却、伤害/暴击、数量上下界、间隔、实际线性弹速和寿命。原版声明只影响原版尾部；新定义内部部件使用根定义的 `NativeParts`，两域分别接纳和隔离。参数快照/热读及限制见[RandomShoot与部件指南](examples/monster-random-shoot-parameters/ModEntry.cs)。

原版bird与nian_head的multiImpact独立开放十字段，详见[多段冲刺](examples/monster-multi-impact-parameters/ModEntry.cs)。撤回保留当前位移和身体快照，后续段读取恢复值；原生IImpact接口继承不授予未消费的弧线字段。

声音使用同 Owner 的 Audio 注册句柄：`new MonsterSound { Sound = registration.GetSound("sound"), VolumeDb = -6 }`。注册、路由和资源路径见[音频指南](AUDIO-CONTENT.md)。声音失败保留主体；声音撤回先结束领域播放，再释放媒体。

每个事件/槽按完整资源引用、音量和Loop意图独立SET。`RequestedResource` / `EffectiveResource`显示路径与设置，`WinnerId`区分同资源的不同声明，没有伪造数值基线。公共层拥有媒体、预算、emitter、voice、End和清理；Monster只保存实际控制器、逻辑活动代次、槽与调用来源。Hurt再次受击重启，Dead正常回池后冻结最后位置并继续尾音。Humming只在下一次Run消费当前胜者，省略Loop按当前模板解析，来源切换/撤回停止旧请求，不自动投递新声；未覆盖与公共起播失败各沿原生调用发声一次。

ready/recovery普通取消或状态退出保留尾音；真实104停止当前怪物同一活动代次的全部对应倒计时，并继续原生Stop。Placed每次真实落地/倒计时/后继Boom读取当前字段胜者；Skill来源独立于发射者controller。自然Skill回池停止land/countdown，爆炸使用有限MonsterExplosion（Monster空间父级、SFX_ACTOR输出）保留冻结尾音；此例外只覆盖已选Placed爆炸，不改其它原生声音默认。明确取消、来源变化/撤回、离房和Invoke异常仍停止准确请求。

未就绪模板保留WaitingNative；继承型Humming在原生就绪/代次刷新时解析recipe，不扫描完整原生音频包。停止或子租约释放失败时Dispose抛出并可重试；覆盖已退出裁决而子租约未释放时快照为CleanupPending / AudioCleanupPending。同Owner其它覆盖和其它Owner继续有效。
