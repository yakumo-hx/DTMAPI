DTMAPI-MP（DTMAPI-多平台）

==================== 简体中文 ====================

DTMAPI-MP是 DTMAPI 多平台安装包，同时支持Steam Deck、普通 Linux x64、Windows x64，并为 macOS CrossOver 提供待实验入口。

功能 Mod 仍需在游戏官方 Mod 页面订阅并启用。正常安装并成功加载后，标题页左上角的小图标可以打开 DTMAPI 配置菜单。

【如何找到安装包文件夹】

Steam 库 -> 多洛可小镇 -> 右侧齿轮（设置）-> 属性 -> 已安装文件 -> 浏览。打开后的游戏目录通常类似：
<Steam 库>/steamapps/common/Doloc Town

返回上级的 <Steam 库>/steamapps，然后进入：
workshop/content/2285550/3792681186

【Windows x64】

双击 1_install_dtmapi.bat 安装。
双击 2_uninstall_dtmapi.bat 卸载。
双击 3_check_dtmapi_status.bat 检查状态。
双击 4_collect_dtmapi_logs.bat 收集日志。
原生 Windows 不需要填写 Linux/Proton 启动选项。

【Steam Deck / Linux x64】

1. Steam Deck 进入桌面模式。
2. 用 Dolphin 打开本项目的完整 Workshop 安装包文件夹。
3. 按 Shift+F4 打开终端；也可以点击“工具 -> 打开终端”。
4. 复制下面的命令并按回车，等待安装完成：
   bash ./1_install_dtmapi.sh

**※** 安装后必须在 Steam -> 多洛可小镇 -> 设置 -> 属性 -> 通用 -> 启动选项中完整填写： WINEDLLOVERRIDES="winhttp=n,b" %command%

没有修改启动选项，DTMAPI 不会加载。

其他命令：
检查：bash ./3_check_dtmapi_status.sh
卸载：bash ./2_uninstall_dtmapi.sh
收集日志：bash ./4_collect_dtmapi_logs.sh

双击 .sh 通常只会打开文本，请使用终端命令。普通 Linux x64 的操作与 Steam Deck 相同，游戏本体仍通过 Steam Proton 运行。

【macOS + CrossOver（实验入口）】

1. 完全退出游戏。
2. 打开 CrossOver。
3. 在左侧选择已经安装了 Windows Steam 和游戏的 Bottle，名称通常可能是 Steam。不要新建空 Bottle。
4. 点击右侧的 Run Command（运行命令）。
5. 点击 Browse（浏览）。
6. 找到本项目的完整 Workshop 安装包文件夹。
7. 选择 DTMAPI-MultiPlatform-Installer.exe。
8. 按安装器提示完成安装。

随后设置 winhttp 覆盖：

1. 回到 CrossOver，仍然选择同一个 Steam Bottle。
2. 打开 Wine Configuration；不同 CrossOver 版本中，它可能位于 Bottle Actions 或 Control Panels 下。
3. 切换到 Libraries 选项卡。
4. 在新增函数库覆盖的位置输入 winhttp。
5. 点击 Add。
6. 选中列表中的 winhttp，点击 Edit。
7. 将加载方式设为 Native, then Builtin。
8. 点击 Apply，再点击 OK。
9. 从这个 Bottle 中正常启动 Steam 和游戏。

【共同说明】

安装器未代码签名，请仅从多洛可小镇的创意工坊订阅。遇到问题请附检查的完整输出、收集到的日志、系统版本，以及 Proton 或 CrossOver 版本。

==================== 繁體中文 ====================

DTMAPI-MP 是 DTMAPI 多平台安裝包，同時支援 Steam Deck、一般 Linux x64、Windows x64，並為 macOS CrossOver 提供尚待測試的實驗入口。

功能 Mod 仍需在遊戲官方 Mod 頁面訂閱並啟用。正常安裝並成功載入後，標題頁左上角的小圖示可以開啟 DTMAPI 設定選單。

【如何找到安裝包資料夾】

Steam 收藏庫 -> 多洛可小鎮 -> 右側齒輪（設定）-> 內容 -> 已安裝檔案 -> 瀏覽。開啟後的遊戲目錄通常類似：
<Steam 收藏庫>/steamapps/common/Doloc Town

返回上層的 <Steam 收藏庫>/steamapps，然後進入：
workshop/content/2285550/3792681186

【Windows x64】

雙擊 1_install_dtmapi.bat 安裝。
雙擊 2_uninstall_dtmapi.bat 解除安裝。
雙擊 3_check_dtmapi_status.bat 檢查狀態。
雙擊 4_collect_dtmapi_logs.bat 收集日誌。
原生 Windows 不需要填寫 Linux/Proton 啟動選項。

【Steam Deck / Linux x64】

1. Steam Deck 進入桌面模式。
2. 使用 Dolphin 開啟本項目的完整 Workshop 安裝包資料夾。
3. 按 Shift+F4 開啟終端機；也可以點選「工具 -> 開啟終端機」。
4. 複製下列命令並按 Enter，等待安裝完成：
   bash ./1_install_dtmapi.sh

