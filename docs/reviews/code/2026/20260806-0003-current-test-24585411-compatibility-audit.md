# 1.00.02 当前 test build 24585411 兼容性审查

## 记录状态

- 日期：2026-08-06
- 状态：`recorded`
- 性质：只读完整 raw/managed inventory 比较、原生方法体与 DTMAPI/第一方产品消费者审查；不是 Runtime、Hook、Mod 或游戏实现
- tracked authoring 基线：`references/doloc-town/reverse/builds/24456188_test_E861E0`
- 上一完整 test 基线：`references/doloc-town/reverse/builds/24567135_test_08C846`
- 当前完整捕获：`references/doloc-town/reverse/builds/24585411_test_68AEA1`
- 最后一个已实际捕获的 public 基线：`references/doloc-town/reverse/builds/23762374_public_C416D4`
- Source：用户要求按 0.6.0 路线图从最新解包和反编译代码判断当前版本与测试版本的差异是否影响 DTMAPI 和第一方功能性 Mod
- Owning Update：[DTMAPI 0.6.0 唯一权威路线图](../../../updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md)
- Capture Update：[当前运行 1.00.02 完整逆向捕获](../../../updates/2026/20260806-0001-current-running-10002-full-reverse-capture.md)

本次没有安装、卸载或启动 DTMAPI/游戏，没有修改官方 `MODS`、Workshop、存档或共享 Runtime，也不需要取得 Runtime lock。官方 bytes、资源导出与反编译源码只位于 Git 忽略的本地 reverse 区；Git 只保存 DTMAPI 自己撰写的计数、消费者映射、兼容结论和阻断边界。

## 一、最终裁决

### 1. 当前完整 test 字节

`24585411_test_68AEA1 / 1.00.02` 对当前 DTMAPI 0.6.0 候选与路线图中的第一方公开发布集合 **没有引入新的代码兼容阻断**。相对上一份完整 `24567135_test_08C846 / 1.00.01`，主反编译树只有 `DolocTown.UI.DolocPagedLinearUI.RefreshView()` 一个方法体变化；类型、公开方法签名和相关字段没有改变。仓库生产 Runtime 与第一方产品没有该类型或方法的直接消费者，唯一 `RefreshView` 反射调用位于可选 QA 的 Animal viewer fixture，且无参数签名仍可解析。

因此最新 test 字节上的处置是：

- 不修改 Runtime 或任何第一方产品源码；
- 不新增 `24585411` Author policy，不批量重签或重打包现有产品；
- 既有 `24456188` 产品继续按 0.6 已实现的 `Drift -> 真实类型/Entry/Hook/产品事务` 路径激活；
- package、receipt、reference、owner 与 payload 身份门继续严格；
- MoreEquipmentSlots 1.0 的动态官方饰品栏 UI 阻断保持原结论，但它不是本次 `RefreshView()` 增量造成的新回归；当前发布集合仍只保留 `0.3.1-dtmapi`。

### 2. 正式版/public 字节

本次**不能把上述结论冒充当前正式版 public 验证**。当前 appmanifest 仍是 `UserConfig=public`、`MountedConfig=test` 的待切状态；top-level/target build 虽为 `24585411`，实际安装 depot manifest 和冻结字节仍属于 `test`。Steam build ID 相同也不能替代完整 public payload identity 与字节比较。

因此路线图第 4 节的 public 条件门仍局部 open：

- 若随后实际挂载的 public 完整 raw/managed inventory 与 `24585411_test_68AEA1` 精确相同，记录 payload 等价即可，不需要广泛重测；
- 若 public 只改资源/文本/配置，先找真实消费者，只定向验证命中的产品；
- 若 public managed assembly 或相关 native owner 改变，先做方法体与真实消费者映射，再只修复或重放受影响项。

最后一个已实际捕获的 public 树仍是 `23762374_public_C416D4`。它与 1.00 test 之间已知存在 DebugConsole、AutoFishing 和旧 MoreEquipment Host 的真实签名/owner 差异，当前 0.6 产品修正正是针对这些变化；所以不能拿旧 public 捕获反向证明当前 1.00 正式版，也不能在 public 字节缺席时宣称十一项产品已对正式版完成代码准入。

## 二、完整增量差异

### 2.1 `24567135_test_08C846 -> 24585411_test_68AEA1`

