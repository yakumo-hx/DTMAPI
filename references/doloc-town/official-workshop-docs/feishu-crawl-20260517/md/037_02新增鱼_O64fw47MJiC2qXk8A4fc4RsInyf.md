# 02新增鱼

Source: https://ka7deoo0opr.feishu.cn/wiki/O64fw47MJiC2qXk8A4fc4RsInyf

02 新增鱼
02 新增
鱼
用户4888
用户5525
用户2854
4月17日修改
一、整体说明
•
鱼模组由【3个】json文件组成，分别是：
◦
道具信息
item_tbitem.json
◦
鱼信息
fishing_tbfish.json
◦
鱼对应的池塘（决定鱼刷新在哪些水域）
mod_tbmodfishingpoolextension.json
•
Content文件夹内必须包含上述【3个】文件。
•
配置示例中，标黄的字段为【需要修改的字段】。
•
鱼的
档案、养殖、加工
途径等【配套设施
】
需要通过【其他模组
】
实现。详见
07 综合案例二（新增鱼)
。
二、配置示例及数据结构说明
1.
item_tbitem.json
•
新增道具：
鲮鱼。
道具信息-配置示例
JSON
[
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
]
2.
fishing_tbfish.json
•
新增鱼：
鲮鱼。
鱼信息-配置示例
JSON
[
{
"id": "
fish0
", //鱼ID
"default_unlock": true, //是否默认解锁
"is_fish": true, //是否会触发钓鱼小游戏
"is_garbage": false, //是否是垃圾
"rarity": 0, //稀有度（0 ~ 4）
"size": 0,  //体型（0 ~ 2）
"exp_fishing": 1, //增加科技点数
"month": [], //出现月份
，可选填1~4
（为空表示没有限制）
"weather_types": [], //出现天气（可
选
填：
1(
晴天
)
、
2(
多云
)
、
3(
雨天
)
、
4(
雷雨
)
、
5(
大风
)
、
6(
酸雨
)
、
7(
烈日
)
，为空表示没有限制）
"time_range": {
"start_time": 0, //出现时间-起点（0~24，为0则没有限制）
"end_time": 0 //出现时间-终点（0~24，为0则没有限制）
},
"fish_bait": [], //暂时无用
"fishing_rod_lv": 0, //渔具等级
"fishing_lv": 0, //暂时无用
//以下是钓鱼小游戏的相关参数
"max_stamina": {
"min_count": 40, //总耐力-最小值
"max_count": 40 //总耐力-最大值
},
"stable_duration": {
"min_count": 5, //平稳期时长-最小值
"max_count": 10 //平稳期时长-最大值
},
"struggle_duration": {
"min_count": 3, //挣扎期时长-最小值
"max_count": 5 //挣扎期时长-最大值
},
"note_speed_multiplier": 1.5, //滚动速度倍率
"initial_stable_probability": 1, //初始平稳
"catch_multiplier": 1, //被捕倍率
"escape_speed": 3, //逃逸速度
"struggle_multiplier": 3, //挣扎倍率
"bonus_probability": 0.2, //奖励点概率
"bonus_duration": {
"min_count": 2, //奖励期时长-最小值
"max_count": 2 //奖励期时长-最大值
},
"bonus_base_score": 5, //基础奖励分
"bonus_extra_score": 0, //额外奖励分
"bonus_multiplier": 1.2 //奖励倍率
}
]
