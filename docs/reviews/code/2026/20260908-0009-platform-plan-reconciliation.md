# 平台路线与工作空间建设、外部研究的协调复核

- Status: recorded
- Role: 本次计划修订的来源、事实判断与选择；实施状态仍归 [platform status](../../../planning/platform-next/status.md)。
- Source: 用户要求检查基础设施建设对现有计划的影响，结合网页版 GPT Pro 研究及 SMAPI 技术研究改进当前/长期计划，并新建 Astra high 任务实施近期工作。用户随后明确：精简开发、测试和记录中的旧冗余要求，不缩减任务；新任务直接使用当前工作区。
- Owning Update: [20260908-0009](../../../updates/2026/20260908-0009-platform-plan-reconciliation.md)。

## 输入与核对范围

本次工作区基线为建设分支提交 `95f3cb32`。网页版研究对应上传包 `db4ed0fd`，两者主要差异为建设收口记录；研究不是新的当前权威。输入包括用户附件 `pasted-text.txt`、`D:/下载/DTMAPI-architecture-evidence-20260908.md`，以及 `E:/Python_project/SMAPIlearning/SMAPI_technical_study` 的全部 19 份 Markdown（README、00–10、20–26）。前两份是同一次 Pro 审阅的判断及 E01–E16 证据索引；其 E 编号不等于本路线 E01–E08 产品场景。

按主题读研究，回查影响决定的现码、编译项目及测试选择器；不重新逐行审核全部摘录、旧产品和历史 smoke。SMAPI 只参考行为和演进理由。完整研究仍在其原目录；本页保留必要结论，不要求下一位实施者再次读完 19 篇。

## 基础设施影响

| 已完成建设 | 对计划的实际影响 | 不应误判为 |
| --- | --- | --- |
| PROJECT/current-state、治理和历史导航瘦身；Update/Issue 投影脚本 | 按任务读上下文；同一实施 Update 接续修正，月度行由 sync 投影；旧来源用当前知识/路径路由 | 改变 Mod 身份、保存语义或取消真实行为验收 |
| Unit 拆为独立项目，`test-unit.ps1` + suites.json 选择实际构建图 | 当前局部检查走选定 suite/focus；旧 Unit DLL 只是兼容 dispatcher，不能只 build 它就假称新套件准备好 | PN-032 的第三方/Mono/性能实验室已经整体完成 |
| 公开源码 CI 与 frozen compatibility 重建 | PN-003 旧载荷已有可复现输入，不再要求普通任务找旧包；新 tests 加进现有 runner/CI | 新 target 已 available，或公开 CI 已覆盖私有 native/游戏行为 |
| SDK preparation 复用、普通产品单次 pack 编译、Catalog 投影 | 使用准备入口和既有构建报告；不重复 prepare/build/pack 同一候选 | PN-015 已解决作者 CLI/IDE 同一工程语义 |
| preflight/status 只读，依赖按需准备 | 缺什么才准备什么，不把完整准备作为普通读状态或局部验证前置 | 游戏已安装/作者命令已恢复 |
| 游戏 runner 按场景加载前提、明确保存模式、结果复用 | 直接游戏操作和产品日志有效；不因 Hook 字样强装 QA/HookProbe；普通 NoNativeSave 原地验证，隔离按实际风险 | 可以用模拟事件代替真实 native 写盘或内容生效 |
| 产物归属、审计正文单份交付、已证明重复缓存清理 | 沿既有输出目录/保留机制，不新增全树 hash 台账或计划专用收据 | 获准删除未知产物、旧公开 ABI 或唯一恢复数据 |

