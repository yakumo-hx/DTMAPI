# 怪物：Runtime 模块与作者入口

当前 `0.7.5-review` 候选使用 Runtime 0.7.5、怪物作者项目 v4，运行内容格式为 `dtmapi.runtime-content/1`；源码评审不代表完整发行验收。`DTMAPI.Monsters` 是 Runtime 内置 Provider，没有独立 Host、玩家启用行或产品私带契约。模板、攻击槽及可覆盖字段仍由实际 Catalog 逐项证明。

普通作者可从[PackA 项目](examples/monster-inheritance/PackA.dtmonster.json)和[当前字段指南](MONSTER-075-CURRENT.md)开始，使用作者项目和相匹配的模板目录快照，通过离线编译器导出普通 ContentPack。离线编辑、编译和导出不要求游戏或 Author SDK；本源码分支不包含完整可视化编辑器或已打包作者套件。高级作者直接引用 Runtime 的纯托管 `DTMAPI.MonsterContent.dll` 和 Abstractions，使用普通 netstandard2.0 工程、`Private=false`；可同时携带官方原生 `Content`。

## 登记与生命周期

```csharp
var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId);
var result = api.RegisterDefinition(new MonsterDefinition {
    LocalId = "my-bee", TemplateId = "bee", Health = 80
});
if (!result.Success) throw new InvalidOperationException(result.Diagnostic.Message);
var registration = result.Value;
// 原生就绪后按 Snapshot 与 Spawn 结果使用。
registration.Dispose();
```

有效注册自动持有需求。`RequestModule()` 是可选预热/驻留租约，释放它不清除有效定义。API 查询不启动后端；原生未就绪属于等待。成功撤回后，同进程可再次注册相同 ID；旧定义、实例及播放句柄由作用域和代次隔离。清理未完成时保留释放句柄和必要驱动，恢复后重试；明确不可恢复的身份错误才报告 RestartRequired。

同 Owner 多个注册独立撤回；Owner 退出撤回其全部贡献。跨 Owner 行为和定义引用保持真实包依赖及实际使用关系。不要按 Mod 名称安排退出次序。行为、原版覆盖、出生池、出生规则和实例 API 继续提供，具体见[实例](MONSTER-INSTANCE.md)、[覆盖](MONSTER-NATIVE-OVERRIDES.md)、[序列](MONSTER-SEQUENCES.md)及[双作者](MONSTER-DUAL-AUTHOR.md)。

## 文件与声音

游戏包仅以 `PackData/content.json` 为领域入口：schemaVersion 1，按需包含 monsters、audio、animals。编译器写入 monsters 的定义、覆盖、出生池和规则，并将声音写入 audio。资源路径一律相对真实包根。包内 `PackData/authoring/` 是可重开的源项目和目录，修改后须重新导出。

C# 先通过同 Owner 的 Audio 注册取得声音句柄，再设置 `new MonsterSound { Sound = sound, VolumeDb = -6 }`。死亡、爆炸及普通怪物事件须使用各自有限路由，见[音频 API](AUDIO-CONTENT.md)。声音准备、绑定失败只撤回声音贡献，保留有效主体并沿用原生音频；释放声音时先停止领域播放，再释放媒体。不含声音的怪物内容不会创建 Audio 后端。

## 安装与构建

安装配套 Runtime，退出游戏，把作品放入官方 profile 的 MODS 并启用；无需安装怪物 Host。普通包不重复声明内置 Host/模块依赖，显式填写的真实包 Required/VersionRange 仍生效。契约 DLL 只作编译引用，不随产品分发。

```powershell
./tools/scripts/build-mod-project.ps1 -ProjectPath E:/MyMods/MyMonster/MyMonster.csproj -ReferencesRoot E:/DtmApi/contracts -OutputRoot E:/MyMods/MyMonster-build
./tools/scripts/export-monster-project.ps1 -ProjectPath ./author-sdk/examples/monster-inheritance/PackA.dtmonster.json -CatalogPath E:/DtmApi/catalog-snapshot.json -OutputRoot E:/MyMods/PackA-export
```

命令中的 `E:/MyMods` 与 `E:/DtmApi` 是示例路径，需替换为自己的工程、契约引用和模板目录位置；普通代码工程的引用方式见[作者指南](README.md)。

