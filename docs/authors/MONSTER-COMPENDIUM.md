# 怪物图鉴（Experimental）

本段在游戏原生怪物图鉴中展示作者条目。

## 定义条目

普通 C# 在 `MonsterDefinition` 中设置可选的 `Compendium`：

```csharp
definition.Compendium = new MonsterCompendium {
    Title = "青灯蛾",
    Habitat = "旧温室的阴影里",
    Description = "这只蛾的翅膀像两片浅绿色叶子。",
    Category = MonsterCompendiumCategory.Organism,
    ShowLocked = true,
    Png = originalPreviewBytes
};
```

`Title` 非空，三种文本均不设平台长度上限。文本不接受富文本标签和控制字符，允许换行。分类为 Organism、Machinery、Bionics 或 Boss。预览使用有效 PNG，不设平台文件大小或像素尺寸预算；登记时冻结复制，解码失败拒绝整个定义。图片由作者提供合法来源。

## 解锁与保存

Owner ID 与 Local ID 共同形成独立原生记录键。首次正常击败后，游戏原生死亡路径记录击杀并解锁图鉴；无奖励撤回不解锁。选择已解锁条目沿原生 UI 标记已读。`ShowLocked=true` 在击败前显示未知剪影；false 则首次击败后才显示。

击杀、解锁和已读只随原生游戏保存提交；未保存退出会回到上次提交状态。模块不增加 sidecar，不直接保存游戏，不补发历史死亡事件。说明为展示文本，不创建原生研究文档节点、不增加其节点计数或奖励。需要保存的实测必须遵循项目的存档规则。

## 缺包与撤回

对应定义未加载或被撤回时，图鉴中不显示该条目。原生击杀与已读记录保留。重新加载同一内容 ID 后，条目重新显示作者的标题、说明和预览，并沿用原有进度。

定义撤回只释放其表行、Sprite 和 Texture；不会删除历史原生记录，也不会修改其他 Owner 或模板条目。原版图鉴覆盖、研究章节/奖励、自定义解锁条件和持久活体 Boss 尚未开放。
