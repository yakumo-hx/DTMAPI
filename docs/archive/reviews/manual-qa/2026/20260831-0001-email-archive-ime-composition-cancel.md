# Manual QA Review: Email Archive 中文输入法组合输入被取消

## Review Header

- Time: `2026-08-31T00:30:00+08:00`
- Review Status: `recorded`
- Source: 用户在已发布的公共 `Project:沙盒` 手工输入反馈。
- Scope: 根因诊断与后续验收边界；本轮不修改本地业务代码或线上 Wiki 资源。
- User constraints: 判断是沙盒限制还是信件搜索实现问题；不要把未经证实的浏览器现象归咎于 Wiki 沙盒。
- Related review/update/debug records:
  - [Email Archive online-pattern and redundancy review](../../code/2026/20260829-0001-email-archive-online-pattern-redundancy-review.md)
  - [信件档案 Wiki 沙盒发布与验收 Update](../../../updates/2026/20260830-0001-email-archive-wiki-unsaved-staging.md)
  - [信件档案中文输入法与数据驱动链接修复 Update](../../../updates/2026/20260831-0002-email-archive-ime-and-data-driven-links.md)
- Files/docs inspected:
  - `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.js`
  - `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/EmailArchive.js`
  - 当前公共 `Project:沙盒` 及其已验证的 Debug ResourceLoader 加载链
  - `docs/workflows/codex-feedback-to-goal.md`
  - `docs/reviews/README.md`
- Not inspected: 无法由自动化控制用户的 Windows 中文输入法候选窗口；用户的真实输入法现象作为手工 QA 事实保留。

## Issue Review

### Issue 1: 输入拼音时输入法候选窗口立即关闭

Original feedback:

- “沙盒界面我搜索不了，这个是沙盒界面的限制还是什么？”
- “具体而言就是我打拼音这个输入法会直接关掉。输入法闪一下就关闭了。”

Screenshot/log transcription:

- 无截图或日志。
- 用户描述的可见序列为：聚焦信件搜索框，开始输入拼音，中文输入法候选界面短暂出现后立即关闭，无法正常完成组合输入。

Review record:

- User-confirmed facts:
  - 问题发生在公共 `Project:沙盒` 中的信件档案搜索框。
  - 触发条件是使用拼音输入法进行中文组合输入，而不是普通已提交文本的筛选结果错误。
- Screenshot/log observations:
  - 无。
- Code/doc facts inspected:
  - 搜索框只在 `renderShell()` 初始化时创建；每次筛选不会替换搜索框 DOM 节点，因此“输入框被整个重建”不是准确根因。
  - 根节点的委托 `input` 监听器在每一次输入事件中立即读取 `event.target.value.trim()`，随后调用 `renderAndSyncSearch()`。
  - `renderAndSyncSearch()` 在同步重绘统计、分类、联系人列表和阅读器后，无条件执行 `search.value = state.query`。
  - 代码没有监听 `compositionstart`、`compositionupdate` 或 `compositionend`，也没有检查 `event.isComposing`。
  - 当前线上 Debug JS 已在此前发布验收中确认与本地交付 JS 一致，因此这条本地代码路径就是沙盒现行代码路径。
- Codex inference:
  - 中文输入法在候选尚未提交时会连续发出组合输入事件；现行监听器在组合期间同步重绘其他大块 DOM，并把正在组合的输入值重新写回同一个输入框。该输出到输入的回写会打断浏览器与系统输入法维护的组合会话，符合“候选框闪一下就关闭”的用户症状。
  - 问题属于 `EmailArchive.js` 的 IME 事件处理，不是 `Project:沙盒` 的权限、命名空间或 ResourceLoader 限制。普通粘贴或直接填入已经提交的中文仍可能正常，因为它们不依赖持续的系统组合会话。
- Ownership:
  - `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.js` 的搜索输入与渲染调度。
  - 沙盒当前由管理员放在 `零件:Debug.js` 的副本承载；线上修复最终需要管理员更新该零件，或改用以后确定的正式资源页。
- Root-cause hypotheses:
  - 高置信根因：组合输入期间无条件筛选渲染并执行 `search.value` 回写。
  - 次要放大因素：每一个拼音按键都会同步重绘分类、联系人列表与阅读器，增加组合会话被扰动的机会和输入延迟。
- Rejected/unproven hypotheses:
  - 已否定：Wiki 的 `Project:沙盒` 禁止中文输入。沙盒命名空间不会改变浏览器输入法协议，且缺陷可由信件脚本的明确事件路径解释。
  - 已否定：搜索框本身在每个字符后被 `innerHTML` 替换。当前 `render()` 不调用 `renderShell()`，搜索输入节点保持原节点。
  - 未证实：某个第三方浏览器扩展单独关闭输入法。现有代码已经足以解释症状，无需先假设扩展冲突。
- Required downstream updates:
  - 用户授权实施后，新建或更新一条 Email Archive 前端修复 Update；本 Manual QA Review 保持为实施前根因记录。
  - 不需要 DTMAPI Debug、Hook、API 或游戏烟测记录，因为问题不属于游戏 Runtime。
