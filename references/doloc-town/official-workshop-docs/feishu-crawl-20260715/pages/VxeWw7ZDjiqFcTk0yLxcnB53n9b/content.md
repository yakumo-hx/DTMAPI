# 04 新增配方

Source: <https://ka7deoo0opr.feishu.cn/wiki/VxeWw7ZDjiqFcTk0yLxcnB53n9b>

Source modified label: 4月29日修改

## 一、整体说明

- 配方模组由【2个】json文件组成，分别是：

- ◦配方信息recipe_tbrecipe.json

- ◦配方对应的设备mod_tbmodrecipegroupextension.json

- Content文件夹内必须包含上述【2个】文件。

- 配置示例中，标黄的字段为【需要修改的字段】。

- 若要新增料理食谱配方，见03 新增料理。

## 二、配置示例及数据结构说明

### recipe_tbrecipe.json

新增配方：1*垃圾盆栽（equipment0） = 1*树种（seed_tree） + 1*垃圾（rubbish）+ 1*土块（soil）。

_配方信息-配置示例_

```json
[
  {
    "id": "equipment0", //配方ID，一般等同道具ID
    "default_unlock": true, //是否默认解锁
    "title": {
      "key": "", //配方文本（为空则根据输出道具自动生成）
      "text": "" //配方标题（为空则根据输出道具自动生成）
    },
    "cost_time": 0, //制作时间（单位为TU，即游戏内5min）
    "output_item": {
      "item_name": "equipment0", //输出道具的道具ID
      "min_count": 1, //最小输出数量
      "max_count": 1 //最大输出数量
    },
    "input_items": [
      {
        "item_name": "seed_tree", //消耗道具的道具ID
        "item_count": 1 //消耗道具数量
      },
      {
        "item_name": "rubbish", //消耗道具的道具ID
        "item_count": 1 //消耗道具数量
      },
      {
        "item_name": "soil",//消耗道具的道具ID
        "item_count": 1 //消耗道具数量
      }
    ],
    "recipe_sub_type": "none", //配方小类，见对照表
    "tech_point": 1, //完成配方增加的科技点数
    "show_in_handbook": true //配方是否显示在图鉴
  }
]
```

- 配方小类ID见08 ID对照表（配方）

### mod_tbmodrecipegroupextension.json

- 在设备工作台（manual_workbench）新增【垃圾盆栽】的设备制作配方（decoration0)。

_配方组信息-配置示例_

```json
[
  {
    "id": "equipment_workbench", //配方组ID，同设备ID
    "extra_recipes": [
      "decoration0" //配方ID（多个配方则用","隔开）
    ]
  }
]
```

- 常用配方组ID见08 ID对照表（配方）

## 三、参考示例

- 示例路径（官方示例模组，获取方式见查看示例模组）

【创意工坊文件根目录】\Content\02 基础内容模组示例\04 新增配方

## Links

- [03 新增料理](https://ka7deoo0opr.feishu.cn/wiki/KaKyw74DnibQhmkjPlMcJgZXnBf)
- [08 ID对照表（配方）](https://ka7deoo0opr.feishu.cn/wiki/Vm9YwaBVMiZtvbkf5kIcASronye)
- [查看示例模组](https://ka7deoo0opr.feishu.cn/wiki/GmfKwSHv0i5E9EkaupNcjRdIn4d)
