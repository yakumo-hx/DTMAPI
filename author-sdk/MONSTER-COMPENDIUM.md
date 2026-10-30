# 怪物图鉴（Experimental）

当前配对为 Host 0.1.11、MonsterContent / 原生模块 0.1.10、Runtime / SDK 0.7.5，作者项目 v4、载荷 schema 2、绑定 `dtmapi.monsters/2` / 2.0.0。所有当前包统一声明 Host `[0.1.11,0.2.0)`；模板能力仍由实际目录逐项证明。

要求候选 Host `DTMAPI.Monsters >= 0.1.11, < 0.2.0`、配套共享契约及候选 Runtime 0.7.5。本段在游戏原生怪物图鉴中展示作者条目，仍未公开发布。

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

编辑器的“图鉴”页提供对应字段、原创 PNG 导入、预览选择和撤销。项目保存 `compendium.assetId`，载荷保存内容寻址的 `compendium.image`；编译器核对图片散列和全部字段。旧目录报告 `CompendiumUnsupported`；选中旧 Host 导出报告 `ExportHostIncompatible`。没有图鉴块的既有定义保持原行为。

完整入口见 [Strict C# 样例](examples/monster-compendium/ModEntry.cs)及[编辑器项目](../tools/monster-editor/samples/compendium.dtmonster.json)。样例图片由[生成脚本](../tools/monster-editor/scripts/create-compendium-sample.mjs)原创绘制，代码与配置都继承本机 bee 模板；图鉴名称和预览不改模板的原生条目。

## 解锁与保存

Owner ID 与 Local ID 共同形成独立原生记录键。首次正常击败后，游戏原生死亡路径记录击杀并解锁图鉴；无奖励撤回不解锁。选择已解锁条目沿原生 UI 标记已读。`ShowLocked=true` 在击败前显示未知剪影；false 则首次击败后才显示。

击杀、解锁和已读只随原生游戏保存提交；未保存退出会回到上次提交状态。模块不增加 sidecar，不直接保存游戏，不补发历史死亡事件。说明为展示文本，不创建原生研究文档节点、不增加其节点计数或奖励。需要保存的实测必须遵循项目的存档规则。

## 缺包与撤回

模块仍处于活动状态时，已解锁但对应定义缺失的条目显示“内容不可用”、Owner/Local ID 和通用预览。原生击杀与已读记录保留，恢复同一内容 ID 后重新显示作者图文。缺包条目只在打开图鉴期间临时挂接，关闭/标题/模块退出时撤回；没有额外持久目录。Host 或整个模块停用时，不承诺显示缺包占位。

定义撤回只释放其表行、Sprite 和 Texture；不会删除历史原生记录，也不会修改其他 Owner 或模板条目。原版图鉴覆盖、研究章节/奖励、自定义解锁条件和持久活体 Boss 尚未开放。
