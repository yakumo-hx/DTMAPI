# 20260911-0008：Y 控制台 1.1.2 发布观察

## Metadata

- Update ID: `20260911-0008`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户明确告知“1.1.2 发布了，请更新有关记录”。本项记录用户已经完成的发布，不执行新的上传；随后 marker 准入研究归[同案 Review](../../reviews/code/2026/20260911-0004-player-070-subscribed-mod-compatibility.md)。

## Summary

Y-Key Console 最新公开版本更新为 **1.1.2**。用户确认版本；Steam 实时公开接口确认原条目 `3742714442` 在 2026-09-11 19:06:03（+08:00）更新、公开可见且未封禁，公布大小 627,499 字节，与已接收本地 1.1.2 候选大小一致。接口没有提供 Mod manifest 或 DLL，因此大小一致不当作逐字节同一证明。

本机 19:24 的只读检查仍见 1.1.1 订阅缓存，其原生 ACF 最后写入早于本次发布。Catalog 用 `latestPublicationObservation` 记录最新发布，同时保留带明确 superseded 标记的 `currentPublishedArtifact` 作为最近一次准确订阅收据；不能把其旧 manifest/hash 改名为 1.1.2。订阅清单仍记录实际安装观察。公开接口的 `hcontent_file` 也不冒充尚未观察到的原生 installed manifest。

## Changed Files

- `tools/release/dtmapi-product-catalog.json`：记录 1.1.2 最新公开发布及待订阅核验边界，保留旧实物收据。
- `tools/release/current-subscription-manifest.json`：补充该项本地再观察和发布记录入口，未重标其他项或原整份 ACF 采集时间。
- `products/first-party/DebugConsole/README.md`：当前公开版本改为 1.1.2。
- `docs/reviews/code/2026/20260911-0004-player-070-subscribed-mod-compatibility.md`：区分玩家反馈取证时的 1.1.1 与后续 1.1.2 发布。
- 本 Update 及九月月度行。

## Validation

- PASS：用户确认与实时 `GetPublishedFileDetails/v1` 对照，result=1、visibility=0、banned=0；更新时刻与文件大小已记录。
- PASS：只读检查本机 ACF、manifest、DLL，仍为旧 1.1.1；没有把缓存滞后解释成用户未发布。
- PASS：JSON 解析及该项公开 metadata/旧订阅边界的定向对账、文档链接、`git diff --check`；文档治理通过（9,933 checks）。
- 全仓 Catalog 检查报告两项失败，均来自其他地图任务新增 `tests/mod-fixtures/qa/HeartMap/manifest.json` 尚未进入 Catalog，造成顶层 source manifest 数 22→23；本次发布记录未产生失败。保留该任务改动，不把全仓结果写成 PASS。
- 游戏、构建、SDK 封包：本项不需要；没有改产品或 Runtime 字节。旧产品验收与 0.7.0 完整 Release PASS 保留在原 owner。

## Evidence

- [实时公开接口响应](../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/y-console-publication-api.json)：2026-09-11 11:22:03Z 采集；hcontent_file=`8228556265328991395`，公开大小 627,499。
- [本地订阅观察](../../debug/evidence/PLAYER-SUPPORT-20260911-070-YCONSOLE/y-console-publication-local-observation.json)：11:24:26Z 仍为 1.1.1；同目录保存原生 ACF 副本。
- [Steam 条目](https://steamcommunity.com/sharedfiles/filedetails/?id=3742714442)的网页抓取仍返回旧缓存，故发布时刻以实时公开接口响应为准；不采用抓取页面中的隐藏通用模板提示推断下架状态。
- 旧 1.1.1 准确发行事实归[原发布 Update](../../archive/updates/2026/20260823-0003-y-console-text-input-hotkey-guard.md)；已接收 1.1.2 候选归[最终产品验收](../../archive/updates/2026/20260831-0006-y-console-runtime-lightweighting.md)。

## Rollback Notes

仅还原本次记录增量；修改前相关文件保存在同案 ignored evidence 的 `publication-records-before`。不回滚用户已完成的 Steam 发布，不覆盖其他任务的 Catalog/订阅/索引改动。

## Follow-Up

后续：用户已发布 1.1.3，准确新订阅由 [0011](20260911-0011-runtime-y-hotfix-publication.md) 核实。下文保留本次 1.1.2 观察时的边界，不重标旧收据。

Steam 订阅完成同步后，可用准确新 manifest、完整树与 DLL hash 关闭交付一致性核验；目前不声称已取得这些字节，也不据此声称原反馈玩家已经恢复运行。本次没有刷新订阅缓存、改 live upload、启动游戏或再次上传。全局无后续上传授权状态保持。
