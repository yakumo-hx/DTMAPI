using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class ProductRuntimeAdapter :
        IDebugConsoleRuntime,
        IDebugConsoleInputRuntime,
        IDebugConsoleModalRuntime,
        IDebugConsoleNativeRuntime, IMonsterConsoleRuntime
    {
        private readonly IDtmHelper helper;
        private readonly IUiModalSessions modal;

        internal ProductRuntimeAdapter(IDtmHelper helper)
        {
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            modal = helper.UI as IUiModalSessions ??
                throw new InvalidOperationException("DebugConsole requires the DTMAPI 0.7.5 Experimental modal-session capability.");
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

        public DtmModalOpenResult OpenModal(
            string menuId,
            string ownerId)
        {
            if (!ownerId.Equals(
                    helper.ModManifest.UniqueID,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("DebugConsole modal owner does not match its helper.");
            }
            return modal.OpenModal(menuId);
        }

        public void Suppress(string button)
        {
            helper.Input.Suppress(button);
        }

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

        public object? GetMonsterContentApi()
        {
            // Resolve only an already-loaded optional contract. A plain Y installation
            // does not create a Monster backend merely to display the catalog.
            var contract = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("DTMAPI.MonsterContent.IMonsterContentApi", false)).FirstOrDefault(t => t != null);
            if (contract == null) return null;
            return typeof(IModRegistry).GetMethod("GetApi")!.MakeGenericMethod(contract).Invoke(helper.ModRegistry, new object[] { "DTMAPI.Monsters" });
        }
        public IReadOnlyList<AnimalContentSource> GetAnimalContentSources()
        {
            IContentItemInfo[] indexedItems = helper.Content
                .GetAllIndexedItems()
                .ToArray();
            var candidates = new List<AnimalContentSource>();
            foreach (IContentAssetInfo asset in helper.Content
                .FindAssets("json")
                .Where(IsRuntimeContentAsset))
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
                        typeof(RuntimeContentMetadata));
                    using (FileStream stream = File.OpenRead(asset.SourcePath))
                    {
                        var content = serializer.ReadObject(stream) as RuntimeContentMetadata;
                        if (content?.SchemaVersion != 1) continue;
                        var definitions = content.Animals?.Definitions ?? Array.Empty<CustomAnimalSourceDefinition>();
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

        private static bool IsRuntimeContentAsset(
            IContentAssetInfo asset)
        {
            string relative = (asset?.RelativePath ?? string.Empty)
                .Replace('\\', '/')
                .TrimStart('/');
            return relative.Equals(
                "PackData/content.json",
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
        private sealed class RuntimeContentMetadata
        {
            [DataMember(Name = "schemaVersion")]
            public int SchemaVersion { get; set; }
            [DataMember(Name = "animals")]
            public AnimalContentMetadata? Animals { get; set; }
        }

        [DataContract]
        private sealed class AnimalContentMetadata
        {
            [DataMember(Name = "definitions")]
            public CustomAnimalSourceDefinition[]? Definitions { get; set; }
        }

        [DataContract]
        private sealed class CustomAnimalSourceDefinition
        {
            [DataMember(Name = "speciesId")]
            public string SpeciesId { get; set; } = string.Empty;
        }

    }
}
