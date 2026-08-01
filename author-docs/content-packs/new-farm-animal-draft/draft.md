一、整体说明
- 本说明用于制作基于 DTMAPI 的 JSON + PNG + WAV 新增养殖动物内容包。
- 通过以下【10个】json文件的配合，实现新增并获取养殖动物的全流程：
- 包展示信息 info.json
- DTMAPI 内容包清单 Content/DTMAPI/manifest.json
- 自定义动物桥 Content/DTMAPI/custom-animals.json
- 动物声音替换 Content/DTMAPI/audio-replacements.json
- 动物基础数据 Content/animal_tbanimal.json
- 动物袋道具 Content/item_tbitem.json
- 产物掉落库 Content/item_tbitemspawn.json
- 动物商店扩展 Content/mod_tbmodstoreextension.json
- 动物图鉴信息 Content/animal_tbanimaldocument.json
- PNG 帧清单 Content/Sprites/hatch_frame_manifest.json
其中，Content/DTMAPI/manifest.json、Content/DTMAPI/custom-animals.json、Content/DTMAPI/audio-replacements.json 为被 DTMAPI 识别的核心配置。
- 配置示例中，标黄的字段为【需要修改的字段】。
说明：标黄内容表示从哈奇实包复制到自己的动物包时必须替换的包名、作者、物种 ID、动物袋 ID、动画键、PNG 前缀、WAV 路径或显示文本。未标黄的 chicken、原版声音事件和 animal_shop，在继续使用哈奇/鸡路线时可以先保持不变。
二、配置示例及数据结构说明
以下 JSON 均来自本机哈奇实包。复制成自己的包时，示例里的 hatch、sack_hatch、hatch_produce、anim_animal_hatch、DTMAPI.HatchAssets 等标黄字段都应替换为自己的唯一值。
1. info.json
- 位置：包根目录。
- 用途：创意工坊/本地包的展示信息。
- 第一轮本地测试时它不会决定动画和声音是否生效，但包名、作者、版本建议写清楚。
{
  "name": "示例模组",    // 模组名（没有指定翻译文本时会显示的默认名字）
  "author": "xxx",    // 作者名
  "version": "1.0.0",    // 版本号
  "description": "包含了一些示例模组。",    // 模组描述（没有指定翻译文本时会显示的默认描述）
  "tags": [    // 模组标签
    "Mod",
    "..."
  ],
  "localized_name": {
    "schinese": "",    // 模组名的中文翻译（默认名为中文时可以留空）
    "tchinese": "範例模組",    // 模组名的繁体中文翻译
    "english": "Example Mod"    // 模组名的英文翻译
  },
  "localized_description": {
    "schinese": "",    // 模组简介的中文翻译（默认简介为中文时可以留空）
    "tchinese": "包含了一些範例模組。",    // 模组简介的繁体中文翻译
    "english": "Includes some example mods."    // 模组简介的英文翻译
  }
}
{
  "name": "DTMAPI Hatch Assets",
  "author": "DTMAPI",
  "version": "0.5.3-alpha-hatch-png-prototype",
  "description": "Developer-only local package. Adds Hatch to validate DTMAPI PNG custom livestock loading.",
  "tags": ["Mod", "Gameplay", "Functional", "DTMAPI", "Developer"],
  "localized_name": {
    "schinese": "DTMAPI Hatch Assets",
    "tchinese": "DTMAPI Hatch Assets",
    "english": "DTMAPI Hatch Assets"
  }
}
2. Content/DTMAPI/manifest.json
- 用途：DTMAPI 识别内容包。
- Type 写 ContentPack；纯 JSON/PNG/WAV 包的 EntryDll 为空字符串。
- UniqueID 必须唯一，建议使用 作者名.包名。
{
  "Name": "DTMAPI Hatch Assets",
  "Author": "DTMAPI",
  "Version": "0.5.3-alpha-hatch-png-prototype",
  "Description": "Developer-only content pack for proving Hatch PNG custom animal loading.",
  "UniqueID": "DTMAPI.HatchAssets",
  "EntryDll": "",
  "MinimumDTMApiVersion": "0.5.2-alpha",
  "Type": "ContentPack",
  "Dependencies": []
}
3. Content/DTMAPI/custom-animals.json
- 用途：DTMAPI 自定义动物桥的核心配置。
- animatorMode 当前 PNG 路线写 pngSpriteOverride。
- packageItemId 对应动物袋道具；shopItemListId 对应商店扩展目标。
[
  {
    "speciesId": "hatch",
    "templateSpeciesId": "chicken",
    "aiTemplate": "chicken",
    "animatorMode": "pngSpriteOverride",
    "adultAnimatorKey": "dtmapi_anim_animal_hatch",
    "childAnimatorKey": "dtmapi_anim_animal_hatch_child",
    "frameManifest": "Content/Sprites/hatch_frame_manifest.json",
    "templateSpritePrefix": "anim_animal_chicken",
    "customSpritePrefix": "anim_animal_hatch",
    "movementMultiplier": 1.0,
    "metabolismMultiplier": 1.0,
    "packageItemId": "sack_hatch",
    "shopItemListId": "animal_shop"
  }
]
4. Content/DTMAPI/audio-replacements.json
- 用途：按物种和阶段替换动物叫声。
- speciesId 必须写新物种，不能写模板 chicken，否则会污染原版鸡。
- nativeSoundEvent 必须和 animal_tbanimal.json 中对应阶段的 sound_event 完全一致。
[
  {
    "id": "hatch-pet-child",
    "category": "AnimalVoice",
    "speciesId": "hatch",
    "stage": "child",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_CHICKEN_CHILD",
    "file": "Content/Audio/hatch_pet_young.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  },
  {
    "id": "hatch-pet-adult",
    "category": "AnimalVoice",
    "speciesId": "hatch",
    "stage": "adult",
    "nativeSoundEvent": "PLAY_ANIMAL_PET_CHICKEN",
    "file": "Content/Audio/hatch_pet_adult.wav",
    "suppressNativeWhenReady": true,
    "cooldownMilliseconds": 80
  }
]
5. Content/animal_tbanimal.json
- 用途：定义游戏里这只动物是什么、怎么移动、怎么成长、怎么产出。
- id 是动物 ID，必须等于 speciesId。
- schedule_id 决定使用哪套原版 AI 行为；哈奇沿用 chicken。
- produce_spawn_entry.spawn_lut 指向产物掉落库。哈奇当前示例产物是原版 meat。
[
  {
    "id": "hatch",
    "title": { "key": "animal_title_hatch", "text": "Hatch" },
    "defaul_input_name": { "key": "animal_default_input_name_hatch", "text": "Hatch" },
    "schedule_id": "chicken",
    "levels": [
      {
        "sound_event": "PLAY_ANIMAL_PET_CHICKEN_CHILD",
        "animator": { "url": "dtmapi_anim_animal_hatch_child" },
        "sprite_size": { "x": 32, "y": 26 },
        "icon": { "url": "anim_animal_hatch_young_idle_0" },
        "description_in_sack": { "key": "animal_child_desc_hatch", "text": "A small Hatch waits inside the sack." },
        "price": 640
      },
      {
        "sound_event": "PLAY_ANIMAL_PET_CHICKEN",
        "animator": { "url": "dtmapi_anim_animal_hatch" },
        "sprite_size": { "x": 36, "y": 34 },
        "icon": { "url": "anim_animal_hatch_adult_idle_0" },
        "description_in_sack": { "key": "animal_adult_desc_hatch", "text": "A Hatch waits inside the sack." },
        "price": 900
      }
    ],
    "size": 1,
    "space": 2,
    "move_speed": 1.8,
    "run_speed": 5.4,
    "produce_spawn_entry": {
      "spawn_lut": "hatch_produce",
      "count_range": { "min_count": 1, "max_count": 1 }
    },
    "produce_require_mood": 40,
    "manual_metabolism": false,
    "jump_height": 2
  }
]
6. Content/item_tbitem.json
- 用途：新增“动物袋”道具。玩家拿到这个袋子后，释放出对应物种。
- id 对应 custom-animals.json.packageItemId。
- function.preset_animal 必须等于动物 ID。
[
  {
    "id": "sack_hatch",
    "sub_type": "husbandry_animal",
    "salable": true,
    "disposable": false,
    "consumable": true,
    "ui_sprite_asset": { "url": "anim_animal_hatch_adult_idle_0" },
    "title": { "key": "item_sack_hatch", "text": "Sack (Hatch)" },
    "description_basic": { "key": "item_sack_hatch_desc", "text": "A sack containing a Hatch." },
    "function": {
      "$type": "ItemFunctionAnimalPackage",
      "ui_sprite_catch": { "url": "icon_item_sack_full" },
      "preset_animal": "hatch",
      "reusable": false,
      "type_when_full": "husbandry_animal"
    }
  }
]
7. Content/item_tbitemspawn.json
- 用途：定义动物产物掉落库。
- id 对应 animal_tbanimal.json.produce_spawn_entry.spawn_lut。
- 第一轮建议先产原版已有道具，例如哈奇使用 meat；确认链路通过后再新增自己的产物道具。
[
  {
    "id": "hatch_produce",
    "spawn_datas": [
      {
        "spawn_weight": 1000,
        "min_count": 1,
        "max_count": 1,
        "item_name": "meat"
      }
    ]
  }
]
8. Content/mod_tbmodstoreextension.json
- 用途：把动物袋追加到原版动物商店。
- id 写目标商店列表；哈奇使用 animal_shop。
- extra_items[*].item_name 写动物袋道具 ID。
[
  {
    "id": "animal_shop",
    "extra_items": [
      {
        "item_name": "sack_hatch",
        "storage": 0,
        "default_unlock": true,
        "season_spawn_data": [
          { "count_range": { "min_count": 2, "max_count": 4 }, "spawn_weight": 0 },
          { "count_range": { "min_count": 2, "max_count": 4 }, "spawn_weight": 0 },
          { "count_range": { "min_count": 2, "max_count": 4 }, "spawn_weight": 0 },
          { "count_range": { "min_count": 1, "max_count": 3 }, "spawn_weight": 0 }
        ]
      }
    ]
  }
]
9. Content/animal_tbanimaldocument.json
- 用途：图鉴/说明显示。
- id 仍然等于动物 ID。
- 图鉴资源通常引用待机帧，例如 anim_animal_hatch_young_idle_0 和 anim_animal_hatch_adult_idle_0。
[
  {
    "id": "hatch",
    "ui_sprite_asset": { "url": "anim_animal_hatch_young_idle_0" },
    "ui_adult_sprite_asset": { "url": "anim_animal_hatch_adult_idle_0" },
    "infancy_asset": { "url": "anim_animal_hatch_young_idle_0" },
    "adult_asset": { "url": "anim_animal_hatch_adult_idle_0" },
    "document_infos": [
      {
        "id": "hatch_0",
        "document_type": 1,
        "value": 0,
        "description_append": { "key": "document_animal_hatch_0", "text": "Hatch PNG custom animal prototype." }
      },
      {
        "id": "hatch_1",
        "document_type": 4,
        "value": 1,
        "description_append": { "key": "document_animal_hatch_1", "text": "Uses chicken behavior while its frames are loaded directly from PNG files." }
      }
    ],
    "display": true
  }
]
10. Content/Sprites/hatch_frame_manifest.json
- 用途：给作者和工具看的帧清单。
- 当前运行时主要按帧文件名和前缀映射；清单不会自动生成或补齐 PNG。
- 哈奇实包里 jump_ready 记录为 jump_0 的别名，因此没有单独的 jump_ready PNG。
{
  "animal_id": "hatch",
  "display_name": "Hatch",
  "animator_mode": "pngSpriteOverride",
  "template_sprite_prefix": "anim_animal_chicken",
  "custom_sprite_prefix": "anim_animal_hatch",
  "canvas_by_stage": {
    "adult": { "width": 32, "height": 32 },
    "young": { "width": 28, "height": 24 }
  },
  "states": [
    { "stage": "adult", "state": "idle", "frame_count": 4 },
    { "stage": "adult", "state": "move", "frame_count": 8 },
    { "stage": "adult", "state": "eat", "frame_count": 7 },
    { "stage": "adult", "state": "sleep", "frame_count": 1 },
    { "stage": "adult", "state": "jump", "frame_count": 2 },
    { "stage": "adult", "state": "jump_ready", "alias_of": "jump", "frame_count": 1 },
    { "stage": "young", "state": "idle", "frame_count": 4 },
    { "stage": "young", "state": "move", "frame_count": 8 },
    { "stage": "young", "state": "eat", "frame_count": 7 },
    { "stage": "young", "state": "sleep", "frame_count": 1 },
    { "stage": "young", "state": "jump", "frame_count": 2 },
    { "stage": "young", "state": "jump_ready", "alias_of": "jump", "frame_count": 1 }
  ]
}
三、动画帧图片格式要求
1. 图片文件要求
- 每一帧都是独立 PNG，不是 spritesheet。
- PNG 放在 Content/Sprites/ 下。
- 文件名格式建议固定为 anim_animal_你的动物ID_阶段_动作_编号.png。
- 阶段名使用 young 和 adult；动作名使用 idle、move、eat、jump、sleep。
- 不要准备 left/right 两套方向图。哈奇实包没有左右方向 PNG，游戏里的左右朝向来自原版渲染器翻转。
- 同一阶段的 PNG 画布尺寸建议一致。哈奇实包：幼体 22 张都是 28x24；成年 22 张都是 32x32。
- sprite_size 是动物交互/显示手感字段，不是 DTMAPI 用来校验 PNG 画布的字段。第一轮建议沿用模板或小步微调，不要直接把 PNG 最大尺寸抄进去。
2. 模板动作帧数量
下面按“实际需要准备的 PNG 文件数”整理。哈奇 chicken 路线来自本机哈奇实包；其他模板为当前路线的参考计数，换模板时必须重新手测。
模板/路线              阶段    eat  idle  jump  move  sleep  PNG合计
Hatch chicken路线       young   7    4     2     8     1      22
Hatch chicken路线       adult   7    4     2     8     1      22
goat                   young   3    4     1     8     1      17
goat                   adult   4    4     2     8     1      19
marsh_pangolin         young   7    4     2     8     1      22
marsh_pangolin         adult   8    4     2     8     1      23
slime                  young   4    4     8     4     4      24
slime                  adult   4    4     8     4     4      24
3. 哈奇文件名示例
anim_animal_hatch_young_idle_0.png
anim_animal_hatch_young_idle_1.png
anim_animal_hatch_young_idle_2.png
anim_animal_hatch_young_idle_3.png

anim_animal_hatch_adult_move_0.png
anim_animal_hatch_adult_move_1.png
...
anim_animal_hatch_adult_move_7.png

anim_animal_hatch_adult_sleep_0.png
四、参考示例
1. 哈奇实包文件结构
DTMAPI_HatchAssets/
  info.json
  Content/
    animal_tbanimal.json
    animal_tbanimaldocument.json
    item_tbitem.json
    item_tbitemspawn.json
    mod_tbmodstoreextension.json
    Audio/
      hatch_pet_young.wav
      hatch_pet_adult.wav
    DTMAPI/
      manifest.json
      custom-animals.json
      audio-replacements.json
    Sprites/
      hatch_frame_manifest.json
      anim_animal_hatch_young_idle_0.png
      anim_animal_hatch_young_move_0.png
      ...
      anim_animal_hatch_adult_sleep_0.png
2. 哈奇关键 ID 对照
用途                         哈奇实包值
包 UniqueID                  DTMAPI.HatchAssets
动物 speciesId / animal id    hatch
复用模板                      chicken
AI 模板                       chicken
原版帧名前缀                  anim_animal_chicken
自定义帧名前缀                anim_animal_hatch
幼体 animator key             dtmapi_anim_animal_hatch_child
成年 animator key             dtmapi_anim_animal_hatch
动物袋道具                    sack_hatch
产物掉落库                    hatch_produce
商店列表                      animal_shop
幼体声音事件                  PLAY_ANIMAL_PET_CHICKEN_CHILD
成年声音事件                  PLAY_ANIMAL_PET_CHICKEN
3. 从哈奇复制一只新动物的顺序
1. 复制 DTMAPI_HatchAssets 到新文件夹，不要直接修改哈奇本体。
2. 先改包 ID：DTMAPI.HatchAssets 改成自己的 UniqueID。
3. 再改动物 ID：把 hatch 统一改成自己的 speciesId。
4. 改动物袋：sack_hatch 改成自己的动物袋 ID，并同步 packageItemId 和 preset_animal。
5. 改 PNG 前缀：anim_animal_hatch 改成自己的 customSpritePrefix，并按同样前缀重命名所有 PNG。
6. 替换 WAV：保持 audio-replacements.json 的阶段和声音事件一致，只改 file 路径 和文件。
7. 第一轮可以继续产原版 meat，先确认动物显示、声音和生产链路通过。
8. 最后再调价格、商店数量、图鉴文本、产物和数值。
4. 启动前检查清单
- Content/DTMAPI/manifest.json 存在，Type 为 ContentPack，EntryDll 为空。
- speciesId、animal_tbanimal.id、preset_animal 三者一致。
- adultAnimatorKey / childAnimatorKey 和 levels[*].animator.url 一致。
- templateSpeciesId、aiTemplate、schedule_id 同步指向同一个模板。
- nativeSoundEvent 和 levels[*].sound_event 完全一致。
- PNG 数量和编号完整；同阶段画布尺寸一致。
- WAV 路径从包根目录可找到，例如 Content/Audio/hatch_pet_young.wav。
- 商店扩展追加的是动物袋 ID，不要覆盖整个原版商店列表。
5. 游戏内手测顺序
1. 启动游戏后确认日志里注册了内容包、custom-animals.json 和 audio-replacements.json。
2. 拿到动物袋，确认袋子图标和名称不是原版鸡。
3. 释放幼体，确认显示为新 PNG，不是原版鸡。
4. 抚摸幼体，确认播放幼体 WAV。
5. 等待成长或用测试流程进入成年，确认成年 PNG 正常。
6. 抚摸成年，确认播放成年 WAV。
7. 观察待机、移动、吃饭、睡觉、跳跃，不应缺帧或闪回原版图。
8. 收取产物，确认产物进入对应设备或掉落逻辑。
9. 同时放一只原版鸡，确认原版鸡贴图和声音没有被污染。
10. 保存、退出、重进，再看新动物仍能正常加载。