- Acceptance checks:
  - 在真实 Windows 中文拼音输入法下逐键输入“飞廉”“墨翟”“码头的商人”，候选窗口持续存在直到用户选字或确认。
  - 组合期间不覆盖搜索框值、不丢焦点、不重置光标；提交候选后只执行一次有效筛选。
  - 普通英文、直接中文粘贴、退格、清空、分类切换和搜索结果计数继续正常。
  - 搜索后精确 ID 路由、联系人展开与阅读器选择不回归；桌面和移动端搜索框都复验。
  - 线上控制台无新增警告或错误。
- Blocker conditions:
  - 自动化不能代替用户真实系统输入法候选窗口验收；实现可以先通过事件序列单元测试和 Chrome 普通输入检查，但最终关闭本问题需要一次真实中文输入法手工确认。

## Cross-Issue Summary

- Confirmed user facts: 拼音组合输入在信件沙盒搜索框中立即中断。
- Screenshot/log facts: 无截图或日志。
- Code-path findings: 现行 JS 不识别组合输入，在每个 `input` 事件中重绘并回写同一个输入框的 `value`。
- Risks: 只加防抖但仍在组合期间回写 `value` 不能可靠修复；大范围重构筛选或 DOM 没有必要。
- Suggested implementation scope:
  - 在组合期间跳过筛选渲染与 `value` 回写，在 `compositionend` 后应用最终查询。
  - 删除不必要的活动搜索框值回写，或仅在输入框未聚焦且值确实不同时同步。
  - 保持现有 DOM、CSS、Lua、数据结构、分类和 ID 路由不变。
- Items that should not be carried forward: “沙盒不支持输入法”与“必须重做整个搜索组件”均不成立。

## Implementation Record Decision

- Create/update an implementation update record: 本轮否；用户只要求诊断。获得修复授权后创建一条小范围前端修复 Update。
- Additional debug/API/hook/smoke records required: 否；该问题只属于 Wiki 前端输入事件。
- Suggested task titles: `Email Archive IME-safe search input`。
- Completion standard: 事件级检查通过，并由用户在真实中文拼音输入法中确认候选框不会再闪退。

## Follow-up Observation: 2026-09-03

### 问题 1：搜索输入仍伴随反复渲染

原始反馈：

- “所以目前两个任务，1修正搜索输入反复渲染问题”。
- 图片转写：无截图。

审查记录：

- 用户确认事实：上一轮组合输入保护发布后，搜索输入的反复渲染仍需继续修正。
- 代码/文档事实：现行 `input` 处理虽然在组合输入期间跳过提交，但每次接受普通输入后仍同步调用完整 `render()`；该函数重复刷新统计、分类、移动栏、联系人列表和阅读器。
- Codex 推断：组合事件保护解决了旧的直接回写路径，但同步全量刷新仍会制造不必要的布局和输入法扰动；本轮应让输入框保持原节点，并将搜索更新防抖后限制在结果相关区域。
- 反证/未证实：尚不能声称真实系统输入法问题已经关闭；最终仍需用户在发布后的浏览器中完成拼音候选窗口验收。
- 归属：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.js`。
- 验收点：组合期间零刷新；组合结束只提交一次；连续普通输入经防抖只刷新结果区；统计、分类和搜索输入节点不重建；现有筛选、路由和移动端交互不回归。
- blocker 判定：本地事件测试可以完成实现门槛，线上真实输入法关闭门槛仍需管理员更新资源后由用户确认。

### 问题 2：条件表重新生成

原始反馈：

- “2.修正条件表。”
- “把程序条件尽可能直接变为可读条件。可以其中部分这样操作，另一部分手工补充？”
- 图片转写：无截图。

审查记录：

- 用户确认事实：旧 `Data:Email/conditions.json` 不应继续影响信件档案；当前最新解包基线是唯一游戏触发事实来源，无法自动确定的内容允许人工补充。
- 代码/文档事实：现行 Wiki 运行时只加载 `Data:Email/tbemail.json` 与 `Module:Email/EmailArchiveManual`。旧追踪脚本仍读取旧 Wiki 条件、绑定旧构建目录并按 ID 推断，不能作为本轮生成基础。
- Codex 推断：适合采用离线混合生成——从最新基线提取结构化监听器、附加条件和发信动作，转换为玩家可读文本，再应用少量人工覆盖；未知节点留空并只进入本地维护报告。
- 反证/未证实：仅发现信件 ID 的文本引用不能证明常规触发条件；版本补丁补发、动态模板和当前无发送引用的记录不得自动解释为普通获取条件。
- 归属：本地条件生成工具、人工覆盖数据和最终 `EmailArchiveManual.lua`；不增加 Wiki 运行时模块。
- 验收点：工具只读取明确指定或自动发现的最新 DTMAPI 解包基线；不读取旧 `conditions.json`、旧 Wiki 页面或 DLK 游戏导出；输出记录基线身份、自动/人工数量及未解析项；阵营触发严格匹配当前图数据；生成的 Manual Lua 保留联系人和分类，未知条件为空。
- blocker 判定：不因未解析信件阻断页面或构建；未解析项只阻止该封信自动获得条件文本。

## Implementation Link: 2026-09-03

- 两项实现与本地 focused validation 见 [20260903-0001：信件档案搜索刷新与条件再生成](../../../../updates/2026/20260903-0001-email-archive-search-and-condition-regeneration.md)。
- 问题 1 继续保持 open，等待管理员更新 `零件:Debug.js` 后用真实 Windows 中文输入法完成最终手工验收。
