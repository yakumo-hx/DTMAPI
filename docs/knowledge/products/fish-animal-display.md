# 鱼卵与动物进度显示：共享查询与产品渲染

这是 2026 年 7 月迁移研究的提炼。现行契约查 [API 路由](../../api/README.md)、[Hook 路由](../../hook-map/README.md) 与各产品源码；历史准入序号、包哈希和单次 PASS 不代表当前包。此处不升级未完成性能建议。

FishBreedingAssistant 的鱼卵标题装饰与 AnimalHusbandryProgress 的进度行确有两个独立消费者，共同依赖 `DolocAPI.QueryItemProto(itemId).Title`。当时只把这个主线程、只读、字符串入出的查询提为 Experimental `IItemDisplayNameApi`。Fish 的标题 Hook、格式及重复标记检查，Animal 的 DTO、四补丁、克隆行、刷新和清理继续归产品。空的 generated 鱼类生长表没有权威数据，不能作为生产 fallback 或公共内容承诺。

显示名称缓存曾连续漏过三层生命周期验证：只测试 service.Clear，没有证明桥接 fanout；直接测试桥接入口，没有证明实际 static callback 在 Camera 零需求时也能抵达；有 callback 清理，还没有保证 Hook 安装前不会保留缓存。最终历史方案以共同原生环境边界独立保护两类逻辑需求，只缓存成功非空值，物理 Hook 未就绪时可返回查询值但不留缓存。教训是测试生产调用链中的断点，避免用内部直调覆盖整条链的声明。

Animal 的 UI 研究发现约 80 ms 一次重复扫描和写入稳定行，并非原生要求持续刷新。历史优化边界是构建时排序及格式化、缓存克隆控件与反射访问器、激活前写入、至多一次下一帧保护，然后等显式 dirty 边界；`心情` 首帧覆盖与反复切换仍需可见验收。此处是当时批准的设计，是否已实现必须查当前产品。类似反射/UI 代码不构成 SharedNative 的第二个原生 owner。

Steam 云冲突导致的窗口、错误来源包、Animal 面板关闭与 ReturnHome 同帧竞争属于测试基础设施证据。已有有效产品动作和 owner 清理不能全部抹除；修改了名称缓存的生产分发链，则必须用对应真实入口补证。对 Fish/Animal 的修正无需重跑未改变的 ActionSpeed/OneAction 行为，也没有自动触发完整 Release 或长 GC 的要求。

来源：[Fish 准入](../../archive/reviews/code/2026/20260722-0006-fishbreedingassistant-fourth-product-admission-review.md)、[Animal 准入](../../archive/reviews/code/2026/20260722-0007-animalhusbandryprogress-fifth-product-admission-review.md)、[最小共享查询](../../archive/reviews/code/2026/20260722-0008-fish-animal-shared-boundary-review.md)、[三层缓存修正及 QA 范围](../../archive/reviews/code/2026/20260722-0009-five-product-update-continuation-audit.md)、[刷新成本与优化前提](../../archive/reviews/code/2026/20260722-0011-production-qa-seam-and-animal-refresh-audit.md)。

后续 [第六产品区间审查](../../archive/reviews/code/2026/20260723-0003-sixth-product-commit-range-audit.md)确认 Animal 已关闭健康态每 80 ms 刷新；这使上文从“尚待实施建议”更新为历史上已落地的优化，仍不代替当前源码核验。

6 月反复切换先显示心情的玩家反馈要求区分首帧与最终图；inactive clone 先填值、去掉会重置标题的 localization 再 activate，是历史观察方向。same-callback 文本零命中与延时截图仍不足以声称人眼无闪烁。旧记录把渲染放在 GameBridge 并要求 Goal+Update 等多份文档，后来的 ProductNative 迁移和当前单 Update 流程已改变这些归属要求。

来源：[编号手测原件](../../archive/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md)、[首帧手测门](../../archive/reviews/manual-qa/2026/20260611-0002-animalviewer-first-frame-flicker-manual-gate.md)。

FishBreedingAssistant 曾因构建引用 local-only generated lookup 而从公开构建临时排除；该处理保留原件，不把禁发布数据搬入公开源，也不靠空查表宣称完整功能。后续恢复需要合法可重建数据或原生查询，以当前产品构建 owner 为准。记录中当时用 PATH SDK 的做法已被当前 Get-DotNetExe 规则替代。来源：[公开构建排除](../../archive/updates/2026/20260608-0022-public-build-fishbreeding-exclusion.md)。

五产品 Update 保留了逐层测试漏项链：直接调用 Service.Clear 通过，遗漏 GameBridge fanout；改从 bridge 调用通过，又绕过 production static callback 的 Camera-demand gate。最终从真实静态边界验证独立 ItemDisplayName demand，并在 Hook 未 ready 时只返回成功结果而不缓存。首个实机又因后续合法 query/ReturnedToTitle 覆盖唯一诊断槽而误红，需保留因果状态，不能禁止后续查询来凑证据。既有 Fish/Animal UI、title 和 owner 清理不因新增 EnvironmentReset 门而作废。来源：[五产品后续共享生命周期闭合](../../archive/updates/2026/20260722-0004-five-product-baseline-correction.md)。
