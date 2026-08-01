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
            if (reason.IndexOf("Loaded by DTMAPI runtime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                reason.IndexOf("由 DTMAPI 运行时加载", StringComparison.OrdinalIgnoreCase) >= 0)
                return Get("reason.loadedByRuntime", reason);
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
            ["tab.advanced"] = "高级",
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
            ["mods.empty"] = "当前没有发现 DTMAPI Mod。",
            ["mods.count"] = "Mod",
            ["mods.detailsTitle"] = "所选 Mod 详情",
            ["mods.detailsTitleFormat"] = "所选 Mod：{0}（{1}）",
            ["mods.dependencies"] = "依赖",
            ["mods.restartLabel"] = "重启",
            ["mods.dependenciesNone"] = "未声明依赖",
            ["mods.dependencyRequired"] = "必需",
            ["mods.dependencyOptional"] = "可选",
            ["mods.dependencyIssue"] = "有 {0} 项问题",
            ["mods.dependencyOk"] = "正常",
            ["mods.dependencyRequiredMissing"] = "缺少必需依赖",
            ["mods.dependencyOptionalMissing"] = "可选依赖未安装",
            ["mods.dependencyVersionOld"] = "版本过低",
            ["mods.restartRequired"] = "需要重启",
            ["mods.restartNone"] = "当前无需重启",
            ["mods.stateLabel"] = "状态",
            ["mods.identityLabel"] = "身份",
            ["mods.placementLabel"] = "位置",
            ["mods.compatibilityLabel"] = "兼容性",
            ["mods.identity.strict"] = "受限代码 Mod",
            ["mods.identity.advanced"] = "高级代码 Mod",
            ["mods.identity.contentPack"] = "内容包",
            ["mods.placement.local"] = "本地受管",
            ["mods.placement.officialLocal"] = "官方本地受管",
            ["mods.placement.workshopVerified"] = "创意工坊原生验证",
            ["mods.placement.workshopCompatibility"] = "创意工坊兼容路径",
            ["mods.compatibilityVerified"] = "已验证",
            ["mods.compatibilityUnverified"] = "未验证",
            ["mods.compatibilityNotApplicable"] = "不适用",
            ["mods.gameBuildLabel"] = "游戏构建",
            ["mods.gameAssemblyLabel"] = "游戏程序集 SHA-256",
            ["mods.restartRunAgain"] = "重启 Doloc Town 后，这个 Mod 才能再次运行。",
            ["mods.restartUnloadDisabled"] = "重启 Doloc Town 后，已禁用的代码 Mod 才会卸载。",
            ["mods.restartAssemblyChanges"] = "程序集更新以及禁用或卸载变更会在重启后生效。",
            ["mods.loadedYes"] = "已载入",
            ["mods.loadedNo"] = "未载入",
            ["mods.officialEnabled"] = "官方已启用",
            ["mods.officialDisabled"] = "官方已禁用",
            ["mods.officialUnmanaged"] = "非官方启停路径",
            ["mods.unknownName"] = "未命名",
            ["mods.unknownId"] = "无 ID",
            ["mods.unknownVersion"] = "无版本",
            ["mods.unknownSource"] = "未知来源",
            ["mods.unknownIdentity"] = "未知身份",
            ["manager.modelUnavailable"] = "Manager 状态暂不可用。",
            ["manager.modelRefreshFailed"] = "Manager 状态刷新失败：",
            ["pager.page"] = "{0}-{1}/{2}  第 {3}/{4} 页",
            ["pager.showing"] = "{0}：显示第 {1}-{2} 项，共 {3} 项",
            ["status.runtimeActiveSince"] = "运行开始：",
            ["status.gamePath"] = "游戏路径：",
            ["status.modsSummary"] = "发现 Mod：{0} | 已加载：{1} | 配置页：{2}",
            ["status.titleEntry"] = "标题入口：Unity UI Canvas，仅在无遮挡标题主页显示，位置为左上角。",
            ["status.officialEnablement"] = "官方启用状态归官方 Mod UI 所有；DTMAPI 配置不会热卸载 DLL 或覆盖官方启停。",
            ["status.diagnosticsHotkey"] = "F8 诊断覆盖层默认禁用；玩家请使用标题页左上角 DTMAPI 设置入口。",
            ["status.refresh"] = "刷新",
            ["status.refreshed"] = "Manager 状态已刷新。",
            ["status.refreshFailed"] = "Manager 刷新失败：",
            ["status.copySummary"] = "复制摘要",
            ["status.summaryCopied"] = "Manager 摘要已复制。",
            ["status.summaryCopyUnavailable"] = "无法访问剪贴板；摘要已写入运行日志。",
            ["status.managerOverall"] = "总体状态：",
            ["status.managerMods"] = "Mod：已载入 {0} | 被阻止 {1} | 已禁用 {2}",
            ["status.managerDependencies"] = "依赖问题：{0} | 当前需要重启：{1}",
            ["status.managerDiagnostics"] = "需要关注：错误 {0} | 警告 {1}",
            ["status.managerRefreshOk"] = "状态刷新：正常",
            ["status.managerRefreshFailed"] = "状态刷新失败：",
            ["status.advancedHint"] = "更详细的注册表、兼容性、Hook 和功能证据请查看“高级”页。",
            ["status.state.healthy"] = "正常",
            ["status.state.attention"] = "需要处理",
            ["status.state.degraded"] = "部分功能受限",
            ["status.state.failed"] = "存在错误",
            ["errors.empty"] = "没有记录到 DTMAPI 错误或警告。",
            ["errors.label"] = "错误",
            ["warnings.label"] = "警告",
            ["errors.noneSelected"] = "没有记录到错误。",
            ["warnings.noneSelected"] = "没有记录到警告。",
            ["hooks.empty"] = "没有记录到 Hook 状态。",
            ["hooks.title"] = "Hook",
            ["advanced.title"] = "高级诊断",
            ["advanced.registry"] = "注册表证据",
            ["advanced.features"] = "功能证据",
            ["advanced.summary"] = "注册表 {0} | 清单问题 {1} | 依赖错误/警告 {2}/{3} | 高级准入收据 {4}/{5} | 需重启 {6} | Hook/功能问题 {7}/{8}",
            ["advanced.unavailable"] = "内容/清单注册表尚不可用；请在发现完成后刷新。",
            ["advanced.featuresEmpty"] = "没有记录到功能状态证据。",
            ["advanced.registryClean"] = "注册表检查正常；没有抽样诊断或旧注册表差异。",
            ["advanced.registryUnavailable"] = "内容/清单注册表证据尚不可用。",
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
            ["reason.notLoaded"] = "此 Mod 尚未加载；请重启游戏或检查依赖错误。",
            ["reason.loadedByRuntime"] = "已由 DTMAPI 运行时载入。"
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
            ["tab.advanced"] = "Advanced",
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
            ["mods.empty"] = "No DTMAPI mods are currently discovered.",
            ["mods.count"] = "Mods",
            ["mods.detailsTitle"] = "Selected Mod details",
            ["mods.detailsTitleFormat"] = "Selected Mod: {0} ({1})",
            ["mods.dependencies"] = "Dependencies",
            ["mods.restartLabel"] = "Restart",
            ["mods.dependenciesNone"] = "none declared",
            ["mods.dependencyRequired"] = "required",
            ["mods.dependencyOptional"] = "optional",
            ["mods.dependencyIssue"] = "{0} issue(s)",
            ["mods.dependencyOk"] = "ok",
            ["mods.dependencyRequiredMissing"] = "required missing",
            ["mods.dependencyOptionalMissing"] = "optional missing",
            ["mods.dependencyVersionOld"] = "version too old",
            ["mods.restartRequired"] = "restart required",
            ["mods.restartNone"] = "not currently required",
            ["mods.stateLabel"] = "State",
            ["mods.identityLabel"] = "Identity",
            ["mods.placementLabel"] = "placement",
            ["mods.compatibilityLabel"] = "compatibility",
            ["mods.identity.strict"] = "Strict CodeMod",
            ["mods.identity.advanced"] = "Advanced CodeMod",
            ["mods.identity.contentPack"] = "ContentPack",
            ["mods.placement.local"] = "managed local",
            ["mods.placement.officialLocal"] = "managed official local",
            ["mods.placement.workshopVerified"] = "native-verified Workshop",
            ["mods.placement.workshopCompatibility"] = "compatibility Workshop",
            ["mods.compatibilityVerified"] = "verified",
            ["mods.compatibilityUnverified"] = "unverified",
            ["mods.compatibilityNotApplicable"] = "not applicable",
            ["mods.gameBuildLabel"] = "game build",
            ["mods.gameAssemblyLabel"] = "game assembly SHA-256",
            ["mods.restartRunAgain"] = "Restart Doloc Town before this Mod can run again.",
            ["mods.restartUnloadDisabled"] = "Restart Doloc Town to unload this disabled CodeMod.",
            ["mods.restartAssemblyChanges"] = "Assembly updates and disable/unload changes take effect after restart.",
            ["mods.loadedYes"] = "loaded",
            ["mods.loadedNo"] = "not loaded",
            ["mods.officialEnabled"] = "official enabled",
            ["mods.officialDisabled"] = "official disabled",
            ["mods.officialUnmanaged"] = "not officially managed",
            ["mods.unknownName"] = "unnamed",
            ["mods.unknownId"] = "no id",
            ["mods.unknownVersion"] = "no version",
            ["mods.unknownSource"] = "unknown source",
            ["mods.unknownIdentity"] = "unknown identity",
            ["manager.modelUnavailable"] = "Manager model unavailable.",
            ["manager.modelRefreshFailed"] = "Manager model refresh failed: ",
            ["pager.page"] = "{0}-{1}/{2}  Page {3}/{4}",
            ["pager.showing"] = "{0}: showing {1}-{2} of {3}",
            ["status.runtimeActiveSince"] = "Runtime active since ",
            ["status.gamePath"] = "Game path: ",
            ["status.modsSummary"] = "Mods discovered: {0} | loaded: {1} | config pages: {2}",
            ["status.titleEntry"] = "Title settings entry: Unity UI Canvas, visible only on the unobstructed title homepage, anchored top-left.",
            ["status.officialEnablement"] = "Official enablement remains source-owned; DTMAPI config editing does not hot-unload DLLs or override official state.",
            ["status.diagnosticsHotkey"] = "F8 diagnostics overlay is disabled by default; use the title-page DTMAPI Settings entry.",
            ["status.refresh"] = "Refresh",
            ["status.refreshed"] = "Manager status refreshed.",
            ["status.refreshFailed"] = "Manager refresh failed: ",
            ["status.copySummary"] = "Copy Summary",
            ["status.summaryCopied"] = "Manager summary copied.",
            ["status.summaryCopyUnavailable"] = "Clipboard unavailable; summary written to the runtime log.",
            ["status.managerOverall"] = "Overall status: ",
            ["status.managerMods"] = "Mods: loaded {0} | blocked {1} | disabled {2}",
            ["status.managerDependencies"] = "Dependency issues: {0} | restart required now: {1}",
            ["status.managerDiagnostics"] = "Needs attention: errors {0} | warnings {1}",
            ["status.managerRefreshOk"] = "Manager refresh: healthy",
            ["status.managerRefreshFailed"] = "Manager refresh failed: ",
            ["status.advancedHint"] = "Use Advanced for registry, compatibility, Hook, and feature evidence.",
            ["status.state.healthy"] = "healthy",
            ["status.state.attention"] = "attention required",
            ["status.state.degraded"] = "degraded",
            ["status.state.failed"] = "failed",
            ["errors.empty"] = "No DTMAPI errors or warnings recorded.",
            ["errors.label"] = "Errors",
            ["warnings.label"] = "Warnings",
            ["errors.noneSelected"] = "No errors recorded.",
            ["warnings.noneSelected"] = "No warnings recorded.",
            ["hooks.empty"] = "No hook statuses recorded.",
            ["hooks.title"] = "Hooks",
            ["advanced.title"] = "Advanced diagnostics",
            ["advanced.registry"] = "Registry evidence",
            ["advanced.features"] = "Feature evidence",
            ["advanced.summary"] = "registry {0} | manifest issues {1} | dependency errors/warnings {2}/{3} | Advanced receipts {4}/{5} | restart required {6} | Hook/feature issues {7}/{8}",
            ["advanced.unavailable"] = "Content/manifest registry unavailable; refresh after discovery completes.",
            ["advanced.featuresEmpty"] = "No feature status evidence recorded.",
            ["advanced.registryClean"] = "Registry checks are clean; no sampled diagnostics or legacy diffs.",
            ["advanced.registryUnavailable"] = "Content/manifest registry evidence is not available yet.",
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
            ["reason.notLoaded"] = "Mod is not loaded; restart or check dependency errors before editing config.",
            ["reason.loadedByRuntime"] = "Loaded by DTMAPI runtime."
        };
    }
}
