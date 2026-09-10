# Y 键控制台运行轻量化与折中生命周期审查

## Review Header

- Date: `2026-08-31`
- Status: `recorded`
- Scope: DebugConsole ProductNative 打开态热路径、关闭/重开对象生命周期、目录 UI/查询分配及 Native 生成峰值
- Source: 用户要求综合本轮只读审计与侧边对话总结执行修正，并明确采用折中重开生命周期、分批提交和必要测试
- Owning Update: [20260831-0006-y-console-runtime-lightweighting](../../../updates/2026/20260831-0006-y-console-runtime-lightweighting.md)
- Related records:
  - [Y 键控制台动物目录与传送清理](../../../../updates/2026/20260831-0001-y-console-runtime-animal-catalog-and-teleport-cleanup.md)
  - [ISSUE-014：Y Console Close Double Toggle](../../../../debug/issues/ISSUE-014-20260712-y-console-close-double-toggle.md)
  - [DebugConsole Input Hook Map](../../../../hook-map/focused/DebugConsoleInput.md)

本 Review 固化实施前代码事实、用户取舍和验收边界。实现、文件清单与最终验证只写入 Owning Update。

## 用户要求与折中生命周期

- 先提交当前工作树，再开始本项；后续修正按独立批次提交。
- 保持玩家可见功能、Y/Escape 输入结果、两干净帧 drain、目录排序/筛选结果、动态动物宿主判断、生成后置验证和旧城守护者可见部分成功语义不变。
- 关闭后不销毁整棵 UI。保留 Canvas、可复用结构和当前布局实际需要的 `30` 个 catalog cell。
- 关闭时清除 tooltip、cell 当前 DTO/动作上下文、动态文本、状态和可选 sprite 缓存，使玩家大部分未开启控制台的时间不继续持有目录 payload 与 sprite 引用。
- 同几何、同语言重开复用结构并重新投影动态状态；几何或语言变化仍允许完整 Rebuild。
- 分别降低打开静置、开关、目录交互和 Native 批量动作成本；机械拆分类文件不作为性能完成证据。

## 已确认代码事实

### 1. 默认关闭态不是首要热点

- 控制台关闭且移动倍率为 `1x` 时，产品不订阅 `UpdateTicked`。
- 十九个 ProductNative Harmony patch 仍常驻，但关闭态回调仅做静态门判断；没有证据支持以动态卸载/重装 patch 冒生命周期风险。
- 非 `1x` 时逐帧检查玩家 body 是即时重绑契约，不得通过降频改变。

### 2. 打开静置仍有逐帧反射和分配

- `DebugConsoleUi.Update` 在打开态每帧读取 Escape pressed/down 与 Y pressed；当前 RawInput 每次重新解析 KeyCode、查找 MethodInfo/PropertyInfo 并创建 Invoke 参数数组。
- `ApplyVisibilityState` 在 dirty 快速返回之前仍重复检查/激活 root、读取 Screen/safeArea、创建 `DebugConsoleLayout` 并查找 EventSystem。
- 正常场景已经有 `EventSystem.current` 时，继续逐帧 `FindObjectOfType` 是可避免的场景扫描。
- RawInput 不能直接替换为 owner-bound `IInputHelper`：ISSUE-014 已证明 modal audience、suppression 与 Bootstrap/Core 分发顺序会影响 Y 的可见边沿。

### 3. 当前关闭/重开同时支付常驻和重建成本

- `Close` 把 `dirty` 设为 true；布尔 setter 把它扩张成 `UiDirtyRegion.All`。
- 关闭只隐藏 root，保留 panel/cell/binder 图；下一次 Open 又标记 All，随后因 Layout 位完整销毁并重建 panel。
- 当前固定池是 `48` 格，而 Wide `1920×1080` 当前页容量为 `30`；多出的十八格在关闭期也被保留。
- 因此当前行为既没有获得最低关闭内存，也没有获得结构复用的重开收益。

