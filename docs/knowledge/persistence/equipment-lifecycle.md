# 装备侧车、所有者与原生 UI

本页汇集历史决策与未完成边界，不把历史方案或旧 PASS 转成现行规范。当前身份和保存规则由 [PROJECT](../../../PROJECT.md)拥有；Hook 合同查 [MoreEquipmentSlots Hook map](../../hook-map/focused/MoreEquipmentSlots.md)。

## 已被多次问题区分的生命周期

- [ISSUE-021](../../debug/issues/ISSUE-021-20260805-moreequipment-native-placement-save-quarantine.md)：原生放入背包/邮件可能先修改再抛异常；立即读回失败时，不能用 Boolean、迟到的计数变化或重试推断原操作成功。恢复 journal 必须先登记 session，再开始原生动作；结果不明保留隔离并拒绝保存。后续输入物品扣除、held-buffer 清空也遵循同样的结果证据原则。
- [ISSUE-024](../../debug/issues/ISSUE-024-20260817-moreequipment-newgame-null-slot-ui.md)：NewGame 的 null slot 不等于未进入档案；丢弃 IsNewGame 令 Product document 为空，五个 Hook 已安装仍无法生成 UI。官方饰品袋只改变原生槽数，不能解释 Product 文档突然存在。
- [ISSUE-026](../../debug/issues/ISSUE-026-20260820-moreequipment-legacy-sidecar-player-mismatch.md)：旧侧车姓名不匹配只证明身份冲突；安装器只改脚本且不拥有 config，时间相邻不能归因为安装器改档。改名、删建、云复制、多安装目录均需独立证据。
- [ISSUE-028](../../debug/issues/ISSUE-028-20260824-moreequipment-save-slot-reuse-v3-sidecar.md)：数字槽与姓名、时钟不足以构成永久所有者身份。相同姓名新档追上旧时钟后，曾有接受旧 Product-v3 状态的风险。

## NewGame 决策历史与残余范围

2026-08-26 用户选择保留侧车与停用物品回收。实现以已证明 NewGame、精确原生 index、当前原生 archive 缺失为边界，清理完整 Product 槽目录并建立未提交的空文档；首次保存前重新绑定原生起名对话产生的姓名，仍通过原有 candidate 提交。只删主 JSON 会遗漏 previous/transition 残余。

此前 native archive 内嵌字符串是条件性设计候选，需要独立原生持久化 spike 与玩家行为决策；没有因此成为已实现能力。Product 在删档和 NewGame 全程缺席、DuplicateGame、已保存人物改名以及首次会话 UI 截图缺口应回到对应 Issue 判定，不用旧 Mitigated 或标题中的 published 文案代替结论。

## UI 与存储证据独立

历史固定 clone 布局会与增长的官方 passive pool 重叠，也可能漏入 selectable/navigation。后来的原生尾部布局、官方 1–5 槽边界、五目标 Product Hook 是各自候选的实现事实。旧四 Hook、旧入口/抽屉、固定一至二槽模型不能用作新布局验收。UI 修改不自动推翻未改的保存契约，但完整候选发布仍需它自己的证据。

Hook map 同时保存多代候选的 Status、Evidence 与 pending 叙述，需拆出历史后校准当前路由；本次阅读不替它宣布发布或关闭问题。

## 提取与冻结兼容的历史设计

20260723-0009 admission 将固定三槽玩法/UI/侧车归唯一 ProductNative 消费者，旧任意 owner API 归同一个兼容 Host，禁止凭共享游戏类型制造 SharedNative。最初四 Hook 与旧 UI 静态模型是当时 build 的结论，后续五 Hook 不得反写原文。原记录要求的备份写回、HookProbe 与两进程恢复也已由后续问题和 PROJECT 新规则细化；保留设计价值而不复活过期运行前提。

## 直接替换案例暴露的工具衔接成本

20260810-0001 保留了用户从第二行否决、入口/抽屉到最终原生尾部横排的选择顺序。旧方案的 UI/Release PASS 不能证明新设计。三槽继续采用相同帽子/被动饰品准入与 Product typed shield，不改官方外观槽或 progression。

该记录全文中的失败可归为三类：

