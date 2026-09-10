# JSON + PNG + WAV 自定义小动物指南

本指南说明如何用 DTMAPI 当前的“哈奇路线”做一个独立新小动物：作者只准备 JSON、PNG、WAV，不写 DLL，不复制游戏本体音频资源，也不修改原版动物。

这份文档按“能照着建包”的方式写。先做一个能跑的最小包，再慢慢调数值、美术和产物。示例动物叫 `my_mireling`，复用原版沼泽兽的动画机和 AI，但使用自己的 PNG 帧、自己的产物、自己的叫声 WAV。

## 能力状态

以下是本教程建立时的验证样例。当前 AnimalPack 的内容和剩余验收由[产品 Update](../../docs/updates/2026/20260827-0003-unified-animal-pack-economy-and-care-station.md)维护，历史来源与基线见[动物内容知识](../../docs/knowledge/products/animal-content.md)。

- 哈奇：复用原版 `chicken` 动画机和 AI，用 loose PNG 替换每一帧，用 `AnimalVoice` JSON + WAV 替换幼体/成年叫声。已通过 slot 7 自动 smoke 和用户手测，且不污染原版鸡。
- 抛壳蟹：复用 `goat` 行为，另有 AssetBundle 动画机路线，并已补 `AnimalVoice` JSON + WAV。用户手测确认幼体/成年声音通过，且不污染原版动物。

本指南优先讲哈奇同款 `pngSpriteOverride` 路线，因为它最贴近“JSON、PNG、WAV 即可”的作者工作流。抛壳蟹的 AssetBundle 路线更适合需要完整 Unity 动画机的高级包，不是这份最简教程的主线。

当前不覆盖：

- BGM/音乐/循环音频替换。
- DLL 代码 Mod。
- 公开稳定 C# API。`AnimalVoice` 现在是内容包 JSON 能力和 DTMAPI GameBridge 内部行为。
- 从零新增完全独立 AI。当前路线是“新物种复用原版模板 AI/动画机”。

## 如果你第一次写 JSON，先看这里

JSON 很挑格式。大多数“游戏没加载”不是美术问题，而是一处标点写错。先记这几条：

- 字段名和值里的文字要用英文双引号：`"speciesId": "my_mireling"`。
- 用英文逗号、英文冒号，不要用中文标点：`,`、`:`。
- 最后一项后面不要加逗号。
- JSON 里不能写 `// 注释`。
- 文件建议保存为 UTF-8。
- 不要改字段名。比如 `speciesId` 不能写成 `speciesID`、`species_id` 或 `物种ID`。
- 复制示例时，一次只改一个名字；改完就用 JSON 校验器或编辑器检查。

最容易漏改的名字可以按这个表全局搜索：

| 旧示例名 | 你要换成 |
| --- | --- |
| `my_mireling` | 你的物种 ID |
| `sack_my_mireling` | 你的动物袋物品 ID |
| `my_mireling_produce` | 你的产物掉落库 ID |
| `my_mireling_scale` | 你的产物物品 ID |
| `YourName.MyMireling` | 你的 DTMAPI `UniqueID` |
| `anim_animal_my_mireling` | 你的 PNG 资源名前缀 |

## 第一轮推荐路线

第一轮不要同时验证商店、产物、睡眠、美术细节和声音。推荐顺序：

1. 先让内容包能被游戏和 DTMAPI 识别。
2. 用调试方式拿动物袋，先放出动物，不依赖商店刷新。
3. 先确认外观、幼体/成年切换、抚摸/受击声音。
4. 再测睡觉/醒来。
5. 最后再调商店售卖、产物数量和价格。

这样出问题时，能更快判断是“动物本体没加载”，还是“商店/产物配置还没通”。

## 官方文档和 DTMAPI 扩展的关系

Doloc Town 官方小动物文档目前主要给出小动物 ID 对照和示例位置。官方 ID 表包含：

| 原版动物 | ID |
| --- | --- |
| 立尾雉 | `chicken` |
| 角羊驼 | `goat` |
| 沼泽兽 | `marsh_pangolin` |
| 变形蜜虫 | `slime` |

DTMAPI 在官方内容包之上额外读取：

```text
Content/DTMAPI/manifest.json
Content/DTMAPI/custom-animals.json
Content/DTMAPI/audio-replacements.json
```

官方 JSON 继续负责“这个动物是什么、在哪里卖、产什么、图鉴显示什么”。DTMAPI JSON 只负责“它复用哪个原版动画机/AI、如何把模板帧映射到自己的 PNG、如何把该物种叫声替换成 WAV”。

## 最小文件树

建议先照这个结构建包。把整个 `MyMirelingPack/` 放进本地内容包目录，不要只复制里面的 `Content/`。

```text
MyMirelingPack/
  info.json
  icon.png
  preview.png
  Content/
    animal_tbanimal.json
    animal_tbanimaldocument.json
    item_tbitem.json
    item_tbitemspawn.json
    mod_tbmodstoreextension.json
    Audio/
      my_mireling_pet_young.wav
      my_mireling_pet_adult.wav
    Sprites/
      my_mireling_frame_manifest.json
      anim_animal_my_mireling_young_idle_0.png
      ...
      anim_animal_my_mireling_adult_sleep_0.png
    DTMAPI/
      manifest.json
      custom-animals.json
      audio-replacements.json
```

`info.json` 是官方内容包入口。没有它，官方本地模组刷新可能会把包从启用列表里移除，DTMAPI 也就看不到这个内容包。

本地测试时，真正必须先有的是：

- 根目录 `info.json`。
- `Content/DTMAPI/manifest.json`。
- `Content/DTMAPI/custom-animals.json`。
- `Content/DTMAPI/audio-replacements.json`。
- 官方动物、物品、商店相关 JSON。
- PNG 和 WAV 文件。

`icon.png`、`preview.png` 可按发布需要保留。新内容包不需要创建 `Content/DTMAPI/dtmapi-package.json`；`manifest.json` 才是 DTMAPI 内容身份入口。旧包里的 `dtmapi-package.json` 只按非破坏性 legacy/build 元数据读取，永远不是安装回执，也不授权覆盖、移动、删除或修改启用状态。

## 命名规则

先定四个名字，后面所有 JSON 和文件都围绕它们：

| 名称 | 示例 | 用途 |
| --- | --- | --- |
| 物种 ID | `my_mireling` | `animal_tbanimal.json` 的 `id`，也是 DTMAPI `speciesId` |
| 包裹物品 ID | `sack_my_mireling` | 玩家买到/调试给到的动物袋 |
| 产物掉落库 ID | `my_mireling_produce` | `produce_spawn_entry.spawn_lut` |
| 产物物品 ID | `my_mireling_scale` | 实际产出的物品 |

推荐规则：

