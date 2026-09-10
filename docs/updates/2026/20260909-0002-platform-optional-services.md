# 20260909-0002: PN-005 可选服务与旧 helper ABI

## Metadata

- Update ID: `20260909-0002`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: [连续执行与有界内部片授权](20260909-0001-platform-m1-acceptance-continuation.md)，沿主架构 A02 和既有 owner coordinator；不新增 Review。

## Summary

新增独立可选服务入口，保持旧 IDtmHelper/Entry 成员不变。Core 以固定契约 Type 解析服务，按 owner 缓存和确定清理，关闭及清理再入后不能取得活服务。本片先用内部测试服务证明生命周期，不声称 Data/Reflection 已交付，不改 SDK available target 或发行版本。

## Changed Files

- Abstractions 可选入口与扩展、Core helper/owner cleanup 集成。
- Core 有界生命周期测试及现有 retained ABI harness 的旧 helper 实现者/消费者实际调用场景。

## Validation

最终出口：[PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)与 R2 已接受本卡有界结果；下列内部阶段的 pending/not-run 是当时状态，已由末尾实测结论收口。

- PASS：`platform-services-core` 三个入口，覆盖精确类型、重复获取、owner 隔离、关闭/再激活旧引用、清理再入/失败重试、构造途中关闭、错线程和现有 Runtime owner 清理顺序。
- PASS：`test-helper-retained-abi.ps1` 将旧 helper 实现者与旧 DtmMod consumer 分别对冻结 0.5.5 payload 编译，再由现有 ABI harness 只绑定当前候选 Abstractions。旧实现者真实实例化，旧 consumer 通过当前 DtmMod.Entry 抽象槽调用全部旧 helper getter、ReadConfig/WriteConfig，结果 73/一次写入；旧 helper 的新可选查询返回缺失。未给旧接口增加成员，未用当前接口重编译这两个 fixture。
- Mono 和正式 SDK 外部作者证明由 PN-020 消费候选；当前 M1 新游戏/原生失败缺口不因此豁免。

## Evidence

- `artifacts/pn005-core.log`、`artifacts/pn005-helper-abi.log`、`artifacts/pn005-helper-abi.json`。JSON 绑定 frozen/candidate/两个 fixture DLL 的 SHA-256 与实际绑定路径；明确 shippingMono=false。

## Rollback Notes

PN-018 加入两个确切生产服务 Type 后，以最终候选重跑 owner focus 和原 frozen DLL 的实际绑定：`artifacts/pn005-core-final.log` / `artifacts/pn005-helper-abi-final.json` 通过；未重编译旧 fixture。内部测试服务仍不在生产目录中。

撤回新可选入口和 Core 实现即可；旧接口和 frozen payload 不变，不写玩法存档。

## Follow-Up

内部验证及 PN-018 独立路径/global IO 片已完成；主卡产品状态保持 pending，公共 target 冻结仍按 PN-007/020/R2。

PN-020 补原 retained DLL 的游戏 Mono 调用：仅 QA participant 增加显式环境路径控制的 `RetainedHelperAbiProbe`，先核对先前报告中的两枚完整 SHA-256，再加载原始 bytes、实例化 helper/consumer、实际 Entry 调用并验证新服务缺失分支。普通启动不请求就不加载，无新生产 loader/公共 API。该探针不编译、不载入 frozen Abstractions。候选 F 初次因 optional 参数方法组不匹配而编译失败，改为显式 lambda 后 G 构建/发行检查 PASS；SDK ZIP 与 D 完全相同。原始两 DLL 的 Mono gate 在 G 两次冷启通过：原 hash、rebuilt=false、Result=73/Writes=1，实际绑定当前 Abstractions。

最终实测结论：原 retained helper/consumer 以既有原字节在 shipping Mono 实例化并调用 Entry；可选缺失 null/required NotSupportedException 符合契约。两个新作者共存和 owner 关闭已证。 证据及失败沿 [PN-020 实机证据](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md)。公共面继续 Experimental；最终冻结归 PN-007.b，未发布。