具体建设证据沿 [当前月度记录](../../../updates/INDEX-2026-09.md)的 0001–0008；命令现状以 [scripts](../../../../tools/scripts/README.md#choose-validation)为准。本次不重跑这些完整建设套件。

## 研究判断的取舍

| 主题/来源 | 核对与决定 | 落点 |
| --- | --- | --- |
| Pro：保持骨架、开放陌生作者、共性工具优先 | 接受。物理目录不等于实际编译归属，保留现有 Entry/Owner/事件机制；M3 任意合法新 ID 不改 Catalog/SDK 名单是必需出口 | A01/A08/A17；PN-010/011/023，M1 也用陌生 Strict ID |
| Pro SDK160；研究 25 F01/F02 | 三个源文件与 9 月 7 日研究 SHA-256 相同，现码控制流仍在。F01 复用直接引用源码的纯字符串探针反例，非本轮运行；F02 备份失败仍返回目标路径且调用方继续默认写入；SDK160 扫全文会匹配注释/字符串 | PN-014/config-correctness 先修 F01/F02，PN-015 同批修 SDK160；不等待 PN-019 新 API |
| 构建后端 | 接受短期一份 BuildPlan/CLI 与 IDE 投影；普通辅助库、嵌入资源、Debug 不能长期靠逐产品特例，M3 实测后可替换内部 backend | AD-01、PN-015/022；不现在重写整个 SDK |
| 研究 25 F03：底层写盘与外层返回 | 在 build `24456188_test_E861E0` 回查 `DataPersistenceManager.SaveGame`：fileDataHandler 成功后还有 AfterSaveData；`DolocAPI.SaveGame` 还有提示调用。当前 Hook 优先外层 bool postfix，存在待证异常窗口，非已复现玩家损坏 | PN-024/R4a 四种结果、D02/RT-07/E05；M1/M2 不擅改保存 Hook 或旧事件语义 |
| 研究 25 F04：预发行比较 | 当前版本函数截去 `-`/`+`，不可据此声称完整 SemVer；需要统一确定新格式、保留旧数字包解释 | RT-05、PN-011；不在 M1 顺手改加载兼容 |
| 研究 25 F05/F06：owner 文件和 migration | 当前 text query 按相对路径选首个，旧 migration 每次成功 Read 执行；不直接改变旧接口含义 | PN-018/013 加同名双 owner；PN-019 版本迁移与旧 Action 兼容 |
| 研究 05/22：内容隔离比 SMAPI 更强 | 接受。SMAPI 的 prevAsset 是引用，不能替 DTMAPI 深层候选事务作证明；先深改再抛错，测下一 editor 与资源成本 | D04、PN-013/E06；不可隔离资产暂不承诺事务编辑 |
| 研究 26：作者不反馈时更新/弃用 | 接受“未知不等于无人使用”，也不把作者沉默当永久否决。按公开契约、薄适配、双渠道提醒、已知样本、旧 DLL 和公告窗口决定删除 | AD-09、PN-033/014、E08；沿 API matrix/发行记录，不新建平行弃用台账 |
| 研究 26 的 90 天/两个周期例子 | 作为以后首次弃用政策的候选，不追认为 SMAPI 统一规定，也不在本轮追溯缩短既有承诺或宣布具体删除 | R6；当前没有接口被批准删除 |
| 更强隔离、IL 重写、在线服务、多人与全量实体模型 | 保留条件性远期方向，不加入 M1 前置；不过度解读 catch、private DLL、PDB、loaded 或历史 PASS | 现有能力地图/路线继续有效 |

## 近期实施的已选方案

F01 先去掉唯一配置写入链的手工格式化后处理，保留 DataContractJsonSerializer 的编码与原子发布；无其他消费者时删除内部 Prettyish。无需为了排版引新 JSON 依赖或重写所有 codec。用独立解析器确认转义/标点值保持，游戏实测随 M1 配置场景完成。

F02 区分解析损坏与 IO/访问错误。只有原字节确实备份成功才自动写默认；备份名唯一，失败抛出可归因错误且保留原件。旧 migration Action 语义留 PN-019，当前短修复不升级公共配置契约。

SDK160 从真实项目引用/语义引用及现有 PE 依赖闭包作决定；字符串、注释和同名用户类型不是宿主程序集引用。引用到禁用宿主时继续拒绝，未支持的引用方式明确报告；不升级成通用 analyzer 平台，不用复杂正则替换旧子串。最终 pack/Runtime/Doctor 的现有闭包校验不能因前端放宽而消失。

完整 M1 工作仍为 PN-015→004→016→017→008；PN-014/config-correctness 是其中配置验收之前的有界修复。新任务先完成该修复，再连续实施整条 M1；允许符号准备与事实调查按已有依赖提前做，不重启全仓设计。源/包/真实进程条件不变的结果可复用；PN-008 仍实际走一次公开作者闭环，不重复全部前置故障注入。R1 用这些证据收口已支持范围，随后交回 M2 入口。

本轮只改设计与接手资料。新任务按用户指定的当前工作区、GPT-6 Astra / high 执行；不会把任务创建等同于实施完成。
