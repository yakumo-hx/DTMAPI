# 01新增道具

Source: https://ka7deoo0opr.feishu.cn/wiki/OFS1w4gFSiDkHRkXX1Pcu4ihnNh

01 新增道具
01 新增
道具
用户4888
用户5525
用户2854
4月29日修改
一、整体说明
•
道具模组由【1个】json文件组成，即：道具信息
item_tbitem.json
。
•
Content文件夹内必须包含上述【1个】文件。
•
配置示例中，标黄的字段为【需要修改的字段】。
•
新增道具的【获取途径
】
需要通过【其他内容模组
】
实现。详见
06 综合案例一（新增道具）
、
07 综合案例二（新增帽子）
、
08 综合案例三（新增设备）
。
•
官方参考模板中，提供了各不同类型道具的模板，详见
数据模板
。（部分内容比较复杂还是请先看完文档示例。）
二、配置示例及数据结构说明
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
•
子道具类型、获取途径、功能类型见
ID对照表（道具）
。
三、图片格式要求
四、图片命名对照表
