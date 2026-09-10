# 20260831-0007：Y 键控制台后续路线图归一化

## Metadata

- Update ID: `20260831-0007`
- Date: `2026-08-31`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: `debugconsole/performance/technical-debt/roadmap/crops/inventory/save/documentation/governance`
- Source: 用户要求核对并补齐 Y 键控制台未实施路线；随后要求把范围催熟和确认式删除当前快捷栏物品作为两个独立产品功能插入路线，并明确催熟归 Y 键控制台 ProductNative、物品删除遵循官方原生保存提交语义。

## Source Request

用户先要求只读核对最新路线图与未实施内容是否一致；核对结果确认主要方向一致，
但 Catalog 生命周期、传送动态可用性、Compatibility 配套删除和 ABI/文档/测试收尾
没有被逐项写清，三个仅部分优化的方向也未获得明确去留。用户随后要求直接补齐。

2026-09-01，用户又要求在不把功能重新塞回 DTMAPI mandatory 本体的前提下恢复
Y 键控制台催熟，并研究是否真的需要通用 Crop Hook；同时增加只删除当前快捷栏
槽位整栈的二次确认功能，且删除必须沿用官方槽位移除和原生 SaveGame 提交边界，
不能通过直接改存档、sidecar 或私有数组制造不可恢复的旁路硬删除。

## Authority Boundary

- [Y 键控制台运行轻量化](../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)继续拥有
  `1.1.2` 已实现行为、测试和玩家接收事实；本 Update 不重写其实现证据。
- 本 Update 拥有后续技术债的规范化清单，以及两个新增产品功能在整体路线中的位置；
  两项功能的实现前事实与裁决由
  [范围催熟与选中物品原生销毁路线审查](../../reviews/code/2026/20260901-0001-y-console-crop-maturity-and-selected-item-destruction-roadmap-review.md)
  持有。未来实现必须从对应风险边界创建新的 bounded Update，不得直接把本清单或
  Review 的存在当作代码已完成。
- [2026-08-01 世界动作路线](../../archive/planning/2026/20260801-debugconsole-world-actions-roadmap.md)
  是 `superseded / frozen` 审计材料，不再接收实现进度。其 Maintenance 段仍提到
  nullable 标注，仅代表旧 handoff；当前事实由 Update `20260831-0006` 的完整构建
  `0 warnings / 0 errors` 证据接管。仍未完成的是 Compatibility 物理退役。

## Product Feature Insertions

以下两项是新增产品功能，不纳入十五项技术债编号，也不冒充已经实现：

### Feature F1：Y 键控制台范围催熟

- 由 `DTMAPI.DebugConsoleMod` 的 ProductNative 单独拥有配置、请求/结果、排序、UI
  和三类官方成熟责任函数适配；停止产品继续消费 public `CropMaturityResult`。
- 采用“当前房间半径过滤 + 最近优先稳定排序 + 最大数量上限”。建议默认半径
  `8` 格、最多 `24` 个，配置菜单范围分别为 `1..64`、`1..200`；实现 Update 可在
  真实 UI/运行验证后调整默认值，但不得退回无界全房间或跨房间坐标比较。
- 覆盖普通 `PlantBasin`、`PlantBasinTree` 和 `PlantBasinGrass`，分别适配官方
  双参数与单参数 `DEBUG_SetLevel` 签名，并以成熟状态/等级后置条件计成功。
- 本功能不新增 Harmony Crop Hook。当前一次性点击动作可从当前房间设备集合完成
  发现和调用；只有至少两个独立真实生产消费者需要同一生命周期事件时，才另开
  SharedNative/API Review。NeverPublish 示例与 QA fixture 不计为该门的消费者。
- 保持可见部分成功，不做批量伪事务回滚；玩法状态只进入 Working，之后正常原生
  `SaveGame` 成功才提交，不自动保存、不写 sidecar。

### Feature F2：二次确认删除当前快捷栏整栈

