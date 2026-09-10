# 作者教程与交付投影

本页提炼 2026 年 6–7 月动物教程与飞书交付记录。现行字段说明由[内容包教程](../../../author-docs/content-packs/custom-animal-json-png-wav.md)维护；数值计算要求由[JSON 派生值验证](../../../author-docs/content-packs/json-derived-value-validation.md)维护。历史模板不代表当前 Catalog 身份或发布版本。

## 正文、草稿和生成件

[新增养殖动物草稿](../../archive/authoring/2026/new-farm-animal-draft/README.md)及其 `source-feishu-current.txt` 是当时的作者材料。`generated/hatch-feishu-doc.preview.md` 是由哈奇实包生成的交付投影；剪贴板 `02-system-clipboard-after-copy-button.txt` 与它的全部正文相同，仅末尾多一个 CRLF。两者不能算两套独立规格。窄表、宽表的系统剪贴板都只留下“暂时无法在飞书文档外展示此内容”，DOM 快照只能证明当时编辑器状态，不能证明表格内容已正确交付。关联 HTML、帧图和截图应随原稿保留或在确认可重建之后清理，不能只搬 Markdown。

草稿示例仍包含 `movementMultiplier`、`metabolismMultiplier` 等旧配置；后来的教程已经区分桥实际消费的字段。初始模板的帧计数、哈奇实际 PNG 数量和原生模板帧数量也不是同一事实。查当前能力时读现行教程及对应桥实现；复现旧交付时才使用完整草稿目录。旧的包名称、作者、路径和版本是历史记录，不批量改成当前值。

## 测试适用范围

新增物种首次接入需要证明 ID 引用、购买和释放、幼体/成年、动画和声音、产物、原版物种不受影响；持久化行为改变时再证明原生保存与读回。这个首次接入清单不应变成每次改简介、PNG 或 WAV 都必须保存、退出、重进的要求。普通修改的前提、场景和停止条件仍由[产品验证工作流](../../workflows/product-change-validation.md)统一选择。

生产配方不能以 JSON 可解析代替运行正确：原生转换可能把正的作者输入舍入为零，而失败直到材料已被消耗后才出现。保留派生值的边界检查和发生变化的生产行为验证；已确认的公式不需要为无关文字或图片变更重复研究。

## 展示输出不是另一套作者源

7 月 Hatch 教程先做 OpenAPI blocks，用户后选本地 rich-copy；网页预览正常不证明飞书粘贴保真。真实剪贴板会折叠逐行 span、去掉 mark；换行和替换标记实际回读后才获玩家确认，表宽依赖 sheet colgroup 的后继仅静态验证。blocks/Markdown/HTML/图集/报告应从一份输入生成，不人工同步五份源；历史默认读 LocalLow 实包造成展示调整连带改游戏包，应由现行仓库 source owner 替代。dtmapi-package.json 是当时工具标记，不是动物/audio 加载配置，已从教程正文移除。

来源：[初始生成器](../../archive/updates/2026/20260701-0010-hatch-feishu-doc-generator.md)、[用户选复制](../../archive/updates/2026/20260701-0011-hatch-feishu-rich-copy-html.md)、[真实代码块粘贴](../../archive/updates/2026/20260701-0012-feishu-rich-copy-codeblock-compat.md)、[教程字段修正](../../archive/updates/2026/20260701-0013-hatch-feishu-tutorial-polish.md)、[表宽对照](../../archive/updates/2026/20260701-0014-feishu-rich-copy-table-widths.md)。外部编辑器以后改变时需重新核实，旧 CSS 不是当前兼容保证。

## 正输入仍可能得到零时长

[城市分解机](../../archive/reviews/manual-qa/2026/20260713-0002-garbage-shredder-last-run-log-review.md) 不能只查原表：启用 Workshop 配方是真实输入。cost_time=1 在农场可工作，城市倍率 0.0835 经 RoundToInt 成零，原生先取材料再 Work(0)，失败后内部槽占位，保存才持久化。当时该 group 最小整数为 6，不是永久规则。只清授权副本的卡物修已保存状态，不修触发配方，末尾用户关闭了 DTMAPI/ActionSpeed 归因。

