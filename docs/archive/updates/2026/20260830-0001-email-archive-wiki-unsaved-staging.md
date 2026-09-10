# 20260830-0001：信件档案 Wiki 沙盒发布与验收

## Metadata

- Update ID: `20260830-0001`
- Date: `2026-08-30`
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit, runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`
- Source: 用户要求使用已登录 Chrome 核对管理员是否尚未更新信件 JS/CSS，确认所有拟创建或修改页面均有本地源或精确 Wiki 回退点，并将可编辑页面预填后停在由用户手动提交的状态；不得破坏当前线上信件界面。

## Scope

- 只读查询六个拟变更 Wiki 页面的存在状态、最新修订号、时间与 SHA1。
- 重跑 205 封信的本地构建、JS 语法检查与扁平路由单元检查。
- 将业务 Lua、人工数据 Lua 与公共沙盒页面预填到 Chrome 编辑器，不点击保存或发布。
- 记录当前账号无权编辑 `零件` 命名空间；JS、CSS 与 Entrance 登记必须由管理员处理。
- 新增只匹配 `Project:沙盒` 的管理员合并片段，避免沙盒第一版资源进入当前线上 `信件` 页面。
- 复核管理员后续把交付 JS/CSS 临时挂到既有 Debug 零件的实现，并将该路径确认为当前沙盒测试方案；Entrance 片段降为备选，不再是测试前置条件。
- 在不发布 `Project:沙盒` 的前提下生成真实 Wiki 预览，区分 Lua/数据渲染成功与 Debug 前端尚未挂载两个层次。
- 用户发布公共沙盒后，在真实 `action=view` 页面验证 Debug ResourceLoader、页面挂载、搜索、分类、阅读器、ID 深链接、重复标题和浏览器历史导航。
- 定位头像全部退回首字占位符的原因，并在用户明确授权后以小编辑更新两份 Lua：人工表修正联系人归并，业务模块把站内角色肖像转换为可进入 JSON 的实际图片元素。
- 使用当前 DTMAPI 反编译与 AssetRipper 数据核对自动化工作台触发链，并逐封核对飞廉、萨科相关信件，避免按含混发件人文字粗暴归并。
- 在公共沙盒复验 205 封信、26 个联系人、20 个实际角色头像、精确 ID 路由、搜索和控制台；正式 `信件` 页、`Entrance.js`、游戏 Runtime、Workshop、存档及 Doloc Town 本地部署均不修改。

## Changed Files

- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-30-wiki-precommit/Entrance_ProjectSandbox_registration.js`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-30-wiki-precommit/Project_沙盒.wikitext`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-30-wiki-precommit/TARGETS.md`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchive.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/wiki_source/EmailArchiveManual.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery/EmailArchive.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery/EmailArchiveManual.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/EmailArchive.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/EmailArchiveManual.lua`
- `E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/email_archive_visual_acceptance.html`
- `.codex/wiki-maintenance/2026-08-31-wiki-email-archive-batch-036.json`
- `.codex/wiki-maintenance/revisions/014-wiki0828-2026-08-31.csv`
- `.codex/wiki-maintenance/revisions/INDEX.md`
- `.codex/wiki-maintenance/README.md`
- `.codex/wiki-maintenance/state.json`
- `docs/updates/2026/20260830-0001-email-archive-wiki-unsaved-staging.md`
- `docs/updates/INDEX-2026-08.md`

## Validation

- 初始 Chrome/API 审计确认正式 `信件` 页仍调用 `Module:Email/EmailTable`，公共沙盒使用管理员临时挂到 `零件:Debug.js/.css` 的交付资源；`Entrance.js` 保持 `r20585`，因此沙盒测试与正式界面隔离。
- `Project:沙盒` 为 `r26256`（父修订 `r26178`，SHA1 `88daf549f5db18137980b9429ad285b3a2cc0e6e`），真实 `action=view` 根节点获得 `data-dlk-email-mounted="1"`。管理员提供的 Debug JS/CSS 去掉页头与尾部换行后与本地交付一致，ResourceLoader 输出有效且浏览器控制台无警告或错误。
- 本地 205 封线上数据快照暴露 6 个伪联系人：`蘑菇爱好者澳柯玛`、`无署名`、`急急急急急`、`在吃下午茶的泽尼瑟`、`满心期待飞廉`、`飞廉和圣特`。人工表按别名或精确信件 ID 归并后联系人从 32 降为 26，伪联系人剩余 0。
- 当前反编译构建 `24966367_public_958EAF` 的 `player_behavior.asset` 中，`automation_guide_pre`、`automation_guide`、`automation_guide_ad` 由同一“建造自动化工作台”触发链发出；结合主信件发件人及内容，把匿名前置信和催促信按 ID 归入墨翟。`ruinedcity_main` 只做 ID 级补充，不把“无署名”等歧义文字注册为全局别名。
- 飞廉与萨科逐封复核：`villain_*` 归飞廉；`sacco_*` 通常归萨科，但 `sacco_favorability7` 的发件人是“飞廉担心”且正文为飞廉第一人称，因此保留在飞廉；`sacco_favorability8` 仍归萨科。
- 头像根因是角色肖像模板返回的 `[[File:...]]` 源码进入 JSON 后不再解析。业务 Lua 现提取文件名、通过站内 `filepath` 取得资源 URL，再输出受控 `<img>`；没有硬编码头像 URL或新数据源。
- 用户授权后，`模块:Email/EmailArchiveManual` 以小编辑 `r26285` 发布，父修订 `r26234`，SHA1 `343499e559ca47270d1ca29e4df4eddef9d1740c`，摘要精确为“修正分组错误”；`模块:Email/EmailArchive` 以小编辑 `r26286` 发布，父修订 `r26233`，SHA1 `9e036bc2515dccc25092f5a2f9da1603b969e421`，摘要精确为“修正头像读取”。两份线上源码均与本地源一致。
- `python tools/build_email_archive_rebuild.py` 通过并输出 205 封信、26 个联系人、88 条非空条件；`node --check wiki_source/EmailArchive.js` 与 `node tools/test_email_archive_flat.js` 通过。交付目录仍恰好六个文件。
- 发布后公共沙盒读取 205 封信、26 个联系人；20 个有角色肖像的联系人全部生成同域静态资源 `<img>` 且自然尺寸非零，6 个组织或系统联系人按设计使用文字回退。未知或离屏惰性加载项逐一展开后为 20/20。
- 精确路由复验通过：`automation_guide_pre`、`automation_guide_ad`、`ruinedcity_main` 归墨翟，`mushroom_festival` 归澳柯玛，`villain_favorability10` 与 `sacco_favorability7` 归飞廉，`sacco_favorability8` 归萨科。搜索“飞廉和圣特”只命中飞廉下的对应 ID，原始发件人仍可搜索。
- `#first_wharf` 刷新、同名信 ID 区分、前进/后退、搜索、分类、清空筛选及附件链接在本轮前已通过；本轮没有修改 DOM、JS 或 CSS。独立本地 1440×900 与 390×844 视觉验收仍有效，外部 Chrome 未重复覆盖移动端视口。
- 正式 `信件` 页面、`零件:Entrance.js` 和管理员 Debug JS/CSS 均未修改；当前线上正式信件界面不受本轮 Lua 修正影响。

