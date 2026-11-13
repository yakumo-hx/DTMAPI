# 怪物

DTMAPI 内置怪物模块。你可以在原版怪物的基础上做新怪物，改原版怪物的数值、掉落和攻击参数，决定它们在哪里出生，也可以给怪物配声音。

怪物 API 和内容格式都是 Experimental，以后的版本还会改，改动写进 CHANGELOG。

## 两种做法

- **内容包**：在 `PackData/content.json` 中描述怪物，不写代码。
- **代码 Mod**：引用 `DTMAPI.MonsterContent.dll`，用 `IMonsterContentApi` 注册定义、生成和移除怪物、写自定义行为。

两种可以一起用：一个包放内容，另一个包用代码调它的怪物，见[两个作者合作](MONSTER-DUAL-AUTHOR.md)。

## 内容包

在普通内容包的 `PackData/content.json` 中声明怪物，字段见[当前字段](MONSTER-075-CURRENT.md)，包的结构见[内容包指南](CONTENT-PACKS.md)。

## 代码 Mod

引用 `DTMAPI.Abstractions.dll` 和 `DTMAPI.MonsterContent.dll`（都设 `Private=false`，不要复制进包里）：

```csharp
using DTMAPI.MonsterContent;

var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId);
var result = api.RegisterDefinition(new MonsterDefinition
{
    LocalId = "my-bee",
    TemplateId = "bee",
    Health = 80
});
if (!result.Success)
    helper.Monitor.Log(result.Diagnostic.Message);

// 不再需要时释放。Mod 退出时也会自动释放。
result.Value.Dispose();
```

`IMonsterContentApi` 还提供：

| 方法 | 用来做什么 |
| --- | --- |
| `RegisterOverride` | 改原版怪物的字段，见[覆盖原版怪物](MONSTER-NATIVE-OVERRIDES.md)。 |
| `RegisterDropPool`、`RegisterSpawnRule` | 掉落池、出生规则。 |
| `RegisterBehavior` | 用代码写怪物的行为。只按顺序走位、出招的，也可以不写代码，用[行为序列](MONSTER-SEQUENCES.md)配置。 |
| `Spawn`、`Despawn`、`GetInstances` | 生成、移除、查询自己定义的怪物，见[实例](MONSTER-INSTANCE.md)。 |
| `GetTemplates`、`GetCatalog` | 查原版模板和当前的模板目录。 |

每次注册返回一个句柄，各自释放，互不影响。释放以后，同一个 ID 可以再注册。

## 声音

先用[音频 API](AUDIO-CONTENT.md) 注册声音，拿到 `IAudioSound`，再写进怪物定义：`new MonsterSound { Sound = sound, VolumeDb = -6 }`。受伤、技能这类普通声音用 `NativeMonster` 通道，死亡用 `NativeMonsterDeath`，爆炸用 `NativeMonsterExplosion`。内容包的声音写在 `content.json` 的 `audio` 一节。

声音准备失败时，怪物照样在，播原来的声音。

## 原版攻击的参数

通过 `GetCatalog` 查询当前模板和攻击槽开放的参数，字段含义见[原版覆盖指南](MONSTER-NATIVE-OVERRIDES.md)。

弹体的伤害和暴击在命中时才读，所以改了参数或释放覆盖，已经发出、还没命中的弹体也会跟着变。

## 更多

[当前字段](MONSTER-075-CURRENT.md) · [覆盖原版怪物](MONSTER-NATIVE-OVERRIDES.md) · [行为序列](MONSTER-SEQUENCES.md) · [实例](MONSTER-INSTANCE.md) · [血量阶段](MONSTER-HEALTH-PHASES.md) · [图鉴](MONSTER-COMPENDIUM.md) · [冲刺](MONSTER-DASH.md) · [散射技能](MONSTER-SCATTER.md) · [攻击动画](MONSTER-ACTION-ANIMATION.md) · [两个作者合作](MONSTER-DUAL-AUTHOR.md)

## 要注意的

- `overrides.scale` 只改显示大小，碰撞和攻击发射点不变；要改身体大小用 `body.scale`。
- 原版怪物的普通换图用官方 `Content`，不经过这个模块。