源码样例见 [PackA](examples/monster-inheritance/PackA.dtmonster.json)、[PackB](examples/monster-inheritance/PackB.dtmonster.json)、[定义注册](examples/monster-inheritance/ModEntry.cs)、[Wilds 内容](examples/monster-wilds/Wilds.dtmonster.json)与[Wilds 事件](examples/monster-wilds/events/ModEntry.cs)。Chromatic 已退役，不作为本版交付示例。原版普通换图使用官方 Content；新定义外观及当前原版覆盖的区别见[字段覆盖指南](MONSTER-NATIVE-OVERRIDES.md)。现有动物、物品和存档身份不改写。旧 Host 包、旧内容 reader 和仅用于旧接线的依赖已经清退。

## 原生能力索引


原生 `traffic_light` 的共享灯态、活动判断、三槽攻击与普通双入口见[交通灯样例](examples/monster-native-traffic-light/ModEntry.cs)。

原生 Shoot 支持九项参数，包括前摇开关、恢复时长、弹速与寿命；[射击参数样例](examples/monster-shoot-parameters/ModEntry.cs)说明两类作者入口。已入队弹体保留速度和寿命，伤害/暴击在命中时读取，原版覆盖或撤回也影响此前发射但尚未命中的弹体。

原生 MultiShoot 支持十二项参数，增加次数上下界、逐发时长与固定方向；[连射参数样例](examples/monster-multishoot-parameters/ModEntry.cs)同时覆盖普通 drone_99 和专用 bastion。次数在本轮开始抽取，后续发次重新读取节奏与方向开关；普通怪物和 bastion 保留各自原生发射时点。

原生 SalvoShoot 支持十一项参数，提供弹数、扇角与间隔；[齐射参数样例](examples/monster-salvo-parameters/ModEntry.cs)说明原生半开扇形与序列快照。齐射创建时固定全部方向及间隔，弹速/寿命逐发读取，伤害/暴击命中时读取。

原生 ScatteringShoot 支持十六项参数，逐轮弹数/扇角与整次轮数/时序/方向分别读取；[散射参数样例](examples/monster-scattering-parameters/ModEntry.cs)说明初始和每轮准备、四种原生方向模式及累积偏移。

原生 DragonShoot 支持十三项参数；[往返扫射样例](examples/monster-dragon-parameters/ModEntry.cs)说明端点重复、单方向与单发的区别、整次序列快照和逐发热读。

原生 ScatteringThrow 支持十二项参数；[散投样例](examples/monster-throw-parameters/ModEntry.cs)说明向上扇形、弹体初速度与重力、发射回调和后坐时长的读取时点。

原生 BatteringBullet 支持十二项参数；[冲撞抛射样例](examples/monster-battering-bullet-parameters/ModEntry.cs)说明地形终点、身体伤害快照、撞墙/边缘回退和撞后抛射。

原生 ChomperBite 支持七项参数；[食人花咬击样例](examples/monster-chomper-bite-parameters/ModEntry.cs)说明原生动画伤害窗口、出招范围与取消边界。

原生 Sting 支持八项参数；[蜂刺样例](examples/monster-sting-parameters/ModEntry.cs)说明固定方向、身体快照、距离和名义速度。

原生 ChomperMimicry 支持四项参数；[拟态参数样例](examples/monster-mimicry-parameters/ModEntry.cs)说明冷却、诱饵寿命及其与主体钻地等待的独立关系。

原生 ThunderPile 支持十四项参数；[雷桩参数样例](examples/monster-thunder-parameters/ModEntry.cs)说明次数、逐桩准备和每根雷桩的独立配置，完整飞船及双炮台继续继承。

原生 ShadowImpact 支持八项参数；[残影冲刺样例](examples/monster-shadow-impact-parameters/ModEntry.cs)说明主体与两个残影的独立轨迹、前摇后读取及快照。

原生 BulletTrain 支持十四项参数；[列队射击样例](examples/monster-bullet-train-parameters/ModEntry.cs)说明主体路线与轮次快照、逐轮扇形读取和原样方向。

原生 Impact 支持十项参数；[冲击样例](examples/monster-impact-parameters/ModEntry.cs)说明前摇后读取方向、直线/弧线、控制偏移与身体快照。

原生 FungusShoot 支持八项参数；[菌类射击样例](examples/monster-fungus-parameters/ModEntry.cs)说明真实动画事件、固定三发扇形与 Drop 初速度。

原生 Battering 支持八项参数；[普通冲撞样例](examples/monster-battering-parameters/ModEntry.cs)说明出招距离、地形路径、身体快照和独立兄弟槽。

原生 BombingDive 支持十二项参数；[飞掠投弹样例](examples/monster-dive-parameters/ModEntry.cs)说明水平路径、目标高度/距离与墙体门、沿途向上投射及逐发读取。
