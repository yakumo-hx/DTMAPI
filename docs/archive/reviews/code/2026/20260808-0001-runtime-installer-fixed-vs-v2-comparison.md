# Runtime Workshop 安装器修复版与隔离 V2 对比审查

- 日期：`2026-08-08`
- 状态：`recorded`
- 性质：独立代码/设计审查与假游戏压力测试；不是实现完成、真实游戏验收或发布批准
- Source：用户说明此前 GC 问题确由低版本 Runtime 引起，并要求分别审查已修复的现行 `0.6.1` 安装器与单独重设计的 V2，再进行比较
- 基线提交：`905dccc398f125cd830325d0ee934bb3d87f8529`
- 前置审查：[20260807-0002 三提交设计与压力审查](20260807-0002-runtime-installer-three-commit-stress-audit.md)
- 现行 authority：[Runtime Workshop Installer Boundary](../../../../architecture/runtime-workshop-installer-boundary.md)
- V2 候选设计：[Runtime Workshop Installer V2 Candidate](../../../architecture/2026/runtime-workshop-installer-v2-candidate.md)
- 现行修复 Update：[20260808-0001 Runtime installer 0.6.1 stress-boundary fixes](../../../updates/2026/20260808-0001-runtime-installer-061-stress-boundary-fixes.md)
- V2 Update：[20260807-0003 Runtime installer convergent candidate](../../../updates/2026/20260807-0003-runtime-installer-convergent-candidate.md)
- 相关 Debug：[ISSUE-023 Runtime installer stress boundaries](../../../../debug/issues/ISSUE-023-20260808-runtime-installer-stress-boundaries.md)

本次只使用仓库生成包、仓库内受管测试目录和伪造的游戏标记目录。没有安装到真实 Doloc Town，没有启动游戏，没有修改 Steam 订阅目录、官方 `MODS`、玩家日志或存档。两套实现都仍在同一未提交工作树中；本 Review 不替任一 Update 提升状态。

## 一、结论

**两套候选都不能按当前字节发布，但现行修复版更适合作为 `0.6.1` 的收敛基线。**

原因不是“现行版测试更多”，而是本次 GC 事件已经证明安装器必须能够识别低版本。现行版仍保留版本、FileVersion、长度/哈希、Compatibility Host 和双收据检查；同一个 `0.5.5`/零字节伪安装被它明确判为失败。V2 的轻量检查只看文件名是否存在，同一伪安装返回 `0` 并逐项打印 `[OK]`。V2 安装器还把零字节 BepInEx 视为完整，跳过修复并报告安装成功。这使 V2 当前实现与此次问题的根因直接冲突。

现行修复也没有完全闭合。它新增了 nonce 证明，却仍用三个 `%RANDOM%` 直接组成 `%TEMP%` 文件名。Windows 同时启动的 CMD 进程会得到相同随机序列；24 路真实分发器压力中，10 路最终误报“没有可用 PowerShell”。V2 已用“先独占创建随机会话目录”的方式解决了这一点。这部分应从 V2 回移到现行版，然后再冻结 `0.6.1` 候选。

建议决策：

1. 现行修复版继续作为 `0.6.1` 发布候选基线，只修主机探测会话的跨进程唯一性，并补相同压力回归。
2. V2 保留为后续精简/重构原型，不整体替换现行版；吸收它的原子探测目录、路径/reparse 防护、简洁 BAT 和日志原子发布实现。
3. V2 在再次比较前必须恢复最小但真实的版本/哈希/Compatibility/BepInEx 完整性证明，并把公开界面恢复成用户指定的四项。
4. 两套代码均不因本次假目录测试获得 Steam 发布或真实玩家验收授权。

## 二、冻结的四项语义

这四项是比较基准，不由某套脚本现有行为反向定义。

### 2.1 安装