- 只用小写英文、数字、下划线。
- 不要复用原版 ID，也不要复用别的内容包 ID。
- PNG 资源名前缀统一用 `anim_animal_<species>`，例如 `anim_animal_my_mireling`。
- DTMAPI 动画机 key 统一用 `dtmapi_anim_animal_<species>` 和 `dtmapi_anim_animal_<species>_child`。
- 官方动物阶段一般在 PNG 文件名里叫 `young` / `adult`，但 DTMAPI 音频 JSON 的阶段字段叫 `child` / `adult`。这是当前游戏/DTMAPI 命名差异，要按各自字段写。

改名后再检查一次这些对应关系：

| 位置 | 必须一致 |
| --- | --- |
| `animal_tbanimal.json` 的 `id` | `custom-animals.json` 的 `speciesId`、动物袋 `preset_animal` |
| `Content/DTMAPI/manifest.json` 的 `UniqueID` | 日志里识别内容包时显示的包 ID |
| `levels[0].animator.url` | `custom-animals.json` 的 `childAnimatorKey` |
| `levels[1].animator.url` | `custom-animals.json` 的 `adultAnimatorKey` |
| `levels[*].sound_event` | `audio-replacements.json` 对应阶段的 `nativeSoundEvent` |
| `produce_spawn_entry.spawn_lut` | `item_tbitemspawn.json` 的 `id` |
| `mod_tbmodstoreextension.json` 的 `item_name` | 动物袋物品 ID |

## 选择模板

先选一个原版动物作为模板。模板决定 AI、移动/睡觉/吃饭状态、动画状态名、原生叫声事件。

| 模板 | 适合做什么 | `schedule_id` / `templateSpeciesId` / `aiTemplate` | 幼体推荐声音事件 | 成年推荐声音事件 | 当前验证状态 |
| --- | --- | --- | --- | --- | --- |
| 立尾雉 | 小型鸟类、小型宠物 | `chicken` | `PLAY_ANIMAL_PET_CHICKEN_CHILD` | `PLAY_ANIMAL_PET_CHICKEN` | 哈奇已通过 loose PNG、AI、幼体/成年声音、原版鸡不污染测试；最适合从哈奇改第一只新动物 |
| 角羊驼 | 中型四足动物 | `goat` | `PLAY_ANIMAL_PET_SHEEP_CHILD` | `PLAY_ANIMAL_PET_SHEEP` | 抛壳蟹已验证 AI 模板、AssetBundle 动画机和声音；loose PNG 路线使用同一套映射规则，但新包仍要手测 |
| 沼泽兽 | 大型低速动物 | `marsh_pangolin` | `PLAY_ANIMAL_PET_PANGOLIN` | `PLAY_ANIMAL_PET_PANGOLIN` | 田鼠采用的模板；新作者包仍需验证自己的 ID、素材和产物 |
| 变形蜜虫 | 小型软体/跳跃感动物 | `slime` | `PLAY_ANIMAL_PET_SLIME` | `PLAY_ANIMAL_PET_SLIME` | 油浮游采用的模板；新作者包仍需验证自己的 ID、素材和产物 |

表里的声音事件是“按模板推荐”的默认选择，不是 AI/动画模板的硬绑定。真正决定 `AnimalVoice` 是否命中的是：

```text
animal_tbanimal.json 的 levels[*].sound_event
+ audio-replacements.json 的 nativeSoundEvent
+ DTMAPI 已审核的动物短音效事件白名单
+ 当前动物上下文 speciesId/stage
```

高级作者可以在白名单内选择其他原版动物短音效事件，例如复用 `slime` AI 但使用 `PLAY_ANIMAL_PET_HONEY_AMOEBA`。这样做时，两个 JSON 的事件名必须完全一致，并且要手测确认原版模板动物没有被污染。第一只动物建议先用模板推荐事件。

## PNG 帧数要求

`pngSpriteOverride` 不是新建动画机，而是让原版模板动画机继续请求自己的原版帧名，然后 DTMAPI 把这些帧名映射到你的 PNG。你必须给够模板会请求的帧。

| 模板 | 阶段 | eat | idle | jump | move | sleep |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `chicken` | young | 7 | 4 | 2 | 4 | 1 |
| `chicken` | adult | 7 | 4 | 3 | 8 | 1 |
| `goat` | young | 3 | 4 | 1 | 8 | 1 |
| `goat` | adult | 4 | 4 | 2 | 8 | 1 |
| `marsh_pangolin` | young | 7 | 4 | 2 | 8 | 1 |
| `marsh_pangolin` | adult | 8 | 4 | 2 | 8 | 1 |
| `slime` | young | 4 | 4 | 8 | 4 | 4 |
| `slime` | adult | 4 | 4 | 8 | 4 | 4 |

文件名格式：

```text
anim_animal_my_mireling_young_idle_0.png
anim_animal_my_mireling_young_idle_1.png
anim_animal_my_mireling_young_idle_2.png
anim_animal_my_mireling_young_idle_3.png

anim_animal_my_mireling_adult_eat_0.png
...
anim_animal_my_mireling_adult_eat_7.png
```

注意：

- 每个 PNG 是一张单独帧，不是 spritesheet。
- 背景要透明，不要带白底、黑底或导出画布色。
- 同一个阶段的所有 PNG 建议使用同一画布尺寸。例如幼体全部 `32x18`，成年全部 `38x29`。
- 同一个阶段的所有 PNG 要让脚底、身体中心或主要接地点尽量对齐，否则走路时会跳动。
- 多给几张通常问题不大，但没有意义。原版 Animator 没有请求到的额外编号帧不会被 DTMAPI 主动扫描或播放，只会增加包体和维护成本。
- `jump_ready` 不需要单独画。当前哈奇路线会把模板的 `jump_ready` 映射到你的 `jump_0`。
- 先用复制帧占位也可以。比如你还没画完 `adult_eat_7`，可以先复制 `adult_eat_6` 命名为 `adult_eat_7`，保证行为链路先跑通。

### `sprite_size` 和判定大小

DTMAPI 的 PNG 路线不读取 `sprite_size` 来决定映射哪张 PNG，也不会拿它校验 PNG 画布大小。PNG 映射主要看 `templateSpritePrefix`、`customSpritePrefix` 和原版动画机请求的帧名。

但 `sprite_size` 不是装饰字段。它是官方 `animal_tbanimal.json` 的原生字段，游戏会把当前阶段的 `sprite_size` 转成动物渲染器的 `BoxCollider2D` 尺寸，并用它计算表情气泡偏移。也就是说，它更接近“交互/受击/可接触的身体核心范围”，不等于 PNG 画布，也不等于动物占几格。

相关字段要分开理解：

