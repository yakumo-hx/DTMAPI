# 20260908-0012: 任意作者官方 Local 安装事务

## Metadata

- Update ID: `20260908-0012`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: PN-004 / AD-03，复用既有部署事务与官方来源。

## Summary

公开 SDK 安装/更新任意合法作者 ID 到官方 MODS，保存恢复历史并分开磁盘、官方启用与 Runtime 驻留状态。旧 game/Mods 仅保留恢复与撤回。

## Changed Files

- SDK 部署路径、journal reader、公开命令、进程边界与事务测试。

## Validation

- PASS：`official-local` 公开子进程覆盖任意 ID、预期 ID/版本/hash、Core 官方来源读者、更新/撤回、失败窗口、重复恢复、未知文件拒绝、并发锁；Windows 实际 Process.Start 被持有的 exe share-none lease 拒绝。最终输出在 `artifacts/m1-official-local-final.log`。
- PASS：外部 `Visitor.PlatformJourney` / `Visitor.EntryFailure` 经真实 SDK 安装到官方 MODS，由原生界面启用；Runtime 使用一致 Local ID/root。运行中 update 拒绝，冷更新与最终公开 withdraw 成功；SDK 没有写官方启用文件。
- 首轮失败与修复：native ModManager 补齐 info.json 的三语言空字符串，导致精确收据漂移；packager 现在生成完整原生字段。另修正失败回滚在仍持有操作锁时重复取锁。保留原生改写证据后，仅恢复首轮已知测试 info.json；修复候选 0.1.1 经原生扫描后直接公开更新到 0.1.2、再经扫描直接撤回成功，均未手工修包。
- PASS：历史 schema 1/2 包 reader、schema 3 恢复/撤回仍通过 `platform-sdk-targets`。旧测试把公开 deploy 全部暂停的断言改为历史 source local select 仍 SDK003；没有放开旧 game/Mods 来源。

## Evidence

- [运行证据与候选身份](../../debug/evidence/GAME-SMOKE/20260908-225439-platform-m1/README.md)。外部目录下 `journey-v3-update.json`、两个 `*-withdraw.json`、`update-running-rejected.json` 保存公开命令输出。
- schema 4 绑定官方根；receipt 保持 schema 2。恢复仍以既有完整 inventory 和包外 journal 为准，无 force/adopt。首轮失败不是可复用的产品 PASS。

## Rollback Notes

两个样例已通过官方界面停用、公开 SDK 撤回，保留同卷 recovery。Runtime、MoreSaves 非存档临时移动、配置和测试前官方启用字节已恢复；原生档及 sidecar 未写回。

## Follow-Up

安装切片完成；完整 M1 生命周期/输入缺口沿 [PN-008](20260908-0015-platform-author-journey.md) 与 R1，不从此卡推导产品全通过。
