using System;
using System.Collections.Generic;
using System.Globalization;

namespace DTMAPI.BepInExBootstrap
{
    internal sealed class DtmUiText
    {
        private readonly Dictionary<string, string> primary;
        private readonly Dictionary<string, string> english;

        public DtmUiText(string? language = null)
        {
            Language = DetectLanguage(language);
            primary = Language.Equals("english", StringComparison.OrdinalIgnoreCase) ? English : SimplifiedChinese;
            english = English;
        }

        public string Language { get; }

        public string Get(string key, string fallback)
        {
            if (primary.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
                return value;
            if (english.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                return value;
            return fallback;
        }

        public string DisplayLanguageName(string value)
        {
            string normalized = (value ?? string.Empty).Trim().Replace('-', '_').ToLowerInvariant();
            if (normalized == "auto" || normalized.Length == 0)
                return Get("language.auto", "Auto");
            if (normalized == "schinese" || normalized == "zh" || normalized == "zh_cn" || normalized == "chinese")
                return Get("language.schinese", "Chinese");
            if (normalized == "english" || normalized == "en" || normalized == "en_us" || normalized == "en_gb")
                return Get("language.english", "English");
            return value ?? string.Empty;
        }

        public string TranslateEnablementReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return string.Empty;
            if (reason.IndexOf("official Doloc Town or Steam Workshop path", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("官方 Mod 界面", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("Steam 创意工坊路径中禁用", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.officialDisabled", reason);
            if (reason.IndexOf("Official enablement state was not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("官方启用状态", StringComparison.OrdinalIgnoreCase) >= 0 && reason.IndexOf("启用这个本地 Mod", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.officialMissingEntry", reason);
            if (reason.IndexOf("Official enablement state file was not found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("官方启用状态文件", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.officialMissingFile", reason);
            if (reason.IndexOf("Official enablement state could not be read", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("无法读取官方启用状态", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.officialReadFailed", reason);
            if (reason.IndexOf("local DTMAPI marker file", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("本地 DTMAPI 禁用标记", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.localMarker", reason);
            if ((reason.IndexOf("already loaded", StringComparison.OrdinalIgnoreCase) >= 0 &&
                reason.IndexOf("restart", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (reason.IndexOf("已经加载", StringComparison.OrdinalIgnoreCase) >= 0 &&
                reason.IndexOf("重启", StringComparison.OrdinalIgnoreCase) >= 0))
                return Get("reason.loadedDisabled", reason);
            if (reason.IndexOf("no longer discovered", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("不再发现", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.notDiscovered", reason);
            if (reason.IndexOf("not loaded", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("尚未加载", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.notLoaded", reason);
            return reason;
        }

        private static string DetectLanguage(string? requestedLanguage = null)
        {
            string requested = requestedLanguage ??
                Environment.GetEnvironmentVariable("DTMAPI_LANGUAGE") ??
                Environment.GetEnvironmentVariable("DTMAPI_UI_LANGUAGE") ??
                string.Empty;
            requested = requested.Trim().Replace('-', '_').ToLowerInvariant();
            if (requested == "auto")
                requested = string.Empty;
            if (requested == "en" || requested == "en_us" || requested == "en_gb" || requested == "english")
                return "english";
            if (!string.IsNullOrWhiteSpace(requested))
                return "schinese";

            CultureInfo culture = CultureInfo.CurrentUICulture;
            return culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "english" : "schinese";
        }

        private static readonly Dictionary<string, string> SimplifiedChinese = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ui.title"] = "DTMAPI 设置",
            ["ui.close"] = "关闭",
            ["tab.config"] = "配置",
            ["tab.mods"] = "Mod",
            ["tab.status"] = "状态",
            ["tab.errors"] = "错误",
            ["tab.hooks"] = "Hook",
            ["tab.logs"] = "日志",
            ["config.empty"] = "当前没有已启用的 DTMAPI Mod 注册配置页。",
            ["language.auto"] = "自动",
            ["language.schinese"] = "中文",
            ["language.english"] = "English",
            ["config.listTitle"] = "已启用的 DTMAPI Mod",
            ["config.lockedBadge"] = "锁定",
            ["config.lockedPrefix"] = "锁定：",
            ["config.save"] = "保存",
            ["config.reset"] = "重置",
            ["config.cancel"] = "取消",
            ["config.saved"] = "已保存",
            ["config.resetPending"] = "已重置，待保存",
            ["config.canceled"] = "已取消",
            ["item.locked"] = "锁定",
            ["item.on"] = "开",
            ["item.off"] = "关",
            ["item.noChoices"] = "无选项",
            ["item.pressKey"] = "按下按键...",
            ["item.capture"] = "捕获",
            ["item.none"] = "无",
            ["item.actionInvoked"] = "动作已执行",
            ["mods.notice"] = "DTMAPI 只显示状态；启用、禁用和排序仍由 Doloc Town 官方 Mod 界面或 Steam 创意工坊管理。",
            ["mods.state.loaded"] = "已加载",
            ["mods.state.loadedDisabled"] = "已加载，停用需重启",
            ["mods.state.restart"] = "需重启或检查依赖",
            ["mods.state.locked"] = "官方路径锁定",
            ["mods.source.local"] = "本地 DTMAPI 文件",
            ["mods.source.official"] = "官方/Steam 管理",
            ["mods.sourceLabel"] = "来源",
            ["status.runtimeActiveSince"] = "运行开始：",
            ["status.gamePath"] = "游戏路径：",
            ["status.modsSummary"] = "发现 Mod：{0} | 已加载：{1} | 配置页：{2}",
            ["status.titleEntry"] = "标题入口：Unity UI Canvas，仅在无遮挡标题主页显示，位置为左上角。",
            ["status.officialEnablement"] = "官方启用状态归官方 Mod UI 所有；DTMAPI 配置不会热卸载 DLL 或覆盖官方启停。",
            ["status.diagnosticsHotkey"] = "F8 诊断覆盖层默认禁用；玩家请使用标题页左上角 DTMAPI 设置入口。",
            ["errors.empty"] = "没有记录到 DTMAPI 错误。",
            ["hooks.empty"] = "没有记录到 Hook 状态。",
            ["logs.export"] = "导出日志",
            ["logs.exported"] = "日志已导出：",
            ["logs.latest"] = "最新日志：",
            ["logs.latestExport"] = "最近导出：",
            ["debug.title"] = "DTMAPI 调试控制台",
            ["debug.title.y"] = "Y键控制台",
            ["debug.subtitle"] = "存档内实验工具",
            ["debug.tab.items"] = "物品",
            ["debug.tab.weather"] = "天气",
            ["debug.tab.teleport"] = "传送",
            ["debug.section.items"] = "物品",
            ["debug.section.time"] = "时间",
            ["debug.section.speed"] = "移速",
            ["debug.section.weather"] = "天气",
            ["debug.section.teleport"] = "传送",
            ["debug.status.ready"] = "就绪",
            ["debug.missing.inventory"] = "物品调试 API 不可用。",
            ["debug.missing.weather"] = "天气调试 API 不可用。",
            ["debug.missing.teleport"] = "传送调试 API 不可用。",
            ["debug.missing.time"] = "时间调试 API 不可用。",
            ["debug.items.search"] = "搜索",
            ["debug.items.count"] = "{0} 个物品 | 第 {1}/{2} 页",
            ["debug.items.all"] = "全部",
            ["debug.items.mod"] = "模组物品",
            ["debug.items.sourceColumn"] = "来源",
            ["debug.items.categoryColumn"] = "子分类",
            ["debug.items.sourceBase"] = "本体",
            ["debug.items.sourceMods"] = "模组",
            ["debug.items.categoryAll"] = "全部分类",
            ["debug.items.source"] = "来源：{0}",
            ["debug.items.idLine"] = "ID：{0}",
            ["debug.items.categoryLine"] = "分类：{0}",
            ["debug.items.sourceLine"] = "来源：{0}",
            ["debug.items.statusLine"] = "状态：{0}",
            ["debug.items.tagsLine"] = "标签：{0}",
            ["debug.items.canGive"] = "可给予",
            ["debug.items.sourceDisabled"] = "来源未启用",
            ["debug.items.notLoaded"] = "未加载进运行时表",
            ["debug.items.notLoaded.short"] = "未加载",
            ["debug.items.unavailable"] = "不可给予",
            ["debug.items.unspawnable"] = "不可生成",
            ["debug.items.gave"] = "已给予 {0} x {1}",
            ["debug.items.gaveRightClick"] = "右键已给予 {0} x {1}",
            ["debug.items.failed"] = "给予失败：{0}",
            ["debug.time.state"] = "{0}/{1}/{2} {3:00}:{4:00}  {5}",
            ["debug.time.next"] = "下个时段",
            ["debug.time.skipped"] = "已推进到 {0:00}:00",
            ["debug.time.failed"] = "时间失败：{0}",
            ["debug.save.here"] = "存这里",
            ["debug.save.saved"] = "已保存槽位 {0}",
            ["debug.save.failed"] = "保存失败：{0}",
            ["debug.speed.state"] = "移速 {0:0.#}x",
            ["debug.speed.changed"] = "移速 {0:0.#}x",
            ["debug.speed.failed"] = "移速失败：{0}",
            ["debug.weather.state"] = "{0}/{1}/{2} {3}:00  {4}",
            ["debug.weather.note"] = "实验性：通过游戏原生天气系统切换当前天气，并覆盖当前预报时段。",
            ["debug.weather.current"] = "当前",
            ["debug.weather.forecast"] = "今日",
            ["debug.weather.table"] = "表",
            ["debug.weather.set"] = "切换",
            ["debug.weather.changed"] = "天气：{0} -> {1}",
            ["debug.weather.changed.simple"] = "天气：{0}",
            ["debug.weather.failed"] = "天气失败：{0}",
            ["debug.weather.unknown"] = "天气",
            ["debug.teleport.state"] = "当前位置：{0} ({1}) {2:0.##},{3:0.##}",
            ["debug.teleport.currentOnly"] = "当前位置：{0}",
            ["debug.teleport.current"] = "当前位置",
            ["debug.teleport.note"] = "仅白名单；使用游戏原生标记点传送，并记录传送前/请求后位置。",
            ["debug.teleport.go"] = "传送",
            ["debug.teleport.place"] = "地点",
            ["debug.teleport.count"] = "{0} 个目的地 | 第 {1}/{2} 页",
            ["debug.teleport.requested"] = "已请求传送：{0}",
            ["debug.teleport.failed"] = "传送失败：{0}",
            ["debug.teleport.exportCsv"] = "导出CSV",
            ["debug.teleport.exportedCsv"] = "已导出 {0} 个传送点",
            ["debug.teleport.exportCsvFailed"] = "CSV失败：{0}",
            ["common.none"] = "无",
            ["reason.officialDisabled"] = "此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。",
            ["reason.officialMissingEntry"] = "官方启用状态中还没有这个 Mod；请先在 Doloc Town 官方 Mod 界面启用它。",
            ["reason.officialMissingFile"] = "还没有官方启用状态文件；请先打开 Doloc Town 官方 Mod 界面一次。",
            ["reason.officialReadFailed"] = "官方启用状态读取失败；请检查 SAVE/mod_infos.json。",
            ["reason.localMarker"] = "此 Mod 被本地 DTMAPI 禁用标记锁定。",
            ["reason.loadedDisabled"] = "此 Mod 已加载；在官方界面禁用后，需要重启游戏才能完全停用。本局配置已锁定。",
            ["reason.notDiscovered"] = "此 Mod 当前未被 DTMAPI 发现。",
            ["reason.notLoaded"] = "此 Mod 尚未加载；请重启游戏或检查依赖错误。"
        };

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ui.title"] = "DTMAPI Settings",
            ["ui.close"] = "Close",
            ["tab.config"] = "Config",
            ["tab.mods"] = "Mods",
            ["tab.status"] = "Status",
            ["tab.errors"] = "Errors",
            ["tab.hooks"] = "Hooks",
            ["tab.logs"] = "Logs",
            ["config.empty"] = "No enabled DTMAPI mods have registered config pages.",
            ["language.auto"] = "Auto",
            ["language.schinese"] = "Chinese",
            ["language.english"] = "English",
            ["config.listTitle"] = "Enabled DTMAPI mods",
            ["config.lockedBadge"] = "locked",
            ["config.lockedPrefix"] = "Locked: ",
            ["config.save"] = "Save",
            ["config.reset"] = "Reset",
            ["config.cancel"] = "Cancel",
            ["config.saved"] = "Saved",
            ["config.resetPending"] = "Reset pending",
            ["config.canceled"] = "Canceled",
            ["item.locked"] = "Locked",
            ["item.on"] = "On",
            ["item.off"] = "Off",
            ["item.noChoices"] = "No choices",
            ["item.pressKey"] = "Press key...",
            ["item.capture"] = "Capture",
            ["item.none"] = "None",
            ["item.actionInvoked"] = "Action invoked",
            ["mods.notice"] = "DTMAPI shows status only. Enable, disable, and ordering stay with Doloc Town or Steam Workshop.",
            ["mods.state.loaded"] = "loaded",
            ["mods.state.loadedDisabled"] = "loaded; restart to stop",
            ["mods.state.restart"] = "restart/dependency required",
            ["mods.state.locked"] = "locked by official path",
            ["mods.source.local"] = "local DTMAPI file",
            ["mods.source.official"] = "official/Steam managed",
            ["mods.sourceLabel"] = "source",
            ["status.runtimeActiveSince"] = "Runtime active since ",
            ["status.gamePath"] = "Game path: ",
            ["status.modsSummary"] = "Mods discovered: {0} | loaded: {1} | config pages: {2}",
            ["status.titleEntry"] = "Title settings entry: Unity UI Canvas, visible only on the unobstructed title homepage, anchored top-left.",
            ["status.officialEnablement"] = "Official enablement remains source-owned; DTMAPI config editing does not hot-unload DLLs or override official state.",
            ["status.diagnosticsHotkey"] = "F8 diagnostics overlay is disabled by default; use the title-page DTMAPI Settings entry.",
            ["errors.empty"] = "No DTMAPI errors recorded.",
            ["hooks.empty"] = "No hook statuses recorded.",
            ["logs.export"] = "Export logs",
            ["logs.exported"] = "Exported logs: ",
            ["logs.latest"] = "Latest log: ",
            ["logs.latestExport"] = "Latest export: ",
            ["debug.title"] = "DTMAPI Debug Console",
            ["debug.title.y"] = "Y-Key Console",
            ["debug.subtitle"] = "Experimental in-save tools",
            ["debug.tab.items"] = "Items",
            ["debug.tab.weather"] = "Weather",
            ["debug.tab.teleport"] = "Teleport",
            ["debug.section.items"] = "Items",
            ["debug.section.time"] = "Time",
            ["debug.section.speed"] = "Move",
            ["debug.section.weather"] = "Weather",
            ["debug.section.teleport"] = "Teleport",
            ["debug.status.ready"] = "Ready",
            ["debug.missing.inventory"] = "Inventory debug API is not available.",
            ["debug.missing.weather"] = "Weather debug API is not available.",
            ["debug.missing.teleport"] = "Teleport debug API is not available.",
            ["debug.missing.time"] = "Time debug API is not available.",
            ["debug.items.search"] = "Search",
            ["debug.items.count"] = "{0} items | page {1}/{2}",
            ["debug.items.all"] = "All",
            ["debug.items.mod"] = "Mod Items",
            ["debug.items.sourceColumn"] = "Source",
            ["debug.items.categoryColumn"] = "Category",
            ["debug.items.sourceBase"] = "Base",
            ["debug.items.sourceMods"] = "Mods",
            ["debug.items.categoryAll"] = "All categories",
            ["debug.items.source"] = "source: {0}",
            ["debug.items.idLine"] = "ID: {0}",
            ["debug.items.categoryLine"] = "Category: {0}",
            ["debug.items.sourceLine"] = "Source: {0}",
            ["debug.items.statusLine"] = "Status: {0}",
            ["debug.items.tagsLine"] = "Tags: {0}",
            ["debug.items.canGive"] = "can give",
            ["debug.items.sourceDisabled"] = "source disabled",
            ["debug.items.notLoaded"] = "not loaded in runtime table",
            ["debug.items.notLoaded.short"] = "not loaded",
            ["debug.items.unavailable"] = "unavailable",
            ["debug.items.unspawnable"] = "not spawnable",
            ["debug.items.gave"] = "Gave {0} x {1}",
            ["debug.items.gaveRightClick"] = "Right-click gave {0} x {1}",
            ["debug.items.failed"] = "Give failed: {0}",
            ["debug.time.state"] = "{0}/{1}/{2} {3:00}:{4:00}  {5}",
            ["debug.time.next"] = "Next period",
            ["debug.time.skipped"] = "Advanced to {0:00}:00",
            ["debug.time.failed"] = "Time failed: {0}",
            ["debug.save.here"] = "Save here",
            ["debug.save.saved"] = "Saved slot {0}",
            ["debug.save.failed"] = "Save failed: {0}",
            ["debug.speed.state"] = "Speed {0:0.#}x",
            ["debug.speed.changed"] = "Speed {0:0.#}x",
            ["debug.speed.failed"] = "Speed failed: {0}",
            ["debug.weather.state"] = "{0}/{1}/{2} {3}:00  {4}",
            ["debug.weather.note"] = "Experimental: switches current weather and patches the current forecast period through the native weather system.",
            ["debug.weather.current"] = "current",
            ["debug.weather.forecast"] = "forecast",
            ["debug.weather.table"] = "table",
            ["debug.weather.set"] = "Set",
            ["debug.weather.changed"] = "Weather: {0} -> {1}",
            ["debug.weather.changed.simple"] = "Weather: {0}",
            ["debug.weather.failed"] = "Weather failed: {0}",
            ["debug.weather.unknown"] = "Weather",
            ["debug.teleport.state"] = "Current: {0} ({1}) {2:0.##},{3:0.##}",
            ["debug.teleport.currentOnly"] = "Current: {0}",
            ["debug.teleport.current"] = "Current location",
            ["debug.teleport.note"] = "Whitelist only; uses native mark-point transport and records before/after request snapshots.",
            ["debug.teleport.go"] = "Go",
            ["debug.teleport.place"] = "Place",
            ["debug.teleport.count"] = "{0} destinations | page {1}/{2}",
            ["debug.teleport.requested"] = "Teleport requested: {0}",
            ["debug.teleport.failed"] = "Teleport failed: {0}",
            ["debug.teleport.exportCsv"] = "Export CSV",
            ["debug.teleport.exportedCsv"] = "Exported {0} teleport rows",
            ["debug.teleport.exportCsvFailed"] = "CSV failed: {0}",
            ["common.none"] = "(none)",
            ["reason.officialDisabled"] = "Mod is disabled by the official Doloc Town or Steam Workshop path.",
            ["reason.officialMissingEntry"] = "Official enablement state does not include this mod yet; enable it in Doloc Town's official Mod UI.",
            ["reason.officialMissingFile"] = "Official enablement state file was not found; open Doloc Town's official Mod UI once.",
            ["reason.officialReadFailed"] = "Official enablement state could not be read; check SAVE/mod_infos.json.",
            ["reason.localMarker"] = "Mod is disabled by a local DTMAPI marker file.",
            ["reason.loadedDisabled"] = "Mod is already loaded; official disable will fully stop it after restarting the game. Config editing is locked for this session.",
            ["reason.notDiscovered"] = "Mod is no longer discovered by DTMAPI.",
            ["reason.notLoaded"] = "Mod is not loaded; restart or check dependency errors before editing config."
        };
    }
}