| 字段 | 主要影响 |
| --- | --- |
| PNG 画布 | 画面能放下多少透明边和动作外扩 |
| `levels[*].sprite_size` | 当前阶段的交互/碰撞框和表情偏移 |
| `size` | 动物在地面横向占格/寻路宽度 |
| `space` | 动物建筑容量占用 |

给作者的两条实用路线：

1. **最稳路线：沿用模板原版大小。** 复用 `chicken` 就先用鸡的幼体/成年 `sprite_size`、`size`、`space`；复用 `slime` 就先用蜜虫的值。美术可以有透明边、尾巴、特效和动作外扩，但判定手感先跟原版模板走。
2. **改体型路线：从模板值小步微调。** 如果新动物确实比模板更大或更小，再改 `sprite_size`。建议一次只调 1-3 像素，进游戏测试抚摸、受击、选中、靠近、转向、睡觉和吃饭，不要直接用 PNG 最大画布或自动 bbox 当最终判定。

例如 `slime` 模板原版 `sprite_size` 是 `19x9`。如果 `oilfloater` 的 PNG 画布是 `32x36`，第一轮更适合先用 `19x9` 或在这个量级上微调，而不是直接写成 `32x36`。如果当前写成 `31x34`，它更像“贴近整张 PNG 的大框”，需要靠游戏内手测确认是不是过大。

### 左右朝向和多余 PNG

当前 `pngSpriteOverride` 只按原版模板实际请求的 Sprite 名称映射。例如：

```text
anim_animal_chicken_adult_move_0
-> anim_animal_my_mireling_adult_move_0
```

运行时不会主动寻找这些文件：

```text
anim_animal_my_mireling_adult_move_0_left.png
anim_animal_my_mireling_adult_move_0_right.png
anim_animal_my_mireling_adult_move_left_0.png
anim_animal_my_mireling_adult_move_right_0.png
```

当前哈奇正式包也是这样：它只有 `adult/young + idle/move/eat/jump/sleep + 编号` 这一套 44 张 PNG，没有 left/right 两套资源。哈奇在游戏里能左右转向，是因为复用了原版 `AnimalRenderer` 的朝向/翻转逻辑，不是因为包里有两套方向图。

因此作者侧建议：

- 必须做：模板帧数表里列出的帧。
- 可以多放但不会用：超出模板编号范围的 PNG。
- 当前不会用：`left/right`、`l/r`、`左/右` 这类方向后缀 PNG。
- 不建议：把左朝向和右朝向混进普通编号帧里，例如 `move_0` 画左、`move_1` 画右。游戏会把它们当连续动画帧播放，不会按朝向挑选。
- 美术上要谨慎：文字、单边徽记、强非对称装饰会被原生水平翻转，可能在转向时看起来不对。

当前最简路线不支持“左边一套图、右边一套图”。如果以后要支持这种美术需求，需要新增 DTMAPI 字段和渲染侧方向选择逻辑，而不是只多放 PNG。

## 第 1 步：根目录 `info.json`

这是官方内容包元数据。先写一个最小可用版本：

```json
{
  "name": "My Mireling",
  "author": "YourName",
  "version": "0.1.0",
  "description": "Adds My Mireling as a DTMAPI JSON/PNG/WAV custom animal.",
  "tags": [
    "Mod",
    "Gameplay",
    "DTMAPI"
  ],
  "localized_name": {
    "schinese": "我的沼灵",
    "tchinese": "我的沼靈",
    "english": "My Mireling"
  },
  "localized_description": {
    "schinese": "添加一个复用沼泽兽模板的小动物。",
    "tchinese": "添加一個複用沼澤獸模板的小動物。",
    "english": "Adds a custom animal using the marsh pangolin template."
  }
}
```

## 第 2 步：`Content/DTMAPI/manifest.json`

这是 DTMAPI 内容包 manifest。`EntryDll` 留空，`Type` 写 `ContentPack`。

```json
{
  "Name": "My Mireling",
  "Author": "YourName",
  "Version": "0.1.0",
  "Description": "Content pack for a JSON/PNG/WAV custom animal.",
  "UniqueID": "YourName.MyMireling",
  "EntryDll": "",
  "MinimumDTMApiVersion": "0.5.2-alpha",
  "Type": "ContentPack",
  "Dependencies": []
}
```

`UniqueID` 必须全局唯一。建议用 `作者名.包名`。

## 第 3 步：`Content/DTMAPI/custom-animals.json`

这是 DTMAPI 自定义动物桥的核心配置。本文示例复用沼泽兽模板：

```json
[
  {
    "speciesId": "my_mireling",
    "templateSpeciesId": "marsh_pangolin",
    "aiTemplate": "marsh_pangolin",
    "animatorMode": "pngSpriteOverride",
    "adultAnimatorKey": "dtmapi_anim_animal_my_mireling",
    "childAnimatorKey": "dtmapi_anim_animal_my_mireling_child",
    "frameManifest": "Content/Sprites/my_mireling_frame_manifest.json",
    "templateSpritePrefix": "anim_animal_marsh_pangolin",
    "customSpritePrefix": "anim_animal_my_mireling"
  }
]
```

字段说明：

| 字段 | 必填 | 说明 |
| --- | --- | --- |
| `speciesId` | 是 | 你的新动物 ID，必须和 `animal_tbanimal.json` 的 `id` 相同 |
| `templateSpeciesId` | 是 | 复用哪个原版动画机，当前推荐 `chicken`、`goat`、`marsh_pangolin`、`slime` |
| `aiTemplate` | 建议写 | 复用哪个原版 AI 默认状态；一般和 `templateSpeciesId` 相同 |
| `animatorMode` | 是 | loose PNG 路线写 `pngSpriteOverride` |
| `adultAnimatorKey` | 是 | 你的成年动物在官方 `animal_tbanimal.json` 里引用的 animator URL |
| `childAnimatorKey` | 是 | 你的幼体动物在官方 `animal_tbanimal.json` 里引用的 animator URL |
| `frameManifest` | 建议写 | 给人和工具看的帧清单；当前桥主要按前缀映射帧名 |
| `templateSpritePrefix` | 是 | 原版模板帧名前缀 |
| `customSpritePrefix` | 是 | 你的 PNG 帧名前缀 |

当前 DTMAPI 运行时只读取上表这些字段。原型工具包里可能还会看到这些字段：

```text
movementMultiplier
metabolismMultiplier
packageItemId
shopItemListId
```

它们不是当前稳定作者接口，运行时不会靠这些字段改变移动、代谢、动物袋或商店。移动/代谢数值写在 `animal_tbanimal.json`，动物袋写在 `item_tbitem.json`，商店写在 `mod_tbmodstoreextension.json`。

不要把 `speciesId` 写成原版模板 ID。比如新动物复用鸡，也要写 `my_bird`，不要写 `chicken`。否则就失去隔离意义。

## 第 4 步：`Content/animal_tbanimal.json`

