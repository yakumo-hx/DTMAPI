# 20260909-0005: PN-019 配置、输入与翻译作者语义

## Metadata

- Update ID: `20260909-0005`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: 连续执行授权、R1 与 [PN-019](../../planning/platform-next/tasks.md#pn-019配置输入与国际化的作者语义)；沿既定边界实现，不新建全平台 Review。

## Summary

新增可选版本化配置、owner 输入诊断和参数化翻译/语言变更通知。配置沿 Config 的用户配置根目录，复用已实现的有界 envelope 与原子存储；旧 ReadConfig/迁移 Action 和输入方法含义保持。新契约是 PN-007.a 的候选输入。

## Changed Files

Abstractions `AuthorSettingsServices` 候选 DTO/服务；ConfigService 复用 GlobalDataService 有界 envelope 的版本化配置适配（独立 Config 根）；InputService 只读诊断；TranslationService 参数化与现有 Core Update 主线程语言通知；可选服务接入和 `author-sdk/PLATFORM-SERVICES.md`。SDK203 也覆盖直接 async-void 语言回调，标题命令文案加入现有中英文表。

## Validation

最终出口：[PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)与 R2 已接受本卡有界结果；下列内部阶段的 pending/not-run 是当时状态，已由末尾实测结论收口。

- PASS：`platform-settings-core` 五入口，涵盖坏/未来配置不覆盖、成功迁移只提交一次、失败迁移/验证保留字节、验证器修改不污染读结果、legacy 文件不变；输入 scope/focus/suppression/泛化修饰键冲突和 neutral rearm；参数单次替换/缺项/转义、regional locale 通知、异常隔离及 owner 关闭移除订阅。
- PASS：`platform-data-core`、`config-correctness`、`platform-services-core`、`platform-runtime-core`；真实 AuthorSdk.Tests `platform-sdk-targets` 含五个 SDK203 回调探针及普通目标矩阵。Core 构建零警告零错误。
- not-run：真实键盘、已有控制器路径、可见语言刷新与配置在 Mono 的双作者使用；归 PN-007.a/020，未测设备如实限制。

## Evidence

依赖 [PN-009](20260909-0004-platform-context-scheduler.md)、[PN-018](20260909-0003-platform-owner-global-data.md)；本卡尚无游戏运行。日志 `artifacts/pn019-settings-tests.log`、同前缀各 focus 日志及 `pn019-sdk-callback-tests.log`。

## Rollback Notes

撤回新增可选入口/订阅和 UI 文案，保留配置与原全局数据；不操作玩家存档。

## Follow-Up

完成内部切片与必要测试后进入 PN-007.a → PN-020 → R2 → PN-007.b；候选不提前成为普通 available target。

最终实测结论：Mono 双作者配置迁移/验证失败保全、冷启读取、英语/中文可见刷新与主线程通知通过。F8 暂停按住跨恢复保持 neutral，释放后新按键触发；实体手柄未连接，打字焦点因果未独立证明，R2 接受这些明确限制。 证据及失败沿 [PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)。公共面继续 Experimental；最终冻结归 PN-007.b，未发布。
