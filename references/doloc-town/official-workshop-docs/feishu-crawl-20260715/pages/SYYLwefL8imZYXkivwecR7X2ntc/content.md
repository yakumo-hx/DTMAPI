# 01 文本本地化

Source: <https://ka7deoo0opr.feishu.cn/wiki/SYYLwefL8imZYXkivwecR7X2ntc>

Source modified label: 4月17日修改

## 一、整体说明

- 文本本地化由【3个】json文件组成，分别是：

- ◦英文localization_tbtextmapperen.json

- ◦简体中文localization_tbtextmapperzh_cn.json

- ◦繁体中文localization_tbtextmapperzh_tw.json

- 游戏默认读取的是各模块json配置文件中填写的文本，在设置中调整游戏语言后，会读取上述的本地化json配置文件中的文本。如果不配置本地化文本，则所有语言均会显示默认文本。

- 配置示例中，标黄的字段为【需要修改的字段】。

## 二、配置示例及数据结构说明

### 1.localization_tbtextmapperen.json

- 帕伊雅的头饰（hat0）、墨镜（hat1）的道具相关文本新增英文本地化。

_英文本地化-配置示例_

```json
[
  {
    "key": "item_hat0", //本地化ID
    "text": "Paiea's headpiece" //本地化文本
  },
  {
    "key": "item_hat0_desc", //本地化ID
    "text": "Cute little flower hair clip, the same style as Paiea's." //本地化文本
  },
  {
    "key": "item_hat1", //本地化ID
    "text": "Sunglasses" //本地化文本
  },
  {
    "key": "item_hat1_desc", //本地化ID
    "text": "Looking cool!" //本地化文本
  }
]
```

### 2.localization_tbtextmapperzh_cn.json

- 帕伊雅的头饰（hat0）、墨镜（hat1）的道具相关文本新增简体中文本地化。

_简体中文本地化-配置示例_

```json
[
  {
    "key": "item_hat0", //本地化ID
    "text": "帕伊雅的头饰" //本地化文本
  },
  {
    "key": "item_hat0_desc", //本地化ID
    "text": "可爱的小花发卡，帕伊雅同款。" //本地化文本
  },
  {
    "key": "item_hat1", //本地化ID
    "text": "墨镜" //本地化文本
  },
  {
    "key": "item_hat1_desc", //本地化ID
    "text": "扮得酷酷的！" //本地化文本
  }
]
```

### 3.localization_tbtextmapperzh_tw.json

- 帕伊雅的头饰（hat0）、墨镜（hat1）的道具相关文本新增繁体中文本地化。

_繁体中文本地化-配置示例_

```json
[
  {
    "key": "item_hat0", //本地化ID
    "text": "帕伊雅的頭飾" //本地化文本
  },
  {
    "key": "item_hat0_desc", //本地化ID
    "text": "可愛的小花髮夾，帕伊雅同款。" //本地化文本
  },
  {
    "key": "item_hat1", //本地化ID
    "text": "墨鏡" //本地化文本
  },
  {
    "key": "item_hat1_desc", //本地化ID
    "text": "扮得酷酷的！" //本地化文本
  }
]
```

## 三、参考示例

- 新增帕伊雅的头饰（hat0）、墨镜（hat1）的道具相关文本新增英文、简体中文、繁体中文的本地化。

- 示例路径（官方示例模组，获取方式见查看示例模组）

【创意工坊文件根目录】\Content\04 其他内容示例\01 文本本地化

## Links

- [查看示例模组](https://ka7deoo0opr.feishu.cn/wiki/GmfKwSHv0i5E9EkaupNcjRdIn4d)