- 一个公开入口：`1_install_dtmapi.bat`。
- 在任何变更前验证真实 PowerShell 主机、包完整性、游戏目录、游戏未运行和同游戏目录变更所有权。
- 重跑必须把 DTMAPI Runtime 收敛到**本包精确版本和字节**，不能只证明五个文件名存在。
- 已有 BepInEx 只有在必要文件非空、Doorstop 配置可用且满足兼容边界时才可保留；中断或截断文件必须在重跑时修复。
- 成功返回 `0` 前必须证明五个 Runtime DLL、Compatibility Host 和安装状态一致；不启动游戏、不管理功能 Mod 或 External BepInEx Plugin。

### 2.2 卸载

- 一个公开入口：`2_uninstall_dtmapi.bat`。
- 默认只卸载 DTMAPI-owned Runtime、Compatibility Host、安装工具/收据和可安全协调的待处理事务。
- 保留 BepInEx、External BepInEx Plugin、Workshop/Local Mods、Steam、存档、配置和支持日志。
- 从未安装时可幂等返回 `0`，但必须明确报告 no-op；待处理事务无法安全协调时应在变更前失败。
- “移除全部 BepInEx”不是本次四项玩家界面的默认卸载语义。

### 2.3 检查

- 一个公开入口：`3_check_dtmapi_status.bat`，完全只读。
- 至少区分未安装、部分安装、低版本/字节漂移、当前包完整安装；校验五 DLL、Compatibility Host、BepInEx/Doorstop、版本/长度/哈希和必要收据。
- `0.5.5`、零字节 DLL、Compatibility Host 缺失或无法证明来源时必须非零，不能只给 warning 后返回 `0`。
- 静态完整不等于当前游戏进程已加载；后者继续由新鲜 Runtime 日志/启动证据证明。

### 2.4 导出日志到桌面

- 一个公开入口：`4_collect_dtmapi_logs.bat`。
- 游戏关闭后导出最新十份 DTMAPI current/history 日志，完整复制、不按大小截断；可选 BepInEx/Unity 日志按文档边界收集，dump 仍为 opt-in。
- 每个源在复制前后保持稳定，目标长度和 SHA-256 匹配。
- 每次调用使用独立唯一的同级 staging，全部验证后一次目录重命名；失败不发布完成外观的部分包。
- 默认输出到桌面并打印实际目录；不能因同秒或并发调用混用一次证据。

## 三、逐套发现

### 3.1 现行 `0.6.1` 修复版

#### F1 — P1：nonce 文件名在并发 CMD 中碰撞，导致真实 PowerShell 被误判不可用

位置：`tools/release/runtime-workshop/invoke-dtmapi-action.cmd:117-126`。

修复版已不再只信退出码：PowerShell 用 `CreateNew` 写入绑定 nonce 和 action 的证明，CMD 必须读取精确证明才接受主机。这正确修复了上一 Review 的假阳性。

剩余问题是 nonce 的存储所有权：

- 每个 CMD 直接用 `%RANDOM%-%RANDOM%-%RANDOM%` 组成同一个 `%TEMP%` 文件名；
- 先删除已存在路径，再启动较慢的 PowerShell parser/probe；
- 多个 CMD 进程同种子时会相互删除、占用或消费同一证明文件。

本轮压力证据：

- 同时启动 128 个 CMD，三段随机值只有 `1` 个唯一结果，128 个进程全部得到 `28015-18794-3191`；
- 24 个真实 `3_check_dtmapi_status.bat` 共用一个临时目录：14 个选到主机，10 个最终打印 `No PowerShell host passed`，9 个出现 proof 竞争警告；
- 动作尚未开始、假游戏未变更、没有残留 proof 文件，所以这是确定的可用性/错误诊断缺陷，不是本轮数据损坏。

证据：[current probe collision summary](../../../../../tmp/test-runs/current-probe-collision-20260808-0723/targeted-review-summary.md)。

验收门槛：采用 V2 已验证的独占会话目录分配，目录创建失败就重新取 token；proof 只能位于本进程成功声明的目录。新增至少 24 路真实分发器压力，所有进程必须先可靠选到主机，然后才由游戏目录锁决定动作结果，不能建议玩家修复 PowerShell。

