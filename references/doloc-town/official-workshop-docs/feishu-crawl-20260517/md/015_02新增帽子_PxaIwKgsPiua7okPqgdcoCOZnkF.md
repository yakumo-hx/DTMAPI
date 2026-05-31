# 02新增帽子

Source: https://ka7deoo0opr.feishu.cn/wiki/PxaIwKgsPiua7okPqgdcoCOZnkF

02 新增帽子
02 新增
帽子
用户4888
用户5525
用户2854
4月29日修改
一、整体说明
•
帽子模组由【2个】json文件组成，分别是：
◦
道具信息
item_tbitem.json
◦
帽子信息
player_tbhat.json
•
Content文件夹内必须包含上述【2个】文件。
•
配置示例中，标黄的字段为【需要修改的字段】。
•
新增帽子的【获取途径
】
需要通过【其他内容模组
】
实现。详见
综合案例一（新增帽子）
。
二、json文件数据结构说明
item_tbitem.json
•
新增道具：
帕伊雅的头饰。
帽子道具信息-配置示例
JSON
{
"id": "
hat0
", //道具ID
"sub_type": "kit_hat", //子道具类型，kit_hat为帽子道具
"salable": true, //是否允许出售
"disposable": true, //是否允许丢失
"consumable": false, //是否为消耗品
"cookable": false, //是否允许烹饪
"electric_energy": 0, //提供发电量(0则不发电)
"viewable": false, //是否显示在图鉴
"source": [], //获取途径（显示在图鉴）
"selling_price":
250
,  //售出价格
"buying_price":
500
, //购买价格
"overlay": 1, //堆叠上限
"ui_sprite_asset": {
"url": "icon_item_
hat0
" //道具图标，格式为：icon_item_道具ID
},
"title": {
"key": "
item_hat0
", //道具标题ID，格式为：item_道具ID
"text": "
帕伊雅的头饰
" //道具标题文本
},
"description_basic": {
"key": "
item_hat0_desc
", //道具描述ID，格式为：item_道具ID_desc
"text": "
可爱的小花发卡，帕伊雅同款。
" //道具描述文本
},
"function": {
"$type": "ItemFunctionHat", //功能类型，ItemFunctionHat为帽子道具
"hat_id": "hat0"//帽子ID，同道具ID
}
player_tbhat.json
•
新增帽子：
帕伊雅的头饰。
帽子信息-配置示例
JSON
{
"id": "
hat0
", //帽子ID，同道具ID
"skill": "",  //技能ID，无特殊情况为空
"defense": 0, //提供防御力
"idle_sprite": {
"url": "
anim_hat_hat0_idle_0
" //帽子贴图-正面，格式为：anim_hat_[帽子ID]_idle_0
},
"climb_sprite": {
"url": "
anim_hat_hat0_climb_0
" //帽子贴图-翻转，格式为：anim_hat_[帽子ID]_climb_0
},
"preview": {
"url": "
preview_hat_hat0
" //帽子贴图-展示台，格式为：preview_hat_[帽子ID]
},
"animator": {
"url": "" //帽子动画机，留空
},
"material": {
"url": "" //帽子材质，留空
},
"prefab": {
"url": "" //帽子预制体，留空
}
,
"hide_hair":
false
// 是否需要隐藏原本的发型(false: 不需要，true: 需要)
}
