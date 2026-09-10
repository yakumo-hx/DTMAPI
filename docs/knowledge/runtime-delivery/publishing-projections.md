# 发布身份、文案投影与旧门槛

本文提炼交付历史中的职责边界，不定义当前发布版本或候选。当前 Mod 身份查 [PROJECT](../../../PROJECT.md)，当前任务路由查 [current-state](../../onboarding/current-state.md)，机器事实查 [Product Catalog](../../../tools/release/dtmapi-product-catalog.json)。阅读原件与 hash 见 [清单](../../archive/migrations/20260908-workspace.json)。

## 早期 release checklist 已有明确替代

[0.5.0-alpha checklist](../../archive/releases/0.5.0-alpha-developer-preview-checklist.md) 和 [hygiene report](../../archive/releases/0.5.0-alpha-release-hygiene-report.md) 自带 July 13 ownership-policy note：旧 marker-owned official-local 删除门已被 [Runtime-only uninstall](../../archive/updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md) 替代，`dtmapi-package.json` 不具有删除授权。旧 `-RemoveOfficialLocalPackages` 的通过记录只能证明当时行为，不再指导当前脚本。

旧第一批八产品、developer 全十四包、Manager/HookProbe/MoreSaves 组合属于当时 release preparation。[June 11 Update](../../archive/updates/2026/20260611-0019-release-hygiene-installer-preview.md) 先承认没有 fresh MoreSaves UI 结果，随后 [June 12 hardening](../../archive/updates/2026/20260612-0001-release-hygiene-hardening-2.md) 用 `GAME-SMOKE/20260612-015402` 关闭该证据切片。该结果证明 12 个 UI 槽，不自动证明 7+ 的所有保存生命周期；后续用户证据与固定 12 产品决定查当前 MoreSaves owner。

Runtime-only 玩家安装与开发本地产品安装是不同内容集合。安装 receipt 只记录所需路径、版本和受管动作，不能把“看见旧 Workshop cache”当作仍订阅或作为清理许可。对 unreadable `mod_infos.json` 停止覆盖、保留 enablement/priority、采用明确文件写入边界是历史有效经验；是否触碰此文件由当前 installer owner 决定，不能把当时备份动作套到普通无保存游戏测试。

## 中文发布文案曾多次重复投影

[June 12 aggregate](../../archive/updates/2026/20260612-0013-dtmapi-mod-publish-zh-json.md) 最初把 Runtime 和 16 个 local testmod 文案集中成 JSON，并同步回源 `official-info.json`/manifest 与脚本内 base64。它明确移除玩家描述中的 API 实现细节，使用功能、依赖和实际限制来说明产品；纯元数据修改没有游戏 smoke。

[June 13 writeback](../../archive/updates/2026/20260613-0018-publish-metadata-writeback.md) 已取消脚本里的重复 base64 Runtime 长文，直接读取聚合文案。真实官方 uploader 消费的是 `description`/`localized_description`，自创 `steamDescription` 不能替代官方字段。写回应保留现有 enabled/priority；发布展示版本、manifest minimum 和 assembly binding 是不同事实，不能在文案同步时顺带改兼容性。

这些记录解释了重复来源，但不授权继续维持 Catalog、聚合 JSON、source info、脚本文本和已安装目录之间多向同步。当前重构应让产品/发布身份单一来源，玩家可编辑长文有明确 owner，生成字段定向投影。历史曾直接改 subscribed Workshop cache 的动作只保留事件记录，不是当前更新路径。

[已退休 Markdown 副本](../../../archive/release-projections/dtmapi-mod-publish-zh-legacy-20260612.md) 已明确其原 bytes 的 Git commit `e7800e14bc6e`、blob `8ef68b1fba2ce88ea32c3a94c5d91d6b9afbea5d`、长度 12728；旧 `testmods/...` 不是当前 source path，不应再把它恢复成 live 发布 authority。

## 旧审查结论不作为自动待办

[June 12 branch audit](../../archive/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md) 及其 [Update](../../archive/updates/2026/20260612-0010-refactor-branch-wide-code-audit.md) 区分 UI first-N 截断与数据缺失、18+ overflow 与存档损坏、纯电 Mine 未实现与标签错误、dead helper 删除与实际视觉修复。可复用的是这类因果区分。

其中 18/24 paging、Camera background 同步、混合燃料 Mine、F9 fish info 和早期 AnimalViewer pending 均属于当时截面，后续 fixed-12、playable-only Camera 与 ProductNative 决策已变化。当前状态只能跟随相应产品 owner；本次归档不恢复旧平台前置工程、整套测试或未核实需求。

## 原生上传的 UI、查询与提交分层

[June 14 0009](../../archive/updates/2026/20260614-0009-local-workshop-upload-plan.md) 用人工 Update plan 改对按钮，却跳过原生 GetDetails 后导致上传超时，因此已被 [0010 display-only](../../archive/updates/2026/20260614-0010-local-workshop-upload-display-only.md) 撤除。随后 [0011 busy fallback](../../archive/updates/2026/20260614-0011-local-workshop-upload-busy-fallback.md) 定位 no-callback 的队列停滞，与最终 Steam SubmitItemUpdate timeout 分开。