#### 现行修复中已通过的原问题

- nonce/action 精确证明能拒绝 silent-zero `cmd.exe`；
- 安装和卸载共享按规范化游戏目录绑定的进程期 mutex；
- 待处理 Runtime 事务使卸载在 live move 前失败；
- no-op 卸载提示和收据名真实且唯一；
- 玩家日志导出为最新十份完整日志，带稳定性、长度/哈希校验和 staging rename；
- 当前检查器能把低版本/零字节伪 Runtime 判为失败。

因此 ISSUE-023 的旧五项问题大部分确已修复，但 F1 表明“主机证明 + 并发边界”尚不能整体标记完成。

### 3.2 隔离 V2 候选

#### F2 — P1：BepInEx “完整”只等于五个路径存在，重跑不能修复截断文件

位置：`tools/release/runtime-workshop-v2-candidate/overlay/Content/DTMAPIInstaller/tools/player-common.ps1:323-343` 与 `install-dtmapi.ps1:22-35`。

`Test-DtmApiBepInExComplete` 不检查长度、Doorstop 的 enabled/target 配置、版本或哈希。只要五个路径存在，安装器就跳过包内 BepInEx，并在最后使用同一 presence predicate 判成功。

复现中五个路径全是零字节；安装返回 `0`，输出 `Existing BepInEx installation is complete; keeping it`，最终 `BepInEx.dll` 仍为零字节。这也否定了 V2 设计“中断后重跑会收敛”的一般表述：BepInEx overlay 若在覆盖某文件时中断并留下截断文件，下一次会因路径仍存在而跳过修复。

验收门槛：至少检查全部固定 BepInEx 必需文件非空和 Doorstop 配置；对包内固定 ZIP/展开目标使用可追溯完整性 authority。加入“必需文件存在但零字节/截断”和“Doorstop disabled/target 错误”重跑案例。

#### F3 — P1：检查器会把低版本、零字节 Runtime 和缺失 Compatibility 判为成功

位置：`tools/release/runtime-workshop-v2-candidate/overlay/Content/DTMAPIInstaller/tools/check-dtmapi-status.ps1:99-174`。

检查器只用 `Test-Path` 检查五个 DLL 和五个 BepInEx 路径；version marker 只打印，不参与返回码；Compatibility Host 完全没有进入 installed-state 检查。

两个独立复现：

1. 五 DLL/BepInEx 路径存在、Compatibility Host 缺失：返回 `0`。
2. 五 DLL 和 Compatibility Host 都是零字节，marker 明确写 `DTMAPI 0.5.5`：仍返回 `0`，逐项打印 `[OK]` 并显示旧 marker。

现行 `0.6.1` 检查器对第二个同一目录返回 `1`，报告五项 FileVersion drift、缺少收据和 Runtime 旧于/不一致。这是两套设计与此次 GC 根因最直接的区别。

证据：[V2 targeted review summary](../../../../../tmp/test-runs/runtime-installer-v2-20260808-071723-231-9606c7f9/targeted-review-summary.md)。

验收门槛：检查必须消费与安装相同的包版本 authority，核对五 DLL 的非空、FileVersion/managed identity/长度/哈希、Compatibility Host 和最小收据；任何不一致返回非零。轻量化可以减少诊断文字，不能把完整性降为文件名存在。

#### F4 — P2 / 语义阻断：V2 把第五个破坏性入口写成了构建和测试的成功条件

位置：`tools/release/runtime-workshop-v2-candidate/build-candidate.ps1:108-116`、`test-candidate.ps1:240-263`。

用户冻结的是安装、卸载、检查、导出日志四项。现行 authority 也明确是四 BAT。V2 却要求第五个 `9_full_uninstall_dtmapi_and_bepinex.bat` 存在，并把“五 BAT”记为 PASS。

