# 06综合案例一（新增道具）

Source: https://ka7deoo0opr.feishu.cn/wiki/KWqewbBLEiRw1Pkcn6lcZpQ7nXg

06 综合案例一（新增道具）
用户4888
4月29日修改
一、整体说明
•
通过以下【2个】json文件的配合，实现
新增并获取
一般道具的全流程：
◦
道具信息
item_tbitem.json
◦
掉落库信息
mod_tbmoditemspawnextension.json
•
游戏内操作指南：
◦
通过
采集泥土资源、翻垃圾桶
的行为，有概率获得新道具
沃土块
。
•
配置示例中，标黄的字段为【需要修改的字段】。
二、配置示例及数据结构说明
1.
item_tbitem.json
•
新增道具
：沃土块。
装饰设备道具信息-配置示例
JSON
[
{
"id": "
fertile_soil
", //道具ID
"sub_type": "
material_nature
", //
子道具类型
，material_nature为自然素材
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": false, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": true, //是否显示在图鉴
"source": ["gather"], //
获取途径
（显示在图鉴），"gather"为“采集”
"selling_price": 100, //售出价格
"buying_price": 500, //购买价格
"overlay": 99, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_fertile_soil
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_fertile_soil
", //道具标题ID，格式为：item_道具ID
"text": "
沃土块
" //道具标题文本
},
"description_basic": {
"key": "
item_fertile_soil_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
肥沃的土壤，可以用来堆肥。
" //道具描述文本
},
"function": {
"$type": "
ItemFunction
" //
功能类型
}
]
2.
mod_tbmoditemspawnextension.json
•
资源土堆、交互物垃圾桶的
掉落库
soil_drop也增加沃土块（fertile_soil）
掉落
。
掉落库信息-配置示例
JSON
[
{
"id": "
soil_drop
", //掉落库ID。详见对照表
"extra_items": [
{
"spawn_weight":
200
,  //生成权重
"min_count": 0, //生成下限
"max_count": 0, //生成上限（0则无上限）
"item_name": "
fertile_soil
" //生成道具ID
}
]
},
{
"id": "
trash_can_small_drop
", //掉落库ID。详见对照表
"extra_items": [
{
"spawn_weight":
200
,  //生成权重
"min_count": 0, //生成下限
"max_count": 0, //生成上限（0则无上限）
"item_name": "
fertile_soil
" //生成道具ID
}
]
}
]