## Evidence

- 管理员目标、旧修订链接与提交顺序：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-30-wiki-precommit/TARGETS.md`。
- 六文件 Entrance 交付目录：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/dist/wiki_delivery_entrance/`。
- 沙盒专用 Entrance 片段：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-30-wiki-precommit/Entrance_ProjectSandbox_registration.js`。
- 公共沙盒精确待提交源：`E:/Python_project/DLK/src/wiki/EmailArchiveAssistant/reports/2026-08-30-wiki-precommit/Project_沙盒.wikitext`。
- 已发布沙盒修订：[Project:沙盒 r26256](https://doloctown.huijiwiki.com/w/index.php?title=Project%3A%E6%B2%99%E7%9B%92&oldid=26256)。
- 信件档案发布、分组证据、头像验收与精确修订元数据：`.codex/wiki-maintenance/2026-08-31-wiki-email-archive-batch-036.json`。

## Rollback Notes

- `模块:Email/EmailArchiveManual` 的本轮回退点为 `r26234`；`模块:Email/EmailArchive` 的本轮回退点为 `r26233`。两份当前线上源码均可从本地交付重建。
- `零件:Entrance.js` 只允许管理员追加沙盒片段；不得用片段文件或其他单文件覆盖其现有钓鱼计算器入口。
- 当前 Debug 沙盒测试不需要修改 `零件:Entrance.js`；原追加片段仅保留为备选交付，除非后续明确改回独立加载方案。
- `Project:沙盒` 已发布；需要整体撤回时恢复父修订 `r26178`。Debug JS/CSS 继续使用清单中的各自父修订回退。

## Follow-Up

- 公共沙盒第一版的分组、头像、搜索与 ID 路由已通过验收；保留它作为所有人可访问的演示页面。
- 正式 `信件` 页切换以及面向所有访客的 JS/CSS 加载方式属于下一项独立发布决策。本 Update 不把 `信件` 登记进 Entrance，也不替换当前正式页面。
- 若后续修改 DOM、JS 或 CSS，再补做线上 390×844 移动端视觉验收；本轮只修改 Lua 数据组织和头像输出，不以此阻断已完成的缺陷验收。