该入口会删除整个 `BepInEx`、整个 `DTMAPI`、根级 Doorstop 文件、通用名 `changelog.txt` 和生产形状的旧事务目录。它有 `REMOVE` 确认、exact-child 和 reparse 防护，这些局部安全措施是好的；但 V2 安装会保留任何“看起来完整”的既有 BepInEx，因此它没有证据证明后来完整卸载的整棵 BepInEx 都由 DTMAPI 安装。确认提示只能表达破坏意图，不能补出所有权证明。

验收门槛：公开候选恢复四 BAT。若确需完整清理，应另立 support-only authority、单独审查来源/允许列表和用户入口，不作为普通四项安装器的第五个成功条件。

#### F5 — P2：V2 的安装与检查使用两套游戏定位器

安装通过 `player-common.ps1` 读取 Steam `libraryfolders.vdf`，检查器的 `Resolve-LiteGameDirectory` 只试 Workshop 同库和注册表 Steam 根下的默认 `steamapps\common\Doloc Town`。当包被按帮助复制到普通目录、游戏又位于第二 Steam 库且未设置 `DTMAPI_GAME_DIR` 时，安装可定位，检查却可能失败。

验收门槛：四动作共享一个只读 resolver；“轻量检查”不应复制并缩减游戏发现规则。

### 3.3 文档一致性

`runtime-installer-history-regression-index.md` 仍把“最新十份 DTMAPI 日志完整复制”列为不得改写当前 `0.6.1` 的 V2-only 选择，但现行 canonical boundary 和代码已经采用同一语义。历史索引因此会误导下一轮比较。修复实现时应只更新该索引的 current/V2 差异，不复制完整完成叙事。

## 四、比较

| 维度 | 现行修复版 | 隔离 V2 | 审查判断 |
| --- | --- | --- | --- |
| 公开界面 | 4 BAT | 5 BAT，含完整卸载 | 现行符合冻结语义 |
| 玩家脚本规模 | 4 BAT、9 PS1、1 CMD，约 8,679 行 | 5 BAT、6 PS1、1 CMD，约 1,177 行 | V2 明显更易理解，但删掉了必要证明 |
| 低版本识别 | 版本、FileVersion、收据、长度/哈希；本轮正确拒绝 | marker 仅显示，文件存在即绿；本轮误收 `0.5.5`/零字节 | 现行胜，且是本次 GC 核心门槛 |
| 主机证明 | nonce/action 证明正确；临时文件分配并发碰撞 | 独占创建 probe session，碰撞后重试 | V2 的分配方式胜，应回移 |
| 安装原子性 | 同卷事务、恢复/回滚、pending fail-closed | 删除/覆盖后靠重跑收敛，无收据恢复 | `0.6.1` 发布继续用现行 |
| BepInEx | 包内固定 ZIP + 固定官方 fallback；必需文件非空、配置检查 | 仅包内 ZIP，离线确定；但只看五路径存在 | V2 来源更简单，完整性不合格 |
| 正常卸载 | Runtime-only，保留 BepInEx/External/Mods | Runtime-only 同样清楚 | 两者正常语义均可 |
| 完整卸载 | 无公开第五入口 | 删除全部 BepInEx/DTMAPI | V2 越过本轮四项语义和所有权边界 |
| 日志导出 | 最新十份完整日志、唯一 staging；真实并发先受 F1 影响 | 同语义，V2 两路并发通过 | 文件发布都好；端到端并发 V2 胜 |
| 路径删除防护 | canonical root/transaction containment | exact-child + 递归 reparse 拒绝，代码更集中 | V2 实现更清晰，值得吸收 |
| 当前发布距离 | 修一个已复现的 probe-session 问题后可重新候选化 | 需重建完整性/检查/界面语义 | 现行明显更近 |

