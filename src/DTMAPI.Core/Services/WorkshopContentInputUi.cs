using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Services
{
    internal sealed class WorkshopService : IWorkshopHelper
    {
        private readonly List<IWorkshopModInfo> mods = new List<IWorkshopModInfo>();

        public void SetMods(IEnumerable<DiscoveredMod> discovered)
        {
            mods.Clear();
            mods.AddRange(discovered.Select(m => new WorkshopModInfo(m)));
        }

        public IReadOnlyList<IWorkshopModInfo> GetOfficialMods() => mods.ToArray();
        public IReadOnlyList<IWorkshopModInfo> GetDtmApiMods() => mods.Where(m => !string.IsNullOrWhiteSpace(m.UniqueID)).ToArray();
        public bool IsOfficialEnablementManaged(IWorkshopModInfo mod) => !mod.CanDTMApiToggle || mod.Source.Equals("Workshop", StringComparison.OrdinalIgnoreCase);
        public string GetEnablementHint(IWorkshopModInfo mod)
        {
            if (IsOfficialEnablementManaged(mod))
                return "请通过 Doloc Town 官方 Mod 界面或 Steam 创意工坊启用/禁用。";
            return "本地非创意工坊 Mod；DTMAPI 可使用本地禁用标记。";
        }
    }

    internal sealed class ContentQueryService : IContentQueryHelper
    {
        private readonly List<ContentAssetInfo> assets = new List<ContentAssetInfo>();

        public void Rebuild(IEnumerable<DiscoveredMod> discovered)
        {
            assets.Clear();
            foreach (DiscoveredMod mod in discovered)
            {
                if (!Directory.Exists(mod.RootPath))
                    continue;
                foreach (string path in Directory.GetFiles(mod.RootPath, "*.*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                    if (extension != "json" && extension != "png" && extension != "txt" && extension != "csv")
                        continue;
                    string relative = path.Substring(mod.RootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    assets.Add(new ContentAssetInfo(extension, relative, mod.Manifest.UniqueID, path));
                }
            }
        }

        public IReadOnlyList<IContentAssetInfo> FindAssets(string contentType)
        {
            return assets.Where(a => a.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)).Cast<IContentAssetInfo>().ToArray();
        }

        public IReadOnlyList<string> GetKnownContentTypes()
        {
            return assets.Select(a => a.ContentType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        public bool TryReadTextAsset(string relativePath, out string text)
        {
            ContentAssetInfo? asset = assets.FirstOrDefault(a => a.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            if (asset != null && File.Exists(asset.SourcePath))
            {
                text = File.ReadAllText(asset.SourcePath);
                return true;
            }
            text = string.Empty;
            return false;
        }
    }

    internal sealed class ContentAssetInfo : IContentAssetInfo
    {
        public ContentAssetInfo(string contentType, string relativePath, string sourceModId, string sourcePath)
        {
            ContentType = contentType;
            RelativePath = relativePath;
            SourceModId = sourceModId;
            SourcePath = sourcePath;
        }

        public string ContentType { get; }
        public string RelativePath { get; }
        public string SourceModId { get; }
        public string SourcePath { get; }
    }

    internal sealed class InputService : IInputHelper
    {
        private readonly HashSet<string> registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> down = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void RegisterButton(string button)
        {
            if (!string.IsNullOrWhiteSpace(button) && !button.Equals("None", StringComparison.OrdinalIgnoreCase))
                registered.Add(button.Trim());
        }

        public void UnregisterButton(string button)
        {
            if (!string.IsNullOrWhiteSpace(button))
                registered.Remove(button.Trim());
        }

        public IReadOnlyList<string> GetRegisteredButtons() => registered.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();

        public void SetPressed(string button)
        {
            down.Add(button);
            pressed.Add(button);
        }

        public void SetReleased(string button)
        {
            down.Remove(button);
        }

        public void ClearFrame() => pressed.Clear();
        public bool IsDown(string button) => down.Contains(button);
        public bool WasPressed(string button) => pressed.Contains(button);
        public void Suppress(string button) => suppressed.Add(button);
        public IReadOnlyList<string> GetSuppressedButtons() => suppressed.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public enum DtmOverlayPage
    {
        Status,
        Mods,
        Config,
        Errors,
        Hooks,
        Logs
    }

    public sealed class UiRuntimeService : IUiHelper
    {
        private readonly Func<string> exportLogs;
        private readonly Action<string> opened;
        private readonly Action<string> closed;

        public UiRuntimeService(Func<string> exportLogs, Action<string> opened, Action<string> closed)
        {
            this.exportLogs = exportLogs;
            this.opened = opened;
            this.closed = closed;
        }

        public bool IsOpen { get; private set; }
        public DtmOverlayPage CurrentPage { get; private set; } = DtmOverlayPage.Status;
        public string LastExportPath { get; private set; } = string.Empty;
        public string? RequestedConfigUniqueId { get; private set; }
        public string InputContext { get; private set; } = "Unknown";
        public bool CanDrawOverlay { get; private set; } = true;
        public bool GameplayHotkeysAllowed { get; private set; } = true;
        public string DrawBoundaryReason { get; private set; } = string.Empty;
        public bool BlocksGameplayHotkeys => IsOpen || !GameplayHotkeysAllowed;
        public bool BlocksModUpdates => IsOpen;

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open(DtmOverlayPage.Status);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            string menuId = "DTMAPI." + CurrentPage;
            IsOpen = false;
            closed(menuId);
        }

        public void OpenDtmApiStatusPage() => Open(DtmOverlayPage.Status);
        public void OpenModListPage() => Open(DtmOverlayPage.Mods);
        public void OpenConfigPage(string? uniqueId = null)
        {
            RequestedConfigUniqueId = uniqueId;
            Open(DtmOverlayPage.Config);
        }
        public void OpenErrorPage() => Open(DtmOverlayPage.Errors);
        public void OpenHookStatusPage() => Open(DtmOverlayPage.Hooks);
        public void OpenLogsPage() => Open(DtmOverlayPage.Logs);
        public void SetPage(DtmOverlayPage page) => Open(page);

        public void SetUiContext(string inputContext, bool canDrawOverlay, bool gameplayHotkeysAllowed, string reason)
        {
            InputContext = string.IsNullOrWhiteSpace(inputContext) ? "Unknown" : inputContext;
            CanDrawOverlay = canDrawOverlay;
            GameplayHotkeysAllowed = gameplayHotkeysAllowed;
            DrawBoundaryReason = reason ?? string.Empty;
        }

        public string ExportLogs()
        {
            LastExportPath = exportLogs();
            CurrentPage = DtmOverlayPage.Logs;
            Open(DtmOverlayPage.Logs);
            return LastExportPath;
        }

        private void Open(DtmOverlayPage page)
        {
            bool wasOpen = IsOpen;
            CurrentPage = page;
            IsOpen = true;
            if (!wasOpen)
                opened("DTMAPI." + page);
        }
    }
}