这是官方动物定义。它决定动物名、成长阶段、原生声音事件、动画机 key、大小、成长、产物等。

下面是一个完整可改的沼泽兽模板示例：

```json
[
  {
    "id": "my_mireling",
    "title": {
      "key": "animal_title_my_mireling",
      "text": "My Mireling"
    },
    "defaul_input_name": {
      "key": "animal_default_input_name_my_mireling",
      "text": "My Mireling"
    },
    "schedule_id": "marsh_pangolin",
    "levels": [
      {
        "sound_event": "PLAY_ANIMAL_PET_PANGOLIN",
        "animator": {
          "url": "dtmapi_anim_animal_my_mireling_child"
        },
        "sprite_size": {
          "x": 32,
          "y": 18
        },
        "icon": {
          "url": "anim_animal_my_mireling_young_idle_0"
        },
        "description_in_sack": {
          "key": "animal_child_desc_my_mireling",
          "text": "A small damp creature waits inside the sack."
        },
        "price": 4000
      },
      {
        "sound_event": "PLAY_ANIMAL_PET_PANGOLIN",
        "animator": {
          "url": "dtmapi_anim_animal_my_mireling"
        },
        "sprite_size": {
          "x": 38,
          "y": 29
        },
        "icon": {
          "url": "anim_animal_my_mireling_adult_idle_0"
        },
        "description_in_sack": {
          "key": "animal_adult_desc_my_mireling",
          "text": "A heavy sack gives off cool mist."
        },
        "price": 5600
      }
    ],
    "size": 3,
    "space": 4,
    "move_speed": 1.2,
    "run_speed": 3.6,
    "run_threshold": 3,
    "default_energy": 50,
    "eat_interval": 12,
    "eat_count": 20,
    "grow_interval": 12,
    "grow_cost": 6,
    "grow_increase": 1.04167,
    "fertility_interval": 72,
    "fertility_cost": 0,
    "fertility_increase": 1.04167,
    "breed_duration": 1152,
    "breed_energy_cost": 50,
    "metabolism_interval": 3,
    "metabolism_cost": 1.5,
    "metabolism_increase": 1.04167,
    "produce_spawn_entry": {
      "spawn_lut": "my_mireling_produce",
      "count_range": {
        "min_count": 1,
        "max_count": 1
      }
    },
    "produce_require_mood": 40,
    "excrete_interval": 288,
    "excrete_cost": 10,
    "hungry_threshold": 80,
    "mood_decrease_threshold": 50,
    "max_nature_mood": 80,
    "mood_increase_fondle": 10,
    "mood_decrease_thunder": 10,
    "escape_probability_base": 0.05,
    "escape_probability_increase": 0.05,
    "mood_affected_distance": 5,
    "mood_range_horizontal": 3,
    "mood_range_vertical_bottom": 2,
    "mood_range_vertical_top": 2,
    "produce_tech_point": 5,
    "breed_tech_point": 40,
    "manual_metabolism": true,
    "jump_height": 1.2
  }
]
```

最容易写错的字段：

- `id` 必须等于 `custom-animals.json` 的 `speciesId`。
- `schedule_id` 建议等于模板 ID。复用沼泽兽就写 `marsh_pangolin`。
- `levels[0].animator.url` 必须等于 `childAnimatorKey`。
- `levels[1].animator.url` 必须等于 `adultAnimatorKey`。
- `levels[*].sound_event` 要和后面的 `audio-replacements.json` 对上。
- `levels[*].sprite_size` 不要自动等于 PNG 画布；第一版建议沿用模板原版值，或者从模板值小步微调后手测显示、碰撞和选择框。
- `produce_spawn_entry.spawn_lut` 要和 `item_tbitemspawn.json` 的 `id` 对上。

### 动物数值速查

这些字段大多是官方 `animal_tbanimal.json` 的原生配置。DTMAPI 不重新解释它们；内容包启用后，由游戏的官方内容重载把这些值读进新的动物定义。

游戏里 1 天约等于 288 个 TU。下面的“约几天”只用于估算，实际还会受能量、心情、睡眠、设备、能否找到食物等条件影响。

| 想改什么 | 主要字段 | 怎么理解 |
| --- | --- | --- |
| 幼体长大要多久 | `grow_interval`、`grow_increase`、`grow_cost` | 幼体每过 `grow_interval` TU 尝试消耗能量并增加成长值；成长到 100 后进入预成年，通常到下一次换日变成年。 |
| 成年后多久能再次繁殖 | `fertility_interval`、`fertility_increase`、`fertility_cost` | 成年动物的繁殖进度从 0 到 100。进度满、有足够能量、同房间有同物种成年伙伴、房间容量够时，才会繁殖。 |
| 繁殖设备占用多久 | `breed_duration` | 原生育婴设备按这个时长运行；它是设备占用时间，不是动物怀孕表现。 |
| 繁殖消耗多少能量 | `breed_energy_cost` | 成功进入繁殖时从亲代扣除。 |
| 产物多久一次 | `metabolism_interval`、`metabolism_increase`、`metabolism_cost` | 成年动物的代谢进度从 0 到 100。进度满、心情满足、找到对应生产设备后，才会吐出产物。 |
| 产出什么 | `produce_spawn_entry.spawn_lut`、`produce_spawn_entry.count_range`、`item_tbitemspawn.json` | `spawn_lut` 指向产物掉落库；`count_range` 是一次生产要抽多少个产物单位。 |
| 一次能不能多个产物 | `count_range`、`spawn_datas[*].min_count/max_count` | 可以。底层产物是物品列表；但生产设备有容量上限，超量可能截断或挤满设备。第一版建议先做 1 个。 |
| 占多少房间容量 | `space` | 房间容量扣减用。繁殖也会检查家所在房间是否还能容纳新幼体。 |
| 地面/寻路体型 | `size` | 更像地面占格/横向体型，不等于 PNG 画布，也不等于 `sprite_size`。 |
| 交互/受击判定 | `levels[*].sprite_size` | 见前面的 `sprite_size` 说明。先沿用模板值最稳。 |
| 多久找一次食物、一次吃多少 | `eat_interval`、`eat_count` | 动物饥饿时每隔 `eat_interval` TU 重新找食物；找到普通饲料槽后拿走 `eat_count` 点饲料能量。 |
| 产物需要多少心情 | `produce_require_mood` | 心情不足时不会普通生产。 |
| 产后代谢是否归零 | `manual_metabolism` | `true` 时产出后代谢清零；`false` 时扣 100，超出的进度保留。原版角羊驼/沼泽兽是 `true`，鸡/蜜虫是 `false`。 |