[大茶壶](../../archive/reviews/manual-qa/2026/20260715-0003-flourishing-flora-large-teapot-zero-time.md) 另有 midpoint-to-even：RoundToInt(0.5×1)=0。最终用户确认使用已修正式游戏，额外 A/B 停止；修复机制未审，不猜 clamp/rounding，也不照搬分解机持久 buffer 后果。验证最终单位、倍率、除法与舍入，而不是仅要求原字段正数。

## 官方资料的抓取保真与未完成教程审查

7 月重抓以 Wiki token、block ID、data-line-num 和真实有序 sheet 单元格保真；首个“55 页完成”仍漏虚拟化代码前半，合并后才完整。hash 清单只证明保存物未变，不证明内容完整。官方活页和 dated snapshot 各有用途，抓取原字节与作者工作稿分开。

[教程实机审查 owner](../../updates/2026/20260715-0003-official-tutorial-real-environment-audit.md) 仍持有 93 条 native-table expectation 与代表玩家行为未运行；合包启动/静态 parity 不证明全部教程实机成功，35 组重复 ID 也不等于 35 个教程错误。generated JSON/TSV 仍是该活跃审查输入，不能当过期导出删掉。三项 DLK 语法问题已被上游修复，不从旧清单继续追责。

[Fumo](../../archive/reviews/code/2026/20260714-0002-touhou-fumo-official-json-audit.md) 的 item_tbtiem.json 可解析却不被 native 表 basename 请求；Content 子目录名不是表身份。手册 source:[processing] 不生成配方，商店直售无需强造制作链。官方 prose/示例也可能不一致、ID 表可为部分列表；空 growth_months 是不限月份，工具额外禁止为空属于回归。工具最终导入/保留/导出修复统一见[SDK 与作者工具知识](../runtime-delivery/sdk-and-author-tools.md)，不在此复制第二份版本清单。

来源：[抓取边界](../../archive/reviews/code/2026/20260715-0001-official-feishu-crawl-boundary.md)、[保真修正](../../archive/updates/2026/20260715-0001-official-feishu-docs-recrawl.md)、[资料与工具回归](../../archive/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md)。

## Wiki 是独立交付区

8 月 V2.3 本地 30 页/54 target 交付仅准管理员交接与服务器未保存预览；社区、人类接受、Bot mapper、桌面/移动预览、Scribunto/search、保存 revision 都归 wiki 自有 runbook。模型审查不替代 human-community acceptance。用户载荷最终缩为组件、Bot 工具、页面示例、说明，审计证据留仓库；.wiki/.txt 双份是明确给使用者打开的便利，不能仅按重复扩展名删除。生产 mapper 要由获授权 Bot 同 build 生成，本地 reverse fixture 不可发行。

8 月 23 日授权的七次 revision 只覆盖鱼解锁、基因/房间与价格子集，邮件大修和平台改动被排除；三十页英文翻译只改 raw Wikitext 硬编码文本，动态输出归 Module/Data。普通价格已经修正后再广扫会制造新漂移，真实 ID anchor、原生 store unlock 和概念总表应由单一数据生成。具体 native 事实另见[内容研究](native-content-facts.md)。本页只登记边界，不触碰并行 wiki 修改或发起线上操作。

来源：[V2.3 交付审查](../../archive/reviews/code/2026/20260821-0001-wiki-v2-final-delivery-audit.md)、[三十页收口](../../archive/updates/2026/20260821-0001-wiki-v2-thirty-page-source-translation-audit.md)、[授权子集](../../archive/updates/2026/20260823-0004-huijiwiki-gene-room-and-deep-fact-corrections.md)。
