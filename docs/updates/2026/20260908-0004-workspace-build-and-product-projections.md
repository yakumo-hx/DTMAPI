# 20260908-0004: Workspace build reuse and product projections

## Metadata

- Update ID: `20260908-0004`
- Date: `2026-09-08`
- Lifecycle Status: `verified`
- Validation Level: `source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: 用户批准的工作空间建设计划；范围由现行作者交付架构及 Catalog 所有权限定，无需另建 Review。

## Summary

普通产品打包保留 validate 和一次内部编译的 pack；SDK 准备结果按实际输入复用。补齐只读预检、目录边界及 Catalog 的可生成投影，保留历史报告读取、正式双打包和冻结兼容载荷。

## Changed Files

- SDK 构建与 pack 报告、产品包装和发布读取脚本。
- SDK 准备摘要、操作预检及对应测试。
- Catalog/玩家包定义投影、发布语言数据及使用入口。
- Issue 的 `State` / `Current boundary` 归回 28 份 Issue 正文；索引通过 `sync-issue-index.ps1` 生成，文档检查执行只读一致性校验。
- `global.json` 与实际 SDK 解析核验；冻结 0.5.5 自有源码快照和共享兼容载荷准备入口。

## Validation

- 建设前 SDK 完整准备及 release check：通过，30.265 秒。
- 建设前产品包装：发现旧 schema `const` 读取与 target catalog 不匹配，在产品编译前失败；不能记作产品测试失败或成功打包耗时。
- `PackBuildTests`：通过。编译输出使用包含空格/中文的显式目录；同输入跨目录的身份相同，源码变化会改变身份，包内 DLL 就是这次编译结果。
- MoreSaves 普通包装实际完成一次编译，ZIP SHA-256 与建设前相同（`3021e7340c30bc71727301e61fe02d0dc7e7070782740ad7ebf6a760c853f571`）。现行 v2 报告、真实旧 v1 报告读取，以及错误身份/未知版本拒绝通过。
- SDK 准备行为 14 项通过：产品及样例/bin/obj 不参与 SDK 失效；SDK 源码、传递引用、编译属性、实际打包资产及构建工具改变会失效；损坏输出和目录重叠被拒绝。准备开始/结束输入不同时不出可复用 receipt。
- 只读预检 11 项通过：缺少依赖不安装，不创建输出；编译参考与实际游戏分别呈现；错基线、目录重叠和未知产品在执行前拒绝。Unit 路线不要求游戏和 SDK。
- Catalog 检查通过；产品投影在 PowerShell 7 / Windows PowerShell 5.1 各 55 项通过，包括脱离仓库的 Windows PowerShell 进程读取和目录越界拒绝。15 份定义的包名、UniqueID、源码/包 DLL 和目录保持；显示名使用 Catalog 的已有值。发布语言 17 行不再手工复制版本/路径/身份；人工 Steam 文案原样保留。
- 第四轮完整 Release 的玩家包静态约束发现投影导出直接依赖可选 `Get-FileHash`。改用现有 .NET 哈希助手，新增“该命令不可用”的实际导出检查；来源 Catalog 的哈希含义不变。Windows PowerShell 父进程下的 JSON 数组计数和预期拒绝的 stderr 单独处理，仍要求失败码及非法目录原因；正常完成明确返回 0。证据为 `product-projection-portability-winps.log`、`product-projection-portability-ps7.log`。
- 修正后的 Runtime 精确候选包通过 Windows 安装器完整矩阵（31.897 秒）：可选命令、特殊路径、假宿主、并发/事务及第三方所有权边界均由既有矩阵执行，`runtime-portability-installer-2.log/json` 保留候选位置和退出码。没有操作真实游戏目录或发布。
- 修正两处未发布样例材料漂移：AutoHarvest 与 CropHarvesting QA 的 official-info 版本从 `1.0.0` 回到既有 manifest/Catalog 的 `0.1.0-dtmapi`。不改变公开发布观察或上传授权。
- Issue 索引 6 项 Windows PowerShell 行为检查通过：中文/空格/竖线、只读与幂等、状态变化、重复 ID 和非法状态均有证明。
- 公开入口首次以 Windows PowerShell 父进程执行 Issue 检查时，发现无 BOM 脚本中的中文夹具名被按系统编码解释。夹具名现由固定 Unicode 码点构造；预期失败独立接收 stderr，仍核对原因、非零码与原件未变，6 项重新通过（`issue-index-winps-repair-2.log`）。索引生产脚本未因此改动。
- 干净输入闭包检查发现旧兼容 DLL 依赖本地历史产物，当前 0.6.1 程序集不能替代已发布 0.5.5。现保留来源提交的 16 份自有编译输入（180322 字节），通过公开依赖重新构建出原合同精确 SHA `d04d34cd…f2bf8`；没有改预期哈希，也没有纳入 DLL 或第三方源码。普通构建、完整 SDK 打包和公开测试使用同一准备入口。
- 工具链检查在 PowerShell 7 / Windows PowerShell 5.1 各 26 项通过：不再凭“安装了 8.x”判断可用，而是在仓库上下文解析实际 SDK。工作空间基线为 8.0.421；冻结字节重建要求原精确工具链，不能用更高补丁版本冒充。
- 最终 Windows PowerShell 5.1 冻结准备检查 14 项通过（`final-frozen-compatibility.log`）：空目录精确重建、复用、输出损坏、源码范围、错误覆盖和越界拒绝；即使一起改写缓存文件及缓存收据哈希，也不能越过原合同的固定锚点。实际 native 版本输出完整接收后再读退出码，避免提前关闭管道造成假失败。
- 首轮耗时：旧 SDK 准备 30.265 秒，新 SDK 初次准备 18.210 秒；两者是不同缓存条件下的实测，不据此声明固定收益。旧已准备 SDK 的 validate/build/pack 合计 1.668 秒（2 次编译）；新包装总计 3.617 秒（含 SDK 输入核验，1 次编译）。两项范围不同，不直接比较百分比；收益证据是重复 SDK 构建和产品编译的消除。
- 最终集成前的正式产品合同诊断通过（86.499 秒）：Catalog 选中的 10 个产品各打包两次，共 20 次编译，显式复用同一 SDK；5 个 Runtime 程序集与 11 个公开产品的合同检查通过。保留正式确定性双打包要求，诊断结果不代替完整 Release 入口；实际构建与合同结果见 `product-contract-diagnostic.json` 和 `product-contract-diagnostic-builds.json`。
- 最终候选 `e1321953` 的完整 Release 和公开源码入口均从头通过（结果由 0005 拥有）。之后实际 SDK `-Check -NoProvision` 复用核验 2.763 秒；MoreSaves 普通包装 3.665 秒，包含 SDK 核验，只运行 validate 与一次内部编译的 pack；SDK 收据前后 SHA 相同。包与建设前保留 ZIP 的 SHA 相同，真实 v2/旧 v1 报告、编译输出身份及非法版本拒绝检查通过（1.151 秒）。完整数据见 [最终复用与产品测量](../../debug/evidence/WORKSPACE-CONSTRUCTION/20260908/build/final-reuse-and-product.json)。不将范围不同的旧命令合计与新包装耗时直接换算节省比例。
- 本包没有游戏行为修改；游戏 runner 与最终集成验收由其工作包记录。

## Evidence

- 原始测量在 `temp/workspace-construction-build-baseline/`；必要日志、报告与改造前投影保留到 `docs/debug/evidence/WORKSPACE-CONSTRUCTION/20260908/build/`。产物清理不会删掉这些测量原件。
- 对应结构与历史记录：[20260908-0002](20260908-0002-workspace-structure-and-history.md)。

## Rollback Notes

按本工作包提交回退源码、脚本和生成投影。缓存为可重建产物；不得回退其他工作包、Wiki 并行改动、玩家原件或冻结载荷。

## Follow-Up

本包验收完成。日常产品只复用对应 SDK 和测试范围，不重复本次测量、历史审阅或全库迁移。
