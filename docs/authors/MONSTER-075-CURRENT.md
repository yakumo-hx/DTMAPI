# 0.7.5 当前怪物作者入口

当前使用 Runtime 0.7.5 内置模块；内部作者项目仍为 v4。以本次 Runtime 导出的目录为字段能力来源。现用项目须重导出；没有旧包迁移或兼容承诺。

新定义可编辑 `overrides.expValue` 与 `overrides.scale`。后者通过仅含渲染器的子对象改变显示大小，沿用原生动画帧，碰撞、出生占位、攻击发射点不随之扩大；原有 `body.scale` 继续改变身体。Shuttle 移动提供 `waitGameSeconds`（非负）和 `moveGameSeconds`（正数），其他移动类型不消费这两个字段。

`nativeParameters` 只提供当前模板实际配置的组件组：`amoeba` 的受击时长、反推力及辐射开关/效果引用，`bastion.animationDelay`，`trafficLight` 的悬浮高度、速度区间和准备抖动频率，`nian` 的倒地时长、砍击次数、工具伤害、倒地防御、爆发力、鞭炮免疫及掉落阶段时长。`sight.disableEmotion` 控制原生警戒表情。原生部件可独立配置 `health/defense/hurtDuration/scale`，留空继承。年兽头尾和飞船炮台仍由原生遭遇生成。

运行计数器、未使用类、未消费的攻击属性不在该入口内。例如变形虫跳跃的 `MaxJumpHeight` 与 `HasReadyAction` 不影响当前原生执行路径；目录说明保留这个事实。

`MonsterDrops` 和 JSON `drops` 统一使用 `inherit`、`append`、`replace`。原版目标在 `nativeOverrides[].fields.drops` 配置，新定义在 `monsters[].drops` 配置。可引用 `source: local/effective/managed` 的 `poolId`，也可提供 `entries` 内联池；两者择一。`minCount/maxCount` 是池的总抽取次数，条目上的同名字段是该物品数量边界。多个追加全部执行；替换按官方加载顺序、同作者声明顺序选择胜者。撤回只移除该作者贡献，原生解锁、物品总数与保底保持。`maxCount: null` 表示单项不限量，0 不是禁用掉落的开关。

要向已有新增定义贡献掉落，在同一个 `nativeOverrides` 入口将 `targetId` 写成 `ModID:定义ID`，只填写 `fields.drops`。该目标接受同样的三种操作，与原模板及同模板其他定义隔离；目标作者自己的掉落也参与相同顺序。跨作者目标需要必需依赖及版本范围。目标尚未发布或已撤回时，贡献等待目标，不影响其他怪物。普通项目先登记池和定义，再登记目标声明。

```json
"drops": {
  "mode": "replace", "minCount": 25, "maxCount": 25,
  "entries": [
    {"itemId":"titanium_ore","weight":1,"minCount":5,"maxCount":5},
    {"itemId":"slime_gel","weight":1,"minCount":20,"maxCount":20}
  ]
}
```

```json
"attackComposition": [
  {"sourceTemplate":"drone","sourceAttack":"AtkNormal","targetAttack":"AtkSkill01"}
]
```

来源攻击的弹体、技能和资源仍由原生工厂创建，私有副本仅修改槽位并应用参数。目标保留身体、移动、索敌、动画及 AI；继承 AI 必须调用该目标槽才会自动出招，也可用配置序列或代码调用。实际发射事件、攻击动画、伤害框、跳跃路径和子体依赖必须由目标满足。目录给出来源招式的具体消费说明；缺少资源、身体或原生攻击验证失败会拒绝，而非按怪物配对名单放行。

Y 1.1.9 可通过可选 `IMonsterCatalogApi` 查询已登记的新定义及来源，并调用 `SpawnFromCatalog`。它不激活没有需求的后端；生成实例仍归定义作者所有，退出、隐藏、Owner 撤回沿模块既有清理。普通作者的 `IMonsterContentApi.Spawn` 仍只操作自己的定义。