`schedule_id` 和 `custom-animals.json` 里的 `aiTemplate` 决定动物使用哪套原版 AI 路线：鸡找鸡窝，蜜虫找蜂巢，角羊驼找粘毛滚筒，沼泽兽找挤奶机。只改 JSON 不能凭空新增第五种生产设备，也不能给同一种设备加“只服务某个物种”的过滤。

## 第 5 步：产物和动物袋

### `Content/item_tbitemspawn.json`

这个文件定义产物掉落库：

```json
[
  {
    "id": "my_mireling_produce",
    "spawn_datas": [
      {
        "spawn_weight": 1000,
        "min_count": 1,
        "max_count": 1,
        "item_name": "my_mireling_scale"
      }
    ]
  }
]
```

新作者最推荐一开始写成上面这样：一次生产 1 个，掉落库里只有 1 行，`min_count=1`、`max_count=1`。

原版有些表会用 `min_count=0`、`max_count=0` 表示“这一行可无限抽取”，再由 `produce_spawn_entry.count_range` 决定最终抽几个。它是原生掉落库的进阶语义，不适合第一轮排错。第一轮不要用 `0/0` 去证明产物路线。

如果你要一次生产多个物品，可以把 `produce_spawn_entry.count_range` 改大，或给 `spawn_datas` 写多行权重。注意生产设备存的是“物品单位”，不是“一次生产记录”；设备满了以后，多出来的产物可能进不去。

### `Content/item_tbitem.json`

这个文件至少放两个物品：产物、动物袋。

```json
[
  {
    "id": "my_mireling_scale",
    "sub_type": "husbandry_animal_product",
    "salable": true,
    "disposable": true,
    "consumable": false,
    "cookable": false,
    "electric_energy": 0,
    "viewable": false,
    "source": [],
    "selling_price": 120,
    "buying_price": 240,
    "overlay": 99,
    "ui_sprite_asset": {
      "url": "icon_item_milk"
    },
    "title": {
      "key": "item_my_mireling_scale",
      "text": "Mireling Scale"
    },
    "description_basic": {
      "key": "item_my_mireling_scale_desc",
      "text": "A damp scale shed by a mireling."
    },
    "function": {
      "$type": "ItemFunction"
    }
  },
  {
    "id": "sack_my_mireling",
    "sub_type": "husbandry_animal",
    "salable": true,
    "disposable": false,
    "consumable": true,
    "cookable": false,
    "electric_energy": 0,
    "viewable": false,
    "source": [],
    "selling_price": 2800,
    "buying_price": 5600,
    "overlay": 1,
    "ui_sprite_asset": {
      "url": "anim_animal_my_mireling_adult_idle_0"
    },
    "title": {
      "key": "item_sack_my_mireling",
      "text": "Sack (My Mireling)"
    },
    "description_basic": {
      "key": "item_sack_my_mireling_desc",
      "text": "A sack containing a mireling."
    },
    "function": {
      "$type": "ItemFunctionAnimalPackage",
      "ui_sprite_catch": {
        "url": "icon_item_sack_full"
      },
      "preset_animal": "my_mireling",
      "reusable": false,
      "type_when_full": "husbandry_animal"
    }
  }
]
```

`preset_animal` 必须等于你的动物 ID。否则打开袋子会生成错误动物或生成失败。

`ui_sprite_asset.url` 可以先用已有图标占位，例如 `icon_item_milk`。等链路跑通后，再替换成自己的物品图标。

### 隐藏产物和喂食贡献

原版支持“隐藏产物”：它不是普通产物掉落库里的随机副产物，而是一套单独的养殖进度。动物吃到特定来源的食物后，隐藏产物进度条会增长；进度达到阈值后，下一次生产会在普通产物之外额外吐出隐藏产物。原版鸡、角羊驼、沼泽兽、蜜虫都有这种额外养殖产物。

这条路线对应两个进阶表：

```text
Content/animal_tbhusbandry.json
Content/animal_tbhusbandryenergy.json
```

它们的关系是：

- `animal_tbhusbandry.json`：给某个动物 ID 定义隐藏输出、输出数量、进度阈值，以及是否只接受某些贡献来源。
- `animal_tbhusbandryenergy.json`：给某些可吃物品定义能量和隐藏产物进度贡献值。
- 动物生产时会先拿普通 `produce_spawn_entry` 的产物，再追加已经达到阈值的隐藏产物。

换成人话就是：隐藏产物进度条也是作者配置出来的。`animal_tbhusbandry.json` 里的阈值决定“进度条多长/多少点满”，`animal_tbhusbandryenergy.json` 里的贡献值决定“吃一次这种食物涨多少”，限制贡献来源的字段决定“哪些食物能涨这条隐藏进度”。

当前建议把隐藏产物当成第二轮或第三轮内容。默认饲料槽只补动物的饲料能量，不增长隐藏产物进度；能增长隐藏进度的是会把具体食物物品 ID 传给动物的吃食路径，例如吃农作物、牧草盆、野外/房间资源草。也就是说，隐藏产物是作者可以配置的原生能力，但不是“往默认饲料槽里放任意物品就会涨进度”的规则。第一只新动物先用普通产物跑通，隐藏产物单独手测。

### 原版生产设备容量和混养

不同模板会找不同生产设备。容量按“物品单位”算。

| 模板/路线 | 生产设备 | 原版容量 | 混养注意 |
| --- | --- | --- | --- |
| `chicken` | 鸡窝 | 10 | 鸡窝不记录来源物种，只存物品 ID。 |
| `slime` | 蜂巢 | 10 | 蜜虫模板动物会共用同一批蜂巢容量。 |
| `goat` | 粘毛滚筒 | 20 | 角羊驼模板动物会共用滚筒容量。 |
| `marsh_pangolin` | 挤奶机 | 20 | 挤奶机还需要处于可用/充能状态。 |

混养同一模板的新动物时，设备层没有物种过滤。例如两个不同自定义动物都复用 `slime`，它们都会找蜂巢，产物会混在同一设备里，容量也一起占。一个设备里确实可以存多种产物物品 ID；设备不记录这些产物来自哪只动物。想让玩家好区分，最好给不同动物使用不同产物物品 ID，并在手测里放一只原版模板动物确认没有视觉/声音污染。

## 第 6 步：图鉴

`Content/animal_tbanimaldocument.json`：

```json
[
  {
    "id": "my_mireling",
    "ui_sprite_asset": {
      "url": "anim_animal_my_mireling_young_idle_0"
    },
    "ui_adult_sprite_asset": {
      "url": "anim_animal_my_mireling_adult_idle_0"
    },
    "infancy_asset": {
      "url": "anim_animal_my_mireling_young_idle_0"
    },
    "adult_asset": {
      "url": "anim_animal_my_mireling_adult_idle_0"
    },
    "document_infos": [
      {
        "id": "my_mireling_0",
        "document_type": 1,
        "value": 0,
        "description_append": {
          "key": "document_animal_my_mireling_0",
          "text": "A custom animal using the marsh pangolin template."
        }
      },
      {
        "id": "my_mireling_1",
        "document_type": 4,
        "value": 1,
        "description_append": {
          "key": "document_animal_my_mireling_1",
          "text": "Its frames are loaded from loose PNG files."
        }
      }
    ],
    "display": true
  }
]
```