**※** 安裝後必須在 Steam -> 多洛可小鎮 -> 設定 -> 內容 -> 一般 -> 啟動選項中完整填寫： WINEDLLOVERRIDES="winhttp=n,b" %command%

若未修改啟動選項，DTMAPI 不會載入。

其他命令：
檢查：bash ./3_check_dtmapi_status.sh
解除安裝：bash ./2_uninstall_dtmapi.sh
收集日誌：bash ./4_collect_dtmapi_logs.sh

雙擊 .sh 通常只會開啟文字內容，請使用終端命令。一般 Linux x64 的操作與 Steam Deck 相同，遊戲本體仍透過 Steam Proton 執行。

【macOS + CrossOver（實驗入口）】

1. 完全結束遊戲。
2. 開啟 CrossOver。
3. 在左側選擇已經安裝 Windows Steam 與遊戲的 Bottle，名稱通常可能是 Steam。請勿建立空白 Bottle。
4. 點選右側的 Run Command（執行命令）。
5. 點選 Browse（瀏覽）。
6. 找到本項目的完整 Workshop 安裝包資料夾。
7. 選擇 DTMAPI-MultiPlatform-Installer.exe。
8. 依照安裝程式提示完成安裝。

接著設定 winhttp 覆寫：

1. 回到 CrossOver，仍然選擇同一個 Steam Bottle。
2. 開啟 Wine Configuration；在不同 CrossOver 版本中，它可能位於 Bottle Actions 或 Control Panels 下。
3. 切換到 Libraries 分頁。
4. 在新增函式庫覆寫的位置輸入 winhttp。
5. 點選 Add。
6. 選取清單中的 winhttp，點選 Edit。
7. 將載入方式設為 Native, then Builtin。
8. 點選 Apply，再點選 OK。
9. 從這個 Bottle 中正常啟動 Steam 與遊戲。

【共同說明】

安裝程式未經程式碼簽署，請僅透過《多洛可小鎮》的 Steam 創意工坊訂閱。遇到問題時，請附上檢查的完整輸出、收集到的日誌、系統版本，以及 Proton 或 CrossOver 版本。

==================== English ====================

DTMAPI-MP is the DTMAPI multi-platform installer. It supports Steam Deck, standard Linux x64, and Windows x64, and provides an experimental entry point for macOS CrossOver that still needs testing.

Functional mods must still be subscribed to and enabled on their official in-game Mod pages. After DTMAPI is installed and loaded successfully, use the small icon in the top-left corner of the title screen to open the DTMAPI configuration menu.

【Finding the package folder】

In Steam, open Library -> Doloc Town -> gear icon (Manage) -> Properties -> Installed Files -> Browse. The game folder normally looks like:
<Steam library>/steamapps/common/Doloc Town

Go back to <Steam library>/steamapps, then open:
workshop/content/2285550/3792681186

【Windows x64】

Double-click 1_install_dtmapi.bat to install.
Double-click 2_uninstall_dtmapi.bat to uninstall.
Double-click 3_check_dtmapi_status.bat to check status.
Double-click 4_collect_dtmapi_logs.bat to collect logs.
Native Windows does not need the Linux/Proton launch option.

【Steam Deck / Linux x64】

1. On Steam Deck, switch to Desktop Mode.
2. Open this item's complete Workshop package folder in Dolphin.
3. Press Shift+F4 to open a terminal; you can also use Tools -> Open Terminal.
4. Paste the following command, press Enter, and wait for installation to finish:
   bash ./1_install_dtmapi.sh

**※** After installation, open Steam -> Doloc Town -> Settings -> Properties -> General -> Launch Options and enter this exact line: WINEDLLOVERRIDES="winhttp=n,b" %command%

DTMAPI will not load unless the launch option is changed.

Other commands:
Check: bash ./3_check_dtmapi_status.sh
Uninstall: bash ./2_uninstall_dtmapi.sh
Collect logs: bash ./4_collect_dtmapi_logs.sh

Double-clicking a .sh file usually opens it as text, so run it from the terminal. Standard Linux x64 uses the same steps as Steam Deck; the Windows game build still runs through Steam Proton.

【macOS + CrossOver (experimental entry point)】

1. Quit the game completely.
2. Open CrossOver.
3. In the left sidebar, select the existing Bottle that contains Windows Steam and the game. It may simply be named Steam. Do not create an empty Bottle.
4. Click Run Command on the right.
5. Click Browse.
6. Find this item's complete Workshop package folder.
7. Select DTMAPI-MultiPlatform-Installer.exe.
8. Follow the installer prompts.

Then configure the winhttp override:

1. Return to CrossOver and keep the same Steam Bottle selected.
2. Open Wine Configuration. Depending on your CrossOver version, it may be under Bottle Actions or Control Panels.
3. Open the Libraries tab.
4. Enter winhttp in the field for a new library override.
5. Click Add.
6. Select winhttp in the list and click Edit.
7. Set its load order to Native, then Builtin.
8. Click Apply, then OK.
9. Start Steam and the game normally from this Bottle.

【Notes for all platforms】

The installer is not code-signed. Subscribe only through the Doloc Town Steam Workshop. When reporting a problem, include the complete check output, collected logs, OS version, and Proton or CrossOver version.
