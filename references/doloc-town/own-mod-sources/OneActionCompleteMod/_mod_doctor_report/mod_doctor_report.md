# 多洛可小镇 Mod 体检报告

- 生成时间：2026-05-19T22:45:57
- 工具版本：0.1.0
- 模组目录：`E:\Python_project\DLK\src\mods\OneActionCompleteMod`
- 识别类型：空包/未识别
- 基准来源：game
- 配置 JSON：0
- PNG：0
- DLL：0
- 问题统计：{'error': 2, 'warning': 2}

## 问题清单

### DOCTOR-001 [error] 根目录缺少 info.json

- 问题在：info.json
- 问题原因是：游戏只会把包含 info.json 的目录识别为本地/创意工坊模组。
- 证据：`info.json`
- 建议：在模组根目录创建 info.json。
- 置信度：high

### DOCTOR-002 [error] 根目录缺少 Content 文件夹

- 问题在：Content
- 问题原因是：官方 ModInfo.UpdateCache 只扫描根目录下的 Content。
- 证据：`Content`
- 建议：创建 Content 文件夹并放入 JSON/PNG/DLL 内容。
- 置信度：high

### DOCTOR-003 [warning] 根目录缺少 icon.png

- 问题在：icon.png
- 问题原因是：游戏内模组列表和创意工坊展示通常需要图标。
- 证据：``
- 建议：添加根目录 icon.png。
- 置信度：high

### DOCTOR-004 [warning] 根目录缺少 preview.png

- 问题在：preview.png
- 问题原因是：创意工坊展示通常需要预览图。
- 证据：``
- 建议：添加根目录 preview.png。
- 置信度：high

## 说明

- 本工具只做静态检查，不启动游戏、不上传创意工坊、不修改模组文件。
- error 表示很可能导致加载失败、功能失效或运行时崩溃；warning 表示高风险或不推荐写法；info 表示环境或兼容性提示。
- 若报告提示“可能复用原版资源”，请结合实际设计人工确认。
