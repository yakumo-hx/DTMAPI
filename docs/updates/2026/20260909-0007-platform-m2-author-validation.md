# 20260909-0007: PN-020 M2 双作者验证

## Metadata

- Update ID: `20260909-0007`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: 连续执行授权与 [PN-020](../../planning/platform-next/tasks.md#pn-020m2-基础平台产品验收)，消费 [PN-007.a 候选](20260909-0006-platform-sdk-candidate.md)。

## Summary

两个仓库外 Strict Mod（M2.Aurora 与 M2.Borealis）通过隔离 SDK 0.2/API 0.7 的正常 CLI 构建；另用普通 SDK/frozen 0.5.5 构建旧 helper/consumer 对照。验证平台服务在原生 Mono 的组合、失败与 owner 边界；不以托管 PASS 替代实机。

## Changed Files

仓库外作者源码/包与验证记录；仅在验证发现明确问题时修订其所属 PN-005/009/018/019 实现。

## Validation

已在 Mono 观察：后台 Post/Delay 主线程执行、NextTick 后续 tick、显式取消/期限到期、容量/公平、事件与命令异常后存活；两次读档的 save epoch、房间 world epoch 和回标题取消/LIFO；资源首次清理失败后重试；标题 help/别名/引号参数可见输出；版本化配置迁移/验证失败保全、包内 owner 文件隔离、两个 owner 全局计数冷启动保留；F8 物理输入、暂停时不触发 Gameplay、中文/英文通知与作者页面刷新。冻结 0.5.5 编译的外部 Legacy helper/consumer 返回 73/Writes=1，可选服务缺失返回 null，required 抛 NotSupportedException。

最终 PASS：E 完整双循环与正常退出；G 原始 retained 两 DLL 的 Mono 原字节调用；G 新游戏序章后 WorldReady；G 真实 IO 失败 epoch 清空→同进程重试新 epoch；稳定标题 SDK 引号参数、无 world/跨 owner 拒绝与清理重试；暂停按住 F8 跨恢复 neutral rearm。各次关闭出现一次主线程 ShuttingDown、Mod Dispose 和零剩余根。三个最终 runner 均 Passed。

历史 D 提前结束的循环/owner-close Failed、E 初次 ReturnHome 关闭会话后的 host-unavailable 和第一次超输入期限未送键均保留，不计作通过。新游戏长序章使十分钟 save-scope 回调在同一 epoch 自然运行，探针 ERROR 标签不代表跨失效执行。原生空档/损坏分支不承诺普遍恢复；实体手柄未连接，标题打字框不单独证明 Gameplay 焦点抑制。Runtime Validation 保持 partial 表达这些限制。

## Evidence

外部工程根 `E:/Python_project/DTMAPI-author-m2-20260909`。固定 SDK 和三包见 [候选 Update](20260909-0006-platform-sdk-candidate.md)；运行证据目录 `docs/debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime`。D 与仅修订 Bootstrap 的 E 分别留存；失败、截图和 CLI 回包不覆盖。包通过 install-local 部署到既有可丢弃 fixture 的 MODS，fixture SAVE 启用条目单独准备；官方原生 UI 仍显示真实 profile，本轮未修改它，不能冒称 UI 启用测试。

## Rollback Notes

公开 withdraw 三个包、清除会话、还原 fixture 启用和五个原 Runtime DLL、移除 QA；30 个玩家原生文件与 5 个 sidecar 的 hash/长度/时间/数量不变。global 数据撤回后保留；10:57:07 释放 runtime lock，不写回玩家原生档。

## Follow-Up

[R2](../../reviews/code/2026/20260909-0003-platform-m2-r2.md) GO：接受有明确限制的 M2，继续 PN-007.b；未测设备和宿主限制保留，未发布。