- 第一次动作创建进程内易失快照，绑定 SaveLoaded 会话、背包、非负快捷栏槽位、
  物品对象引用、ID 和数量；第二次确认前全部重验，任何变化都取消。
- 确认后只调用官方 `DolocAPI.DestroyItem(index)`，沿用
  `InventorySystem.Take(index)` 的精确槽位移除与 receiver/UI 更新；只绕过官方 UI
  的 `IsItemDisposable` 前置限制，不直接写 inventory 数组、不按 ID 搜索其他栈。
- 第一版拒绝空槽、`-1/-2` 特殊选择、非背包对象和仍含物品的容器。确认状态在关闭
  控制台、切换存档、返回标题或 owner deactivation 时清除，默认焦点必须是取消。
- 删除后仍只是 Working inventory 变化。未成功原生保存就退出/重载时按最后一次
  官方存档恢复；正常 `SaveGame` 成功后才随其他玩法状态提交。不得新增自动保存、
  InstantSave、删除 journal、sidecar 重放或所谓进程内撤销。

### Execution Order

1. 先完成技术债 `1..4` 的 Catalog 轻量结构/稳定 listener 复用与 binder/input
   O(1) 移除；当前搜索、来源、分类和页码字段已保留，不重复实现“状态持久化”。
2. 分别创建 F1 与 F2 的 bounded Update。F1 先把催熟 DTO 消费移回产品内部；F2
   单独验证确认时序和两种原生保存结果，两项不合并为一个运行验收。
3. 再做技术债 `5..6` 的缓存/传送目录分离，以及 `7..10` 的 Compatibility 物理
   退役。F1 必须先让产品对催熟 DTO 的依赖归零，退役才能按真实消费者证明完成。
4. 最后处理 `11..15` 与三个 partially optimized carry-over；新增功能不等待全面
   强类型 UI 改写，也不与 Compatibility breaking removal 合成一个大提交。

## Canonical Deferred Roadmap

以下十五项与审计中的十五个未实施项一一对应。它们都不是已接收 `1.1.2` 的阻塞项。

### Catalog 生命周期与集合维护

1. 保留并复用 `catalogChromeRoot` 的轻量结构；当前搜索文本、来源、分类和页码字段
   已能跨普通关闭/重开保留，未完成的是结构对象复用，不得把它误写成状态保存缺失。
2. 保留并复用搜索框、来源、分类和分页控件及其稳定 listener；仅在语言、几何、
   完整 dispose 或其他已证明需要重建的边界释放。
3. 将 binder 所有权集合改为 O(1) 移除，同时保持现有解绑和销毁顺序。
4. 将 input-field 注册集合改为 O(1) 移除，同时保持焦点识别和 Y/Escape 输入语义。

### 稳定目录与动态状态

5. 建立按语言区分并在语言/目录变化时失效的本地化标签缓存。
6. 把固定传送 spec/排序/静态标签与每次查询所需的动态投影明确分离；MarkPoint、
   当前房间、Native host、房间/坐标和 availability 不得被错误缓存为静态事实。

### `0.3.1` Compatibility 物理退役

7. 在重新核对当前 API、ABI 和实际消费者为零后，删除未使用的 `0.3.1`
   Compatibility 路由。
8. 删除该路由专属的 28 个 DTO 依赖；共享类型必须先按实际 owner 分类，不能仅按
   名称批量删除。
9. 同步删除只服务该路由的 executor、proxy、Compatibility host 接线和 QA plumbing，
   不保留无入口的伪兼容执行链。
10. 在同一退役 Update 中更新现有 ABI/消费者检查、公共 API 矩阵、Catalog/registry、
    文档与 source/unit/QA 断言；复用既有权威，不建立平行的退役清单。

### Patch、UI、反射与诊断 I/O

11. 在行为边界稳定后执行纯机械维护清理，包括无用 using、冗余 helper 和剩余可安全
    拆分的 partial；nullable warning 已清零，不再作为未完成项。