当前官方动物图鉴配置读取的是上面这套字段：

```text
ui_sprite_asset
ui_adult_sprite_asset
infancy_asset
adult_asset
document_infos
display
```

不要把原型生成包里的这些字段当成当前可直接发布的稳定格式：

```text
child_icon
adult_icon
child_preview
adult_preview
entries
```

除非未来 DTMAPI 明确提供转换工具并验证它们能进入官方表，否则作者包应使用本节示例里的官方字段名。

## 第 7 步：加入动物商店

`Content/mod_tbmodstoreextension.json`：

```json
[
  {
    "id": "animal_shop",
    "extra_items": [
      {
        "item_name": "sack_my_mireling",
        "storage": 0,
        "default_unlock": true,
        "season_spawn_data": [
          {
            "count_range": {
              "min_count": 1,
              "max_count": 2
            },
            "spawn_weight": 1000
          },
          {
            "count_range": {
              "min_count": 1,
              "max_count": 2
            },
            "spawn_weight": 1000
          },
          {
            "count_range": {
              "min_count": 1,
              "max_count": 2
            },
            "spawn_weight": 1000
          },
          {
            "count_range": {
              "min_count": 1,
              "max_count": 2
            },
            "spawn_weight": 1000
          }
        ]
      }
    ]
  }
]
```

如果只是第一轮链路测试，也可以先不依赖商店刷新，直接用 DTMAPI 调试控制台给 `sack_my_mireling`，确认袋子能放出动物。

`spawn_weight` 写 `0` 表示该季节不会自然刷出这件商品。四季都写 `0` 适合做 debug-only 原型，但不能用来验证“动物商店会卖”。

## 第 8 步：PNG 帧清单

`Content/Sprites/my_mireling_frame_manifest.json`：

```json
{
  "animal_id": "my_mireling",
  "display_name": "My Mireling",
  "animator_mode": "pngSpriteOverride",
  "template_sprite_prefix": "anim_animal_marsh_pangolin",
  "custom_sprite_prefix": "anim_animal_my_mireling",
  "canvas_by_stage": {
    "adult": {
      "width": 38,
      "height": 29
    },
    "young": {
      "width": 32,
      "height": 18
    }
  },
  "states": [
    {
      "stage": "young",
      "state": "idle",
      "frame_count": 4
    },
    {
      "stage": "young",
      "state": "move",
      "frame_count": 8
    },
    {
      "stage": "young",
      "state": "eat",
      "frame_count": 7
    },
    {
      "stage": "young",
      "state": "jump",
      "frame_count": 2
    },
    {
      "stage": "young",
      "state": "sleep",
      "frame_count": 1
    },
    {
      "stage": "adult",
      "state": "idle",
      "frame_count": 4
    },
    {
      "stage": "adult",
      "state": "move",
      "frame_count": 8
    },
    {
      "stage": "adult",
      "state": "eat",
      "frame_count": 8
    },
    {
      "stage": "adult",
      "state": "jump",
      "frame_count": 2
    },
    {
      "stage": "adult",
      "state": "sleep",
      "frame_count": 1
    }
  ],
  "notes": [
    "The marsh_pangolin template needs adult eat_0..7.",
    "jump_ready is mapped to jump_0 by the DTMAPI PNG route."
  ]
}
```

当前运行时主要按 `templateSpritePrefix` 和 `customSpritePrefix` 映射，不依赖清单枚举每个文件；但保留这个清单很有用：

- 作者自己检查帧数。
- 未来工具可以校验缺帧。
- 别人接手你的包时能看懂模板需求。

换句话说，当前运行时不会因为清单里写了 `frame_count`、`files` 或 `alias_of` 就自动生成或补齐 PNG。真正会被渲染请求到的 PNG 仍然要按文件名实际存在。`jump_ready -> jump_0` 是当前 DTMAPI PNG 路线里的固定兼容映射，不是从清单动态读取出来的规则。

## 第 9 步：WAV 叫声替换

`Content/DTMAPI/audio-replacements.json`：

```json
[
  {
    "id": "my-mireling-pet-child",
    "category": "AnimalVoice",
    "speciesId": "my_mireling",
    "stage": "child",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_PANGOLIN",
    "file": "Content/Audio/my_mireling_pet_young.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  },
  {
    "id": "my-mireling-pet-adult",
    "category": "AnimalVoice",
    "speciesId": "my_mireling",
    "stage": "adult",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_PANGOLIN",
    "file": "Content/Audio/my_mireling_pet_adult.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  }
]
```

为什么幼体和成年可以用同一个 `nativeSoundEvent`？

因为 `AnimalVoice` 不是只按事件名全局替换。DTMAPI 会在原生 `Animal.PlayAnimalSound()` 上下文里同时检查：

```text
speciesId + stage + nativeSoundEvent
```

所以 `my_mireling` 的幼体/成年可以共用原版 `PLAY_ANIMAL_PET_PANGOLIN`，但分别播放不同 WAV；原版沼泽兽不会被污染，其他复用沼泽兽模板的新动物也不会被污染。

`nativeSoundEvent` 的来源应当是同一只动物在 `animal_tbanimal.json` 里对应阶段的 `levels[*].sound_event`。只改 `audio-replacements.json` 不够；如果两个文件不一致，DTMAPI 会放行原版声音。

当前 `AnimalVoice` 支持的阶段：

```text
child
adult
```

当前已审核的动物短音效事件：

```text
PLAY_ANIMAL_PET_CHICKEN
PLAY_ANIMAL_PET_CHICKEN_CHILD
PLAY_ANIMAL_PET_SHEEP
PLAY_ANIMAL_PET_SHEEP_CHILD
PLAY_ANIMAL_PET_SLIME
PLAY_ANIMAL_PET_PANGOLIN
PLAY_ANIMAL_PET_PANGOLIN_CHILD
PLAY_ANIMAL_PET_HONEY_AMOEBA
PLAY_ANIMAL_PET_HONEY_AMOEBA_CHILD
```

写错会怎样：

- WAV 缺失：DTMAPI 不替换，原版声音照常播放。
- 路径试图 `../` 逃出包目录：DTMAPI 拒绝。
- `stage` 写成 `young`：DTMAPI 拒绝。这里必须写 `child`。
- 事件写成 BGM 或未审核事件：DTMAPI 拒绝。
- `speciesId` 写成模板 ID：会变成错误作用域，且有污染风险；不要这样写。

