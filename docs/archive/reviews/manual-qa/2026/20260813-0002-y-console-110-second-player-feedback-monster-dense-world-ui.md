# Y 键控制台 1.1.0 第二轮玩家反馈：怪物参数类型与世界栏密度复核

- Review ID: `20260813-0002`
- Date: `2026-08-13`
- Status: `recorded`
- Scope: 第二轮 1.1.0 玩家复验中的宽屏世界栏、传送目录、怪物生成和目录卡片内容布局
- Source: 用户在本地官方 1.1.0 修正候选上的四项手工反馈，以及一张 `1920x1080` 游戏截图
- Screenshot: `C:\Users\ADMINI~1\AppData\Local\Temp\codex-clipboard-6479d2f1-55dc-40b9-967d-28d358edcef6.png`
- Runtime evidence: `D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`，会话截止 `2026-08-13 01:22:25 +08:00`
- Owning Update: [20260812-0001-y-console-semantic-ui-productnative](../../../updates/2026/20260812-0001-y-console-semantic-ui-productnative.md)

本 Review 按用户本轮反馈顺序冻结截图事实、玩家确认和再次实现前的根因。天气与动物的本轮成功事实只作为部分玩家证据；怪物及布局修正仍归既有 Update，独立玩家复验前其生命周期保持 `implemented`。

## 1. 右侧按钮仍然过大；右侧不得滚动，传送点应尽量全部同页

### 原始反馈与截图转写

- 用户要求右侧按钮继续缩小，世界/高级栏不使用滚动，并在一页中完整放下操作。
- 传送点尤其要缩小，在同一页显示尽可能多，最好显示全部。
- 截图中七种天气已按固定 `4+3` 顺序显示并可标出“当前/预报”；其下传送仅显示两列四行，右栏底部可见滚动提示，高级操作未在当前视口内出现。
- 传送目录共 `22` 个稳定项：十三个交通站加九个地标；当前 `2×4` 页大小需要三页，与语义目录快速选择目标不符。

### 代码事实与根因

- 宽屏组合世界栏显式调用 `CreateResponsiveRegion(... forceScroll: true)`，内容高度硬编码为 `1500`，因此即使 `1500×820` 面板有足够横向空间也必定建立 `ScrollRect`。
- 传送按钮固定为两列、`196×52`，页大小固定 `8`；这不是由当前 420 逻辑单位侧栏可用面积计算出的密度。
- 时间、移速、天气、传送和高级操作各自保留了较大的纵向间隔。按 44 物理像素点击下限重新排布后，天气可用七列单行，传送可用四列六行，十个高级操作可用两行；无需滚动即可落在状态栏上方。

### 修正边界

- 宽屏组合栏不再创建 `ScrollRect`；时间、移速、天气、传送和高级操作在同一页从上到下紧凑排列。
- 七个天气仍保持固定顺序；按钮以颜色及可同时出现的当前/预报标记表达状态，不分页、不重排。
- `22` 个传送项全部同页，宽屏按四列六行；窄屏世界标签利用更宽的单栏区域动态增加列数。删除传送分页状态与按钮。
- 点击目标仍不少于 44 物理像素。不可用项维持禁用配色和标记，点击后在状态栏显示精确翻译原因；不得为了密度恢复子串或坐标回退。

## 2. 怪物召唤仍然全部失败

### 原始反馈

- 用户确认修正候选中的怪物卡仍然生成失败。
- 动物生成已成功，且容量限制合理；因此本轮不重写动物落点、容量或隐藏产物算法。

### 运行证据与根因

