DTMAPI 玩家存档与闪退日志一键收集器

使用方法：

1. 完全退出 Doloc Town，不要在游戏运行时收集。
2. 解压本 ZIP，双击 1_collect_save_and_crash_logs.bat。
3. 等待窗口显示 [OK]。
4. 桌面会生成 DTMAPI-player-support-日期时间-编号.zip，把这个 ZIP 原样发给维护者。

脚本会自动寻找，无需玩家手动打开隐藏的 AppData：

- 完整 SAVE 文件夹，包括 current、.prev0/.prev1...、.bak 和 JSON；
- Player.log 与 Player-prev.log；
- DTMAPI 当前和历史日志、状态（含 DebugConsole 最近操作记录）与已有报告；
- BepInEx LogOutput.log；
- Unity Crashes、crash.dmp、error.log；
- 与 DolocTown/Unity 相关的本地 Windows dump 和最近七天 Application 崩溃事件。

安全边界：

- 只读取、复制源文件，不会解析、解密、修复、改名、删除或覆盖玩家存档。
- 若游戏仍在运行，脚本会拒绝收集，避免得到复制到一半的存档。
- 若找不到 SAVE 或复制期间源文件变化，脚本会生成说明但返回失败；请按窗口提示处理后重试。
- ZIP 含有完整玩家存档和本机路径，只应发送给可信的项目维护者。
