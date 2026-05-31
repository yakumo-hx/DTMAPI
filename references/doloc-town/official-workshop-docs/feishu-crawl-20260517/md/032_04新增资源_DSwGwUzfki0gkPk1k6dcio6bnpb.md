# 04新增资源

Source: https://ka7deoo0opr.feishu.cn/wiki/DSwGwUzfki0gkPk1k6dcio6bnpb

04 新增资源
04 新增
资源
用户4888
用户5525
用户2854
4月17日修改
一、整体说明
•
资源模组由【4个】json文件组成，分别是：
◦
资源信息
resource_tbresource.json
◦
资源生成信息（决定资源在哪些场景生成）
mod_tbmodresourcespawnextension.json
◦
掉落物道具信息
item_tbitemspawn.json
◦
掉落库信息（决定资源掉落什么道具）
item_tbitemspawn.json
•
Content文件夹内必须包含上述【4个】文件
◦
（若掉落物并非新增物品，则无需包含
item_tbitemspawn.json
）
•
配置示例中，标黄的字段为【需要修改的字段】。
•
资源掉落物的
加工方式、资源图鉴
等【配套设施
】
需要通过【其他模组
】
实现。详见
08 综合案例三（新增资源）
。
二、配置示例及数据结构说明
1.
resource_tbresource.json
•
添加单阶段资源
沃土堆
（fertile_soil）的相关信息。
资源信息-配置示例
JSON
[
{
"id": "
fertile_soil
", //资源ID
"default_unlock": true, //是否默认解锁
"resource_type":
2
, //
资源类型
，详见对照表
"size": {
"x":
3
, //占地格子数：宽
"y":
2
//占地格子数：高
},
"pixel_offset": {
"x": 1, //最大偏移像素：左
"y": 1 //最大偏移像素：右
},
"spawn_months": [], //出现月份（为空表示没有限制）
"level_datas": [
{
"max_health": 10, //阶段生命值
"growth_value": {
"x": 0, //阶段成长值：最小值
"y": 0 //阶段成长值：最大值
},
"drop_spawn_entry": {
"spawn_lut": "
fertile_soil_drop
", //掉落库ID（决定掉落的道具）
"count_range": {
"min_count":
2
, //掉落数量范围：最小值
"max_count":
3
//掉落数量范围：最大值
}
},
"tech_points": [
{
"type": 1, //增加科技点类型（0自然，1操作，2科技，3养殖）
"count": 1 //增加科技点数值
}
],
"bullet_level_constraint": 0, //采集所需等级：无人机插件
"tool_constraints": [
{
"tool_type":
1
, //采集所需工具（0斧头,1镐子，2镰刀）
"tool_level":
0
//采集所需工具等级（0老旧，1铜，2铁，3钢，4钛）
}
],
"skins": [
{
"url": "
sprite_resource_fertile_soil
" //场景贴图
}
],
"sub_prefabs": [
{
"url": "" //预制体，仅红树有用，无需填写
}
]
}
],
"fit_slot_datas": [], //树脂数据，无需填写
"contained_slot_datas": [], //树脂数据，无需填写
"resin_collector_output": "" //树脂数据，无需填写
}
]
