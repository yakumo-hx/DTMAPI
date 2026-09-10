# 20260909-0018: PN-037.b 配置菜单方向导航

## Metadata

- Update ID: `20260909-0018`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `unit, runtime, player`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: [PN-037.b](../../planning/platform-next/execution-next.md#pn-037b配置菜单的方向导航) 与 D09 的已定标题配置边界。

## Summary

标题页配置提供可见焦点图；导航、文字编辑和按键捕获分离。原生菜单动作通过 GameBridge 读取，平台模态结束前保持 neutral。游戏内打开仍不支持。

## Changed Files

GameBridge 原生标题输入适配与仅标题模态隔离；Bootstrap 控件焦点及菜单动作。原生页面和其他菜单设置在退出时恢复。

## Validation

焦点图、重复/neutral 与输入编辑刷新屏障定向测试通过，长列表/事务鼠标实测通过；C 进档和正常退出通过，导航 Harmony owner 19:18:54 明确清理。后续用户用 F8/F9 进入后的方向/Enter、单次 Tab、Enabled 修改/取消/保存/关闭重开均通过；冷启动值保留。实体手柄单独 pending-player，因此 Runtime 保留 partial。

独立验收 [A1](../../reviews/code/2026/20260909-0009-platform-m3-input-release-acceptance.md#a1--p2方向导航缺少原生标题入口)当时发现：角落图标没有接入原生标题显式导航，面板内焦点图仅在打开后运行。此前记录不能证明“仅方向/确认从原生标题进入”，因此本卡当时恢复 in-progress。已通过的面板内证据继续有效；入口返修不属于缺手柄造成的未测项，最终结果见下。

## Evidence

A1 返修已增加 GameBridge 原生标题动作适配：RenderTextMenu 前插入唯一 owned action，保持原生 Quit 最后；刷新去重、Unregister/Shutdown 撤回自身对象及委托，复用原生布局/确认和现有模态 neutral。匹配游戏程序集 SHA 已复算不变。`artifacts/pn037-native-entry-065-tests.log` 八项定向入口通过。随后用户只用实体方向/确认完成入口、配置事务、关闭重开和原生设置/选档页检查，明确回复“全部通过”；21:07:50 正常退出，测试资产恢复且存档不变，见[原生入口证据](../../debug/evidence/GAME-SMOKE/20260909-platform-native-entry-065/README.md)。自动按键无正对照，不冒充人工证明。

[重试证据](../../debug/evidence/GAME-SMOKE/20260909-platform-input-retry/README.md)：Tab 改走单后端采样，避免跨后端重复边沿，长按方向键逻辑不变；七项定向回归与轻按 Tab 用户复验通过。探针十五个控件误绑同一 Enabled 的问题在独立包 0.1.2 修正，未改 Runtime 保存事务。最终配置旅程及测试环境恢复通过。

[输入所有权](../../hook-map/focused/ControllerInput.md)、[早期 UI 证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn037-runtime/README.md)。修复 end-edit 重建吞掉 Save 点击；一百二十帧长按屏障测试及真实单击保存通过。第二页长列表、取消与重置已鼠标验证；后续键盘结论以重试证据为准，安装导航 Hook 本身不等于操作验收。

## Rollback Notes

撤回本片焦点/输入适配并解除本片所有者 Hook；键鼠配置与保存事务保留。

## Follow-Up

原生入口首次返修已通过。用户随后要求并列入口独立开关、默认关闭、允许重启生效，并按当前分辨率适配。该变更继续本卡：输入设置新增 ShowNativeTitleEntry，启动时读取一次；保存/重置不热改动作列表。配置 Canvas 根据实时窗口宽高约束整个面板与命中区域，分辨率改变只调整比例，不重建事务控件；原生入口沿游戏原生布局。该增量已单独验收，旧“全部通过”仅证明变更前的入口。

完整 0.7.0 r2 候选的增量实测已确认：默认关闭保留原生五项；开启并保存后当前进程不插入，冷启动只新增一个入口。游戏原生显示设置从 2560×1440 改为 1280×800，配置面板比例即时从 1.6 调整至 0.8888889，长列表、命中区域与取消事务正常；随后恢复原显示设置。用户实体方向/Enter 从原生入口打开，Tab/方向导航、关闭选项并保存、当前进程入口保持及正常退出均回复“通过”，落盘值为 false。

2026-09-10 的 r3 冷启动已确认关闭后的撤回：原生五项完整、并列入口消失、独立图标保留；`ui-disabled-cold-r3.jpg` 和 `soak-title-baseline.json` 保留可见观察。r3 的 Runtime 源码和构建选项与 r2 未变，来源身份、安装器提示及测试修正另有记录；实体键盘和动态分辨率证据按该边界复用。随后至少 60 分 46 秒标题停留、读档和正常退出通过，导航 Hook 明确清理；最终测试资产和原安装恢复，35 个存档/sidecar 未变且锁释放，见[准确候选证据](../../debug/evidence/GAME-SMOKE/20260909-platform-release-070/README.md)。本卡的有界实验交付完成；Runtime partial 仅保留未测实体设备维度，后续归 PN-037.c。

重启生效是用户接受的实施范围：启动时读取一次选项，避免本片再引入运行中插拔菜单与焦点转移的状态处理。本轮没有发现热切换高风险的实证，不作该断言。

实体手柄/Steam Input 操作反馈仍分列。菜单 Cancel 在文字编辑时先退出编辑，否则关闭菜单；F8/F9 入口不兼作关闭，用户同意本轮保留。