- 产品：克隆缺运行时 rect 初始化、HideHoverBox 重载不精确、父 LayoutGroup 接管抽屉、原生 CLR 子命名空间写错。fake fixture 同样写错名字会共同假绿。
- QA：重开动画尚在屏外就采图、四 Hook/四参数 attack 旧断言、首次空侧车强制要求磁盘文件、normal-save 越过 Product owner 重测整个 attack tail。把稳定性检查放在稳定帧后；保存场景只证自己的提交边界。
- 编排：隔离根漏投影 MODS/enablement，发物 helper 意外落入冻结 Host，冷恢复缺已安装 manifest 信任投影；后来重复遗漏同一 fixture 输入。这些前提应由共享准备过程一次给齐，不靠每轮手写外层脚本。失败阶段立即收口，不串联多个独立超时。

运行时 render 不应在 tween 中枚举全屏控件或以瞬间越界永久隐藏槽；Product 检查稳定槽关系，QA 在稳定后检查完整 viewport。旧 Candidate11 里程碑被明确移出默认 Release replay，保留显式历史入口；纯内部错误消息变化和过期 semantic token 不应再成为新产品的长期默认门。

## 早期手测已证明什么

[20260614-0001](../../archive/reviews/manual-qa/2026/20260614-0001-equipment-slots-protected-storage-review.md) 的用户已确认属性生效、不改人物外观、保存重载不复制、不跨档污染、禁用后回背包。同期残留的是同进程禁用后空槽仍可见且拒绝放入；完整重启后消失，应归 UI 注册清理，不能倒说受保护存储从未工作。第三方纸箱扩容只是尾部回收设计输入，未有注册合同或兼容承诺。

[菌菇帽 Review](../../archive/reviews/manual-qa/2026/20260614-0002-equipment-slots-mushroom-hat-review.md) 说明有用装备不一定有 Skill：原生帽子的 Defense 是独立通道。无 Skill 不能一律拒收，有 Defense 应应用；两者皆无仍可存储、恢复、显示，而不写原生外观槽。

[盾帽手测](../../archive/reviews/manual-qa/2026/20260614-0003-equipment-slots-shield-hat-manual-qa.md) 五项依次为：①原生盾帽优先且一次只消耗当前盾；②混合普通帽只消耗盾帽；③三个额外盾帽作为独立次数、未双扣；④菌菇/防御帽效果生效；⑤额外盾帽没有原生黄色盾条，用户明确接受本轮不修。后续改造必须保护这些真实人工基线，也不能把第五项重新设为用户从未要求的阻断。

## 覆盖范围错误比测试次数少更值得修正

[20260806-0001](../../archive/reviews/manual-qa/2026/20260806-0001-moreequipment-official-slot-growth-review.md) 用户原序为：①AutoFishing/MoreSaves/DebugConsole 正常；②五个无实质变化产品沿用 smoke；③Manbo/Zoom 本轮不处理；④复核官方扩槽。随后另轮问题 1 用实际第三原生槽否决了硬编码二槽候选。记录最后才形成已验收的原生 `1–5 + Product 3`，第六原生槽明确不支持。不能只读早期 `1 -> 2` 设计，也不能把原生能继续创建误称任意数量视觉可用。临时挂载加入上传专属 workshop.json 导致七文件冷恢复包被拒，是 package 与 upload authority 混用，不是保存失败。

[20260817-0001](../../archive/reviews/manual-qa/2026/20260817-0001-moreequipment-newgame-null-slot-review.md) 证明 NewGame `null,true` 是合法生命周期，三个槽消失发生在 document 建立之前；饰品袋只扩原生数组，不能建立 Product document。已有 MoreSaves 知识未传播进装备测试是覆盖债务。当前进展继续看 ISSUE-024/028；不能将该审查的“未实施”盖过后来 mitigation，也不能将缺少首会话截图的 mitigation 写成 UI verified。

[20260820-0001](../../archive/reviews/manual-qa/2026/20260820-0001-moreequipment-legacy-sidecar-player-mismatch.md) 中人物名 mismatch 只证明归属冲突被拒，无法区分改名、删档、云恢复或另一安装根；单条错误不足以授权删/改玩家 JSON。安装器没有写该文件这一直接因果被排除，改变何时加载的间接触发仍待证。后续 [20260824-0001](../../archive/reviews/manual-qa/2026/20260824-0001-moreequipment-deleted-slot-reuse-review.md) 的 Product-v3 同名时钟回退是不同现场；用户后来明确放弃旧 owner 才授权整目录清理。同一异常在 Monitor 与事件层两条输出不是两个根因。

早期 [存储 smoke](../../archive/updates/2026/20260603-0015-equipment-slots-storage-recovery-smoke.md)、[只读 UI strip](../../archive/updates/2026/20260603-0021-mine-placement-equipment-ui-smoke.md) 和 [机械拆分](../../archive/updates/2026/20260608-0016-gamebridge-equipmentslots-feature-split.md) 各有不同证据范围：配置面板 equip/recover 不能证明原生鼠标交互；render/bind 日志不能补不存在的截图；机械移动后重复 build、test、game 是当时执行史，不能推导今后每次结构移动都必须重复整套。