V2 的价值是真实的：它证明这套玩家安装器可以把入口和路径操作缩小很多，并找到了当前 nonce 文件分配的正确方向。但“代码少”不是目标本身。当前 V2 的约 7,500 行减少量中，包含了识别低版本、证明安装字节和协调所有权所需的行为；这些不能全部作为冗余删除。

## 五、压力与验证结果

| 对象 / 测试 | 结果 | 说明 / 证据 |
| --- | --- | --- |
| 现行修复版临时 Runtime-only 生成包 | PASS | `tmp/test-runs/runtime-installer-fixed-package-20260808-071906-953-1706dc5f/DTMAPI` |
| 现行 `test-runtime-workshop-installer-061.ps1` | PASS | 特殊路径、silent-zero host、锁、rollback、pending uninstall、完整日志、no-op；保留根 `runtime-installer-061-9614f2db7559431287fbe9c68773e404` |
| 现行通用订阅包审计 | PASS，0 blocker | [stress summary](../../../../../tmp/test-runs/subscription-audit-current-20260808-0720/DTMAPI%20Workshop%20Audit%2020260808-072009/Results/stress-summary.md) |
| 现行 Runtime-only uninstall / invalid target focused checks | PASS | ownership 测试完成且临时根已清理；invalid target 为 `2/2/2` |
| 现行 24 路真实 dispatcher | FAIL | 14 选到主机、10 误报无 PowerShell；见 F1 证据 |
| 现行 dual-host transaction 独立重跑 | INCOMPLETE | 外层 124 秒时限先终止；匹配的残留 child 已核对命令行并停止。本轮不把它计为 PASS 或代码失败；Update 中既有 17+17 证据未被本次替代 |
| V2 tracked 20-case matrix | PASS | [stress summary](../../../../../tmp/test-runs/runtime-installer-v2-20260808-071723-231-9606c7f9/stress-summary.md)；PS5.1/PS7、特殊路径、四路锁、两路日志、reparse、完整卸载均过 |
| V2 通用订阅包审计 | PASS，0 blocker | [stress summary](../../../../../tmp/test-runs/subscription-audit-v2-20260808-0721/DTMAPI%20Workshop%20Audit%2020260808-072027/Results/stress-summary.md) |
| V2 缺 Compatibility 检查 | FAIL | checker 返回 `0` |
| V2 `0.5.5` + 五个零字节 Runtime 检查 | FAIL | checker 返回 `0`；现行 checker 对同目录返回 `1` |
| V2 五个零字节 BepInEx 后重跑安装 | FAIL | install 返回 `0`，BepInEx.dll 仍为 0 字节 |
| 真实游戏 / Steam / 受影响玩家 | NOT RUN | 安装器审计使用伪游戏目录；无发布授权 |

两套“官方矩阵全绿”与新增 FAIL 不矛盾：现有矩阵验证了各自写下的 contract，但 V2 的 contract 没有把版本/长度/Compatibility 列为成功条件；现行矩阵也只以外部持锁测试并发，没有同时启动多条 CMD host probe。因此本次发现属于覆盖边界缺失，而不是原有证据造假。

## 六、下一步边界

本 Review 不授权直接修复或发布。若进入实现，最小顺序是：

1. 在现行 CMD 中回移 V2 的独占 probe-session 目录分配；保留现行 `CreateNew` proof 和 action/nonce 精确匹配。
2. 把 24 路真实 dispatcher 测试加入 `0.6.1` focused matrix，分别覆盖 check、collect 和锁保护的 install/uninstall；probe 全部成功后才判断锁结果。
3. 重跑受影响的 installer matrix、订阅包审计和双 host transaction focused gate；冻结新的临时包字节。
4. V2 后续单独把公开入口减回四个，复用一个 resolver，并引入紧凑的 package/install manifest，而不是回到“文件名存在”判定。
5. V2 完整卸载若继续存在，先另做所有权/允许列表设计审查，不与普通 Runtime-only uninstall 混为一个发布选择。
6. 任何真实发布仍需按现有 Update/Debug 边界完成候选冻结和受影响玩家验收。