- 最新日志对 `drone_ex`、`bird`、`amoeba`、`aircraft`、`amoeba_wet_land`、`amoeba_radiation`、`ball_drone`、`fungus`、`scarecrow`、`seed_carrier`、`traffic_light` 等不同怪物稳定记录同一失败：`native-create-rolled-back; Object of type 'UnityEngine.Vector3' cannot be converted to type 'UnityEngine.Vector2'.`
- 每次失败都是 `created=0`，没有留下实体；回滚路径没有破坏管理器数量。
- 当前公开构建与受管策略构建的 `IMonsterHost.GenerateMonster(MonsterProto, Vector2, bool)` 签名一致。`DolocAPI.AgentPosition` 的运行类型则是 `UnityEngine.Vector3`。
- 普通 C# 调用可以应用 Unity 提供的 `Vector3 -> Vector2` 隐式转换；`MethodInfo.Invoke` 不执行用户定义的隐式运算符，只按运行参数类型绑定。因此把装箱的 `AgentPosition` 直接传给反射接口必然在进入原生宿主前失败。

### 修正边界

- 继续以 `DolocAPI.AgentPosition` 为位置语义，但读取其 `x/y`，按 `GenerateMonster` 第二个参数的精确运行类型构造真正的 `UnityEngine.Vector2` 后再调用。
- 目标参数不是 `UnityEngine.Vector2`、坐标不可读或构造失败时 fail-closed，并记录明确失败码；不改用坐标扫描、旧命令或其他怪物宿主。
- 返回实体、proto 值/稳定 ID、Host、Controller、管理器包含关系、精确前后数量、整批回滚和回滚失败熔断全部保留。

## 3. 目录图标/名称没有真正位于同一正方形卡片内

### 原始反馈与截图转写

- 用户指出名称既然放在物品 UI 下方，图像应当居中；另一种可接受方案是把名称放进物品框或卡片间隔内。
- 截图中卡片背景是正方形，但名称显示在深色背景的下缘之外，视觉上像独立占用行间距；图标也因此显得偏上而不是作为“图标+名称”整体居中。

### 代码事实与根因

- 卡片根节点已是 `84×84`，图标的 `44×42` 区域也已近似水平居中。
- 名称、来源和状态文本却使用了全根节点拉伸锚点，同时再施加 `y=-47/-72` 和正高度；这会先取得完整 `84` 高度，再额外偏移/扩张，使文本矩形实际越出卡片。问题不是根节点仍非正方形，而是子文本 RectTransform 的锚点语义错误。

### 修正边界

- 图标区显式水平居中在卡片上部；名称使用卡片内部下部的固定文本矩形并启用换行/截断，不再使用全节点拉伸锚点。
- 来源行只在需要时占用卡片内最底部；状态标记保持卡片内叠加。动物两行状态名称不得越出卡片或侵入下一行。
- 根卡片仍按固定池复用，修正只调整既有子节点 RectTransform，不增加逐次创建或监听器。

## 本轮已确认的正向事实

- 玩家日志完整记录七种天气的成功切换，包括 `SUNNY`、`CLOUDY`、`RAIN`、`THUNDERSTORM`、`WINDY`、`ACID_RAIN` 和 `SCORCH_SUN`；“天气全部禁用”已由第一轮修正消除。
- 玩家确认动物召唤成功且容量限制合理；日志同时记录成功的 1/10 批次和明确的 `animal-capacity-insufficient` 失败。本轮不放宽容量保护。

## 验收门

- 静态/单元：怪物调用参数必须由 `AgentPosition.x/y` 构造为接口要求的精确 `Vector2`；宽屏世界栏无强制 ScrollRect；传送无分页且 22 项同页；卡片文本采用显式内部矩形。
- 产品候选：运行 Unit、Author SDK、DebugConsole native trace、Runtime floor、Advanced fixture、retained ABI、Catalog/sidecar 和文档检查。
- 游戏：取得共享 Runtime 锁，以第三存档执行最小 `NoNativeSave` 验证；至少证明一个怪物 1 个和 10 个批次成功、Y 控制台右栏无滚动且全部传送可见、目录名称位于卡片内，并在任何恢复动作前证明存档归档哈希/长度/mtime 不变。
- 新候选只更新本地官方目录并保持启用；不上传 Workshop，不改变订阅字节或发布授权。
