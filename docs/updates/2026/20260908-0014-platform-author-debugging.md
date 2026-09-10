# 20260908-0014: PN-017 作者符号与调试

## Metadata

- Update ID: `20260908-0014`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source,unit,runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: 已授权 M1 实施任务，沿 platform-next 本卡与现有原生事实。

## Summary

SDK symbols 命令验证 DLL/PDB identity；实时 snapshot 分开 Host/API target、官方启用、owner、磁盘 SHA/MVID 与驻留 MVID。真实 Mono 已证明源码行定位与 trace，断点附加未证明。

## Changed Files

- SymbolInspector、CLI 帮助/README、AuthorAssemblyObservation、AuthorSessionReloadBridge、符号与快照测试。只读元数据不执行被检查 DLL；保持协议字段总数上限。

## Validation

- PASS：`pack-build` 的匹配、缺失、错配 PDB；公开 `symbols-matched.json` 成功、另一个 Mod 的 PDB 产生 `symbols-mismatch.json` 拒绝。Debug 包含 PDB，Release 由显式 symbols 选项控制。
- PASS（有精度限制）：shipping Mono Entry 异常准确落在首版 `ModEntry.cs:9`；主样例事件异常报告 `ModEntry.cs:35`（回调末行），实际 throw 在 32 行。PDB 文档 SHA-1 与 Roslyn UTF-8/BOM 编码后的同版源码匹配，sequence points 含 32 行与末行，未发现错符号。作者凭公共 GetErrors 的 owner、事件名、唯一错误消息与同版源码定位该 throw，不能承诺事件堆栈总是精确故障行。下一 tick 继续，修复后公开 update/重启成功。
- PASS：最终 live snapshot 报 Host 0.6.1/API 0.5.5、Local、enabled/owner true、磁盘/驻留 MVID 相同。0.1.1→0.1.2 MVID 实际变化。运行中 update 被拒绝，不能把旧进程报告冒充新字节；强行替换磁盘后的驻留不一致实机负例未执行。
- PASS：官方未启用 snapshot；真实 session 超时关闭；官方停用后清理 owner/event/input，session 关闭，后续请求明确 host-unavailable。依赖阻塞与协议/关闭/身份负例为 `platform-session-core` 受控证据，没有声称依赖失败全经实机。
- PASS（仅库存/扫描范围）：实际导出报告 token 命中 0、存档/凭据/整份配置 entry 0。历史日志含可识别用户路径，且包含历史 crash.dmp，不能据文件名或 token 扫描保证内存内容无敏感信息；文档要求分享前检查，未上传。
- 未证明：shipping Mono 断点 attach。已观察实际 UnityPlayer/Mono 模块与监听端口，未取得调试握手或断点命中，不能从端口推断协议，也不能据此断言不支持。源码行流程已可用，这项限制不否定定位能力。

## Evidence

- [运行证据](../../debug/evidence/GAME-SMOKE/20260908-225439-platform-m1/README.md)；源码版本、符号/Doctor/session JSON 留在外部作者目录。报告库存与 host debugger observation 留在 evidence owner。
- 最终 SDK 已重新打入行号/报告限制文档，prepare/release check PASS。与实测包对比只有 README/release inventory 改变，CLI/DLL/模板/冻结 payload/所有 schema 字节相同，证据沿原运行复用。

- 2026-09-09 验收更正：[A1](../../reviews/code/2026/20260909-0001-platform-m1-acceptance-continuation.md)复现实际错配返回 internal/SDK999，预期 SDK191 未成立。pack-build 的 missing/mismatched 两个调用实际上执行未知命令 SDK191，并因 usage/SDK001 非零而假绿。原正确配对、缺文件公开检查和 Mono 定位证据保留；本卡须修错误分类和真实负例，不能再引用该两测试为通过依据。
- 2026-09-09 A1 返修通过：SymbolInspector 捕获错配的 InvalidDataException，SDK191 保留 DLL/PDB 路径和同次构建的修复提示；测试明确核对实际 symbols 命令、退出码 1 和 SDK191。新提取 SDK 的独立进程匹配返回 0/SDK190，错配与缺失均为 1/SDK191。固定包与复验路径沿 [PN-015 返修证据](20260908-0011-platform-build-plan.md#evidence)，不将此结果扩大为尚缺的实机驻留负例通过。

## Rollback Notes

先证明游戏退出与 NoNativeSave 字节未变，再恢复精确非存档测试资产；不恢复/覆盖玩家存档。

## Follow-Up

2026-09-09 重启后 PN-016 已取得新游戏、原生失败及同进程 IO 失败恢复/owner-close，见 [续测记录](../../debug/evidence/GAME-SMOKE/20260909-platform-m1-reboot/README.md)。本卡诊断出口按日志+源行定位接受；Runtime Validation 保留 partial，明确断点 attach/命中未证明，事件堆栈精度限制不变。无需因此重跑已通过的符号检查。

A1 生产返修完成。2026-09-09 续测在实际 shipping Mono 中保持旧 resident MVID，临时替换本样例磁盘 DLL 后公开 snapshot 明确 disk/resident 不一致，恢复原字节后重新一致；[三份公开 JSON 与恢复证据](../../debug/evidence/GAME-SMOKE/20260909-platform-m1-continuation/README.md)。驻留负例已补；R1 按明确的调试能力范围开放 M2，不称断点通过。
