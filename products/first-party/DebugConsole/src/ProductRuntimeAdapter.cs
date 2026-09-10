using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class ProductRuntimeAdapter :
        IDebugConsoleRuntime,
        IDebugConsoleInputRuntime,
        IDebugConsoleModalRuntime,
        IDebugConsoleNativeRuntime
    {
        private const string MenuId = "DTMAPI.DebugConsole";
        private readonly IDtmHelper helper;
        private readonly object modal;
        private readonly MethodInfo tryOpenModal;
        private readonly MethodInfo isModalOpen;
        private readonly MethodInfo closeModal;

        internal ProductRuntimeAdapter(IDtmHelper helper)
        {
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            modal = helper.UI;
            Type modalType = modal.GetType();
            tryOpenModal = ResolveModalMethod(
                modalType,
                "TryOpenModal");
            isModalOpen = ResolveModalMethod(
                modalType,
                "IsModalOpen");
            closeModal = ResolveModalMethod(
                modalType,
                "CloseModal");
            string configPath =
                helper.Config.GetConfigPath(helper.ModManifest);
            DtmApiPath =
                Directory.GetParent(
                    Directory.GetParent(configPath)?.FullName ??
                    string.Empty)?.FullName ??
                string.Empty;
            if (DtmApiPath.Length == 0)
            {
                throw new InvalidOperationException(
                    "Could not resolve the DTMAPI state directory from the owner-bound config path.");
            }
        }

        public IMonitor RuntimeMonitor => helper.Monitor;
        IMonitor IDebugConsoleNativeRuntime.Monitor => helper.Monitor;
        public string NativeOwnerLabel => "ProductNative";
        public IDebugConsoleInputRuntime Input => this;
        public IDebugConsoleModalRuntime UI => this;
        public ITranslationHelper Translation => helper.Translation;
        public string ProductVersion => helper.ModManifest.Version;
        public string DtmApiPath { get; }

        public bool ModalOpen
        {
            set => DebugConsoleInputGate.ModalOpen = value;
        }

        public bool NativeInputDrainActive
        {
            set => DebugConsoleInputGate.NativeInputDrainActive = value;
        }

        public bool IsOpen =>
            isModalOpen.Invoke(
                modal,
                new object[] { MenuId }) is bool open &&
            open;
        public string ActiveMenuId => IsOpen ? MenuId : string.Empty;

        public bool OpenOwnerBoundCustomMenu(
            string menuId,
            string ownerId)
        {
            if (!ownerId.Equals(
                    helper.ModManifest.UniqueID,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return tryOpenModal.Invoke(
                modal,
                new object[] { menuId }) is bool opened &&
                opened;
        }

        public void Close()
        {
            closeModal.Invoke(
                modal,
                new object[] { MenuId });
        }

        public void Suppress(string button)
        {
            helper.Input.Suppress(button);
        }

        public bool HasOwnerLegacyButtonRegistration(
            string ownerId,
            string button) => false;

        public bool HasOwnerTypedKeybindForButton(
            string ownerId,
            string button) => true;

        public bool TryDispatchLegacyModalButtonPressed(
            string menuId,
            string ownerId,
            string button) => false;

        public void SetHookStatus(
            string hookId,
            string status,
            string source,
            string details)
        {
            helper.Monitor.LogOnce(
                "debugconsole-status-" + hookId + "-" + status,
                "DebugConsole status " + hookId +
                "=" + status +
                " source=" + source +
                " details=" + details + ".");
        }

        public void RecordError(
            string owner,
            string message,
            string details)
        {
            helper.Monitor.Log(
                owner + ": " + message + " " + details,
                LogLevel.Error);
        }

        public System.Collections.Generic.IReadOnlyList<IContentItemInfo>
            GetIndexedItems() =>
            helper.Content.GetAllIndexedItems();

        public IContentItemInfo? GetIndexedItem(string itemId) =>
            helper.Content.GetAnyIndexedItem(itemId);

        public IReadOnlyList<AnimalContentSource> GetAnimalContentSources()
        {
            IContentItemInfo[] indexedItems = helper.Content
                .GetAllIndexedItems()
                .ToArray();
            var candidates = new List<AnimalContentSource>();
            foreach (IContentAssetInfo asset in helper.Content
                .FindAssets("json")
                .Where(IsCustomAnimalDefinitionAsset))
            {
                try
                {
                    IContentItemInfo? source = indexedItems
                        .Where(item => IsPathInsideRoot(
                            asset.SourcePath,
                            item.RootPath))
                        .OrderByDescending(item => item.Enabled)
                        .ThenBy(item => item.LoadOrder < 0
                            ? int.MaxValue
                            : item.LoadOrder)
                        .FirstOrDefault();
                    string sourceId = FirstText(
                        source?.SourceId,
                        asset.SourceModId);
                    string displayName = FirstText(
                        source?.SourceModTitle,
                        asset.SourceModId,
                        sourceId);
                    var serializer = new DataContractJsonSerializer(
                        typeof(CustomAnimalSourceDefinition[]));
                    using (FileStream stream = File.OpenRead(asset.SourcePath))
                    {
                        var definitions = serializer.ReadObject(stream) as
                            CustomAnimalSourceDefinition[] ??
                            Array.Empty<CustomAnimalSourceDefinition>();
                        foreach (CustomAnimalSourceDefinition definition in
                            definitions)
                        {
                            string animalId =
                                (definition.SpeciesId ?? string.Empty).Trim();
                            if (animalId.Length == 0)
                                continue;
                            candidates.Add(new AnimalContentSource
                            {
                                AnimalId = animalId,
                                SourceId = sourceId,
                                DisplayName = displayName,
                                SourceKind = FirstText(
                                    source?.SourceKind,
                                    "DTMAPI"),
                                Enabled = source?.Enabled ?? true,
                                EnablementKnown =
                                    source?.EnablementKnown ?? true,
                                WorkshopId = source?.WorkshopId
                            });
                        }
                    }
                }
                catch (Exception error)
                {
                    helper.Monitor.Log(
                        "Y-Key Console ignored custom animal source metadata " +
                        asset.RelativePath + ": " +
                        error.GetType().Name + ": " + error.Message,
                        LogLevel.Warn);
                }
            }

            var result = new List<AnimalContentSource>();
            foreach (IGrouping<string, AnimalContentSource> group in
                candidates.GroupBy(
                    candidate => candidate.AnimalId,
                    StringComparer.OrdinalIgnoreCase))
            {
                AnimalContentSource[] owners = group.ToArray();
                if (owners.Select(owner => owner.SourceId)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Skip(1)
                    .Any())
                {
                    helper.Monitor.Log(
                        "Y-Key Console left custom animal species '" +
                        group.Key +
                        "' unattributed because multiple active content " +
                        "sources declared it.",
                        LogLevel.Warn);
                    continue;
                }
                result.Add(owners[0]);
            }
            return result;
        }

        public void SetCreativeHookDemand(bool enabled)
        {
            // ProductNative owns the complete hook set for its assembly lifetime.
            // The callback gate itself is toggled by DebugConsoleNativeActions.
        }

        public void SetMovementMultiplier(
            object? player,
            double multiplier) =>
            DebugConsoleMovementHooks.SetMultiplier(
                player,
                multiplier);

        public void Status(
            string id,
            string status,
            string source,
            string details) =>
            SetHookStatus(id, status, source, details);

        public void Error(string operation, Exception error) =>
            RecordError(
                "DTMAPI.DebugConsoleMod",
                operation + " failed.",
                error.ToString());

        private static bool IsCustomAnimalDefinitionAsset(
            IContentAssetInfo asset)
        {
            string relative = (asset?.RelativePath ?? string.Empty)
                .Replace('\\', '/')
                .TrimStart('/');
            return relative.Equals(
                "Content/DTMAPI/custom-animals.json",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathInsideRoot(
            string sourcePath,
            string rootPath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) ||
                string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }
            try
            {
                string source = Path.GetFullPath(sourcePath);
                string root = Path.GetFullPath(rootPath).TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                return source.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith(
                        root + Path.DirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase) ||
                    source.StartsWith(
                        root + Path.AltDirectorySeparatorChar,
                        StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string FirstText(params string?[] values) =>
            values.FirstOrDefault(value =>
                !string.IsNullOrWhiteSpace(value))?.Trim() ??
            string.Empty;

        [DataContract]
        private sealed class CustomAnimalSourceDefinition
        {
            [DataMember(Name = "speciesId")]
            public string SpeciesId { get; set; } = string.Empty;
        }

        private static MethodInfo ResolveModalMethod(
            Type type,
            string name)
        {
            return type.GetMethod(
                       name,
                       BindingFlags.Public |
                       BindingFlags.Instance,
                       null,
                       new[] { typeof(string) },
                       null) ??
                throw new InvalidOperationException(
                    "DTMAPI 0.5.5 owner-bound modal helper method '" +
                    name +
                    "' is unavailable.");
        }
    }
}