| 层 | 旧/新数量 | 精确差异 | 解释 |
| --- | ---: | --- | --- |
| raw official files | `542 -> 542` | 新增 2、删除 2、同路径改变 4、不变 536 | GU/UU content-hash bundle 各换名；`globalgamemanagers`、`level87`、`Assembly-CSharp.dll`、Addressables catalog 改变 |
| managed assemblies | `181 -> 181` | 改变 1、不变 180 | 只有 `Assembly-CSharp.dll` 改变，长度仍为 6,384,128 bytes |
| ILSpy main | `3,666 -> 3,666` | 改变 1、不变 3,665；`+12/-10` 行 | 只有 `DolocPagedLinearUI.cs`，无类型文件增删 |
| ILSpy firstpass | `29 -> 29` | 0 改变 | 精确不变 |
| GenDatas | `195 -> 195` | 0 改变 | 195 张实际 JSON 配置逐字不变 |
| Build Settings scene identity | `90 -> 90` | 0 index/path 增删或移动 | raw 仅 `level87` 内容改变；它仍映射旧城市废墟场景 |

raw 新旧 bundle 名只反映 Addressables 内容哈希变化；AssetRipper 在两次独立恢复间会重建大量 GUID/fileID/YAML，因此 45,770 个导出 hash 变化不能被解释成同数量的功能变化。可用于本次判断的稳定事实是：raw 路径集合只发生上述 2 增、2 删、4 改；managed assembly 只有主程序集变化；配置表逐字不变；scene index/path 集不变。

仓库没有 `level87` 或旧城市废墟场景的生产消费者，也没有按本次 GU/UU bundle hash 名直接加载资源的第一方产品。Wwise bundle 不在本次 raw 变化集合中。会读取当前实例化官方 UI 的产品仍以当前 build 的代码/布局审查和既有当前候选 smoke 为准，不能用 AssetRipper 噪声替代行为证据。

### 2.2 `24456188_test_E861E0 -> 24585411_test_68AEA1`

完整主反编译树累计只有三个同路径文件改变，firstpass 仍全部不变：

1. `SteamWorkshopUploader.UploadUpdate`：已有 Workshop item 更新不再重写 tags；生产 GameBridge 实际 Hook 的 `ResolveUploadPlan(ModInfo, Action<WorkshopUploadPlan>)` 未变。
2. `VersionPatcher.LoadAllVersionPatchesBeyond(string)`：执行前增加版本排序；生产 Runtime 不依赖返回顺序，只有可选 QA continuation probe 安装无状态 breadcrumb，签名未变。
3. `DolocPagedLinearUI.RefreshView()`：改为先渲染当前页有效范围，再隐藏页尾未使用槽，不再先清空全部 slot；这是分页 UI stale-slot 可见性修订，不改变类型或调用合同。

这三项之外的 3,663 个主反编译文件逐字不变。因此 0.6 已经针对 `23762374 -> 24456188` 完成的 DebugConsole、AutoFishing、MoreEquipment、MoreSaves 与其他产品 native-owner 审查，可以通过这条完整增量链映射到 `24585411`，不需要重新猜测所有 Hook。

## 三、消费者映射

### 3.1 Runtime / GameBridge

- 生产 GameBridge 只对 `SteamWorkshopUploader.ResolveUploadPlan` 安装两个参数的 Prefix/Postfix；变化的 `UploadUpdate` 不在 Hook 路径。
- `VersionPatcher` 只属于可选 QA native-continuation probe；普通玩家启动不安装该 probe。两项目标签名和参数数目不变。
- 生产 Runtime、GameBridge 与第一方产品均不引用 `DolocPagedLinearUI`。仓库唯一 `RefreshView` 反射调用位于 `DTMAPI.GameBridge.DolocTown.QA` 的 Animal viewer fixture，仍调用公开无参数方法。
- Runtime-floor 聚焦门通过，继续证明 Author SDK API target、四项 current 244 policy 的 0.6 floor 与 retained 0.5.5 Runtime rejection 边界没有漂移。

结论：最新 test delta 不要求 Runtime、Loader、Doctor、Manager、GameBridge Hook 或兼容分类代码变化。

### 3.2 第一方功能性 Mod

