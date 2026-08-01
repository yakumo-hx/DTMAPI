# 新增养殖动物本地底稿

本文件夹是飞书网页版《新增养殖动物》的本地底稿快照。

- 来源页面：https://kcndb2wpn7uc.feishu.cn/wiki/FnazwrRSoiTy7IkHfTBchuxSnwh
- 来源标题：新增养殖动物
- 快照日期：2026-07-01
- 处理原则：先原封不动保存当前网页版内容，后续重排在本地草稿中进行，再决定是否贴回飞书。

## 文件说明

- `source-feishu-current.html`：从当前飞书页复制得到的富文本 HTML，尽量保留代码块、黄底标注等网页版格式。
- `source-feishu-current.txt`：从当前飞书页复制得到的纯文本，适合检索和对照。
- `draft.md`：当前阶段的可编辑底稿，暂时与纯文本快照一致，后续可在这里拆分/重排。
- `source-visible.png`：复制时页面可见区域截图，便于确认当时的视觉状态。

## 下一步草稿方向

1. 主教程区改成官方示例那种“示例值 + 注释说明”。
2. 只在代码块里标黄需要替换的值，正文不再大段标黄。
3. 哈奇实包放到参考示例，不作为主教程长 JSON。
4. 动画帧数量、命名和图片要求集中放到第三节。

## 自动生成器

- `generate_hatch_feishu_doc.py`：从当前本地哈奇实包生成飞书 Docx OpenAPI 用的本地 payload、5 张动作帧合成图和 Markdown 预览。
- 默认哈奇输入源：`C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_HatchAssets`。
- 默认输出目录：`generated/`。

## 文档生成约定

- 教程正文只展示新增养殖动物完整链路需要使用的 JSON。新包不创建 `Content/DTMAPI/dtmapi-package.json`；`manifest.json` 足以提供 DTMAPI 内容身份。原始快照中的旧 marker 仅是历史 build 元数据，永远不是安装/卸载所有权回执。
- 正文不使用“核心配置：是/否”这种标注。展示出来的 JSON 都是模板链路的一环。
- 不写“第一轮”等测试兜底措辞；模板默认值直接写成“哈奇模板使用……”或“本模板默认……”。验证清单用于交付确认，不把责任推给读者。
- `schinese` 和 `text` 的展示文本使用简中；代码 ID 保持 `hatch`、`sack_hatch`、`hatch_produce` 等真实键值。
- 表格优先做宽矮结构。能横向合并信息时，不做窄列长行。
- 图片标题统一放在图片上方，并以 `·` 开头，例如 `· 哈奇 idle 动作帧图`。
- 飞书粘贴代码块使用飞书复制结构：`class="language-JSON"`、`data-lark-language="JSON"`、`data-wrap="false"` 和内联黄底样式。

本地 dry-run：

```powershell
python author-docs/content-packs/new-farm-animal-draft/generate_hatch_feishu_doc.py
```

不用 token 的飞书粘贴流程：

1. 运行上面的 dry-run。
2. 打开 `generated/hatch-feishu-rich-copy.html`。
3. 点击页面顶部的“复制富文本”。
4. 切到飞书新建文档正文位置，按 `Ctrl+V` 粘贴。

这个 HTML 会内嵌 5 张动作帧合成图，并保留表格、代码块、JSON 语言提示和浅黄色替换字段标记。若浏览器拦截剪贴板写入，页面会自动选中正文，此时按 `Ctrl+C` 再粘贴到飞书即可。

代码块复制兼容说明：

- 新版 HTML 按飞书自身复制出来的结构生成代码块：`class="language-JSON"`、`data-lark-language="JSON"`、`data-wrap="false"`。
- 黄底标记使用飞书复制内容中常见的内联 `background-color:rgba(255,246,122,0.8)`，不再使用普通网页的 `<mark>` 标签。
- 如果粘贴后仍显示为 `Plain Text` 或代码变成单行，先重新运行生成器并刷新/重新打开 `hatch-feishu-rich-copy.html`，不要复用旧页面。

飞书上传需要显式提供 `FEISHU_ACCESS_TOKEN` 和 `FEISHU_DOC_ID`，否则脚本只生成本地文件，不会改动飞书页面。
