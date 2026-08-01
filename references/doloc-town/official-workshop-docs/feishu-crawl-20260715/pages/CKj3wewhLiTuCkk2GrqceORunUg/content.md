# 05 新增商店道具

Source: <https://ka7deoo0opr.feishu.cn/wiki/CKj3wewhLiTuCkk2GrqceORunUg>

Source modified label: 4月20日修改

## 一、整体说明

- 创意工坊版本更新后，电话亭里新增了两个商店，分别是 ‘工坊贸易部’（一般商店）与 ‘工坊兑换部’（兑换商店）。(两个商店仅在玩家订阅并启用相关模组后才可见)

- ‘工坊贸易部’类似普通NPC商店，可以买卖，但仅可通过金币购买。‘工坊兑换部’则类似兑换商店，仅供物品兑换。玩家可以通过创意工坊将物品配置进两个商店中供玩家兑换或购买。

- 一般商店配方模组由【1个】json文件组成，即：

- ◦一般商店的商品信息mod_tbmodstoreextension.json

- 兑换商店配方模组由【1个】json文件组成，即：

- ◦兑换商店的商品信息mod_tbmodexchangestoreextension.json

- Content文件夹内必须包含上述至少【1个】文件。

- 配置示例中，标黄的字段为【需要修改的字段】。

- 需要【第二天】商店刷新后，才会出现新道具。

## 二、配置示例及数据结构说明

### 1.一般商店

#### mod_tbmodstoreextension.json

- 电话亭商店：工坊贸易部（phone_booth_shop）上架道具：垃圾盆栽（equipment0）。

_一般商店商品信息-配置示例_

```json
[
  {
    "id": "phone_booth_shop", //一般商店ID
    "extra_items": [
      {
        "item_name": "equipment0", //商品ID
        "storage": 0, //商品全局存量，即整局游戏最多能卖多少个（0则无存量上限）
        "default_unlock": true, //是否默认解锁
        "season_spawn_data": [ //以下分别为四个月份的商品属性
          {
            "count_range": {
              "min_count": 1, //单次出现最小数量：一月
              "max_count": 1 //单次出现最大数量：一月
            },
            "spawn_weight": 0 //商品刷新权重：一月（0则固定刷新）
          },
          {
            "count_range": {
              "min_count": 1, //单次出现最小数量数量：二月
              "max_count": 1 //单次出现最大数量：二月
            },
            "spawn_weight": 0 //商品刷新权重：二月（0则固定刷新）
          },
          {
            "count_range": {
              "min_count": 1, //单次出现最小数量数量：三月
              "max_count": 1 //单次出现最大数量：三月
            },
            "spawn_weight": 0 //商品刷新权重：三月（0则固定刷新）
          },
          {
            "count_range": {
              "min_count": 1, //单次出现最小数量数量：四月
              "max_count": 1 //单次出现最大数量：四月
            },
            "spawn_weight": 0 //商品刷新权重：四月（0则固定刷新）
          }
        ]
      }
    ]
  }
]
```

- 商品售价读取对应道具在【item_tbitem.json】中的【selling_price】。

- 一般商店ID见09 ID对照表（商店）。

### 2.兑换商店

#### mod_tbmodexchangestoreextension.json

- 电话亭兑换商店：工坊兑换部（phone_booth_exchange_shop）上架道具：帕伊雅的头饰（hat0）。

_配方信息-配置示例_

```json
[
  {
    "id": "phone_booth_exchange_shop", //兑换商店ID
    "extra_items": [
      {
        "ranged_item": {
          "item_name": "hat0", //商品ID
          "min_count": 1, //最小输出数量
          "max_count": 1 //最大输出数量
        },
        "storage": 0, //商品全局存量，即整局游戏最多能卖多少个（0则无存量上限）
        "gold_cost": 100, //消耗金币数量（-1则根据道具售价自动填写）
        "item_costs": [
          {
            "item_name": "weeds", //消耗道具ID
            "item_count": 10 //消耗道具数量
          },
          {
            "item_name": "monster_drop_chomper", //消耗道具ID
            "item_count": 1 //消耗道具数量
          }
        ],
        "pre_condition_item_name": "", //前置商品ID
        "pre_condition_item_count": 0, //前置商品数量
        "tech_point": 1, //增加科技点点数
        "default_unlock": true //是否默认解锁
      }
    ]
  }
]
```

- 兑换商店ID见09 ID对照表（商店）。

## 三、参考案例

- 示例路径（官方示例模组，获取方式见查看示例模组）

【创意工坊文件根目录】\Content\02 基础内容模组示例\05 新增商店道具

## Links

- [09 ID对照表（商店）](https://ka7deoo0opr.feishu.cn/wiki/B9UqwiqJziQi3ckQVlLcCt4Zn9c)
- [查看示例模组](https://ka7deoo0opr.feishu.cn/wiki/GmfKwSHv0i5E9EkaupNcjRdIn4d)