12. 对十九个 Harmony patch 做独立生命周期审查后再按需拆组；不得仅为降低常驻数量
    引入重复 patch/unpatch、漏 Hook 或关闭态语义变化。
13. 逐边界用强类型 Unity UI 访问替换反射驱动的构造和属性访问，不一次性改写全部 UI。
14. 为已确认缺失的 member/type 增加有失效边界的负反射缓存，不缓存可能在后续加载
    出现的动态类型或 owner。
15. 将 breadcrumb 持久化迁移为异步写盘，但必须保持崩溃证据顺序、shutdown delivery
    和现有失败可见性。

## Partially Optimized Carry-Over

以下三项不是新的产品功能，而是此前已经优化一部分、尚未明确关闭的延伸方向：

1. 评估并界定 World/Advanced 动态内容的结构复用；若实施，关闭态只保留有界的轻量
   结构，动态 payload、文本、sprite 和场景引用仍须释放。
2. 完成剩余 Native fallback 的惰性短路，保持当前 fallback 顺序、错误诊断和结果语义。
3. 扩展稳定 Native `MethodInfo` 缓存，但必须保留动态 owner/程序集变化所需的失效边界。

这些延伸方向在未来 Review 中可以因收益不足被明确拒绝或延期，但不能在没有记录的
情况下从路线图静默消失。

## Validation

- PASS: `tools/scripts/check-doc-governance.ps1`
- PASS: `git diff --check`
- Runtime/game/build validation: not required; no source, package, runtime or player-visible behavior changed.

## Changed Files

- `docs/updates/2026/20260831-0007-y-console-deferred-roadmap-normalization.md`
- `docs/updates/2026/20260831-0006-y-console-runtime-lightweighting.md`
- `docs/updates/INDEX-2026-08.md`
- `docs/planning/README.md`
- `docs/reviews/code/2026/20260901-0001-y-console-crop-maturity-and-selected-item-destruction-roadmap-review.md`

## Evidence

- The current UI retains state fields but calls `ReleaseCatalogChrome()` during Catalog refresh and
  retained-close preparation.
- The current teleport directory is static, while `BuildDestination()` resolves MarkPoint, room,
  host method and availability on each projection.
- Update `20260831-0006` records a complete `0 warnings / 0 errors` build, so nullable cleanup is
  closed rather than deferred.
- Current ProductNative maturity reflection only accepts the one-parameter `DEBUG_SetLevel`, while
  ordinary official `Crop` uses `DEBUG_SetLevel(bool shouldRender, int level)`; the current product
  cannot claim complete ordinary-crop support until F1 repairs that native-family adaptation.
- All retained official builds expose `DolocAPI.DestroyItem(int)`. Its current implementation routes
  through `InventorySystem.Take(index)` and does not itself write an archive or call `SaveGame`;
  official equipment-bar deletion uses the same call after its disposable-item UI guard.
- Current CropHarvesting is an explicit-operation SharedNative API with no installed Crop Hook, and
  no second independent production consumer was found for a shared crop lifecycle event.

## Rollback Notes

Revert this documentation-only Update and restore the predecessor Follow-Up route. No binary,
configuration, package, save, Workshop or runtime state requires restoration.

## Follow-Up

Each implementation slice starts from this checklist but receives its own Review when required by
UI/input/Hook/API/save/diagnostic risk and its own bounded Update. F1 and F2 must remain separate
feature Updates and follow the Review's source/unit plus isolated `NoNativeSave` and
`NativeSaveExpected` matrices; future Y-console runtime routes use UI slot `10` / native index `9`.
F1 does not authorize a generic Crop Hook, and F2 does not authorize an extra persistence owner.

Quantitative allocation/profiler evidence is optional for the already accepted `1.1.2`; a future
change that deliberately retains additional UI structure must bound its closed-state memory and
compare it with reopen allocation before acceptance. Steam submission remains separately
authorized work.
