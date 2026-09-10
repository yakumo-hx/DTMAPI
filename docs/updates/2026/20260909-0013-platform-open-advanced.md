# 20260909-0013: PN-010 任意作者自助 Advanced

## Metadata

- Update ID: `20260909-0013`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `runtime`
- Runtime Validation: `passed`
- Related Issue State: `none`
- Source: 连续执行授权、[P05](../../architecture/platform-package-contracts.md#p05自助-advanced-与原生来源)、[R3.shared GO](../../reviews/code/2026/20260909-0005-platform-r3-shared.md)。

## Summary

任意合法作者 ID 显式选择 NativeContractVersion=1，SDK 从本机安装生成引用来源、实际成员签名和包绑定，Runtime/Doctor 加载前校验。保持旧 receipt Advanced、Strict 和 legacy 各自 reader，不失败降级。

## Changed Files

已扩展 Shared 原生 provenance、metadata surface/实际成员提取、SDK 任意 ID new/build/pack/install-local、Core classifier/加载前复查、Doctor 与 schema。原生 Hook 和实例查询仍在作者 Mod。全新 0.6.4 内部候选 A/B/C 分别保留，0.6.3 冻结产物不变；最终格式已在独立 sdk-final 冻结，SHA-256 `dba2b1fe4d94b43c0dd621fef460a63cb9f3ecdd19b2a8ad4a4695e3202be27e`。

## Validation

当前阶段：Native V1 源码测试通过（包含实际 Runtime Entry/关闭、静态构造器隔离、重载/静态性/返回类型、数组/by-ref、明确 unsupported 和错误格式三读者不降级），解决直接引用 CS1705，metadata surface 可重复生成且移除原方法体/资源/字段数据。旧 receipt 路径的内存引用报告空路径回归已修复，精确旧快照 SDK pack 通过。

候选 C 加 Core 摘要修复的真实 Mono：`20260909-160031` Passed。Birch（23762374 原生引用在当前 25163613 上运行）、Cedar、Pine 均 Entry/只读 SaveLoaded/准确 owner Hook；旧 receipt Advanced、0.5.5 Strict、legacy 同场进入 Entry。Pine 关闭自身 Hook 后 Cedar 仍有 Hook。`20260909-160157` 故意部分 Entry 失败：Pine Hook 清除，Cedar 后续 SaveLoaded 仍 agent=True/hook=True；RunStatus/最终健康快照为 Failed 是故意 Entry 故障，其他运行器门通过。两次公开 command 尝试均在 QA 自动退出后得到 host-unavailable，未声称命令通过。

首轮 `20260909-155844` 保留失败：新分支误用 legacy 摘要，加载前与全包摘要比较不一致；三作者都在 Assembly.LoadFrom 前被拒绝。修复为既有 Advanced 全包摘要并增加实际 Runtime Entry 回归测试，原作者包不重打后重跑通过。此前不能把仅 classifier 通过视为完整加载成功。

全部测试 Mod 已撤回，两个作者自制旧 reader 样例移入 evidence；正式五 DLL、fixture enablement 恢复，30 个原生 archive 和 5 个 sidecar 的 hash/长度/mtime/计数、真实 enablement 均不变，锁已释放。未启动第二个旧版本游戏进程，不承诺跨游戏版本兼容。

最终补齐 Cecil 实际辅助元数据输入及编译→pack 摘要绑定后，Core 154 入口、Native 严格契约/实际 Runtime Entry、依赖、pack-build 和 Catalog 检查通过。最终 SDK release check 与全解编译通过。最终 Native 0.1.1 包两次打包一致；162502 正常 Mono Passed，162702 故意故障仅健康两项 Failed，独立 Cedar 公开限定名命令成功；162832 正常命令/动态可选成员缺失降级/进档读取/退出 Passed。原 retained 两 DLL 返回 73/writes=1。正常首轮命令漏作者前缀的 unknown-command 原结果保留，未当作 Runtime 故障。最终环境再次撤回与恢复，30+5 保护通过。[R3.native](../../reviews/code/2026/20260909-0006-platform-r3-native.md) GO PN-022；完整 M3、官方 UI 和 Runtime 包验收仍属后续门。

## Evidence

本卡 `artifacts/pn010-*`；[最终实机 evidence](../../debug/evidence/GAME-SMOKE/20260909-platform-pn010-final-runtime/README.md)；[阶段实机 evidence](../../debug/evidence/GAME-SMOKE/20260909-platform-pn010-runtime/)；[聚焦原生 Hook 边界](../../hook-map/focused/OpenNativeAuthorProbe.md)。SDK A `e0f404c8b8eea50736a95edd42dd829527e3304a25bcdc04ad40b11dc089242d`，B `4c7a13d783d87e4ded818e4548192e9e2c4f416b1ab0a5f783916f2dc333fe5a`，C `12ff4ec8bc71c004e8b0a7070e90fb80d14134955e92ba7c7e3f4bf87d18a836`，均未覆盖或发布。

## Rollback Notes

撤回新 native contract 分支及其候选包；旧 reader、旧 receipt 和已有发布包不迁移。

## Follow-Up

R3.native 已接受，继续 PN-022、PN-023 与本批后续工作，不上传。