推荐 WAV：

- 使用短 WAV。
- 不要用 BGM 或循环音频。
- 文件放在包内，例如 `Content/Audio/`。
- 不要复制游戏本体音频资源。

## 换成其他模板怎么改

如果你把 `my_mireling` 改成别的模板，通常改这几处：

| 模板 | `schedule_id` | `templateSpeciesId` / `aiTemplate` | `templateSpritePrefix` | 幼体 `sound_event` | 成年 `sound_event` |
| --- | --- | --- | --- | --- | --- |
| `chicken` | `chicken` | `chicken` | `anim_animal_chicken` | `PLAY_ANIMAL_PET_CHICKEN_CHILD` | `PLAY_ANIMAL_PET_CHICKEN` |
| `goat` | `goat` | `goat` | `anim_animal_goat` | `PLAY_ANIMAL_PET_SHEEP_CHILD` | `PLAY_ANIMAL_PET_SHEEP` |
| `marsh_pangolin` | `marsh_pangolin` | `marsh_pangolin` | `anim_animal_marsh_pangolin` | `PLAY_ANIMAL_PET_PANGOLIN` | `PLAY_ANIMAL_PET_PANGOLIN` |
| `slime` | `slime` | `slime` | `anim_animal_slime` | `PLAY_ANIMAL_PET_SLIME` | `PLAY_ANIMAL_PET_SLIME` |

还要同步改：

- `animal_tbanimal.json` 两个 `levels[*].sound_event`。
- `audio-replacements.json` 两个 `nativeSoundEvent`。
- PNG 帧数量。
- `sprite_size`、`size`、`space`、移动速度、价格等数值。

第一只新动物最推荐两条路线：

- 从哈奇改：选 `chicken`，最接近已验证 loose PNG 产物和声音全链路。
- 从本文改：选 `marsh_pangolin`，适合较大的四足/低速动物，但要记得成年 `eat` 需要 8 帧。

`slime` 路线帧数更特殊，`jump` 和 `sleep` 都要 8/4 帧，适合第二轮再做。

换模板时不要只改 `templateSpeciesId`。至少要同步改 `schedule_id`、`aiTemplate`、`templateSpritePrefix`、帧数、两个阶段的 `sound_event`、两个 `nativeSoundEvent`，然后手测模板原版动物没有被污染。选择与你的动物动作接近的现有模板；其他包的通过记录不能代替新包自己的 ID、素材和行为验证。

## 用哈奇换一轮配置做模板探针

可以，但建议这样做：

1. 复制哈奇内容包到一个新文件夹。
2. 全局改成新的 `speciesId`、`UniqueID`、动物袋 ID、产物 ID。
3. 保留哈奇 PNG 作为临时占位。
4. 改模板字段到你想测试的路线。
5. 补足该模板缺的帧。
6. 手测通过后，再把占位图换成真正美术。

不要直接改已验证的哈奇包本体来试验。哈奇现在是回归样本，保留它能帮助判断新问题是框架问题还是新包配置问题。

## 启动游戏前检查

进游戏前先做一次静态检查：

- 每个 `.json` 都能被 JSON 解析器打开。
- 根目录有 `info.json`，不是只放了 `Content/`。
- `Content/DTMAPI/manifest.json` 的 `Type` 是 `ContentPack`，`EntryDll` 是空字符串。
- `custom-animals.json` 的 `speciesId` 等于 `animal_tbanimal.json` 的 `id`。
- `animal_tbanimal.json` 的 `schedule_id`、`custom-animals.json` 的 `templateSpeciesId` / `aiTemplate` 指向同一个模板。
- `levels[0].animator.url` / `levels[1].animator.url` 和 DTMAPI 的 child/adult key 完全一致。
- PNG 数量等于模板帧数表，不缺号。
- 同阶段 PNG 画布尺寸一致；`sprite_size` 是沿用模板值，或是有意小步微调后的判定尺寸。
- 两个 WAV 文件都存在，并在包目录内。
- 两个 `nativeSoundEvent` 都等于 `animal_tbanimal.json` 对应阶段的 `sound_event`。
- 图鉴 JSON 使用 `ui_sprite_asset`、`document_infos`、`display` 这套当前官方字段。
- 如果要测商店，四季至少有一个非零 `spawn_weight`。
- 如果要测普通产物，第一轮建议使用 `produce_spawn_entry.count_range` 为 `1..1`，并让目标产物行写 `min_count=1`、`max_count=1`。
- 如果要测隐藏产物，单独准备 `animal_tbhusbandry.json` / `animal_tbhusbandryenergy.json`，并确认喂食路径真的会把食物物品 ID 传给动物。

## 手测清单

首次接入一个新物种时使用下面的完整清单。修正已有包时按变化选择场景：

| 本次变化 | 对应验证 |
| --- | --- |
| 简介、翻译或教程文字 | 解析/引用及对应显示检查；不启动完整动物流程 |
| PNG、WAV 或其路径 | 受影响阶段和动作、模板原版动物不受污染；不保存 |
| AI 模板、生命周期或生产规则 | 受影响行为及回退；睡眠/成长只有改变相关规则时选入 |
| 新物种、保存格式或读回/停用恢复 | 完整接入或对应保存场景，fixture 一次准备后复用 |

先完成上面的启动前检查，并确定动物、动作、场景和预期结果。普通检查原地运行，不调用原生保存或玩家睡觉；确需保存、跨日或读回的场景按[产品验证工作流](../../docs/workflows/product-change-validation.md)选择可处置槽位。前提不满足先修前提；选中行为通过且无相关新异常后，退出即结束，不补跑未改变的全套链路。

1. 启用内容包，启动游戏。
2. 查看日志中是否有你的 `UniqueID` 被 DTMAPI 识别。
3. 查看日志中是否注册了你的 `custom-animals.json` 和 `audio-replacements.json`。
4. 用商店或调试控制台获得 `sack_my_mireling`。
5. 放出幼体，确认显示不是原版模板动物。
6. 幼体走路、待机、跳跃、睡觉、吃饭都不缺帧。
7. 抚摸幼体，听到你的幼体 WAV。
8. 攻击/受击幼体，确认同一动物声音路径也使用你的 WAV。
9. 让动物长成成年，或用调试手段切成年。
10. 成年走路、待机、跳跃、睡觉、吃饭都不缺帧。
11. 抚摸成年，听到你的成年 WAV。
12. 攻击/受击成年，确认同一动物声音路径也使用你的 WAV。
13. 放一只原版模板动物，例如原版沼泽兽/鸡/羊驼，确认它仍播放原版声音、显示原版贴图。
14. 在首次接入或改变睡眠/唤醒规则时验证跨日，确认不会卡在睡眠状态或夜间站起移动。
15. 检查产物是否进入预期产物路线。
16. 正常退出游戏，确认没有残留 `DolocTown.exe`。

