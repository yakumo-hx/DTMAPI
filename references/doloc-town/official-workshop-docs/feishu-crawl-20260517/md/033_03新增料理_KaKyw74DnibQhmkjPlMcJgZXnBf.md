# 03新增料理

Source: https://ka7deoo0opr.feishu.cn/wiki/KaKyw74DnibQhmkjPlMcJgZXnBf

03 新增料理
03 新增
料理
用户4888
用户5525
用户2854
5月14日修改
一、整体说明
•
固定配方
料理
（如烤末芋）模组由【4个】json文件组成，分别是：
◦
料理道具信息
item_tbitem.json
◦
料理食用效果
item_tbeatingeffect.json
◦
固定配方信息
recipe_tbrecipe.json
◦
将配方加入对应炊具
mod_tbmodrecipegroupextension.json
•
自由烹饪
料理
（如末日杂拌）
模组由【5个】json文件组成，分别是：
◦
料理道具信息
item_tbitem.json
◦
料理食用效果
item_tbeatingeffect.json
◦
定义食材类（用于自由烹饪）
recipe_tbingredientgroup.json
◦
自由烹饪食谱
recipe_tbdish.json
◦
将食谱加入对应炊具
mod_tbmoddishgroupextension
.json
•
Content文件夹内必须包含上述【4或5个】文件。
•
配置示例中，标黄的字段为【需要修改的字段】。
•
添加料理的道具信息后，还可通过【新增配方】来实现【固定配方料理
】
，详见
04 新增配方
。
二、配置示例及数据结构说明
1.
新增固定配方料理
1.1
item_tbitem.json
•
新增道具：
末芋罐头。
道具信息-配置示例
JSON
[
{
"id": "
endyam_can
", //道具ID
"sub_type": "product_farm", //子道具类型，farm_fish为鱼类
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": false, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": true, //是否显示在图鉴
"source": [
"cook"  //获取途径（显示在图鉴）
],
"selling_price": 100, //售出价格
"buying_price": 200, //购买价格
"overlay": 999, //堆叠上限
"ui_sprite_asset": {
"url": "
icon_item_endyam_can
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_endyam_can
", //道具标题ID，格式为：item_道具ID
"text": "
末芋罐头
" //道具标题文本
},
"description_basic": {
"key": "
item_endyam_can_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
能有效充饥、长久保存。但口感真的不怎么样。
" //道具描述文本
},
"function": {
"$type": "ItemFunctionFood", //功能类型，ItemFunctionFood为食物道具
"eating_effect": "
endyam_can
"  //食用效果ID
}
]