[June 15 watchdog](../../archive/updates/2026/20260615-0001-local-workshop-upload-plan-known-id-watchdog.md) 只在相同 uploader callback 和 workshop id 仍未解决时有界处理，让 native GetDetails 先运行。用户重启后两个确切 item `3743016467`、`3744059735` 的 Steam log 记录 OK，关闭对应手工样本；没有宣称治好全部 Steam timeout。这些 Update 未固定确切 native build，当前实现须回到 focused owner，不能按旧反射名字直接重装 Hook。

历次 aggregate 还曾因 screenshot-file collection false 而 Failed，但实际截图存在、目标 UI 和退出字段通过。必须保留分项事实及失败原因；既不能用 aggregate 一刀切否定目标行为，也不能把 UI 通过当作完成上传。
## 预上传、已发布、当前构建与 retained 不能共用一个期望

08-01 本地发布收口授权的是实际手测的 Runtime 与十产品现有 Workshop item 精确树；它保留 global stop，仅加绑定已有 ID/folder/version/hash 的例外。该时点没有 Steam 上传，之后的 `currentPublishedArtifact` 单独记录实际订阅结果。官方 ModManager 扫 Local 会补空三语言 `localized_name`，造成 90 字节稳定格式差异；builder 后来主动输出该对象。此历史记录未另列明确 native build，因此只保留当时源码链，不外推跨版本。对已发布 0.5.5 无需因生成器对齐再上传。

当前候选 0.6.0 的 info 版本改变是合法 source projection，不能拿 frozen 0.5.5 published info hash 验它。08-04 已把候选门归 `currentSourceBaseline`，已发布 identity 仍归 `currentPublishedArtifact`。改 published 值来“修绿”候选或只比较长度都错误。同理，现存已接受产品 source 未变但当前 SDK/Abstractions 不匹配时，07-31 手测合理复用 admitted package，没有借机重签或替换未验新包。

0.6 的 Candidate11 是九个当前源码 Advanced + 两个 retained（MES/Manbo），不是十一份重建。retained Manbo 的 manifest 0.1.0-dtmapi 与真实 info 1.0.0 不同，由 exact tree 冻结事实，不能用新源码一致性规则修写历史包。现行 SDK schema-2 Advanced marker 与 legacy marker 分支也不能被统一假 fixture 掩盖。选择集合必须从既有 release contract/Catalog 得到，不另存第三套名单。

树算法名及比较器属于身份：08-06 修的是 published Runtime aggregate 用错 OrdinalIgnoreCase，真实路径/字节未变；现行 published algorithm 是 Ordinal。另一个 frozen retained 合同则确有历史 OrdinalIgnoreCase 规则，不能为了统一脚本把旧身份静默重算。跨 PS5.1/PS7 固定排序向量可以直接覆盖这类差异，无需游戏。

独立 Workshop builder 曾漏传每产品 exact policy fixture 的 GameDir，前三项当前游戏 policy 掩盖了其余旧 policy 的失败；修复按唯一 policy 复用一次临时引用根，并将显式 GameDir 一路传到 shared builder。SkipBuild 消费已有 ZIP，不额外要求游戏/重建引用。只修入口接线，不改 policy、SDK target 或产品身份。

来源：07-31 Update `0004`；08-01 Updates `0002`、`0003`；08-04 Reviews `0004`、`0020`、`0021`。0.5.5 本地发布前状态由 08-01 `0002` 接管，再由 `0003` 接管实际发布 metadata；0.6 后续关闭状态归 08-02 唯一 Update。
## 同版本号也可能需要重传，候选投影也可能合法不同

0.6 冻结路线逐文件比较当时九个已发布产品与源构建：Zoom 九文件完全一致，另外八个虽仍标 1.0.0，Entry、准入或包绑定已经改变。需要重传的集合应来自确切最终候选与已发布树的比较，而不是版本号或前一候选的旧表。Runtime 候选与已发布 Runtime 身份同样分开；package manifest 的 `workshop-runtime` 与安装后的 `workshop-runtime-install` 是不同投影，不能要求两份整个文件哈希相同。

旧 retained 的 OrdinalIgnoreCase 与新 published 的 Ordinal 聚合算法均是各自冻结身份的一部分；修正当前汇总不重写旧收据。上传时保留既有 workshop.json 身份，候选完成后再切换到正式目录；不完整构建不冒充最终 candidate。仅 QA/文档提交导致 provenance 改变时，应检查实际 DLL/payload 是否变化，再决定已有证据是否失效。

来源：`docs/updates/2026/20260802-0001-dtmapi-060-aug2-5-authority-roadmap.md`；当前已发布集合及 latest release owner 仍由 Catalog 和 subscription manifest 指向。