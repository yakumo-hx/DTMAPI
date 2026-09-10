# 20260908-0010: 配置序列化与失败恢复

## Metadata

- Update ID: `20260908-0010`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: PN-014/config-correctness；沿 [已选根因与方案](../../reviews/code/2026/20260908-0009-platform-plan-reconciliation.md)，不另建 Review。

## Summary

保留序列化器有效输出与原子替换。读取失败保留原件，解析损坏只有实际备份成功才恢复默认；不改变公共配置接口或 migration Action。

## Changed Files

- Core JsonFile/ConfigService：删除唯一手工 formatter，分流读取、解析、备份和默认写入失败。
- Core 配置测试与 suites.json：独立 parser 往返与真实文件故障窗口。

## Validation

- PASS：`tools/scripts/test-unit.ps1 -Focus config-correctness`，4 个现有 Core graph 入口，0 警告/错误。独立 System.Text.Json parser 验证奇偶转义、引号、标点、Unicode；读取权限/IO、备份失败、碰撞、默认原子替换失败、连续恢复逐字节比较。原有恢复与 owner lifetime 回归通过。首次运行测试辅助断言只捕获 InvalidOperationException，调整为显式异常断言后通过。

## Evidence

本切片按源码/单元检查退出；后续外部样例的真实配置往返见 [PN-008](20260908-0015-platform-author-journey.md)。原 F01 探针仍为复用研究证据，不冒称重跑。

## Rollback Notes

可单独撤销源码和测试；保留用户备份，不恢复已修正的错误文件内容。

## Follow-Up

完成 focus 后推进 PN-015。连续 PN-014 不随本切片全部完成。