## 从存储修复到交付，避免重新制造同一门

[20260614-0001](../../archive/updates/2026/20260614-0001-equipment-slots-protected-storage.md) 的第一次 smoke 因已有游戏进程被启动前拒绝，没有游戏结论。后续同时区分 basic smoke、用户人工通过和未运行的自动化范围。per-save 路径从 global 演变而来，title 注册不读 sidecar，tail-first 与名称/时钟 guard 是当时设计；后来发现的 owner incarnation 缺口并不因此消失。

[帽表诊断](../../archive/updates/2026/20260614-0005-equipment-hat-table-diagnostic.md) 的 33 帽/33 item/零缺项只是该次运行时内容快照；它为 [帽子防御](../../archive/updates/2026/20260614-0004-equipment-slots-hat-defense.md) 与 [盾帽实现](../../archive/updates/2026/20260614-0006-equipment-slots-shield-hat-protection.md) 提供类型与效果依据，不等于盾牌行为稳定。盾牌不能简单注入原生 functions，因为原生破盾会清外观帽；额外防御也必须过滤已禁用或超出启用范围的 owner/槽，避免恢复失败留下属性。

[元数据同步](../../archive/updates/2026/20260614-0002-equipment-slots-upload-metadata-sync.md) 已示范纯作者/文案/图片及包字节核对不再运行游戏；[盾帽上传同步](../../archive/updates/2026/20260614-0007-equipment-shield-upload-sync.md) 引用同一已通过 smoke，并未制造第二份行为证据。早期 source manifest、official-info、发布元数据与 live upload 多处同步是历史重复投影；当前唯一元数据 owner 由 Catalog/SDK 规则确定。

[20260826-0001](../../archive/updates/2026/20260826-0001-moreequipment-newgame-slot-reset.md) 的用户已将第十二槽授权为可任意保存/删建、无需复原的测试位。实际执行仍另做隔离 fixture；这是流程选择，不能反过来解释为用户未授权直接测。记录保留三种具体成本：测试路径过长、先用当前 build 触碰已知 hash-fixed policy 的 SDK202、Catalog 日期/版本/冻结 canary 多处同步。实机证明 NewGame 清旧目录、首存、冷载，却没有首会话装备截图，所以仍为 mitigated。

[20260830-0002](../../archive/updates/2026/20260830-0002-moreequipment-101-workshop-release-closeout.md) 只核对用户已上传订阅及改文案，明确 Product 无安装器，不跑 Runtime 安装矩阵，也不因当日 game build 更新重标旧行为证据。源码文案后继包、Steam 当前发布包、原行为测试包是不同事实；精确 DLL 一致允许引用旧行为证据，订阅一致不能把 mitigated 提升为 verified。

## 直接替换长记录的流程成本证据

[20260811-0001](../../archive/updates/2026/20260811-0001-moreequipment-slots-100-direct-replacement.md) 的 63,491 字符已完整读取。三个 UI 决策阶段分别是抽屉、原生一至二槽尾部、原生一至五槽尾部；用户两次改变/纠正边界，有效实现工作也在其中，不能把全部耗时都归测试。每阶段各做一次约 940–990 秒完整 Release，另有 685.868 秒诊断尾部；这些是历史发布合同，当前小修不自动继承。

可以避免的编排失败更具体：外层五秒 timeout 杀掉刚启动的验收；截图异步落盘却按同步查文件；路由声明七文件而 stage 重建旧两文件；预期 native stderr 被 ErrorActionPreference=Stop 当异常；Author SDK 需要 pwsh 却被外层 WinPS 5.1 执行；临时准备脚本对空数组取 .Name/.Stream 以及 object-array 加法两次报错。上述均发生在核心产品之外，适合用一个固定准备/执行入口消除，不能再每轮拼临时 wrapper。

同一页还明说第二次漏隔离 MODS/enablement 是“重复已知边界”，typed shield 的正常保存 gate 又错误要求旧 attack-tail handled=true；把保存 gate 改为验证 exact Product shield provider 并保存，已通过的 NoNativeSave 另负责真实伤害/破盾，职责才一致。最终订阅核验只读比较精确包，不重新游戏或完整 Release；两个树 hash 算法排序不同可对应相同文件，不应仅因 aggregate hash 不同误报漂移。