### 4. Catalog dirty 会重建结构并重复整表投影

- 搜索、来源、分类和翻页会销毁/recreate catalog chrome；cell 虽被池化，但重绑会重新创建闭包、delegate、反射参数和值对象。
- `BuildItemsTab` 当前无条件取得物品、怪物、动物三套投影，并在 UI 层重复 `.ToArray()`。
- `GetItems` 对约千项目录重复复制、过滤、四级排序和来源统计；怪物/动物动态可用状态只需在相关目录显示或统一来源 snapshot 失效时投影。
- 优化必须保持当前稳定排序、来源计数定义、搜索匹配、不可用卡显示以及运行时宿主可用性结果。

### 5. Native 侧存在交互峰值而非默认常驻热点

- `NativeAccess` 的成功缓存仍在每次读取时拼接含 `AssemblyQualifiedName` 的字符串键。
- `AddedEntities` 使用 after × before 的引用扫描，拥挤 manager 中是 O(N²)。
- 动物落点最坏检查 Manhattan 半径 `64` 的 `8,321` 个候选；当前候选路径包含字符串坐标、LINQ footprint 和反射参数数组分配。
- 这些路径可以改为结构化键、引用相等集合、数值坐标和复用参数数组，同时保留 snapshot、after 顺序、候选顺序和 walkability 判断。

## 实施批次

1. 缓存打开态 RawInput/Screen/Unity/EventSystem 稳定反射元数据，帧内只采样一次 Y/Escape；几何未变时复用 layout。
2. 删除布尔 dirty 扩张，实施折中 Close/Open：保留 Canvas/结构/30 cells，关闭时释放动态 payload、tooltip、文本和 sprite 引用；同几何/语言重开仅刷新精确 region。
3. 固定 cell 回调和结构引用，减少 catalog chrome 重建与重复属性写入；缓存稳定排序目录、来源统计和选中目录投影。
4. 将 Native 反射缓存键结构化，优化 `1x` Hook 快速返回、实体差分和动物落点临时分配。

旧 `0.3.1` Compatibility DebugConsole 的物理退役、按需 Harmony patch 组、全面强类型 Unity UI 和诊断 breadcrumb 写盘策略不属于本 Update；它们需要独立兼容/诊断边界。

## 拒绝或延后方向

- 不删除 raw Y/Escape，不缩短 Escape drain，不改变 Legacy OR Input System 顺序。
- 不缓存 `Keyboard.current`、key control、玩家、房间或 EventSystem 实例跨设备/房间/存档边界。
- 不降低布局变化或非 `1x` body 变化的发现频率。
- 不删除生成前后 manager snapshot、根级成功统计、Guardian 三实体后置条件或可见部分成功结果。
- 不以 `GC.GetTotalMemory` 冒充 Unity Mono 精确分配证据；本轮以确定性调用/对象计数、源码断言和运行行为验证为准。

## 验收边界

- 默认关闭 `1x` 仍无产品 Update；关闭后无 tooltip、cell DTO/动作上下文、动态状态文本或 sprite cache 引用。
- 同几何/语言重开不完整 Rebuild；几何或语言变化完整 Rebuild 一次，目录/世界/高级动态状态仍刷新。
- Wide `1920×1080` 只保留 `30` 个 cell；其他 breakpoint 按实际 page size 扩容，不因普通翻页反复缩池。
- Y/Escape/held-Y/focused input/two-clean-frame drain 与 ISSUE-014 当前契约一致。
- 搜索、来源、分类、翻页的显示顺序、计数、availability、左右键动作和 tooltip 结果不变。
- 怪物/动物批量生成仍保留 after 顺序、visible partial success 和完整后置验证；动物落点候选顺序不变。
- 聚焦 Unit/source、Author SDK、native trace、文档治理和 Git 空白检查通过；最终生命周期/input 候选执行一次第三存档 `NoNativeSave` 有界游戏验证并保留 clean-exit 证据。

