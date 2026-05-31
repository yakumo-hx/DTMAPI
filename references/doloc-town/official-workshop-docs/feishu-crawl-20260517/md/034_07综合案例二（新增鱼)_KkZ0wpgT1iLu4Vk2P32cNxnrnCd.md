# 07综合案例二（新增鱼)

Source: https://ka7deoo0opr.feishu.cn/wiki/KkZ0wpgT1iLu4Vk2P32cNxnrnCd

07 综合案例二（新增鱼)
07 综合案例二（新增鱼
)
用户5525
用户4888
用户2854
4月29日修改
一、整体说明
•
在
02 新增鱼
、
03 新增料理
（自由烹饪料理）的基础上新增：
◦
养殖鱼-道具信息
item_tbitem.json
、
◦
养殖鱼-孵化
fishing_tbfarmfish.json
◦
养殖鱼-杂交阵型与产出
fishing_tbfarmfishformation.json
、
item_tbitemspawn.json
◦
烘干机配方
recipe_tbrecipe.json
、
mod_tbmodrecipegroupextension.json
◦
加入食材组
mod_tbmodingredientgroupextension.json
◦
鱼图鉴
fishing_tbfishdocument.json
•
配置示例中，标黄的字段为【需要修改的字段】。
二、配置示例及数据结构说明
1.
item_tbitem.json
•
养殖鱼-道具信息
。
鱼信息-配置示例
JSON
[
// 以下为新增道具 ： 鲮鱼
{
"id": "
fish0
", //道具ID
"sub_type": "farm_fish", //子道具类型，farm_fish为鱼类
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": true, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": false, //是否显示在图鉴
"source": [
"fresh_water"  //获取途径（显示在图鉴）
],
"selling_price": 30, //售出价格
"buying_price": 60, //购买价格
"overlay": 999, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_fish0
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_fish0
", //道具标题ID，格式为：item_道具ID
"text": "
鲮鱼
" //道具标题文本
},
"description_basic": {
"key": "
item_fish0_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
普通的鲮鱼。
" //道具描述文本
},
"function": {
"$type": "ItemFunction"  //功能类型，ItemFunction为无功能道具
}
},
// 以下为新增道具 ： 蓝色鲮鱼
{
"id": "
fish1
", //道具ID
"sub_type": "farm_fish", //子道具类型，farm_fish为鱼类
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": true, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": false, //是否显示在图鉴
"source": [], //获取途径（显示在图鉴）
"selling_price": 50,, //售出价格
"buying_price": 100, //购买价格
"overlay": 999, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_fish1
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_fish1
", //道具标题ID，格式为：item_道具ID
"text": "
蓝色鲮鱼
" //道具标题文本
},
"description_basic": {
"key": "
item_fish1_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
蓝色的鲮鱼。
" //道具描述文本
},
"function": {
"$type": "ItemFunction"  //功能类型，ItemFunction为无功能道具
}
},
//以下为新增道具 鱼罐头
{
"id": "
fish_can
", //道具ID
"sub_type": "product_farm", //子道具类型，farm_fish为鱼类
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": false, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": true, //是否显示在图鉴
"source": [
"produce"  //获取途径（显示在图鉴）
],
"selling_price": 100, //售出价格
"buying_price": 200, //购买价格
"overlay": 999, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_fish_can
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_fish_can
", //道具标题ID，格式为：item_道具ID
"text": "
鱼罐头
" //道具标题文本
},
"description_basic": {
"key": "
item_fish_can_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
将鱼肉调味后封存于金属罐中，风味稳定，开罐食用。
" //道具描述文本
},
"function": {
"$type": "ItemFunctionFood", //功能类型，ItemFunctionFood为食物道具
"eating_effect": "fish_can"  //食用效果ID
}
},
//以下为新增道具 美味鱼罐头
{
"id": "
fish_can_plus
", //道具ID
"sub_type": "product_farm", //子道具类型，farm_fish为鱼类
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": false, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": true, //是否显示在图鉴
"source": [
"produce"  //获取途径（显示在图鉴）
],
"selling_price": 300, //售出价格
"buying_price": 600, //购买价格
"overlay": 999, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_fish_can_plus
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_fish_can_plus
", //道具标题ID，格式为：item_道具ID
"text": "
美味鱼罐头
" //道具标题文本
},
"description_basic": {
"key": "
item_fish_can_plus_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
将鱼肉精心调味后封存于金属罐中，风味优良，开罐食用。
" //道具描述文本
},
"function": {
"$type": "ItemFunctionFood" , //功能类型，ItemFunction为无功能道具
"eating_effect": "fish_can"  //食用效果ID
}
]
