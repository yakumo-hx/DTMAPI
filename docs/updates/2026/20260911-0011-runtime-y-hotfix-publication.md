# 20260911-0011: Runtime 0.7.0 补丁与 Y 1.1.3 发布确认

## Metadata

- Update ID: `20260911-0011`
- Date: `2026-09-11`
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户确认新版 DTMAPI 0.7.0 与 Y 1.1.3 已更新，要求更新有关文档并提交有关内容。

## Summary

Windows 和多平台 Runtime 的 **0.7.0 BOM 修复包**、Y 控制台 **1.1.3** 均已由用户发布。Steam 公开接口和独立的本机原生 ACF 确认更新，三个订阅包的全部内容与已测试候选、上传目录逐文件一致。0.7.0 补丁以构建 `7d26482a2a95` 和文件收据区分旧版；安装器构建来源不因发布改变。

北京时间 2026-09-11：Windows 条目 `3743016467` 于 22:56:51 更新，多平台 `3792681186` 于 22:57:05 更新，Y `3742714442` 于 22:57:18 更新。准确 manifest、树收据及 DLL 身份由 Catalog 和下列证据拥有。原实现及行为验证分别归 [0009](20260911-0009-runtime-070-package-marker-bom.md) 和 [0010](20260911-0010-y-console-113-item-id-case.md)。

## Changed Files

- Catalog、订阅清单及相应静态校验常量：更新三个条目的准确发行收据，关闭待发布和待订阅状态，保留原冻结基线。
- Y README、official-info 的 1.1.3 版本投影、修复 Update、当前发布路由与月表：同步已发布事实；包含前次 0.7.0 三语发布文案和订阅控制文件校验的相关未提交记录。
- 提交包含前两项修复的源码、版本、对应测试和研究记录。混合文件只暂存相关部分，SDK 迁移、地图及其他改动保留在工作区。

## Validation

- PASS：三个公开条目 result=1、visibility=0、banned=0；公开大小/更新时间与原生 ACF 的独立安装记录一致，不以公开 handle 代替 native manifest 取证。
- PASS：23:05:47 的只读观察捕获 Windows 29、多平台 38、Y 27 文件。排除原工坊控制文件后的内容与候选/上传目录全部一致；Y DLL 就是已实测的 1.1.3 / netstandard2.0。
- PASS：ACF 采集前后 hash 一致，其余受管产品的 manifest/大小/更新时间也符合此次快照。没有触发下载或覆盖缓存，读取结束后释放 Runtime 锁。
- 复用 0009 的 BOM、准入、双安装器测试和 0010 的定向回归、原生领取测试：源码和包字节未变，没有重建、再上传或启动游戏。
- PASS：文档治理、JSON 和相关差异检查。Catalog 显式使用准确 BOM 候选及真实构建提交 `7d26482a2a95`；发布收据断言通过，整体仍为原有 **FAILED (2)**：HeartMap 原生 manifest 未登记及源清单数量 22/23，不称全仓 Catalog PASS。
- PASS：更新 Catalog 后，对已测多平台候选重跑结构与来源审计，37 个内容文件、21 个共享文件及 PowerShell 5.1/WSL Bash 语法均符合新收据；没有重跑安装生命周期。
- 暂存范围检查保留完整提交清单，混合文件仅收录本任务内容；未提交原始玩家附件、反编译材料、二进制或临时产物。

## Evidence

- `artifacts/publication-20260911-hotfix/steam-public-metadata.json`、`observation.json`：公开 metadata 与时刻。
- 同目录 `appworkshop_2285550.acf`、`subscription-parity.json`：原生成员、installed manifest、完整/内容收据及候选/上传对照。
- 同目录 `before/` 保存变更前发布记录；另保存文档、Catalog、Git 暂存检查日志。原 r6/r2、Y 1.1.1/1.1.2 观察不重标为新包。

## Rollback Notes

记录可按本次提交还原，代码回退见原修复 Update。不回滚用户已经完成的 Steam 发布，不覆盖其他未提交工作；玩家附件、反编译材料、二进制包和临时构建产物不提交。

## Follow-Up

本次记录已核实并随相关修复提交。第三方原包整包玩法以及 Steam Deck/Proton/CrossOver 游戏启动仍沿原验收边界；交付一致性不扩大这些承诺，也不产生下一次上传授权。
