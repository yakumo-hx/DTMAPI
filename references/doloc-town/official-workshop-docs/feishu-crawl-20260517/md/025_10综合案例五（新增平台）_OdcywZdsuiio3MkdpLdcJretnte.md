# 10综合案例五（新增平台）

Source: https://ka7deoo0opr.feishu.cn/wiki/OdcywZdsuiio3MkdpLdcJretnte

10 综合案例五（新增平台）
用户6390
用户4888
用户5525
5月15日修改
一、整体说明
•
通过以下【4个】json文件的配合，实现
新增并获取
平台道具/配方的全流程：
◦
道具信息
item_tbitem.json
◦
平台信息
platform_tbplatform.json
◦
配方信息
recipe_tbrecipe.json
◦
配方对应的平台
mod_tbmodrecipegroupextension.json
•
游戏内操作指南：
◦
自己制作：
▪
去
手工工作台
制作
纤维平台（简易
纹理
平台）
、
岩木平台（随机纹理平台）
。
•
配置示例中，标黄的字段为【需要修改的字段】。
二、配置示例及数据结构说明
1.
简易
纹理
平台
1.1
item_tbitem.json
•
新增道具：
纤维平台
（platform0）。
道具信息-配置示例
JSON
[
{
"id": "
platform0
", //道具ID
"sub_type": "construction_platform", //子道具类型
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": true, //是否为消耗品
"cookable": false, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": false, //是否显示在图鉴
"source": [], //获取途径（显示在图鉴）
"selling_price": 2, //售出价格
"buying_price": 10, //购买价格
"overlay": 999, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_platform0
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_platform0
", //道具标题ID，格式为：item_道具ID
"text": "
纤维平台
" //道具标题文本
},
"description_basic": {
"key": "
item_platform0_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
用
纤维
搭建的平台，样式简单，可以在手工工作台制作。
" //道具描述文本
},
"function": {
"$type": "ItemFunctionPlatform" //功能类型，ItemFunctionPlatform为平台道具
}
]
1.2
platform_tbplatform.json
•
录入平台信息：
简易
纹理
平台（platform0）。
平台信息-配置示例
JSON
[
{
"id": "
platform0
", //平台ID，同道具ID
"scene_sprite_asset": {
"url": "
preview_craft_platform0
" //平台详情图，格式为preview_craft_[平台ID]
},
"platform_suface": {
"left_sock": {
"url": "
tile_platform_platform0_platform_left_0
" //平台表面左侧瓦片贴图，格式为tile_platform_[平台ID]_platform_left_0
},
"left_sequence": [], //平台表面左侧衔接瓦片贴图组（可选）
"fill_tiles": [
{
"sprite_asset": {
"url": "
tile_platform_platform0_platform_middle_0
" //平台表面中间部分随机瓦片贴图1，格式为tile_platform_[平台ID]_platform_middle_0
},
"weight": 100 //该贴图出现权重
}
],
"right_sequence": [], //平台表面右侧衔接瓦片贴图组（可选）
"right_sock": {
"url": "
tile_platform_platform0_platform_right_0
" //平台表面右侧瓦片贴图，格式为tile_platform_[平台ID]_platform_right_0
}
},
"platform_left_column": {
"fixed_sequence": [], //平台左柱体顶部瓦片贴图组（可选）
"fill_tiles": [], //平台左柱体中间部分随机瓦片贴图组（可选）
"column_bottom_tile": {
"url": "
tile_platform_platform0_column_left_bottom_0
"//平台左柱体底部瓦片贴图，格式为tile_platform_[平台ID]_column_left_bottom_0
}
},
"platform_right_column": {
"fixed_sequence": [], //平台右柱体顶部瓦片贴图组（可选）
"fill_tiles": [], //平台右柱体中间部分随机瓦片贴图组（可选）
"column_bottom_tile": {
"url": "
tile_platform_platform0_column_right_bottom_0
" //平台右柱体底部瓦片贴图，格式为tile_platform_[平台ID]_column_right_bottom_0
}
]