| 产品 | 最新差异消费者 | 代码结论 | 现有证据边界 |
| --- | --- | --- | --- |
| AutoFishing | 无 | Wait/Ready/Pull、movement owner 与 22-Hook 文件均未变；244 exact native trace PASS | 当前用户手测正常，既有 behavior/Manager 玩家门有效 |
| DebugConsole | 无 | 19-Hook、四参数 cost、二参数天气与科技点 owner 文件均未变；244 exact native trace PASS | 当前用户手测正常，ISSUE-011 当前候选 Y 控制台路径已通过 |
| MoreSaves | 无直接调用；官方存档页可能间接使用通用分页基类 | LocalSave、文件角色迁移与 `archiveFileCount` owner 未变；产品聚焦门 PASS | `24585411` 当前候选已通过 12 槽 UI、额外槽加载、幂等冷启动和用户手测，故无需因该 UI body 再重放 |
| ActionSpeed | 无 | 九 Hook 目标文件未变；聚焦产品门 PASS | 沿用最后行为变更后的既有 smoke |
| OneActionComplete | 无 | 两 Hook 目标文件未变；聚焦产品门 PASS | 沿用最后行为变更后的既有 smoke |
| FishBreedingAssistant | 无 | roe/title/proto owner 未变；聚焦产品门 PASS | 沿用最后行为变更后的既有 smoke |
| AnimalHusbandryProgress | 仅可选 QA 反射调用 `RefreshView()` | 生产四 patch/三 target 未变；无参 `RefreshView` 可解析；聚焦产品门 PASS | 当前 `24585411` ISSUE-011 run 已完成两次 causal render 与 atomic close |
| ChestLocatorEnhancer | 无 | `GetAvailableInventories` caller/count/max/cost owner 未变；最新树映射后的 244 exact native trace PASS | 既有 smoke + 用户远端箱子识别/真实扣料确认 |
| Zoom | 无 | camera scale/restore owner 文件未变 | 按用户决定不重开专项复查 |
| Manbo | 无；Wwise bundle未变 | paper-box type/event 与 audio hook seam 文件未变 | 沿用现有普通激活门，不新增可听替换专项 |
| MoreEquipmentSlots `0.3.1-dtmapi` | 无 | retained Compatibility ABI/Host 所需 owner 未变 | 继续保留旧加载与恢复路径 |
| MoreEquipmentSlots `1.0.0` | 不是本次分页 body 的消费者 | Product-v3 数据事务不受影响；已知动态 `passiveItems[]` 布局/导航冲突仍单独阻断发布 | 必须先完成动态原生前缀后的 UI composition/navigation 修正 |

## 四、Policy 与验证边界

现有三个 native-trace 脚本按设计把 `24456188 / E861E07E...0923` 作为 exact authoring authority。把 `24585411 / 68AEA11B...15739` 直接传入 DebugConsole trace 会被精确 hash 门拒绝；本审查没有放宽脚本或伪造 policy。正确验证顺序是：

1. 用完整 inventory 证明 `24456188 -> 24585411` 累计仅三个主文件变化；
2. 对三个变化文件逐一做方法体与真实消费者映射；
3. 确认所有产品 native owner 文件不在新增变化集合；
4. 再重放原 `24456188` exact native/product contract，验证当前源码仍符合已经审过的编译基线；
5. 由 Runtime 的 `Drift` 路径在玩家当前 build 上执行真实激活，而不是篡改 Author receipt。

已运行并通过：

- `test-dtmapi-060-debugconsole-native-trace.ps1`
- `test-dtmapi-060-autofishing-native-trace.ps1`
- `test-dtmapi-060-chestlocator-native-trace.ps1`
- `test-dtmapi-060-runtime-floor-compatibility.ps1`
- ActionSpeed、OneActionComplete、FishBreedingAssistant、AnimalHusbandryProgress、MoreSaves 五个 Batch 6 聚焦产品门

本次没有运行完整 Release 或新的游戏 smoke。没有源码实现变化，且受影响 UI 间接路径已有相同 `24585411` 当前候选运行证据；按路线图和 Assurance Proportionality 不追加广泛回归。

## 五、阻断与下一步

- **已关闭：** `24585411_test_68AEA1` 相对 24567135/24456188 的 managed delta 未知；最新 test 对 Runtime/当前第一方发布集合是否需要代码修正；是否需要新增 policy 或批量重签。
- **局部 open：** 当前正式版 public payload 尚未实际挂载和完整捕获，不能给正式版作字节等价或代码准入结论。
- **独立局部 open：** MoreEquipmentSlots 1.0 动态官方栏位 UI composition/navigation；它不阻断 Runtime 与其余已通过产品，也不影响 retained `0.3.1-dtmapi`。
- **下一步：** 等 Steam 实际完成 `MountedConfig=public` 后，先只读捕获 public identity 和完整 inventory；若 exact 等于 `24585411_test_68AEA1`，直接关闭条件门，否则严格按路线图第 4 节只处理真实差异消费者。