建议记录的日志关键词：

```text
ContentPack
CustomAnimals.AnimatorBridge
CustomAnimals.PngSpriteBridge
CustomAnimals.AiTemplateBridge
AudioReplacement content-pack refresh
AudioReplacement event owner=<你的 UniqueID>
played=True suppressed=True
```

## 常见问题

### 游戏或 DTMAPI 完全没识别这个包

优先检查：

- 是否复制了整个包根目录，而不是只复制 `Content/`。
- 根目录是否有 `info.json`。
- `Content/DTMAPI/manifest.json` 是否存在，`Type` 是否是 `ContentPack`。
- JSON 是否有尾逗号、中文冒号、注释或少引号。
- 内容包是否在游戏的本地模组列表中启用。

### 游戏里还是原版动物外观

优先检查：

- `animal_tbanimal.json` 的 `levels[*].animator.url` 是否写成了 `dtmapi_anim_animal_<species>`。
- `custom-animals.json` 的 `adultAnimatorKey` / `childAnimatorKey` 是否完全一致。
- `templateSpritePrefix` 和 `customSpritePrefix` 是否写对。
- PNG 文件名是否按 `anim_animal_<species>_<stage>_<state>_<index>.png` 命名。
- 内容包是否真的启用，根目录是否有 `info.json`。

### 幼体/成年声音没有替换

优先检查：

- `audio-replacements.json` 是否在 `Content/DTMAPI/`。
- WAV 是否在包内，路径是否完全一致。
- `stage` 是否写 `child` / `adult`，不是 `young`。
- `speciesId` 是否等于你的动物 ID。
- `nativeSoundEvent` 是否等于 `animal_tbanimal.json` 里对应阶段的 `sound_event`。
- 日志里是否有 `rejected missing WAV`、`invalid AnimalVoice stage`、`unreviewed AnimalVoice event`。

### 图鉴不显示或图鉴字段没生效

优先检查 `animal_tbanimaldocument.json` 是否使用当前官方字段：

```text
ui_sprite_asset
ui_adult_sprite_asset
infancy_asset
adult_asset
document_infos
display
```

不要使用只在原型包或旧工具输出里出现的 `child_icon`、`adult_icon`、`entries` 字段。

### 原版鸡/羊驼/沼泽兽也变声了

这通常说明配置写成了模板 ID，或你用了旧的全局事件替换思路。`AnimalVoice` 应该写新物种自己的 `speciesId`。例如复用鸡的新动物也必须写：

```text
"speciesId": "my_bird"
```

不要写：

```text
"speciesId": "chicken"
```

### 动物袋打开后不是你的动物

检查 `item_tbitem.json` 里动物袋函数：

```text
"preset_animal": "my_mireling"
```

它必须等于 `animal_tbanimal.json` 的 `id`。

### 商店不卖

优先用调试控制台给动物袋，先验证动物链路。如果动物链路可用，再回头检查：

- `mod_tbmodstoreextension.json` 的 `id` 是否是 `animal_shop`。
- `extra_items[*].item_name` 是否是你的动物袋 ID。
- 四季 `season_spawn_data` 是否都有非零 `spawn_weight`。
- 官方模组是否启用。

### 沼泽兽路线缺成年吃饭帧

`marsh_pangolin` 成年 `eat` 需要 `eat_0` 到 `eat_7`，一共 8 帧。哈奇只有 7 帧，如果拿哈奇占位测试沼泽兽模板，要额外复制一张作为 `adult_eat_7`。

### 蜜虫路线缺跳跃或睡眠帧

`slime` 的幼体/成年都需要：

```text
jump_0..7
sleep_0..3
```

它比鸡/沼泽兽路线更容易缺帧。

### 角羊驼路线产物行为和原版不完全一样

当前最简路线建议先用普通 `produce_spawn_entry` 做一个自定义产物。原版角羊驼和沼泽兽更依赖手动生产设备，并且原版四种动物都还有隐藏养殖产物表。如果你要完全复刻那套隐藏/手动产物，请把 `animal_tbhusbandry.json` / `animal_tbhusbandryenergy.json` 当成单独功能测试，不要把它作为第一只新动物的验收条件。

### 两种自定义动物共用同一种设备，产物会不会串

会。原生设备内部存的是产物物品 ID 列表，所以同一个设备里确实可以同时存多种产物。它不记录“来自哪只动物”。这不是视觉或声音污染，但会影响玩家收产物时的体验。

最稳的做法：

- 同模板混养时给每个动物独立产物物品 ID。
- 给玩家放足设备容量。
- 第一轮测试时先单独养新动物，再加一只原版模板动物做隔离检查。

## 发布前检查

发布前至少检查这些：

- 所有 JSON 都能被 JSON 解析器读取，没有注释、尾逗号或中文标点冒号。
- 包根目录有 `info.json`。
- `Content/DTMAPI/manifest.json` 的 `UniqueID` 唯一。
- `Content/DTMAPI/manifest.json` 的 `EntryDll` 是空字符串，`Type` 是 `ContentPack`。
- 当前 DTMAPI 版本满足 `MinimumDTMApiVersion`。
- 没有 DLL。
- 没有游戏本体复制音频、反编译源码、官方二进制。
- 所有 WAV 都在包内路径。
- 所有 PNG 都在包内路径。
- PNG 帧数量和模板表一致，没有缺号；同阶段画布尺寸一致。
- `sprite_size` 沿用模板值或有意微调，不是从 PNG 画布/bbox 自动抄来的；相关交互手感已经手测过。
- `animal_tbanimaldocument.json` 使用当前官方字段，不使用未验证的原型字段。
- 如果声明商店可买，`spawn_weight` 不是全 0。
- 如果声明有普通产物，最小测试配置能明确产出 1 个物品。
- 如果声明有隐藏产物，已经单独测过特殊喂食路径和阈值。
- 原版模板动物没有被污染。
- 幼体和成年都手测过抚摸/受击声音。
- 至少测过一次睡觉/醒来和正常退出游戏。

## 当前文档的来源

这份指南整理自：

- Doloc Town 官方小动物 ID 和示例说明。
- DTMAPI 哈奇 loose PNG 自定义动物路线。
- DTMAPI 哈奇 `AnimalVoice` 声音替换路线。
- DTMAPI 抛壳蟹手测反馈和声音 JSON 试配。
- 当前 DTMAPI GameBridge 解析字段与白名单。
- 油浮游 `slime` 模板原型包静态审查。
- DTMAPI 自定义动物生产、繁殖、设备和隐藏产物代码级研究报告。
- 面向画师作者和成熟 Mod 作者的文档审查反馈。

文档只记录字段、文件树、行为边界和手测方法；不复制官方反编译源码或游戏二进制内容。
