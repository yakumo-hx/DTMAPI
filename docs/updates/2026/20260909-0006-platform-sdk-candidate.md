# 20260909-0006: PN-007 M2 候选与最终 SDK target 冻结

## Metadata

- Update ID: `20260909-0006`
- Date: `2026-09-09`
- Lifecycle Status: `verified`
- Validation Level: `source, unit, runtime`
- Runtime Validation: `partial`
- Related Issue State: `none`
- Source: 连续执行授权及 [PN-007](../../planning/platform-next/tasks.md#pn-007m2-公共-api-target-与-sdk-完整候选)，[R2 GO](../../reviews/code/2026/20260909-0003-platform-m2-r2.md)。

## Summary

PN-007.a 的隔离候选经 PN-020 双作者 Mono 验收后，完成 b：冻结相同 API 0.7.0 contract/Abstractions 和自有源码重建配方，普通 SDK 0.2.0 同时提供 0.5.5/0.7.0，默认新项目为 0.7.0。旧 0.5.5 载荷与旧项目语义不变。十项新服务继续 Experimental，不包含 Reflection 或 SaveData。源码 Runtime 0.7.0、file 0.7.0.0，assembly compatibility 0.5.3.0；已发布 Runtime 0.6.1 与零未来上传授权均保留。

## Changed Files

普通 SDK catalog、版本投影、双目标 prepare/build、不可变 compatibility/0.7.0 自有源码/hash 清单；SDK/Core/Doctor 目标矩阵、便携离线测试与作者说明。Catalog 仅更新当前源码、API 行摘要和实际源码文件计数，不更改已发布产物身份。历史候选构建器在 0.7.0 available 后明确拒绝再次 staging。

## Validation

PASS：两套 frozen payload 各 14 项源码精确重建、缓存复用、损坏修复/篡改拒绝及输入边界检查；两次普通 SDK 构建均 512 files、确定性 ZIP 同 hash。AuthorSdk.Tests 的 platform-sdk-targets、platform-session-handshake、pack-build、official-local 通过；Runtime/Core/Bridge target 与旧 Advanced reader focus 通过；版本投影/public API metadata、Catalog 和 SDK preparation 14 项通过。新目标 minimum/marker、未知目标、错/缺/篡改载荷保留失败关闭，旧 schema/SDK 0.1 低最低版本 reader 保持。

PASS：空 PATH/DOTNET_ROOT/NuGet、中文空格目录中的 self-contained CLI new/build/pack/Doctor。仓库外“冻结验收 0.2.0”新作者默认 0.7.0 编译 context 服务并确定性打包，显式 0.5.5 工程仍可打包、同份新服务调用产生编译拒绝。SDK prepare -Check 复用当前输入。

证据复用：五个生产项目 .cs/.csproj 与实机候选 G 无差异，Abstractions hash 完全相同；嵌入 catalog 的默认值变化及依赖投影使其余四个 DLL hash 变化，详见 freeze.json，不声称五 DLL 全部 byte-identical。该元数据变更经 SDK/Runtime reader 验证；不重跑未变化的玩法场景。实际 Mono 的控制器/typing-focus 等限制沿 PN-020/R2 保留，未提高稳定性。

失败保留：候选初版漏 QA 链接源码/工具版本、F 的方法组编译失败已各自修复。冻结后旧断言误把 SDK 0.2/目标 0.7 当未知，以及旧 host 0.6.2 低于新的会话最低版本，已改为真正无效/不同的控制输入；没有放宽产品验证。便携脚本仍把已恢复的 deploy/install-local 当禁用且未隔离官方 persistent root，首轮返回 SDK404。该次事务的精确 staging 路径已自行清理，锁内核查不存在；修复子进程环境隔离，旧 source select 仍 SDK003，实际 official-local 行为由专用事务 suite 验证。正式 Runtime package builder 因工作区源码未提交而拒绝，未绕过或冒称发行包成功；仅使用其原有 info 投影块生成 9192-byte 源码 metadata，hash 与已发布 0.6.1 区分。

## Evidence

[PN-020](../../debug/evidence/GAME-SMOKE/20260909-platform-pn020-runtime/README.md) 与 [R2](../../reviews/code/2026/20260909-0003-platform-m2-r2.md) 拥有实机证明和限制。固定候选 G 的 SDK 与 D 相同。最终 `artifacts/pn007-freeze.json` 记录源码/DLL差异及最终包：`artifacts/pn007/frozen-sdk-final/DTMAPI-Author-SDK-0.2.0-win-x64.zip`，SHA-256 `3eeeeda1908735c7b975c9a472e207047c2f1781b131fc98febb58496b8793d1`，repeat 同 hash。

contract SHA-256 `a9b301778d1909317923ad0d47d3f5ac80846094a3266c3e4f10d341baedb616`，Abstractions `69a28c5c8da3a9b19afdff9d7a12c728b44161c31ffc269e790b1b270b16cfd4`。`source-build.json` 的 sourceCommit 仅指工作树基点，逐文件 hash/gitBlob 标明真实未提交输入，不伪称它们已在该 commit。

检查日志为 `artifacts/pn007-freeze-*.log`，外部结果 `pn007-freeze-external.json`，临时目录核查 `pn007-portable-stage-audit.json`；历史候选负例仍为 `pn007-negative-*.json`，不覆盖失败记录。

## Rollback Notes

本轮三个测试包已撤回、会话/QA 清理、五个玩家原 Runtime DLL 已还原，30+5 玩家文件不变。冻结载荷后续不能回填改写；新能力需新 target。没有提交、上传或发行。

## Follow-Up

PN-007.a/b 完成。继续按队列核对下一份完整输入；后续 M3 格式和反射能力不回填 0.7.0。

PN-036 successor：[新内部 0.6.2 组合](20260909-0011-platform-internal-version-realignment.md)已验证；本记录原 0.7/0.8 字节和当时结论仍保留。后续 M3 细则已由 [执行包](../../planning/platform-next/execution-next.md)补齐，继续实施。
