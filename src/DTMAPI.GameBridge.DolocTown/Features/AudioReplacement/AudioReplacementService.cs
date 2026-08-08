using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AudioReplacementService : IAudioReplacementApi
    {
        private const string ReadyStatus = "ready";
        private const string SimpleSfxCategory = "SimpleSfx";
        private const string AnimalVoiceCategory = "AnimalVoice";
        private const string ContentPackSource = "ContentPack";
        private const string CodeModSource = "CodeMod";
        private const string SchemaFileRelativePath = "Content/DTMAPI/audio-replacements.json";
        private static readonly TimeSpan AnimalSoundContextMaxAge = TimeSpan.FromSeconds(2);
        private static readonly HashSet<string> ReviewedSimpleSfxEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PLAY_RESOURCE_PAPER_BOX"
        };
        private static readonly HashSet<string> ReviewedAnimalVoiceEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PLAY_ANIMAL_PET_CHICKEN",
            "PLAY_ANIMAL_PET_CHICKEN_CHILD",
            "PLAY_ANIMAL_PET_SHEEP",
            "PLAY_ANIMAL_PET_SHEEP_CHILD",
            "PLAY_ANIMAL_PET_SLIME",
            "PLAY_ANIMAL_PET_PANGOLIN",
            "PLAY_ANIMAL_PET_PANGOLIN_CHILD",
            "PLAY_ANIMAL_PET_HONEY_AMOEBA",
            "PLAY_ANIMAL_PET_HONEY_AMOEBA_CHILD"
        };

        private readonly DtmApiRuntime runtime;
        private readonly Func<string, string, object?>? readyBackendFactoryForTest;
        private Dictionary<string, AudioReplacementEntry> entries = new Dictionary<string, AudioReplacementEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AudioReplacementState> states = new Dictionary<string, AudioReplacementState>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> contentPackEntryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, long> contentPackOwnerGenerations = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<AnimalSoundContext> animalSoundContexts = new Stack<AnimalSoundContext>();
        private readonly List<AudioReplacementEntry> pendingEntries = new List<AudioReplacementEntry>();
        private readonly HashSet<AudioReplacementEntry> pendingEntrySet = new HashSet<AudioReplacementEntry>();
        private long nextContentPackGeneration;
        private long pendingEntryVisitCountForTest;
        private long retainedCallbackWorkCountForTest;
        private long contentProjectionBuildCountForTest;
        private long optionalFileStatusCallCount;
        private long optionalDirectoryStatusCallCount;
        private bool hookInstalled;
        private volatile bool enabledCallbackDemand;
        private volatile bool animalVoiceCallbackDemand;
        private volatile bool paperBoxDiagnosticCallbackDemand;
        private Action<string>? preCommitFaultForTest;
        private Action<ContentRefreshDirtyBatch>? beforeGenerationCompleteForTest;
        private Action<string>? postCommitFaultForTest;
        private Action<string>? publicationSideEffectFaultForTest;

        public AudioReplacementService(DtmApiRuntime runtime)
            : this(runtime, null)
        {
        }

        internal AudioReplacementService(DtmApiRuntime runtime, Func<string, string, object?>? readyBackendFactoryForTest)
        {
            this.runtime = runtime;
            this.readyBackendFactoryForTest = readyBackendFactoryForTest;
        }

        internal void SetHookInstalled(bool installed)
        {
            hookInstalled = installed;
            UpdateAllOwnerStates("hook-installed=" + installed.ToString(CultureInfo.InvariantCulture));
        }

        internal string GetAudioReplacementLifecycleSummary()
        {
            int loadStarted = 0;
            int readyEntries = 0;
            int audioClips = 0;
            int pendingRequests = 0;
            int asyncOperations = 0;
            int callbackOwners = 0;
            int platformPlayers = 0;
            foreach (AudioReplacementEntry entry in entries.Values)
            {
                if (entry.LoadStarted) loadStarted++;
                if (entry.IsReady) readyEntries++;
                if (entry.AudioClip != null) audioClips++;
                if (entry.Request != null) pendingRequests++;
                if (entry.AsyncOperation != null) asyncOperations++;
                if (entry.AudioCallbackOwner != null) callbackOwners++;
                if (entry.PlatformAudioPlayer != null) platformPlayers++;
            }

            return "entries=" + entries.Count.ToString(CultureInfo.InvariantCulture) +
                ", states=" + states.Count.ToString(CultureInfo.InvariantCulture) +
                ", contentPackEntries=" + contentPackEntryKeys.Count.ToString(CultureInfo.InvariantCulture) +
                ", contentPackOwners=" + contentPackOwnerGenerations.Count.ToString(CultureInfo.InvariantCulture) +
                ", loadStarted=" + loadStarted.ToString(CultureInfo.InvariantCulture) +
                ", readyEntries=" + readyEntries.ToString(CultureInfo.InvariantCulture) +
                ", audioClips=" + audioClips.ToString(CultureInfo.InvariantCulture) +
                ", pendingRequests=" + pendingRequests.ToString(CultureInfo.InvariantCulture) +
                ", asyncOperations=" + asyncOperations.ToString(CultureInfo.InvariantCulture) +
                ", callbackOwners=" + callbackOwners.ToString(CultureInfo.InvariantCulture) +
                ", platformPlayers=" + platformPlayers.ToString(CultureInfo.InvariantCulture) +
                ", pendingUpdaterEntries=" + pendingEntries.Count.ToString(CultureInfo.InvariantCulture) +
                ", animalContexts=" + animalSoundContexts.Count.ToString(CultureInfo.InvariantCulture) +
                ", hookInstalled=" + hookInstalled.ToString(CultureInfo.InvariantCulture);
        }

        public AudioReplacementRegisterResult RegisterReplacement(IManifest owner, AudioReplacementOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            AudioReplacementOptions normalized = NormalizeOptions(options);
            string replacementId = normalized.ReplacementId;
            string key = MakeKey(owner.UniqueID, replacementId);

            if (!ReviewedSimpleSfxEvents.Contains(normalized.NativeSoundEvent))
            {
                string message = "Native sound event is not reviewed for audio replacement: " + normalized.NativeSoundEvent + ".";
                runtime.RuntimeMonitor.Log("AudioReplacement register rejected owner=" + owner.UniqueID + " replacement=" + replacementId + " event=" + normalized.NativeSoundEvent + " reason=unsupported-event");
                return new AudioReplacementRegisterResult
                {
                    Success = false,
                    OwnerId = owner.UniqueID,
                    ReplacementId = replacementId,
                    NativeSoundEvent = normalized.NativeSoundEvent,
                    Enabled = normalized.Enabled,
                    HookInstalled = hookInstalled,
                    FailureReason = message,
                    Message = message
                };
            }

            if (normalized.Enabled && normalized.SuppressNativeWhenReady)
            {
                var candidate = new AudioReplacementEntry(owner.UniqueID, normalized, SimpleSfxCategory, string.Empty, string.Empty, CodeModSource);
                AudioReplacementEntry? conflict = FindSuppressingConflict(candidate, entries.Values.Where(e => !string.Equals(MakeKey(e.OwnerId, e.Options.ReplacementId), key, StringComparison.OrdinalIgnoreCase)));
                if (conflict != null)
                {
                    string message = "Suppressing replacement already registered for " + DescribeConflictScope(candidate) + " by " + conflict.OwnerId + "/" + conflict.Options.ReplacementId + ".";
                    runtime.RuntimeMonitor.Log("AudioReplacement register rejected owner=" + owner.UniqueID + " replacement=" + replacementId + " event=" + normalized.NativeSoundEvent + " reason=conflict existingOwner=" + conflict.OwnerId + " existingReplacement=" + conflict.Options.ReplacementId);
                    return new AudioReplacementRegisterResult
                    {
                        Success = false,
                        OwnerId = owner.UniqueID,
                        ReplacementId = replacementId,
                        NativeSoundEvent = normalized.NativeSoundEvent,
                        Enabled = normalized.Enabled,
                        HookInstalled = hookInstalled,
                        FailureReason = message,
                        Message = message
                    };
                }
            }

            if (entries.TryGetValue(key, out AudioReplacementEntry? previous))
                CleanupEntry(previous);

            var entry = new AudioReplacementEntry(owner.UniqueID, normalized, SimpleSfxCategory, string.Empty, string.Empty, CodeModSource);
            entries[key] = entry;
            RefreshCallbackDemandFlags();

            if (normalized.Enabled)
                EnsureLoadStarted(entry);

            if (string.IsNullOrWhiteSpace(entry.LoadFailureReason))
                UpdateOwnerState(owner.UniqueID, "registered replacement=" + replacementId + " event=" + normalized.NativeSoundEvent + " hook=" + hookInstalled.ToString(CultureInfo.InvariantCulture));
            AudioReplacementState state = GetOwnerState(owner.UniqueID);
            var result = new AudioReplacementRegisterResult
            {
                Success = string.IsNullOrWhiteSpace(entry.LoadFailureReason),
                OwnerId = owner.UniqueID,
                ReplacementId = replacementId,
                NativeSoundEvent = normalized.NativeSoundEvent,
                Enabled = normalized.Enabled,
                HookInstalled = hookInstalled,
                PreloadReady = entry.IsReady,
                FailureReason = entry.LoadFailureReason,
                Message = string.IsNullOrWhiteSpace(entry.LoadFailureReason) ? state.LastMessage : entry.LoadFailureReason
            };

            runtime.RuntimeMonitor.Log("AudioReplacement register owner=" + owner.UniqueID +
                " replacement=" + replacementId +
                " event=" + normalized.NativeSoundEvent +
                " enabled=" + normalized.Enabled +
                " hook=" + hookInstalled +
                " preload=" + entry.LoadStatus +
                " failure=" + entry.LoadFailureReason +
                " path=" + normalized.AudioPath);
            ReconcileCodeRegistrationDemand(owner.UniqueID, "dynamic audio registration");
            ReconcilePendingDemand("dynamic audio registration");
            return result;
        }

        public AudioReplacementState GetState(string uniqueId)
        {
            return CloneState(GetOwnerState(uniqueId ?? string.Empty));
        }

        public BridgeFeatureStatus GetStatus(string uniqueId)
        {
            AudioReplacementState state = GetOwnerState(uniqueId ?? string.Empty);
            return new BridgeFeatureStatus(state.Status, state.LastMessage);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            KeyValuePair<string, AudioReplacementEntry>[] ownedEntries = entries
                .Where(pair => pair.Value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (KeyValuePair<string, AudioReplacementEntry> pair in ownedEntries)
            {
                CleanupEntry(pair.Value);
                entries.Remove(pair.Key);
                contentPackEntryKeys.Remove(pair.Key);
            }
            contentPackOwnerGenerations.Remove(ownerId);
            bool removedState = states.Remove(ownerId);
            if (ownedEntries.Length > 0)
                animalSoundContexts.Clear();
            RefreshCallbackDemandFlags();
            ReconcileCodeRegistrationDemand(ownerId, "audio owner cleanup " + (reason ?? string.Empty));
            ReconcilePendingDemand("audio owner cleanup " + (reason ?? string.Empty));
            return ownedEntries.Length + (removedState ? 1 : 0);
        }

        private void ReconcileCodeRegistrationDemand(string ownerId, string reason)
        {
            bool enabled = entries.Values.Any(entry =>
                entry.OwnerId.Equals(ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                entry.Options.Enabled);
            bool animalVoice = entries.Values.Any(entry =>
                entry.OwnerId.Equals(ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                entry.Options.Enabled &&
                string.Equals(entry.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase));
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AudioReplacement, ownerId, RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "enabled-definitions", enabled, reason);
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext, ownerId, RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "animal-voice-definitions", animalVoice, reason);
        }

        private void ReconcilePendingDemand(string reason)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AudioReplacementPending, GameBridgeDemandRoutes.OperationOwner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "pending-load-or-context", HasPendingRuntimeWork, reason);
        }

        private void RefreshCallbackDemandFlags()
        {
            bool enabled = false;
            bool animalVoice = false;
            bool paperBox = false;
            foreach (AudioReplacementEntry entry in entries.Values)
            {
                if (!entry.Options.Enabled)
                    continue;
                enabled = true;
                if (string.Equals(entry.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
                    animalVoice = true;
                if (string.Equals(entry.Options.NativeSoundEvent, "PLAY_RESOURCE_PAPER_BOX", StringComparison.OrdinalIgnoreCase))
                    paperBox = true;
            }

            enabledCallbackDemand = enabled;
            animalVoiceCallbackDemand = animalVoice;
            paperBoxDiagnosticCallbackDemand = paperBox;
            if (!animalVoice && animalSoundContexts.Count > 0)
                animalSoundContexts.Clear();
        }

        private void RecordPostCommitSideEffectFailure(string ownerId, long generation, Exception exception)
        {
            try
            {
                runtime.Diagnostics.RecordError(
                    ownerId ?? "DTMAPI.GameBridge.AudioReplacement",
                    "Audio replacement generation committed, but a post-commit side effect failed; visible state and terminal receipt remain committed. generation=" + generation.ToString(CultureInfo.InvariantCulture) + ".",
                    exception.ToString());
            }
            catch
            {
            }
        }

        private void ReconcilePublishedDemandBestEffort(
            string ownerId,
            long generation,
            string ownerReason,
            string pendingReason)
        {
            try
            {
                ReconcileCodeRegistrationDemand(ownerId, ownerReason);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, generation, ex);
            }

            try
            {
                ReconcilePendingDemand(pendingReason);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, generation, ex);
            }
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return entries.Values.Count(entry => entry.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) +
                (states.ContainsKey(ownerId) ? 1 : 0);
        }

        internal void Update()
        {
            ClearStaleAnimalSoundContext();

            DateTimeOffset now = DateTimeOffset.UtcNow;
            int index = 0;
            while (index < pendingEntries.Count)
            {
                AudioReplacementEntry entry = pendingEntries[index];
                pendingEntryVisitCountForTest++;
                string key = MakeKey(entry.OwnerId, entry.Options.ReplacementId);
                if (!entries.TryGetValue(key, out AudioReplacementEntry? current) || !ReferenceEquals(current, entry))
                {
                    RemovePendingEntryAt(index);
                    continue;
                }

                if (entry.Options.Enabled &&
                    !entry.IsReady &&
                    (string.Equals(entry.LoadStatus, "pending", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.LoadStatus, "retry", StringComparison.OrdinalIgnoreCase)) &&
                    now - entry.LastLoadAttemptAtUtc >= TimeSpan.FromSeconds(3))
                {
                    EnsureLoadStarted(entry);
                }

                PollLoad(entry);
                if (!ShouldRemainPending(entry))
                {
                    RemovePendingEntryAt(index);
                    continue;
                }
                index++;
            }
        }

        internal int PendingEntryCountForTest => pendingEntries.Count;
        internal long PendingEntryVisitCountForTest => pendingEntryVisitCountForTest;
        internal long RetainedCallbackWorkCountForTest => retainedCallbackWorkCountForTest;
        internal long ContentProjectionBuildCountForTest => contentProjectionBuildCountForTest;

        internal long OptionalFileStatusCallCount => optionalFileStatusCallCount + optionalDirectoryStatusCallCount;
        internal Action<string>? PostCommitFaultForTest
        {
            get => postCommitFaultForTest;
            set => postCommitFaultForTest = value;
        }
        internal Action<string>? PreCommitFaultForTest
        {
            get => preCommitFaultForTest;
            set => preCommitFaultForTest = value;
        }
        internal Action<ContentRefreshDirtyBatch>? BeforeGenerationCompleteForTest
        {
            get => beforeGenerationCompleteForTest;
            set => beforeGenerationCompleteForTest = value;
        }
        internal Action<string>? PublicationSideEffectFaultForTest
        {
            get => publicationSideEffectFaultForTest;
            set => publicationSideEffectFaultForTest = value;
        }
        internal long GetOwnerContentPackGenerationForTest(string ownerId)
        {
            return contentPackOwnerGenerations.TryGetValue(ownerId ?? string.Empty, out long generation) ? generation : 0;
        }
        internal bool HasPendingRuntimeWork => pendingEntries.Count > 0 || animalSoundContexts.Count > 0;
        internal bool HasEnabledDefinitions => enabledCallbackDemand;

        internal bool HasEnabledAnimalVoiceDefinitions => animalVoiceCallbackDemand;

        internal bool HasEnabledPaperBoxDiagnosticDefinitions => paperBoxDiagnosticCallbackDemand;

        internal void ClearSaveLifetimeState(string reason)
        {
            int cleared = animalSoundContexts.Count;
            AnimalSoundContext[] contexts = animalSoundContexts.ToArray();
            if (runtime.ResourceLifecycleCleanupEnabled && cleared > 0)
            {
                foreach (AnimalSoundContext context in contexts)
                {
                    runtime.ReleaseResourceLifecycle(
                        "AnimalSoundContext",
                        context.SpeciesId + ":" + context.Stage + ":" + context.ExpectedNativeSoundEvent,
                        "DTMAPI.GameBridge.AudioReplacement",
                        string.Empty,
                        ResourceLifetime.SaveLifetime,
                        ResourceOwnership.DtmapiOwned,
                        "clear-on-save-boundary",
                        ResourceLifecycleStatus.Cleaned);
                }

                animalSoundContexts.Clear();
            }

            runtime.ObserveResourceCleanup(
                "AudioReplacement",
                reason ?? string.Empty,
                runtime.ResourceLifecycleCleanupEnabled ? cleared : 0,
                "state=AnimalSoundContextStack; requested=" + cleared + "; cleanupEnabled=" + runtime.ResourceLifecycleCleanupEnabled.ToString(CultureInfo.InvariantCulture));
            if (runtime.ResourceLifecycleCleanupEnabled && cleared > 0)
                runtime.RuntimeMonitor.Log("AudioReplacement cleared SaveLifetime AnimalVoice contexts reason=" + (reason ?? string.Empty) + " count=" + cleared + ".");
        }

        internal void RefreshContentPackDefinitions(string reason, bool force)
        {
            if (force)
            {
                runtime.ContentRefreshGenerations.MarkDirty(
                    ContentRefreshDomains.AudioReplacement,
                    runtime.LoadedMods.Select(mod => mod.Manifest.UniqueID),
                    reason ?? "forced audio replacement refresh");
            }

            if (!runtime.ContentRefreshGenerations.TryGetDirty(ContentRefreshDomains.AudioReplacement, out ContentRefreshDirtyBatch dirtyBatch))
                return;

            bool generationCompleted = false;
            bool visibleSnapshotCommitted = false;
            var stagedEntries = new List<AudioReplacementEntry>();
            try
            {
                IReadOnlyList<DiscoveredMod> loadedMods = runtime.LoadedMods;
                contentProjectionBuildCountForTest++;
                DiscoveredMod[] enabledContentPacks = loadedMods
                    .Where(IsEnabledContentPack)
                    .OrderBy(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(mod => mod.RootPath, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var enabledByOwner = enabledContentPacks
                    .GroupBy(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
                var targetOwners = new HashSet<string>(dirtyBatch.OwnerIds.Where(owner => !string.Equals(owner, "all", StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);
                bool refreshAll = dirtyBatch.OwnerIds.Count == 0 || dirtyBatch.OwnerIds.Any(owner => string.Equals(owner, "all", StringComparison.OrdinalIgnoreCase));
                if (refreshAll)
                {
                    foreach (string ownerId in enabledByOwner.Keys)
                        targetOwners.Add(ownerId);
                    foreach (string ownerId in contentPackOwnerGenerations.Keys)
                        targetOwners.Add(ownerId);
                }

                var nextEntries = new Dictionary<string, AudioReplacementEntry>(entries, StringComparer.OrdinalIgnoreCase);
                var nextContentPackKeys = new HashSet<string>(contentPackEntryKeys, StringComparer.OrdinalIgnoreCase);
                var nextOwnerGenerations = new Dictionary<string, long>(contentPackOwnerGenerations, StringComparer.OrdinalIgnoreCase);
                long nextGeneration = nextContentPackGeneration;
                var completions = new ContentRefreshCompletionCollector();
                var ownerPublications = new List<AudioReplacementOwnerPreparation>();
                var removedPublications = new List<AudioReplacementOwnerRemoval>();
                int committed = 0;
                int retained = 0;

                foreach (string ownerId in targetOwners.OrderBy(owner => owner, StringComparer.OrdinalIgnoreCase))
                {
                    nextOwnerGenerations.TryGetValue(ownerId, out long previousGeneration);
                    if (!enabledByOwner.TryGetValue(ownerId, out DiscoveredMod? mod))
                    {
                        AudioReplacementOwnerRemoval removal = StageOwnerRemoval(
                            ownerId,
                            "content pack disabled or unloaded during " + (reason ?? string.Empty),
                            nextEntries,
                            nextContentPackKeys,
                            nextOwnerGenerations);
                        removedPublications.Add(removal);
                        completions.Add(new ContentRefreshCompletion(ownerId, ContentRefreshCompletionStatus.Removed, previousGeneration, 0, 0, "entriesRemoved=" + removal.PreviousEntries.Count));
                        continue;
                    }

                    string schemaPath = Path.Combine(mod.RootPath, "Content", "DTMAPI", "audio-replacements.json");
                    optionalFileStatusCallCount++;
                    if (!File.Exists(schemaPath) && previousGeneration == 0)
                    {
                        completions.Add(new ContentRefreshCompletion(ownerId, ContentRefreshCompletionStatus.Unchanged, 0, 0, 0, "No Audio replacement schema is declared."));
                        continue;
                    }

                    long proposedGeneration = checked(nextGeneration + 1);
                    AudioReplacementOwnerPreparation preparation = PrepareContentPackOwner(
                        ownerId,
                        mod.RootPath,
                        reason ?? string.Empty,
                        string.Empty,
                        proposedGeneration,
                        nextEntries,
                        nextContentPackKeys,
                        nextOwnerGenerations);
                    ownerPublications.Add(preparation);
                    if (preparation.Result.Success)
                    {
                        stagedEntries.AddRange(preparation.Candidates);
                        nextGeneration = proposedGeneration;
                        ApplyPreparedOwner(preparation, nextEntries, nextContentPackKeys, nextOwnerGenerations);
                        committed++;
                    }
                    else if (preparation.Result.RetainedPreviousGeneration)
                    {
                        retained++;
                    }

                    completions.Add(new ContentRefreshCompletion(
                        ownerId,
                        preparation.Result.Success ? ContentRefreshCompletionStatus.Committed : ContentRefreshCompletionStatus.Rejected,
                        previousGeneration,
                        preparation.Result.Generation,
                        preparation.Result.Generation,
                        "status=" + preparation.Result.Status + "; entries=" + preparation.Result.ActiveEntryCount + "; retained=" + preparation.Result.RetainedPreviousGeneration));
                }

                GetCallbackDemandSnapshot(nextEntries.Values, out bool nextEnabledDemand, out bool nextAnimalVoiceDemand, out bool nextPaperBoxDemand);
                beforeGenerationCompleteForTest?.Invoke(dirtyBatch);
                generationCompleted = runtime.ContentRefreshGenerations.CompleteWithAtomicCommit(
                    dirtyBatch,
                    completions,
                    () =>
                    {
                        preCommitFaultForTest?.Invoke("before-visible-snapshot-swap");
                        entries = nextEntries;
                        contentPackEntryKeys = nextContentPackKeys;
                        contentPackOwnerGenerations = nextOwnerGenerations;
                        nextContentPackGeneration = nextGeneration;
                        enabledCallbackDemand = nextEnabledDemand;
                        animalVoiceCallbackDemand = nextAnimalVoiceDemand;
                        paperBoxDiagnosticCallbackDemand = nextPaperBoxDemand;
                    });
                if (!generationCompleted)
                    throw new InvalidOperationException("Audio replacement content generation completion was rejected as stale.");
                visibleSnapshotCommitted = true;

                if (!nextAnimalVoiceDemand && animalSoundContexts.Count > 0)
                    animalSoundContexts.Clear();
                foreach (AudioReplacementOwnerRemoval removal in removedPublications)
                    PublishOwnerRemovalSideEffects(removal);
                foreach (AudioReplacementOwnerPreparation publication in ownerPublications)
                {
                    if (publication.Result.Success)
                        PublishOwnerCommitSideEffects(publication);
                    else
                        PublishOwnerRejectionSideEffects(publication.Result);
                }
                int activeEntryCount = contentPackEntryKeys.Count;
                try
                {
                    runtime.ObserveResourceRefresh("AudioReplacement", reason ?? string.Empty, ResourceLifecycleStatus.Rebuilt, activeEntryCount);
                    runtime.RuntimeMonitor.Log(
                        "AudioReplacement content-pack refresh reason=" + (reason ?? string.Empty) +
                        " committedOwners=" + committed.ToString(CultureInfo.InvariantCulture) +
                        " retainedOwners=" + retained.ToString(CultureInfo.InvariantCulture) +
                        " activeEntries=" + activeEntryCount.ToString(CultureInfo.InvariantCulture) + ".");
                    runtime.ObserveLifecycleResourceEvent(
                        "AudioReplacement",
                        "ContentPackRefresh",
                        "all",
                        "audio-replacements",
                        "reason=" + (reason ?? string.Empty) + "; dirtyGeneration=" + dirtyBatch.Generation + "; committedOwners=" + committed.ToString(CultureInfo.InvariantCulture) + "; retainedOwners=" + retained.ToString(CultureInfo.InvariantCulture) + "; activeEntries=" + activeEntryCount.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    RecordPostCommitSideEffectFailure("DTMAPI.GameBridge.AudioReplacement", nextGeneration, ex);
                }
                postCommitFaultForTest?.Invoke("terminal-receipt-and-visible-snapshot-committed");
            }
            catch (Exception ex)
            {
                if (!visibleSnapshotCommitted)
                    DisposeStagedEntries(stagedEntries);
                if (!generationCompleted)
                {
                    runtime.ContentRefreshGenerations.AbandonAndRequeue(
                        dirtyBatch,
                        "AudioReplacement consumer failed: " + ex.GetType().Name + ": " + ex.Message);
                }
                throw;
            }
        }

        internal AudioReplacementOwnerReloadResult ReloadContentPackOwner(
            string ownerId,
            string rootPath,
            string reason,
            string expectedTreeSha256 = "")
        {
            var nextEntries = new Dictionary<string, AudioReplacementEntry>(entries, StringComparer.OrdinalIgnoreCase);
            var nextContentPackKeys = new HashSet<string>(contentPackEntryKeys, StringComparer.OrdinalIgnoreCase);
            var nextOwnerGenerations = new Dictionary<string, long>(contentPackOwnerGenerations, StringComparer.OrdinalIgnoreCase);
            long proposedGeneration = checked(nextContentPackGeneration + 1);
            AudioReplacementOwnerPreparation preparation = PrepareContentPackOwner(
                ownerId,
                rootPath,
                reason,
                expectedTreeSha256,
                proposedGeneration,
                nextEntries,
                nextContentPackKeys,
                nextOwnerGenerations);
            if (!preparation.Result.Success)
            {
                PublishOwnerRejectionSideEffects(preparation.Result);
                return preparation.Result;
            }

            bool nextEnabledDemand;
            bool nextAnimalVoiceDemand;
            bool nextPaperBoxDemand;
            try
            {
                ApplyPreparedOwner(preparation, nextEntries, nextContentPackKeys, nextOwnerGenerations);
                GetCallbackDemandSnapshot(nextEntries.Values, out nextEnabledDemand, out nextAnimalVoiceDemand, out nextPaperBoxDemand);
            }
            catch
            {
                DisposeStagedEntries(preparation.Candidates);
                throw;
            }
            entries = nextEntries;
            contentPackEntryKeys = nextContentPackKeys;
            contentPackOwnerGenerations = nextOwnerGenerations;
            nextContentPackGeneration = proposedGeneration;
            enabledCallbackDemand = nextEnabledDemand;
            animalVoiceCallbackDemand = nextAnimalVoiceDemand;
            paperBoxDiagnosticCallbackDemand = nextPaperBoxDemand;
            if (!nextAnimalVoiceDemand && animalSoundContexts.Count > 0)
                animalSoundContexts.Clear();
            PublishOwnerCommitSideEffects(preparation);
            return preparation.Result;
        }

        private AudioReplacementOwnerPreparation PrepareContentPackOwner(
            string ownerId,
            string rootPath,
            string reason,
            string expectedTreeSha256,
            long proposedGeneration,
            IReadOnlyDictionary<string, AudioReplacementEntry> candidateEntries,
            IReadOnlyCollection<string> candidateContentPackKeys,
            IReadOnlyDictionary<string, long> candidateOwnerGenerations)
        {
            ownerId = (ownerId ?? string.Empty).Trim();
            reason = BoundDiagnostic(reason ?? string.Empty);
            if (ownerId.Length == 0)
                return RejectOwnerPreparation(ownerId, "invalid-owner", "Audio replacement reload requires a non-empty UniqueID.", reason, candidateEntries, candidateOwnerGenerations);
            if (string.IsNullOrWhiteSpace(rootPath))
                return RejectOwnerPreparation(ownerId, "invalid-root", "Content root is empty.", reason, candidateEntries, candidateOwnerGenerations);

            string canonicalRoot;
            string schemaPath;
            try
            {
                canonicalRoot = Path.GetFullPath(rootPath ?? string.Empty);
                optionalDirectoryStatusCallCount++;
                if (!Directory.Exists(canonicalRoot))
                    return RejectOwnerPreparation(ownerId, "invalid-root", "Content root does not exist: " + canonicalRoot, reason, candidateEntries, candidateOwnerGenerations);
                schemaPath = Path.Combine(canonicalRoot, "Content", "DTMAPI", "audio-replacements.json");
            }
            catch (Exception ex)
            {
                return RejectOwnerPreparation(ownerId, "invalid-root", "Content root is invalid: " + ex.GetType().Name + ": " + ex.Message, reason, candidateEntries, candidateOwnerGenerations);
            }

            AudioReplacementDefinitionModel[] definitions;
            try
            {
                optionalFileStatusCallCount++;
                if (!File.Exists(schemaPath))
                    return RejectOwnerPreparation(ownerId, "missing-format", "Content/DTMAPI/audio-replacements.json does not exist.", reason, candidateEntries, candidateOwnerGenerations);

                string json = File.ReadAllText(schemaPath, Encoding.UTF8);
                definitions = ReadContentPackDefinitions(json);
            }
            catch (Exception ex)
            {
                return RejectOwnerPreparation(ownerId, "invalid-json", "Failed to read audio-replacements.json: " + ex.GetType().Name + ": " + ex.Message, reason, candidateEntries, candidateOwnerGenerations);
            }

            var warnings = new List<string>();
            AudioReplacementEntry[] candidates;
            try
            {
                candidates = BuildContentPackEntries(
                    ownerId,
                    canonicalRoot,
                    definitions,
                    warnings,
                    () => optionalFileStatusCallCount++).ToArray();
            }
            catch (Exception ex)
            {
                warnings.Add("definition build failed: " + ex.GetType().Name + ": " + ex.Message);
                candidates = Array.Empty<AudioReplacementEntry>();
            }

            string[] duplicateKeys = candidates
                .GroupBy(entry => MakeKey(entry.OwnerId, entry.Options.ReplacementId), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateKeys.Length > 0)
                warnings.Add("duplicate replacement ids: " + string.Join(",", duplicateKeys));

            var candidateByKey = new Dictionary<string, AudioReplacementEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (AudioReplacementEntry candidate in candidates)
            {
                string key = MakeKey(candidate.OwnerId, candidate.Options.ReplacementId);
                if (!candidateByKey.ContainsKey(key))
                    candidateByKey[key] = candidate;
            }

            AudioReplacementEntry[] otherEntries = candidateEntries.Values
                .Where(entry => !(string.Equals(entry.Source, ContentPackSource, StringComparison.OrdinalIgnoreCase) && string.Equals(entry.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            var acceptedCandidates = new List<AudioReplacementEntry>();
            foreach (AudioReplacementEntry candidate in candidateByKey.Values.OrderBy(entry => entry.Options.ReplacementId, StringComparer.OrdinalIgnoreCase))
            {
                string candidateKey = MakeKey(candidate.OwnerId, candidate.Options.ReplacementId);
                if (candidateEntries.TryGetValue(candidateKey, out AudioReplacementEntry? sameKeyEntry) &&
                    !(string.Equals(sameKeyEntry.Source, ContentPackSource, StringComparison.OrdinalIgnoreCase) && string.Equals(sameKeyEntry.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)))
                {
                    warnings.Add("replacement=" + candidate.Options.ReplacementId + " conflicts with loaded " + sameKeyEntry.Source + " replacement using the same owner/id key");
                    continue;
                }

                AudioReplacementEntry? conflict = FindSuppressingConflict(candidate, otherEntries.Concat(acceptedCandidates));
                if (conflict != null)
                {
                    warnings.Add("replacement=" + candidate.Options.ReplacementId + " conflicts scope=" + DescribeConflictScope(candidate) + " existingOwner=" + conflict.OwnerId + " existingReplacement=" + conflict.Options.ReplacementId);
                    continue;
                }

                acceptedCandidates.Add(candidate);
            }

            if (warnings.Count > 0 || acceptedCandidates.Count != candidates.Length)
            {
                DisposeStagedEntries(acceptedCandidates);
                return RejectOwnerPreparation(ownerId, "invalid-generation", FormatBoundedDiagnostics(warnings), reason, candidateEntries, candidateOwnerGenerations);
            }

            foreach (AudioReplacementEntry candidate in acceptedCandidates)
            {
                if (!TryPrepareEntryForAtomicSwap(candidate, out string failure))
                {
                    DisposeStagedEntries(acceptedCandidates);
                    return RejectOwnerPreparation(ownerId, "wav-unavailable", "replacement=" + candidate.Options.ReplacementId + " rejected: " + failure, reason, candidateEntries, candidateOwnerGenerations);
                }

                if (candidate.Options.Enabled && !candidate.IsReady)
                {
                    DisposeStagedEntries(acceptedCandidates);
                    return RejectOwnerPreparation(ownerId, "wav-unavailable", "replacement=" + candidate.Options.ReplacementId + " did not produce a ready playback backend.", reason, candidateEntries, candidateOwnerGenerations);
                }
            }

            if (!string.IsNullOrWhiteSpace(expectedTreeSha256))
            {
                string commitTreeSha256;
                try
                {
                    commitTreeSha256 = AuthorFileTreeDigest.Compute(canonicalRoot);
                }
                catch (Exception ex)
                {
                    DisposeStagedEntries(acceptedCandidates);
                    return RejectOwnerPreparation(ownerId, "source-tree-unreadable", "The source tree could not be rehashed before commit: " + ex.GetType().Name + ": " + ex.Message, reason, candidateEntries, candidateOwnerGenerations);
                }

                if (!string.Equals(commitTreeSha256, expectedTreeSha256, StringComparison.OrdinalIgnoreCase))
                {
                    DisposeStagedEntries(acceptedCandidates);
                    return RejectOwnerPreparation(
                        ownerId,
                        "source-tree-hash-mismatch",
                        "The source tree changed while the Audio generation was staged; expected=" + expectedTreeSha256 + "; actual=" + commitTreeSha256 + ".",
                        reason,
                        candidateEntries,
                        candidateOwnerGenerations);
                }
            }

            KeyValuePair<string, AudioReplacementEntry>[] previous = candidateEntries
                .Where(pair => candidateContentPackKeys.Contains(pair.Key) && string.Equals(pair.Value.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            string message = "Audio replacement owner generation committed owner=" + ownerId +
                " generation=" + proposedGeneration.ToString(CultureInfo.InvariantCulture) +
                " entries=" + acceptedCandidates.Count.ToString(CultureInfo.InvariantCulture) +
                " reason=" + reason + ".";
            return new AudioReplacementOwnerPreparation(
                AudioReplacementOwnerReloadResult.Committed(ownerId, proposedGeneration, acceptedCandidates.Count, message),
                previous,
                acceptedCandidates.ToArray(),
                reason);
        }

        private static AudioReplacementOwnerPreparation RejectOwnerPreparation(
            string ownerId,
            string status,
            string failure,
            string reason,
            IReadOnlyDictionary<string, AudioReplacementEntry> candidateEntries,
            IReadOnlyDictionary<string, long> candidateOwnerGenerations)
        {
            ownerId = ownerId ?? string.Empty;
            failure = BoundDiagnostic(failure);
            bool retained = candidateOwnerGenerations.TryGetValue(ownerId, out long generation);
            int activeEntries = candidateEntries.Values.Count(entry =>
                string.Equals(entry.Source, ContentPackSource, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase));
            string message = "Audio replacement owner generation rejected owner=" + ownerId +
                " status=" + status +
                " retained=" + retained.ToString(CultureInfo.InvariantCulture) +
                " generation=" + generation.ToString(CultureInfo.InvariantCulture) +
                " entries=" + activeEntries.ToString(CultureInfo.InvariantCulture) +
                " reason=" + reason +
                " failure=" + failure + ".";
            return new AudioReplacementOwnerPreparation(
                AudioReplacementOwnerReloadResult.Rejected(ownerId, status, generation, activeEntries, retained, message),
                Array.Empty<KeyValuePair<string, AudioReplacementEntry>>(),
                Array.Empty<AudioReplacementEntry>(),
                reason);
        }

        private static void ApplyPreparedOwner(
            AudioReplacementOwnerPreparation preparation,
            IDictionary<string, AudioReplacementEntry> candidateEntries,
            ISet<string> candidateContentPackKeys,
            IDictionary<string, long> candidateOwnerGenerations)
        {
            foreach (KeyValuePair<string, AudioReplacementEntry> pair in preparation.PreviousEntries)
            {
                candidateEntries.Remove(pair.Key);
                candidateContentPackKeys.Remove(pair.Key);
            }
            foreach (AudioReplacementEntry candidate in preparation.Candidates)
            {
                string key = MakeKey(candidate.OwnerId, candidate.Options.ReplacementId);
                candidateEntries[key] = candidate;
                candidateContentPackKeys.Add(key);
            }
            candidateOwnerGenerations[preparation.Result.OwnerId] = preparation.Result.Generation;
        }

        private static AudioReplacementOwnerRemoval StageOwnerRemoval(
            string ownerId,
            string reason,
            IDictionary<string, AudioReplacementEntry> candidateEntries,
            ISet<string> candidateContentPackKeys,
            IDictionary<string, long> candidateOwnerGenerations)
        {
            KeyValuePair<string, AudioReplacementEntry>[] previous = candidateEntries
                .Where(pair => candidateContentPackKeys.Contains(pair.Key) && string.Equals(pair.Value.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (KeyValuePair<string, AudioReplacementEntry> pair in previous)
            {
                candidateEntries.Remove(pair.Key);
                candidateContentPackKeys.Remove(pair.Key);
            }
            candidateOwnerGenerations.Remove(ownerId);
            return new AudioReplacementOwnerRemoval(ownerId, previous, reason);
        }

        private static void GetCallbackDemandSnapshot(
            IEnumerable<AudioReplacementEntry> candidateEntries,
            out bool enabled,
            out bool animalVoice,
            out bool paperBox)
        {
            enabled = false;
            animalVoice = false;
            paperBox = false;
            foreach (AudioReplacementEntry entry in candidateEntries)
            {
                if (!entry.Options.Enabled)
                    continue;
                enabled = true;
                if (string.Equals(entry.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
                    animalVoice = true;
                if (string.Equals(entry.Options.NativeSoundEvent, "PLAY_RESOURCE_PAPER_BOX", StringComparison.OrdinalIgnoreCase))
                    paperBox = true;
            }
        }

        private void PublishOwnerCommitSideEffects(AudioReplacementOwnerPreparation preparation)
        {
            string ownerId = preparation.Result.OwnerId;
            long generation = preparation.Result.Generation;
            foreach (KeyValuePair<string, AudioReplacementEntry> pair in preparation.PreviousEntries)
            {
                try
                {
                    CleanupEntry(pair.Value);
                }
                catch (Exception ex)
                {
                    RecordPostCommitSideEffectFailure(ownerId, generation, ex);
                }
            }

            foreach (AudioReplacementEntry candidate in preparation.Candidates)
            {
                try
                {
                    if (candidate.Options.Enabled)
                        PublishReadyState(candidate);
                    runtime.ObserveResourceLifecycle(
                        "AudioReplacementDefinition",
                        candidate.Options.ReplacementId,
                        candidate.OwnerId,
                        candidate.Options.AudioPath,
                        ResourceLifetime.TitleLifetime,
                        ResourceOwnership.DtmapiOwned,
                        ResourceLifecycleStatus.Declared,
                        "owner-generation=" + generation.ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    RecordPostCommitSideEffectFailure(ownerId, generation, ex);
                }
            }

            try
            {
                publicationSideEffectFaultForTest?.Invoke("owner-commit-before-log-and-lifecycle-publication");
                UpdateOwnerState(ownerId, preparation.Result.Message);
                runtime.RuntimeMonitor.Log(preparation.Result.Message);
                runtime.ObserveLifecycleResourceEvent(
                    "AudioReplacement",
                    "OwnerGenerationCommitted",
                    ownerId,
                    generation.ToString(CultureInfo.InvariantCulture),
                    "entries=" + preparation.Candidates.Count.ToString(CultureInfo.InvariantCulture) + "; reason=" + preparation.Reason);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, generation, ex);
            }
            finally
            {
                ReconcilePublishedDemandBestEffort(
                    ownerId,
                    generation,
                    "audio owner generation committed",
                    "audio owner generation committed");
            }
        }

        private void PublishOwnerRejectionSideEffects(AudioReplacementOwnerReloadResult result)
        {
            try
            {
                publicationSideEffectFaultForTest?.Invoke("owner-rejection-before-log-and-lifecycle-publication");
                runtime.Diagnostics.RecordWarning(result.OwnerId, "Audio replacement generation rejected; last-good generation retained.", result.Message);
                runtime.RuntimeMonitor.Log(result.Message, LogLevel.Warn);
                runtime.ObserveLifecycleResourceEvent(
                    "AudioReplacement",
                    "OwnerGenerationRejected",
                    result.OwnerId,
                    result.Generation.ToString(CultureInfo.InvariantCulture),
                    "status=" + result.Status + "; retained=" + result.RetainedPreviousGeneration.ToString(CultureInfo.InvariantCulture) + "; entries=" + result.ActiveEntryCount.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(result.OwnerId, result.Generation, ex);
            }
            finally
            {
                ReconcilePublishedDemandBestEffort(
                    result.OwnerId,
                    result.Generation,
                    "audio owner generation rejected; preserve last-good demand",
                    "audio owner generation rejected");
            }
        }

        private void PublishOwnerRemovalSideEffects(AudioReplacementOwnerRemoval removal)
        {
            foreach (KeyValuePair<string, AudioReplacementEntry> pair in removal.PreviousEntries)
            {
                try
                {
                    CleanupEntry(pair.Value);
                }
                catch (Exception ex)
                {
                    RecordPostCommitSideEffectFailure(removal.OwnerId, 0, ex);
                }
            }
            try
            {
                publicationSideEffectFaultForTest?.Invoke("owner-removal-before-log-and-lifecycle-publication");
                UpdateOwnerState(removal.OwnerId, "content-pack audio replacement generation removed: " + removal.Reason);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(removal.OwnerId, 0, ex);
            }
            finally
            {
                ReconcilePublishedDemandBestEffort(
                    removal.OwnerId,
                    0,
                    "audio content generation removed",
                    "audio content generation removed");
            }
        }

        internal void BeginAnimalSoundContext(object? animal)
        {
            if (!animalVoiceCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            AnimalSoundContext context = BuildAnimalSoundContext(animal);
            animalSoundContexts.Push(context);
            runtime.ObserveResourceLifecycle(
                "AnimalSoundContext",
                context.SpeciesId + ":" + context.Stage + ":" + context.ExpectedNativeSoundEvent,
                "DTMAPI.GameBridge.AudioReplacement",
                string.Empty,
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "clear-on-save-boundary");
        }

        internal void EndAnimalSoundContext()
        {
            if (!animalVoiceCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            if (animalSoundContexts.Count > 0)
                animalSoundContexts.Pop();
        }

        internal bool HandleNativeSoundEvent(string? eventName, object? emitter, object? eventCallback, bool waitEndOfFrame, ref bool nativeResult)
        {
            if (!enabledCallbackDemand)
                return true;
            retainedCallbackWorkCountForTest++;
            string normalizedEvent = NormalizeEventName(eventName);
            if (normalizedEvent.Length == 0)
                return true;

            AnimalSoundContext? context = GetCurrentAnimalSoundContext();
            AudioReplacementEntry[] candidates = entries.Values
                .Where(e => e.Options.Enabled &&
                    string.Equals(e.Options.NativeSoundEvent, normalizedEvent, StringComparison.OrdinalIgnoreCase) &&
                    IsEntryInScope(e, context))
                .OrderBy(e => e.OwnerId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Options.ReplacementId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (candidates.Length == 0)
                return true;

            if (emitter != null || eventCallback != null)
            {
                foreach (AudioReplacementEntry entry in candidates)
                    RecordEvent(entry, normalizedEvent, played: false, suppressed: false, "native Wwise emitter/callback semantics are not supported by this reviewed 2D replacement path; native sound allowed. " + DescribeEventContext(entry, context) + " emitter=" + (emitter != null) + " callback=" + (eventCallback != null) + " waitEndOfFrame=" + waitEndOfFrame.ToString(CultureInfo.InvariantCulture));
                return true;
            }

            foreach (AudioReplacementEntry entry in candidates)
            {
                if (!entry.IsReady)
                {
                    EnsureLoadStarted(entry);
                    RecordEvent(entry, normalizedEvent, played: false, suppressed: false, "replacement not ready; native sound allowed. " + DescribeEventContext(entry, context) + " loadStatus=" + entry.LoadStatus);
                    continue;
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (IsCooldownActive(entry.Options.CooldownMilliseconds, entry.LastPlayedAtUtc, now))
                {
                    bool suppressDuringCooldown = ShouldSuppressNativeDuringReadyCooldown(entry.IsReady, entry.Options.SuppressNativeWhenReady);
                    RecordEvent(
                        entry,
                        normalizedEvent,
                        played: false,
                        suppressed: suppressDuringCooldown,
                        (suppressDuringCooldown
                            ? "replacement cooldown active; native sound suppressed because replacement is ready. "
                            : "replacement cooldown active; native sound allowed. ") + DescribeEventContext(entry, context));
                    if (suppressDuringCooldown)
                    {
                        nativeResult = true;
                        return false;
                    }

                    continue;
                }

                bool played = TryPlay(entry, out string playMessage);
                bool suppress = played && entry.Options.SuppressNativeWhenReady;
                RecordEvent(entry, normalizedEvent, played, suppress, DescribeEventContext(entry, context) + " " + playMessage);
                if (!played)
                    continue;

                if (suppress)
                {
                    nativeResult = true;
                    return false;
                }

                return true;
            }

            return true;
        }

        internal void RecordPaperBoxInteract(object instance)
        {
            if (!paperBoxDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            runtime.RuntimeMonitor.Log("AudioReplacement paper-box OnInteract owner=DungeonResourceModelPaperBox event=PLAY_RESOURCE_PAPER_BOX instance=" + (instance?.GetType().FullName ?? "null") + ".");
            runtime.SetHookStatus(
                "Audio.PaperBoxNativeOwner",
                "verified",
                "Harmony Postfix: DungeonResourceModelPaperBox.OnInteract",
                "Native paper-box interaction owner ran and posts PLAY_RESOURCE_PAPER_BOX through the Wwise sound-event bridge.");
        }

        private bool IsEntryInScope(AudioReplacementEntry entry, AnimalSoundContext? context)
        {
            return IsEntryInScopeStatic(entry, context);
        }

        private static bool IsEntryInScopeStatic(AudioReplacementEntry entry, AnimalSoundContext? context)
        {
            if (string.Equals(entry.Category, SimpleSfxCategory, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.Equals(entry.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
                return false;
            if (context == null)
                return false;
            if (!string.Equals(entry.SpeciesId, context.SpeciesId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(entry.Options.NativeSoundEvent, context.ExpectedNativeSoundEvent, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(entry.Stage, "any", StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(entry.Stage, context.Stage, StringComparison.OrdinalIgnoreCase);
        }

        private AnimalSoundContext? GetCurrentAnimalSoundContext()
        {
            ClearStaleAnimalSoundContext();
            return animalSoundContexts.Count == 0 ? null : animalSoundContexts.Peek();
        }

        private void ClearStaleAnimalSoundContext()
        {
            if (animalSoundContexts.Count == 0)
                return;
            if (DateTimeOffset.UtcNow - animalSoundContexts.Peek().CreatedAtUtc <= AnimalSoundContextMaxAge)
                return;

            animalSoundContexts.Clear();
            runtime.RuntimeMonitor.Log("AudioReplacement cleared stale Animal.PlayAnimalSound context.", LogLevel.Warn);
        }

        private static AnimalSoundContext BuildAnimalSoundContext(object? animal)
        {
            string species = ReadStringMember(animal, "protoName", "ProtoName");
            object? data = ReadMember(animal, "data", "Data");
            bool isChild = ReadBoolMember(data, "isChild", "IsChild");
            string stage = isChild ? "child" : "adult";
            object? proto = ReadMember(animal, "proto", "Proto");
            string expectedEvent = NormalizeEventName(isChild
                ? ReadStringMember(proto, "SoundEventChild", "soundEventChild")
                : ReadStringMember(proto, "SoundEvent", "soundEvent"));

            return new AnimalSoundContext(species, stage, expectedEvent, DateTimeOffset.UtcNow);
        }

        private static string DescribeEventContext(AudioReplacementEntry entry, AnimalSoundContext? context)
        {
            if (!string.Equals(entry.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
                return "category=" + entry.Category + ".";

            return "category=AnimalVoice species=" + (context?.SpeciesId ?? "none") +
                " stage=" + (context?.Stage ?? "none") +
                " expectedEvent=" + (context?.ExpectedNativeSoundEvent ?? string.Empty) + ".";
        }

        internal static IReadOnlyList<string> BuildContentPackReplacementSummariesForTest(string ownerId, string rootPath, string json)
        {
            var warnings = new List<string>();
            return BuildContentPackEntries(ownerId, rootPath, ReadContentPackDefinitions(json), warnings)
                .Select(e => e.OwnerId + "|" + e.Options.ReplacementId + "|" + e.Category + "|" + e.SpeciesId + "|" + e.Stage + "|" + e.Options.NativeSoundEvent + "|" + e.Options.AudioPath + "|" + e.Options.SuppressNativeWhenReady + "|" + e.Options.CooldownMilliseconds.ToString(CultureInfo.InvariantCulture))
                .ToArray();
        }

        internal static IReadOnlyList<string> BuildContentPackReplacementWarningsForTest(string ownerId, string rootPath, string json)
        {
            var warnings = new List<string>();
            BuildContentPackEntries(ownerId, rootPath, ReadContentPackDefinitions(json), warnings);
            return warnings.ToArray();
        }

        internal static bool MatchesAnimalVoiceScopeForTest(string entrySpeciesId, string entryStage, string entryEvent, string contextSpeciesId, string contextStage, string contextExpectedEvent, string postedEvent)
        {
            var options = new AudioReplacementOptions
            {
                Enabled = true,
                ReplacementId = "unit-test",
                NativeSoundEvent = entryEvent,
                AudioPath = Path.Combine(Path.GetTempPath(), "unit-test.wav"),
                SuppressNativeWhenReady = true,
                Volume = 1,
                CooldownMilliseconds = 0
            };
            var entry = new AudioReplacementEntry("DTMAPI.UnitTests", NormalizeOptions(options), AnimalVoiceCategory, NormalizeId(entrySpeciesId), NormalizeStage(entryStage), ContentPackSource);
            AnimalSoundContext? context = string.IsNullOrWhiteSpace(contextSpeciesId)
                ? null
                : new AnimalSoundContext(NormalizeId(contextSpeciesId), NormalizeStage(contextStage), NormalizeEventName(contextExpectedEvent), DateTimeOffset.UtcNow);
            return string.Equals(entry.Options.NativeSoundEvent, NormalizeEventName(postedEvent), StringComparison.OrdinalIgnoreCase) &&
                IsEntryInScopeStatic(entry, context);
        }

        private static AudioReplacementDefinitionModel[] ReadContentPackDefinitions(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json ?? string.Empty);
            using (var stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(AudioReplacementDefinitionModel[]));
                object? result = serializer.ReadObject(stream);
                if (!(result is AudioReplacementDefinitionModel[] definitions))
                    throw new SerializationException("audio-replacements.json must contain a top-level JSON array.");
                return definitions;
            }
        }

        private static IReadOnlyList<AudioReplacementEntry> BuildContentPackEntries(
            string ownerId,
            string rootPath,
            IEnumerable<AudioReplacementDefinitionModel> definitions,
            List<string> warnings,
            Action? beforeFileStatus = null)
        {
            var result = new List<AudioReplacementEntry>();
            int definitionIndex = 0;
            foreach (AudioReplacementDefinitionModel definition in definitions ?? Array.Empty<AudioReplacementDefinitionModel>())
            {
                int currentDefinitionIndex = definitionIndex++;
                if (definition == null)
                {
                    warnings.Add("replacement definition at index " + currentDefinitionIndex.ToString(CultureInfo.InvariantCulture) + " is null");
                    continue;
                }

                definition.Normalize();
                string category = NormalizeCategory(definition.Category);
                string eventName = NormalizeEventName(definition.NativeSoundEvent);
                string replacementId = string.IsNullOrWhiteSpace(definition.Id) ? eventName : definition.Id;
                if (string.IsNullOrWhiteSpace(replacementId))
                {
                    warnings.Add("replacement id/event missing owner=" + ownerId);
                    continue;
                }

                if (!TryResolvePackPath(rootPath, definition.File, out string audioPath, out string pathFailure))
                {
                    warnings.Add("replacement=" + replacementId + " rejected path=" + pathFailure);
                    continue;
                }

                if (!string.Equals(Path.GetExtension(audioPath), ".wav", StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add("replacement=" + replacementId + " rejected non-wav file=" + definition.File);
                    continue;
                }

                beforeFileStatus?.Invoke();
                if (!File.Exists(audioPath))
                {
                    warnings.Add("replacement=" + replacementId + " rejected missing WAV file=" + audioPath);
                    continue;
                }

                string speciesId = NormalizeId(definition.SpeciesId);
                string stage = NormalizeStage(definition.Stage);
                if (string.Equals(category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(speciesId))
                    {
                        warnings.Add("replacement=" + replacementId + " rejected missing speciesId for AnimalVoice");
                        continue;
                    }

                    if (!IsValidAnimalVoiceStage(stage))
                    {
                        warnings.Add("replacement=" + replacementId + " rejected invalid AnimalVoice stage=" + definition.Stage);
                        continue;
                    }

                    if (!ReviewedAnimalVoiceEvents.Contains(eventName))
                    {
                        warnings.Add("replacement=" + replacementId + " rejected unreviewed AnimalVoice event=" + eventName);
                        continue;
                    }
                }
                else if (string.Equals(category, SimpleSfxCategory, StringComparison.OrdinalIgnoreCase))
                {
                    if (!ReviewedSimpleSfxEvents.Contains(eventName))
                    {
                        warnings.Add("replacement=" + replacementId + " rejected unreviewed SimpleSfx event=" + eventName);
                        continue;
                    }

                    speciesId = string.Empty;
                    stage = string.Empty;
                }
                else
                {
                    warnings.Add("replacement=" + replacementId + " rejected unsupported category=" + definition.Category);
                    continue;
                }

                var options = NormalizeOptions(new AudioReplacementOptions
                {
                    Enabled = definition.Enabled,
                    ReplacementId = replacementId,
                    NativeSoundEvent = eventName,
                    AudioPath = audioPath,
                    SuppressNativeWhenReady = definition.SuppressNativeWhenReady,
                    Volume = definition.Volume <= 0 ? 1 : definition.Volume,
                    CooldownMilliseconds = definition.CooldownMilliseconds,
                    VerboseLogging = definition.VerboseLogging
                });
                result.Add(new AudioReplacementEntry(ownerId, options, category, speciesId, stage, ContentPackSource));
            }

            return result;
        }

        private bool TryPrepareEntryForAtomicSwap(AudioReplacementEntry entry, out string failure)
        {
            failure = string.Empty;
            try
            {
                if (!TryReadPcmWav(entry.Options.AudioPath, out int sampleRate, out int channels, out float[] samples, out string validationMessage))
                {
                    failure = validationMessage;
                    return false;
                }

                if (sampleRate <= 0 || channels <= 0 || samples.Length == 0 || samples.Length % channels != 0)
                {
                    failure = "Invalid PCM WAV metadata. sampleRate=" + sampleRate.ToString(CultureInfo.InvariantCulture) +
                        " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                        " samples=" + samples.Length.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                if (!entry.Options.Enabled)
                {
                    entry.LoadStarted = false;
                    entry.LoadStatus = "disabled";
                    entry.LoadFailureReason = string.Empty;
                    entry.LastMessage = "Disabled WAV validated for owner-generation swap: " + entry.Options.AudioPath + ". " + validationMessage;
                    return true;
                }

                if (readyBackendFactoryForTest != null)
                {
                    object? testBackend = readyBackendFactoryForTest(entry.Options.AudioPath, entry.Options.ReplacementId);
                    if (testBackend == null)
                    {
                        failure = "Injected test backend rejected the validated WAV.";
                        return false;
                    }

                    SetReadyState(entry, clip: null, callbackOwner: null, platformPlayer: testBackend, "test-ready", validationMessage);
                    return true;
                }

                if (TryCreatePcmWavClip(entry.Options.AudioPath, entry.Options.ReplacementId, out object? clip, out object? callbackOwner, out string unityMessage))
                {
                    SetReadyState(entry, clip, callbackOwner, platformPlayer: null, "unity-pcm", unityMessage);
                    return true;
                }

                if (TryCreatePlatformWavPlayer(entry.Options.AudioPath, out object? platformPlayer, out string platformMessage))
                {
                    SetReadyState(entry, clip: null, callbackOwner: null, platformPlayer, "platform", unityMessage + "; " + platformMessage);
                    return true;
                }

                failure = unityMessage + "; platform fallback unavailable: " + platformMessage;
                return false;
            }
            catch (Exception ex)
            {
                failure = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static void DisposeStagedEntries(IEnumerable<AudioReplacementEntry> stagedEntries)
        {
            foreach (AudioReplacementEntry entry in stagedEntries ?? Array.Empty<AudioReplacementEntry>())
            {
                DisposeRequest(entry);
                DestroyUnityObject(entry.AudioClip, 0f);
                entry.AudioClip = null;
                entry.AudioCallbackOwner = null;
                DisposePlatformPlayer(entry.PlatformAudioPlayer);
                entry.PlatformAudioPlayer = null;
            }
        }

        private static bool ShouldRemainPending(AudioReplacementEntry entry)
        {
            if (!entry.Options.Enabled || entry.IsReady)
                return false;
            return string.Equals(entry.LoadStatus, "pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.LoadStatus, "retry", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.LoadStatus, "loading", StringComparison.OrdinalIgnoreCase);
        }

        private void SynchronizePendingEntry(AudioReplacementEntry entry)
        {
            if (ShouldRemainPending(entry) && pendingEntrySet.Add(entry))
                pendingEntries.Add(entry);
        }

        private void RemovePendingEntry(AudioReplacementEntry entry)
        {
            if (!pendingEntrySet.Remove(entry))
                return;
            int index = pendingEntries.IndexOf(entry);
            if (index >= 0)
                pendingEntries.RemoveAt(index);
        }

        private void RemovePendingEntryAt(int index)
        {
            AudioReplacementEntry entry = pendingEntries[index];
            pendingEntries.RemoveAt(index);
            pendingEntrySet.Remove(entry);
        }

        private void EnsureLoadStarted(AudioReplacementEntry entry)
        {
            EnsureLoadStartedCore(entry);
            SynchronizePendingEntry(entry);
        }

        private void EnsureLoadStartedCore(AudioReplacementEntry entry)
        {
            if (entry.LoadStarted || entry.IsReady)
                return;

            optionalFileStatusCallCount++;
            if (!File.Exists(entry.Options.AudioPath))
            {
                entry.LoadStatus = "failed";
                entry.LoadFailureReason = "Audio file missing: " + entry.Options.AudioPath;
                UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                return;
            }

            try
            {
                entry.LastLoadAttemptAtUtc = DateTimeOffset.UtcNow;
                entry.LoadStarted = true;
                entry.LoadStatus = "loading";
                if (TryStartUnityAudioClipRequest(entry, out string requestMessage))
                {
                    entry.LastMessage = requestMessage;
                    UpdateOwnerState(entry.OwnerId, requestMessage);
                    runtime.RuntimeMonitor.Log("AudioReplacement local WAV request started owner=" + entry.OwnerId +
                        " replacement=" + entry.Options.ReplacementId +
                        " event=" + entry.Options.NativeSoundEvent +
                        " path=" + entry.Options.AudioPath +
                        " message=" + requestMessage);
                    runtime.ObserveLifecycleResourceEvent(
                        "AudioReplacement",
                        "LocalWavRequestStarted",
                        entry.OwnerId,
                        entry.Options.ReplacementId,
                        "event=" + entry.Options.NativeSoundEvent);
                    runtime.ObserveResourceLifecycle(
                        "WavRequest",
                        entry.Options.ReplacementId,
                        entry.OwnerId,
                        entry.Options.AudioPath,
                        ResourceLifetime.TitleLifetime,
                        ResourceOwnership.DtmapiOwned,
                        ResourceLifecycleStatus.Acquired,
                        "dispose-on-content-generation-replacement");
                    return;
                }

                runtime.RuntimeMonitor.Log("AudioReplacement local WAV request unavailable owner=" + entry.OwnerId +
                    " replacement=" + entry.Options.ReplacementId +
                    " event=" + entry.Options.NativeSoundEvent +
                    " reason=" + requestMessage +
                    " path=" + entry.Options.AudioPath);
                runtime.ObserveLifecycleResourceEvent(
                    "AudioReplacement",
                    "LocalWavRequestUnavailable",
                    entry.OwnerId,
                    entry.Options.ReplacementId,
                    "event=" + entry.Options.NativeSoundEvent + "; reason=" + requestMessage);

                if (!TryCreatePcmWavClip(entry.Options.AudioPath, entry.Options.ReplacementId, out object? clip, out object? callbackOwner, out string message))
                {
                    if (TryCreatePlatformWavPlayer(entry.Options.AudioPath, out object? platformPlayer, out string platformMessage))
                    {
                        MarkReady(entry, clip: null, callbackOwner: null, platformPlayer, "platform", message + "; " + platformMessage);
                        return;
                    }

                    if (IsTransientUnityClipLoadFailure(message))
                    {
                        MarkLoadRetry(entry, message + "; platform fallback unavailable: " + platformMessage);
                        return;
                    }

                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = message + "; platform fallback unavailable: " + platformMessage;
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                MarkReady(entry, clip, callbackOwner, platformPlayer: null, "unity-pcm", message);
            }
            catch (Exception ex)
            {
                entry.LoadStatus = "failed";
                entry.LoadFailureReason = ex.GetType().Name + ": " + ex.Message;
                UpdateOwnerState(entry.OwnerId, "AudioReplacement load start failed: " + entry.LoadFailureReason);
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to start local WAV load.", ex.ToString());
            }
        }

        private void MarkReady(AudioReplacementEntry entry, object? clip, object? callbackOwner, object? platformPlayer, string backend, string detail)
        {
            SetReadyState(entry, clip, callbackOwner, platformPlayer, backend, detail);
            PublishReadyState(entry);
        }

        private static void SetReadyState(AudioReplacementEntry entry, object? clip, object? callbackOwner, object? platformPlayer, string backend, string detail)
        {
            entry.AudioClip = clip;
            entry.AudioCallbackOwner = callbackOwner;
            entry.PlatformAudioPlayer = platformPlayer;
            entry.LoadStarted = true;
            entry.LoadStatus = ReadyStatus;
            entry.LoadFailureReason = string.Empty;
            entry.ReadyBackend = backend ?? string.Empty;
            entry.ReadyDetail = detail ?? string.Empty;
            entry.LastMessage = "Local WAV ready for " + entry.Options.NativeSoundEvent + " via " + backend + ": " + entry.Options.AudioPath + ". " + detail;
            entry.AsyncOperation = null;
        }

        private void PublishReadyState(AudioReplacementEntry entry)
        {
            UpdateOwnerState(entry.OwnerId, entry.LastMessage);
            runtime.RuntimeMonitor.Log("AudioReplacement local WAV ready owner=" + entry.OwnerId +
                " replacement=" + entry.Options.ReplacementId +
                " event=" + entry.Options.NativeSoundEvent +
                " backend=" + entry.ReadyBackend +
                " path=" + entry.Options.AudioPath +
                " detail=" + entry.ReadyDetail);
            runtime.ObserveLifecycleResourceEvent(
                "AudioReplacement",
                "LocalWavReady",
                entry.OwnerId,
                entry.Options.ReplacementId,
                "event=" + entry.Options.NativeSoundEvent + "; backend=" + entry.ReadyBackend);
            if (entry.AudioClip != null)
            {
                runtime.ObserveResourceLifecycle(
                    "WavAudioClip",
                    entry.Options.ReplacementId,
                    entry.OwnerId,
                    entry.Options.AudioPath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    ResourceLifecycleStatus.Ready,
                    "destroy-existing-cleanup-entry");
            }
            if (entry.PlatformAudioPlayer != null)
            {
                runtime.ObserveResourceLifecycle(
                    "PlatformAudioPlayer",
                    entry.Options.ReplacementId,
                    entry.OwnerId,
                    entry.Options.AudioPath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    ResourceLifecycleStatus.Ready,
                    "dispose-existing-cleanup-entry");
            }
        }

        private bool TryStartUnityAudioClipRequest(AudioReplacementEntry entry, out string message)
        {
            message = string.Empty;

            Type? audioTypeType = FindType("UnityEngine.AudioType");
            Type? unityWebRequestMultimediaType = FindType("UnityEngine.Networking.UnityWebRequestMultimedia");
            Type? unityWebRequestType = FindType("UnityEngine.Networking.UnityWebRequest");
            Type[] requestFactoryTypes = new[] { unityWebRequestMultimediaType, unityWebRequestType }.Where(t => t != null).Cast<Type>().ToArray();
            if (audioTypeType == null || requestFactoryTypes.Length == 0)
            {
                message = "UnityWebRequest audio types unavailable. audioType=" + (audioTypeType != null) +
                    " factories=" + string.Join(",", requestFactoryTypes.Select(t => t.FullName));
                return false;
            }

            object audioTypeWav;
            try
            {
                audioTypeWav = Enum.Parse(audioTypeType, "WAV", ignoreCase: false);
            }
            catch (Exception ex)
            {
                message = "UnityEngine.AudioType.WAV unavailable: " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }

            MethodInfo? getAudioClip = null;
            Type? factoryType = null;
            foreach (Type candidateType in requestFactoryTypes)
            {
                getAudioClip = candidateType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (!string.Equals(m.Name, "GetAudioClip", StringComparison.Ordinal))
                            return false;

                        ParameterInfo[] p = m.GetParameters();
                        return p.Length == 2 &&
                            (p[0].ParameterType == typeof(string) || p[0].ParameterType == typeof(Uri)) &&
                            p[1].ParameterType == audioTypeType;
                    });
                if (getAudioClip != null)
                {
                    factoryType = candidateType;
                    break;
                }
            }

            if (getAudioClip == null)
            {
                message = "UnityWebRequest GetAudioClip(string/Uri, AudioType) unavailable. factories=" +
                    string.Join(" | ", requestFactoryTypes.Select(t => t.FullName + ":" + DescribeMethods(t.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "GetAudioClip"))));
                return false;
            }

            string uri = new Uri(entry.Options.AudioPath).AbsoluteUri;
            object? firstArgument = getAudioClip.GetParameters()[0].ParameterType == typeof(Uri) ? new Uri(uri) : uri;
            object? request = getAudioClip.Invoke(null, new[] { firstArgument, audioTypeWav });
            if (request == null)
            {
                message = "UnityWebRequest GetAudioClip returned null. factory=" + factoryType?.FullName +
                    " method=" + DescribeMethod(getAudioClip);
                return false;
            }

            MethodInfo? send = request.GetType().GetMethod("SendWebRequest", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) ??
                request.GetType().GetMethod("Send", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (send == null)
            {
                DestroyUnityObject(request, 0f);
                message = "UnityWebRequest send method unavailable. requestType=" + request.GetType().FullName;
                return false;
            }

            object? asyncOperation = send.Invoke(request, null);
            if (asyncOperation == null)
                asyncOperation = request;

            entry.Request = request;
            entry.AsyncOperation = asyncOperation;
            entry.LoadFailureReason = string.Empty;
            entry.LoadStatus = "loading";
            message = "UnityWebRequest WAV loading via " + factoryType?.FullName + "." + DescribeMethod(getAudioClip) + " uri=" + uri;
            return true;
        }

        private static bool TryCreatePlatformWavPlayer(string path, out object? player, out string message)
        {
            player = null;
            message = string.Empty;

            Type? soundPlayerType = FindPlatformSoundPlayerType();
            if (soundPlayerType == null)
            {
                message = "System.Media.SoundPlayer type unavailable.";
                return false;
            }

            ConstructorInfo? constructor = soundPlayerType.GetConstructor(new[] { typeof(string) });
            if (constructor == null)
            {
                message = "System.Media.SoundPlayer(string) constructor unavailable. type=" + soundPlayerType.FullName;
                return false;
            }

            object? created = null;
            try
            {
                created = constructor.Invoke(new object[] { path });
                MethodInfo? load = soundPlayerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                load?.Invoke(created, null);
                player = created;
                message = "System.Media.SoundPlayer WAV loaded. type=" + soundPlayerType.AssemblyQualifiedName;
                return true;
            }
            catch (Exception ex)
            {
                DisposePlatformPlayer(created);
                player = null;
                message = "System.Media.SoundPlayer load failed: " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static Type? FindPlatformSoundPlayerType()
        {
            Type? type = Type.GetType("System.Media.SoundPlayer, System", throwOnError: false) ??
                Type.GetType("System.Media.SoundPlayer, System.Windows.Extensions", throwOnError: false) ??
                FindType("System.Media.SoundPlayer");
            if (type != null)
                return type;

            foreach (string assemblyName in new[] { "System", "System.Windows.Extensions" })
            {
                try
                {
                    Assembly loaded = Assembly.Load(assemblyName);
                    type = loaded.GetType("System.Media.SoundPlayer", throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private void MarkLoadRetry(AudioReplacementEntry entry, string message)
        {
            entry.LoadStarted = false;
            entry.LoadStatus = "retry";
            entry.LoadFailureReason = string.Empty;
            entry.LastMessage = "Audio replacement clip creation pending retry: " + message;
            UpdateOwnerState(entry.OwnerId, entry.LastMessage);
            runtime.RuntimeMonitor.Log("AudioReplacement local PCM WAV pending retry owner=" + entry.OwnerId +
                " replacement=" + entry.Options.ReplacementId +
                " event=" + entry.Options.NativeSoundEvent +
                " reason=" + message +
                " path=" + entry.Options.AudioPath);
        }

        private static bool IsTransientUnityClipLoadFailure(string message)
        {
            return message.IndexOf("UnityEngine.AudioClip unavailable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Unity AudioClip.Create returned null", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Unity AudioClip metadata not initialized", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Unity AudioClip.SetData returned false", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryCreatePcmWavClip(string path, string replacementId, out object? clip, out object? callbackOwner, out string message)
        {
            clip = null;
            callbackOwner = null;
            message = string.Empty;

            if (!TryReadPcmWav(path, out int sampleRate, out int channels, out float[] samples, out string readMessage))
            {
                message = readMessage;
                return false;
            }

            if (channels <= 0 || sampleRate <= 0 || samples.Length == 0 || samples.Length % channels != 0)
            {
                message = "Invalid PCM WAV metadata. sampleRate=" + sampleRate.ToString(CultureInfo.InvariantCulture) +
                    " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                    " samples=" + samples.Length.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            Type? audioClipType = FindType("UnityEngine.AudioClip");
            if (audioClipType == null)
            {
                message = "UnityEngine.AudioClip unavailable.";
                return false;
            }

            MethodInfo[] createCandidates = audioClipType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m =>
                {
                    if (m.Name != "Create")
                        return false;

                    ParameterInfo[] p = m.GetParameters();
                    return p.Length >= 5 &&
                        p.Length <= 7 &&
                        p[0].ParameterType == typeof(string) &&
                        p[1].ParameterType == typeof(int) &&
                        p[2].ParameterType == typeof(int) &&
                        p[3].ParameterType == typeof(int) &&
                        p[4].ParameterType == typeof(bool);
                })
                .ToArray();
            MethodInfo? createWithCallback = createCandidates
                .Where(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length == 6 &&
                        string.Equals(p[4].Name, "stream", StringComparison.OrdinalIgnoreCase) &&
                        IsPcmReaderCallbackParameter(p[5]);
                })
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault() ??
                createCandidates
                    .Where(m =>
                    {
                        ParameterInfo[] p = m.GetParameters();
                        return p.Length >= 7 &&
                            p[4].ParameterType == typeof(bool) &&
                            p[5].ParameterType == typeof(bool) &&
                            string.Equals(p[4].Name, "_3D", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(p[5].Name, "stream", StringComparison.OrdinalIgnoreCase) &&
                            IsPcmReaderCallbackParameter(p[6]);
                    })
                    .OrderBy(m => m.GetParameters().Length)
                    .FirstOrDefault();
            MethodInfo? create = createCandidates
                .Where(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return p.Length == 5 && string.Equals(p[4].Name, "stream", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault() ??
                createCandidates
                    .Where(m =>
                    {
                        ParameterInfo[] p = m.GetParameters();
                        return p.Length >= 6 &&
                            p.Length <= 8 &&
                            p[4].ParameterType == typeof(bool) &&
                            p[5].ParameterType == typeof(bool) &&
                            string.Equals(p[4].Name, "_3D", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(p[5].Name, "stream", StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(m => m.GetParameters().Length)
                    .FirstOrDefault();
            MethodInfo? setData = audioClipType.GetMethod("SetData", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float[]), typeof(int) }, null);
            if (create == null || setData == null)
            {
                if (createWithCallback != null)
                    return TryCreatePcmWavClipWithCallback(createWithCallback, samples, sampleRate, channels, replacementId, out clip, out callbackOwner, out message);

                message = "Unity AudioClip.Create/SetData and PCM callback overloads unavailable. createOverloads=" + DescribeMethods(createCandidates);
                return false;
            }

            return TryCreatePcmWavClipWithSetData(create, setData, samples, sampleRate, channels, replacementId, out clip, out message);
        }

        private static bool TryCreatePcmWavClipWithSetData(
            MethodInfo create,
            MethodInfo setData,
            float[] samples,
            int sampleRate,
            int channels,
            string replacementId,
            out object? clip,
            out string message)
        {
            clip = null;
            message = string.Empty;

            int frameCount = samples.Length / channels;
            ParameterInfo[] createParameters = create.GetParameters();
            object?[] createArguments = new object?[createParameters.Length];
            createArguments[0] = "DTMAPI.AudioReplacement." + replacementId;
            createArguments[1] = frameCount;
            createArguments[2] = channels;
            createArguments[3] = sampleRate;
            createArguments[4] = false;
            if (createParameters.Length >= 6 &&
                createParameters[5].ParameterType == typeof(bool) &&
                string.Equals(createParameters[5].Name, "stream", StringComparison.OrdinalIgnoreCase))
            {
                createArguments[5] = false;
            }

            object? created = null;
            try
            {
                created = create.Invoke(null, createArguments);
                if (created == null)
                {
                    message = "Unity AudioClip.Create returned null. overload=" + DescribeMethod(create);
                    return false;
                }

                object? setResult = setData.Invoke(created, new object[] { samples, 0 });
                if (setResult is bool ok && !ok)
                {
                    DestroyUnityObject(created, 0f);
                    created = null;
                    message = "Unity AudioClip.SetData returned false. overload=" + DescribeMethod(create) +
                        " frames=" + frameCount.ToString(CultureInfo.InvariantCulture) +
                        " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                        " samples=" + samples.Length.ToString(CultureInfo.InvariantCulture);
                    return false;
                }

                if (!HasAudioClipMetadata(created, out string createdMetadata))
                {
                    DestroyUnityObject(created, 0f);
                    created = null;
                    message = "Unity AudioClip metadata not initialized after SetData. " + createdMetadata +
                        " overload=" + DescribeMethod(create);
                    return false;
                }

                clip = created;
                message = "PCM WAV loaded. sampleRate=" + sampleRate.ToString(CultureInfo.InvariantCulture) +
                    " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                    " frames=" + frameCount.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                DestroyUnityObject(created, 0f);
                clip = null;
                message = "Unity PCM WAV clip creation failed: " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static bool TryCreatePcmWavClipWithCallback(
            MethodInfo create,
            float[] samples,
            int sampleRate,
            int channels,
            string replacementId,
            out object? clip,
            out object? callbackOwner,
            out string message)
        {
            clip = null;
            callbackOwner = null;
            message = string.Empty;

            int frameCount = samples.Length / channels;
            ParameterInfo[] createParameters = create.GetParameters();
            ParameterInfo? readerParameter = createParameters.FirstOrDefault(IsPcmReaderCallbackParameter);
            if (readerParameter == null)
            {
                message = "Unity AudioClip PCMReaderCallback parameter unavailable. overload=" + DescribeMethod(create);
                return false;
            }

            var reader = new PcmAudioReader(samples, sampleRate, channels);
            MethodInfo? readMethod = typeof(PcmAudioReader).GetMethod(nameof(PcmAudioReader.Read), BindingFlags.Public | BindingFlags.Instance);
            if (readMethod == null)
            {
                message = "DTMAPI PCM audio reader method unavailable.";
                return false;
            }

            Delegate callback = Delegate.CreateDelegate(readerParameter.ParameterType, reader, readMethod);
            reader.Callback = callback;

            object?[] createArguments = new object?[createParameters.Length];
            createArguments[0] = "DTMAPI.AudioReplacement." + replacementId;
            createArguments[1] = frameCount;
            createArguments[2] = channels;
            createArguments[3] = sampleRate;
            if (createParameters.Length >= 6 &&
                string.Equals(createParameters[4].Name, "_3D", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(createParameters[5].Name, "stream", StringComparison.OrdinalIgnoreCase))
            {
                createArguments[4] = false;
                createArguments[5] = true;
                createArguments[6] = callback;
            }
            else
            {
                createArguments[4] = true;
                createArguments[5] = callback;
            }

            object? created = create.Invoke(null, createArguments);
            if (created == null)
            {
                message = "Unity AudioClip.Create returned null. overload=" + DescribeMethod(create);
                return false;
            }

            clip = created;
            callbackOwner = reader;
            message = "PCM WAV callback clip created. sampleRate=" + sampleRate.ToString(CultureInfo.InvariantCulture) +
                " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                " frames=" + frameCount.ToString(CultureInfo.InvariantCulture) +
                " overload=" + DescribeMethod(create);
            return true;
        }

        private static bool IsPcmReaderCallbackParameter(ParameterInfo parameter)
        {
            return parameter.ParameterType.Name.IndexOf("PCMReaderCallback", StringComparison.OrdinalIgnoreCase) >= 0 ||
                parameter.ParameterType.FullName?.IndexOf("PCMReaderCallback", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasAudioClipMetadata(object clip, out string reason)
        {
            float length = ReadFloat(clip, "length", -1f);
            int samples = ReadInt(clip, "samples", -1);
            int channels = ReadInt(clip, "channels", -1);
            int frequency = ReadInt(clip, "frequency", -1);
            reason = "length=" + length.ToString(CultureInfo.InvariantCulture) +
                " samples=" + samples.ToString(CultureInfo.InvariantCulture) +
                " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                " frequency=" + frequency.ToString(CultureInfo.InvariantCulture);
            return length > 0 && samples > 0 && channels > 0 && frequency > 0;
        }

        private static string DescribeMethods(IEnumerable<MethodInfo> methods)
        {
            return string.Join(" | ", methods.Select(DescribeMethod));
        }

        private static string DescribeMethod(MethodInfo method)
        {
            return method.Name + "(" + string.Join(", ", method.GetParameters().Select(p => p.Name + ":" + p.ParameterType.Name)) + ")";
        }

        private static bool TryReadPcmWav(string path, out int sampleRate, out int channels, out float[] samples, out string message)
        {
            sampleRate = 0;
            channels = 0;
            samples = Array.Empty<float>();
            message = string.Empty;

            byte[] bytes;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                bytes = new byte[stream.Length];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read <= 0)
                        break;
                    offset += read;
                }
            }

            if (bytes.Length < 44 || !FourCc(bytes, 0, "RIFF") || !FourCc(bytes, 8, "WAVE"))
            {
                message = "Unsupported WAV file: missing RIFF/WAVE header.";
                return false;
            }

            int audioFormat = 0;
            int bitsPerSample = 0;
            int dataOffset = -1;
            int dataSize = 0;

            int cursor = 12;
            while (cursor + 8 <= bytes.Length)
            {
                string chunkId = ReadAscii(bytes, cursor, 4);
                int chunkSize = ReadInt32LE(bytes, cursor + 4);
                int chunkDataOffset = cursor + 8;
                if (chunkSize < 0 || chunkDataOffset + chunkSize > bytes.Length)
                    break;

                if (chunkId == "fmt ")
                {
                    if (chunkSize < 16)
                    {
                        message = "Unsupported WAV file: fmt chunk too small.";
                        return false;
                    }

                    audioFormat = ReadUInt16LE(bytes, chunkDataOffset);
                    channels = ReadUInt16LE(bytes, chunkDataOffset + 2);
                    sampleRate = ReadInt32LE(bytes, chunkDataOffset + 4);
                    bitsPerSample = ReadUInt16LE(bytes, chunkDataOffset + 14);
                }
                else if (chunkId == "data")
                {
                    dataOffset = chunkDataOffset;
                    dataSize = chunkSize;
                }

                cursor = chunkDataOffset + chunkSize + (chunkSize % 2);
            }

            if (audioFormat != 1 && audioFormat != 3)
            {
                message = "Unsupported WAV format. format=" + audioFormat.ToString(CultureInfo.InvariantCulture) + " only PCM/IEEE-float WAV is supported.";
                return false;
            }

            if (channels <= 0 || sampleRate <= 0 || bitsPerSample <= 0 || dataOffset < 0 || dataSize <= 0)
            {
                message = "Invalid WAV metadata. channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                    " sampleRate=" + sampleRate.ToString(CultureInfo.InvariantCulture) +
                    " bits=" + bitsPerSample.ToString(CultureInfo.InvariantCulture) +
                    " dataSize=" + dataSize.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            int bytesPerSample = bitsPerSample / 8;
            if (bytesPerSample <= 0 || dataSize % bytesPerSample != 0)
            {
                message = "Invalid WAV sample size. bits=" + bitsPerSample.ToString(CultureInfo.InvariantCulture) +
                    " dataSize=" + dataSize.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            int sampleCount = dataSize / bytesPerSample;
            float[] decoded = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                int index = dataOffset + (i * bytesPerSample);
                decoded[i] = DecodeSample(bytes, index, bitsPerSample, audioFormat);
            }

            samples = decoded;
            message = "PCM WAV decoded.";
            return true;
        }

        private static float DecodeSample(byte[] bytes, int index, int bitsPerSample, int audioFormat)
        {
            if (audioFormat == 3 && bitsPerSample == 32)
                return ClampSample(BitConverter.ToSingle(bytes, index));

            switch (bitsPerSample)
            {
                case 8:
                    return ClampSample((bytes[index] - 128) / 128f);
                case 16:
                    return ClampSample(BitConverter.ToInt16(bytes, index) / 32768f);
                case 24:
                    int value24 = bytes[index] | (bytes[index + 1] << 8) | (bytes[index + 2] << 16);
                    if ((value24 & 0x800000) != 0)
                        value24 |= unchecked((int)0xFF000000);
                    return ClampSample(value24 / 8388608f);
                case 32:
                    return ClampSample(BitConverter.ToInt32(bytes, index) / 2147483648f);
                default:
                    return 0f;
            }
        }

        private static float ClampSample(float value)
        {
            if (float.IsNaN(value))
                return 0f;
            if (value > 1f)
                return 1f;
            if (value < -1f)
                return -1f;
            return value;
        }

        private static bool FourCc(byte[] bytes, int offset, string expected)
        {
            return offset >= 0 &&
                offset + expected.Length <= bytes.Length &&
                ReadAscii(bytes, offset, expected.Length) == expected;
        }

        private static string ReadAscii(byte[] bytes, int offset, int count)
        {
            char[] chars = new char[count];
            for (int i = 0; i < count; i++)
                chars[i] = (char)bytes[offset + i];
            return new string(chars);
        }

        private static int ReadInt32LE(byte[] bytes, int offset)
        {
            return bytes[offset] |
                (bytes[offset + 1] << 8) |
                (bytes[offset + 2] << 16) |
                (bytes[offset + 3] << 24);
        }

        private static int ReadUInt16LE(byte[] bytes, int offset)
        {
            return bytes[offset] | (bytes[offset + 1] << 8);
        }

        private void PollLoad(AudioReplacementEntry entry)
        {
            if (!entry.LoadStarted || entry.IsReady || entry.Request == null || entry.AsyncOperation == null)
                return;

            try
            {
                if (!ReadBool(entry.AsyncOperation, "isDone") && !ReadBool(entry.Request, "isDone"))
                    return;

                string error = ReadString(entry.Request, "error");
                string result = ReadObjectString(entry.Request, "result");
                bool failed = !string.IsNullOrWhiteSpace(error) || (result.Length > 0 && !string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase));
                if (failed)
                {
                    string failure = "UnityWebRequest failed result=" + result + " error=" + error;
                    if (TryCreatePlatformWavPlayer(entry.Options.AudioPath, out object? platformPlayer, out string platformMessage))
                    {
                        MarkReady(entry, clip: null, callbackOwner: null, platformPlayer, "platform", failure + "; " + platformMessage);
                        DisposeRequest(entry);
                        return;
                    }

                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = failure + "; platform fallback unavailable: " + platformMessage;
                    DisposeRequest(entry);
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                Type? downloadHandlerAudioClip = FindType("UnityEngine.Networking.DownloadHandlerAudioClip");
                MethodInfo? getContent = downloadHandlerAudioClip?.GetMethod("GetContent", BindingFlags.Public | BindingFlags.Static);
                object? clip = getContent?.Invoke(null, new[] { entry.Request });
                if (clip == null)
                {
                    if (TryCreatePlatformWavPlayer(entry.Options.AudioPath, out object? platformPlayer, out string platformMessage))
                    {
                        MarkReady(entry, clip: null, callbackOwner: null, platformPlayer, "platform", "DownloadHandlerAudioClip.GetContent returned null; " + platformMessage);
                        DisposeRequest(entry);
                        return;
                    }

                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = "DownloadHandlerAudioClip.GetContent returned null; platform fallback unavailable: " + platformMessage;
                    DisposeRequest(entry);
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                if (!IsPlayableClip(clip, out string clipReason))
                {
                    DestroyUnityObject(clip, 0f);
                    if (TryCreatePlatformWavPlayer(entry.Options.AudioPath, out object? platformPlayer, out string platformMessage))
                    {
                        MarkReady(entry, clip: null, callbackOwner: null, platformPlayer, "platform", clipReason + "; " + platformMessage);
                        DisposeRequest(entry);
                        return;
                    }

                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = clipReason + "; platform fallback unavailable: " + platformMessage;
                    DisposeRequest(entry);
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                MarkReady(entry, clip, callbackOwner: null, platformPlayer: null, "unity-web-request", "UnityWebRequest local WAV decoded.");
                DisposeRequest(entry);
            }
            catch (Exception ex)
            {
                entry.LoadStatus = "failed";
                entry.LoadFailureReason = ex.GetType().Name + ": " + ex.Message;
                DisposeRequest(entry);
                UpdateOwnerState(entry.OwnerId, "AudioReplacement load poll failed: " + entry.LoadFailureReason);
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to finish local WAV load.", ex.ToString());
            }
        }

        private bool TryPlay(AudioReplacementEntry entry, out string message)
        {
            object? go = null;
            try
            {
                if (entry.PlatformAudioPlayer != null)
                    return TryPlayPlatformAudio(entry, out message);

                if (entry.AudioClip == null)
                {
                    message = "replacement clip missing; native sound allowed.";
                    return false;
                }

                PcmAudioReader? reader = entry.AudioCallbackOwner as PcmAudioReader;
                if (reader != null)
                    reader.Reset();

                if (reader == null && !IsPlayableClip(entry.AudioClip, out string clipReason))
                {
                    message = clipReason + "; native sound allowed.";
                    return false;
                }

                Type? gameObjectType = FindType("UnityEngine.GameObject");
                Type? audioSourceType = FindType("UnityEngine.AudioSource");
                Type? unityObjectType = FindType("UnityEngine.Object");
                if (gameObjectType == null || audioSourceType == null || unityObjectType == null)
                {
                    message = "Unity audio playback types unavailable; native sound allowed.";
                    return false;
                }

                go = Activator.CreateInstance(gameObjectType, "DTMAPI.AudioReplacement." + entry.Options.ReplacementId);
                MethodInfo? addComponent = gameObjectType.GetMethod("AddComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
                object? source = addComponent?.Invoke(go, new object[] { audioSourceType });
                if (go == null || source == null)
                {
                    DestroyUnityObject(go, 0f);
                    message = "AudioSource creation failed; native sound allowed.";
                    return false;
                }

                if (!TrySetProperty(source, "playOnAwake", false) ||
                    !TrySetProperty(source, "spatialBlend", 0f) ||
                    !TrySetProperty(source, "volume", (float)entry.Options.Volume) ||
                    !TrySetProperty(source, "clip", entry.AudioClip))
                {
                    DestroyUnityObject(go, 0f);
                    message = "AudioSource properties unavailable; native sound allowed.";
                    return false;
                }

                MethodInfo? play = audioSourceType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (play == null)
                {
                    DestroyUnityObject(go, 0f);
                    message = "AudioSource.Play unavailable; native sound allowed.";
                    return false;
                }

                play.Invoke(source, null);
                object? isPlayingValue = audioSourceType.GetProperty("isPlaying", BindingFlags.Public | BindingFlags.Instance)?.GetValue(source, null);
                if (isPlayingValue is bool isPlaying && !isPlaying)
                {
                    DestroyUnityObject(go, 0f);
                    message = "AudioSource did not enter playing state; native sound allowed.";
                    return false;
                }

                float length = reader != null ? reader.DurationSeconds : Math.Max(0.1f, ReadFloat(entry.AudioClip, "length", 2f));
                DestroyUnityObject(go, length + 0.25f);
                go = null;

                entry.LastPlayedAtUtc = DateTimeOffset.UtcNow;
                message = "AudioReplacement played event=" + entry.Options.NativeSoundEvent +
                    " replacement=" + entry.Options.ReplacementId +
                    " suppressNative=" + entry.Options.SuppressNativeWhenReady +
                    " path=" + entry.Options.AudioPath;
                return true;
            }
            catch (Exception ex)
            {
                DestroyUnityObject(go, 0f);
                message = "AudioReplacement playback failed: " + ex.GetType().Name + ": " + ex.Message + "; native sound allowed.";
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to play local replacement audio.", ex.ToString());
                return false;
            }
        }

        private bool TryPlayPlatformAudio(AudioReplacementEntry entry, out string message)
        {
            try
            {
                object? player = entry.PlatformAudioPlayer;
                if (player == null)
                {
                    message = "platform replacement player missing; native sound allowed.";
                    return false;
                }

                MethodInfo? play = player.GetType().GetMethod("Play", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (play == null)
                {
                    message = "System.Media.SoundPlayer.Play unavailable; native sound allowed.";
                    return false;
                }

                play.Invoke(player, null);
                entry.LastPlayedAtUtc = DateTimeOffset.UtcNow;
                message = "AudioReplacement played event=" + entry.Options.NativeSoundEvent +
                    " replacement=" + entry.Options.ReplacementId +
                    " backend=platform" +
                    " suppressNative=" + entry.Options.SuppressNativeWhenReady +
                    " path=" + entry.Options.AudioPath;
                return true;
            }
            catch (Exception ex)
            {
                message = "System.Media.SoundPlayer playback failed: " + ex.GetType().Name + ": " + ex.Message + "; native sound allowed.";
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to play platform replacement audio.", ex.ToString());
                return false;
            }
        }

        private static bool IsPlayableClip(object clip, out string reason)
        {
            float length = ReadFloat(clip, "length", -1f);
            int samples = ReadInt(clip, "samples", -1);
            int channels = ReadInt(clip, "channels", -1);
            int frequency = ReadInt(clip, "frequency", -1);
            string loadState = ReadObjectString(clip, "loadState");
            if (string.Equals(loadState, "Failed", StringComparison.OrdinalIgnoreCase))
            {
                reason = "AudioClip load failed. loadState=" + loadState;
                return false;
            }

            bool loadRequested = false;
            if (loadState.Length > 0 && !string.Equals(loadState, "Loaded", StringComparison.OrdinalIgnoreCase))
                loadRequested = TryLoadAudioData(clip);

            if (length <= 0 || samples <= 0 || channels <= 0 || frequency <= 0)
            {
                reason = "AudioClip metadata invalid. length=" + length.ToString(CultureInfo.InvariantCulture) +
                    " samples=" + samples.ToString(CultureInfo.InvariantCulture) +
                    " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                    " frequency=" + frequency.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            reason = "AudioClip metadata playable. loadState=" + loadState + " loadRequested=" + loadRequested.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryLoadAudioData(object clip)
        {
            try
            {
                MethodInfo? load = clip.GetType().GetMethod("LoadAudioData", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                object? result = load?.Invoke(clip, null);
                return result is bool loaded ? loaded : result != null;
            }
            catch
            {
                return false;
            }
        }

        private void RecordEvent(AudioReplacementEntry entry, string eventName, bool played, bool suppressed, string message)
        {
            entry.LastMessage = message ?? string.Empty;
            entry.LastNativeSoundEvent = eventName;
            entry.LastPlayed = played;
            entry.LastSuppressed = suppressed;
            if (entry.Options.VerboseLogging || played || !suppressed)
                runtime.RuntimeMonitor.Log("AudioReplacement event owner=" + entry.OwnerId +
                    " replacement=" + entry.Options.ReplacementId +
                    " event=" + eventName +
                    " played=" + played +
                    " suppressed=" + suppressed +
                    " message=" + entry.LastMessage);

            UpdateOwnerState(entry.OwnerId, entry.LastMessage);
            runtime.SetHookStatus(
                "Audio.SoundEventReplacement",
                (played || suppressed) ? "verified" : (hookInstalled ? "experimental" : "pending"),
                "Harmony Prefix: WwiseSoundManager.InternalPostSoundEvent",
                entry.LastMessage);
        }

        private void UpdateAllOwnerStates(string message)
        {
            foreach (string ownerId in entries.Values.Select(e => e.OwnerId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
                UpdateOwnerState(ownerId, message);
        }

        private void UpdateOwnerState(string ownerId, string message)
        {
            ownerId ??= string.Empty;
            AudioReplacementEntry[] ownerEntries = entries.Values.Where(e => string.Equals(e.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)).ToArray();
            bool configured = ownerEntries.Length > 0;
            if (!configured)
            {
                states.Remove(ownerId);
                return;
            }

            bool enabled = ownerEntries.Any(e => e.Options.Enabled);
            bool ready = ownerEntries.Any(e => e.IsReady);
            AudioReplacementEntry? last = ownerEntries.OrderByDescending(e => e.LastPlayedAtUtc).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.LastNativeSoundEvent)) ?? ownerEntries.FirstOrDefault();
            states[ownerId] = new AudioReplacementState
            {
                OwnerId = ownerId,
                IsConfigured = configured,
                Enabled = enabled,
                HookInstalled = hookInstalled,
                ReplacementCount = ownerEntries.Length,
                Replacements = ownerEntries.Select(ToInfo).ToArray(),
                LastNativeSoundEvent = last?.LastNativeSoundEvent ?? string.Empty,
                LastReplacementId = last?.Options.ReplacementId ?? string.Empty,
                LastReplacementPlayed = last?.LastPlayed ?? false,
                LastNativeSuppressed = last?.LastSuppressed ?? false,
                LastMessage = message ?? string.Empty,
                Status = !configured ? "not-configured" : (!enabled ? "disabled" : (!hookInstalled ? "configured-pending-hook" : (ready ? "configured-ready" : "configured-loading")))
            };
        }

        private AudioReplacementState GetOwnerState(string ownerId)
        {
            ownerId ??= string.Empty;
            if (states.TryGetValue(ownerId, out AudioReplacementState state))
                return state;

            if (entries.Values.Any(e => string.Equals(e.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)))
            {
                UpdateOwnerState(ownerId, "Audio replacement registered.");
                if (states.TryGetValue(ownerId, out AudioReplacementState? updated))
                    return updated;
            }

            // Read-only state and health queries must not create owner-bound resources.
            // An unconfigured owner receives a transient projection only.
            return new AudioReplacementState
            {
                OwnerId = ownerId,
                HookInstalled = hookInstalled,
                Status = "not-configured",
                LastMessage = "No audio replacement registered."
            };
        }

        private static AudioReplacementState CloneState(AudioReplacementState state)
        {
            return new AudioReplacementState
            {
                OwnerId = state.OwnerId,
                IsConfigured = state.IsConfigured,
                Enabled = state.Enabled,
                HookInstalled = state.HookInstalled,
                ReplacementCount = state.ReplacementCount,
                Replacements = state.Replacements.Select(i => new AudioReplacementEntryInfo
                {
                    ReplacementId = i.ReplacementId,
                    NativeSoundEvent = i.NativeSoundEvent,
                    AudioPath = i.AudioPath,
                    Enabled = i.Enabled,
                    PreloadReady = i.PreloadReady,
                    LoadStatus = i.LoadStatus,
                    LastMessage = i.LastMessage
                }).ToArray(),
                LastNativeSoundEvent = state.LastNativeSoundEvent,
                LastReplacementId = state.LastReplacementId,
                LastReplacementPlayed = state.LastReplacementPlayed,
                LastNativeSuppressed = state.LastNativeSuppressed,
                LastMessage = state.LastMessage,
                Status = state.Status
            };
        }

        private static AudioReplacementEntryInfo ToInfo(AudioReplacementEntry entry)
        {
            return new AudioReplacementEntryInfo
            {
                ReplacementId = entry.Options.ReplacementId,
                NativeSoundEvent = entry.Options.NativeSoundEvent,
                AudioPath = entry.Options.AudioPath,
                Enabled = entry.Options.Enabled,
                PreloadReady = entry.IsReady,
                LoadStatus = entry.LoadStatus,
                LastMessage = string.IsNullOrWhiteSpace(entry.LastMessage) ? entry.LoadFailureReason : entry.LastMessage
            };
        }

        private static AudioReplacementOptions NormalizeOptions(AudioReplacementOptions? options)
        {
            options ??= new AudioReplacementOptions();
            string eventName = NormalizeEventName(options.NativeSoundEvent);
            string replacementId = string.IsNullOrWhiteSpace(options.ReplacementId) ? eventName : options.ReplacementId.Trim();
            string audioPath = options.AudioPath?.Trim() ?? string.Empty;
            if (audioPath.Length > 0)
                audioPath = Path.GetFullPath(audioPath);
            return new AudioReplacementOptions
            {
                Enabled = options.Enabled,
                ReplacementId = replacementId,
                NativeSoundEvent = eventName,
                AudioPath = audioPath,
                SuppressNativeWhenReady = options.SuppressNativeWhenReady,
                Volume = Math.Max(0, Math.Min(2, options.Volume)),
                CooldownMilliseconds = Math.Max(0, options.CooldownMilliseconds),
                VerboseLogging = options.VerboseLogging
            };
        }

        private static string NormalizeEventName(string? eventName)
        {
            string value = eventName ?? string.Empty;
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeCategory(string? category)
        {
            string value = (category ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return AnimalVoiceCategory;
            if (string.Equals(value, "Sound", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "Sfx", StringComparison.OrdinalIgnoreCase))
                return SimpleSfxCategory;
            return string.Equals(value, SimpleSfxCategory, StringComparison.OrdinalIgnoreCase)
                ? SimpleSfxCategory
                : (string.Equals(value, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase) ? AnimalVoiceCategory : value);
        }

        private static string NormalizeId(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static string NormalizeStage(string? stage)
        {
            string value = (stage ?? string.Empty).Trim();
            if (string.Equals(value, "young", StringComparison.OrdinalIgnoreCase))
                return "child";
            if (string.Equals(value, "adult", StringComparison.OrdinalIgnoreCase))
                return "adult";
            if (string.Equals(value, "child", StringComparison.OrdinalIgnoreCase))
                return "child";
            if (string.Equals(value, "any", StringComparison.OrdinalIgnoreCase))
                return "any";
            return value;
        }

        private static bool IsValidAnimalVoiceStage(string stage)
        {
            return string.Equals(stage, "child", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stage, "adult", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(stage, "any", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ShouldSuppressNativeDuringReadyCooldownForTest(bool isReady, bool suppressNativeWhenReady, int cooldownMilliseconds, DateTimeOffset lastPlayedAtUtc, DateTimeOffset now)
        {
            return IsCooldownActive(cooldownMilliseconds, lastPlayedAtUtc, now) &&
                ShouldSuppressNativeDuringReadyCooldown(isReady, suppressNativeWhenReady);
        }

        private static bool IsCooldownActive(int cooldownMilliseconds, DateTimeOffset lastPlayedAtUtc, DateTimeOffset now)
        {
            return cooldownMilliseconds > 0 &&
                now - lastPlayedAtUtc < TimeSpan.FromMilliseconds(cooldownMilliseconds);
        }

        private static bool ShouldSuppressNativeDuringReadyCooldown(bool isReady, bool suppressNativeWhenReady)
        {
            return isReady && suppressNativeWhenReady;
        }

        private static string MakeKey(string ownerId, string replacementId)
        {
            return (ownerId ?? string.Empty).Trim() + "::" + (replacementId ?? string.Empty).Trim();
        }

        private static AudioReplacementEntry? FindSuppressingConflict(AudioReplacementEntry candidate, IEnumerable<AudioReplacementEntry> existing)
        {
            if (!candidate.Options.Enabled || !candidate.Options.SuppressNativeWhenReady)
                return null;

            return existing.FirstOrDefault(e =>
                e.Options.Enabled &&
                e.Options.SuppressNativeWhenReady &&
                ReplacementScopesConflict(candidate, e));
        }

        private static bool ReplacementScopesConflict(AudioReplacementEntry left, AudioReplacementEntry right)
        {
            if (!string.Equals(left.Category, right.Category, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.Equals(left.Options.NativeSoundEvent, right.Options.NativeSoundEvent, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(left.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(left.SpeciesId, right.SpeciesId, StringComparison.OrdinalIgnoreCase) &&
                    StagesOverlap(left.Stage, right.Stage);
            }

            return true;
        }

        private static bool StagesOverlap(string left, string right)
        {
            return string.Equals(left, "any", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(right, "any", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string DescribeConflictScope(AudioReplacementEntry entry)
        {
            if (string.Equals(entry.Category, AnimalVoiceCategory, StringComparison.OrdinalIgnoreCase))
                return entry.Category + "/" + entry.SpeciesId + "/" + entry.Stage + "/" + entry.Options.NativeSoundEvent;
            return entry.Category + "/" + entry.Options.NativeSoundEvent;
        }

        private static bool IsEnabledContentPack(DiscoveredMod mod)
        {
            return mod != null &&
                mod.OfficialEnabled &&
                mod.Manifest != null &&
                string.Equals(mod.Manifest.Type, "ContentPack", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatBoundedDiagnostics(IReadOnlyList<string> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0)
                return "generation validation failed without a specific diagnostic";

            const int maximumItems = 8;
            string value = string.Join("; ", diagnostics
                .Take(maximumItems)
                .Select(BoundDiagnostic)
                .ToArray());
            if (diagnostics.Count > maximumItems)
                value += "; ... " + (diagnostics.Count - maximumItems).ToString(CultureInfo.InvariantCulture) + " additional diagnostics omitted";
            return BoundDiagnostic(value);
        }

        private static string BoundDiagnostic(string value)
        {
            const int maximumLength = 2048;
            string normalized = (value ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
            return normalized.Length <= maximumLength
                ? normalized
                : normalized.Substring(0, maximumLength) + "...";
        }

        private static bool TryResolvePackPath(string rootPath, string relativePath, out string fullPath, out string failure)
        {
            fullPath = string.Empty;
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                failure = "missing file";
                return false;
            }

            string root = Path.GetFullPath(rootPath ?? string.Empty);
            string relative = (relativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            fullPath = Path.GetFullPath(Path.Combine(root, relative));
            string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) && !string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase))
            {
                failure = "file escapes content pack root: " + relativePath;
                fullPath = string.Empty;
                return false;
            }

            return true;
        }

        private static void DisposeRequest(AudioReplacementEntry entry)
        {
            try
            {
                if (entry.Request is IDisposable disposable)
                    disposable.Dispose();
                else
                    entry.Request?.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(entry.Request, null);
            }
            catch
            {
            }
            entry.Request = null;
            entry.AsyncOperation = null;
        }

        private void CleanupEntry(AudioReplacementEntry entry)
        {
            RemovePendingEntry(entry);
            if (entry.Request != null || entry.AsyncOperation != null)
            {
                runtime.ReleaseResourceLifecycle(
                    "WavRequest",
                    entry.Options.ReplacementId,
                    entry.OwnerId,
                    entry.Options.AudioPath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    "dispose-on-content-generation-replacement",
                    ResourceLifecycleStatus.Released);
            }

            DisposeRequest(entry);
            if (entry.AudioClip != null)
            {
                runtime.ReleaseResourceLifecycle(
                    "WavAudioClip",
                    entry.Options.ReplacementId,
                    entry.OwnerId,
                    entry.Options.AudioPath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    "destroy-existing-cleanup-entry",
                    ResourceLifecycleStatus.Released);
            }

            DestroyUnityObject(entry.AudioClip, 0f);
            entry.AudioClip = null;
            entry.AudioCallbackOwner = null;
            if (entry.PlatformAudioPlayer != null)
            {
                runtime.ReleaseResourceLifecycle(
                    "PlatformAudioPlayer",
                    entry.Options.ReplacementId,
                    entry.OwnerId,
                    entry.Options.AudioPath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    "dispose-existing-cleanup-entry",
                    ResourceLifecycleStatus.Released);
            }

            DisposePlatformPlayer(entry.PlatformAudioPlayer);
            entry.PlatformAudioPlayer = null;
        }

        private static void DisposePlatformPlayer(object? player)
        {
            if (player == null)
                return;

            try
            {
                player.GetType().GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(player, null);
            }
            catch
            {
            }

            try
            {
                if (player is IDisposable disposable)
                    disposable.Dispose();
                else
                    player.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)?.Invoke(player, null);
            }
            catch
            {
            }
        }

        private static void DestroyUnityObject(object? target, float delaySeconds)
        {
            if (target == null)
                return;

            try
            {
                Type? unityObjectType = FindType("UnityEngine.Object");
                if (unityObjectType == null)
                    return;

                MethodInfo? destroy = null;
                if (delaySeconds > 0)
                {
                    destroy = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "Destroy" && m.GetParameters().Length == 2);
                    if (destroy != null)
                    {
                        destroy.Invoke(null, new object[] { target, delaySeconds });
                        return;
                    }
                }

                destroy = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Destroy" && m.GetParameters().Length == 1);
                destroy?.Invoke(null, new[] { target });
            }
            catch
            {
            }
        }

        private static Type? FindType(string fullName)
        {
            Type? direct = Type.GetType(fullName, throwOnError: false);
            if (direct != null)
                return direct;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type? found = assembly.GetType(fullName, throwOnError: false);
                    if (found != null)
                        return found;
                }
                catch
                {
                }
            }
            return null;
        }

        private static object? ReadMember(object? instance, params string[] names)
        {
            if (instance == null)
                return null;

            foreach (string name in names)
            {
                for (Type? current = instance.GetType(); current != null; current = current.BaseType)
                {
                    PropertyInfo? property = current.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (property != null)
                    {
                        try
                        {
                            return property.GetValue(instance, null);
                        }
                        catch
                        {
                        }
                    }

                    FieldInfo? field = current.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        try
                        {
                            return field.GetValue(instance);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return null;
        }

        private static string ReadStringMember(object? instance, params string[] names)
        {
            object? value = ReadMember(instance, names);
            return value?.ToString() ?? string.Empty;
        }

        private static bool ReadBoolMember(object? instance, params string[] names)
        {
            object? value = ReadMember(instance, names);
            if (value is bool result)
                return result;
            if (value != null && bool.TryParse(value.ToString(), out bool parsed))
                return parsed;
            return false;
        }

        private static bool ReadBool(object target, string propertyName)
        {
            object? value = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target, null);
            return value is bool result && result;
        }

        private static string ReadString(object target, string propertyName)
        {
            object? value = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target, null);
            return value?.ToString() ?? string.Empty;
        }

        private static string ReadObjectString(object target, string propertyName)
        {
            object? value = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target, null);
            return value?.ToString() ?? string.Empty;
        }

        private static float ReadFloat(object target, string propertyName, float fallback)
        {
            object? value = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target, null);
            if (value is float f)
                return f;
            if (value is double d)
                return (float)d;
            return fallback;
        }

        private static int ReadInt(object target, string propertyName, int fallback)
        {
            object? value = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target, null);
            if (value is int i)
                return i;
            if (value is long l)
                return (int)l;
            return fallback;
        }

        private static bool TrySetProperty(object target, string propertyName, object value)
        {
            PropertyInfo? property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
                return false;
            property.SetValue(target, value, null);
            return true;
        }

        private sealed class AudioReplacementOwnerPreparation
        {
            internal AudioReplacementOwnerPreparation(
                AudioReplacementOwnerReloadResult result,
                IReadOnlyList<KeyValuePair<string, AudioReplacementEntry>> previousEntries,
                IReadOnlyList<AudioReplacementEntry> candidates,
                string reason)
            {
                Result = result;
                PreviousEntries = previousEntries;
                Candidates = candidates;
                Reason = reason ?? string.Empty;
            }

            internal AudioReplacementOwnerReloadResult Result { get; }
            internal IReadOnlyList<KeyValuePair<string, AudioReplacementEntry>> PreviousEntries { get; }
            internal IReadOnlyList<AudioReplacementEntry> Candidates { get; }
            internal string Reason { get; }
        }

        private sealed class AudioReplacementOwnerRemoval
        {
            internal AudioReplacementOwnerRemoval(
                string ownerId,
                IReadOnlyList<KeyValuePair<string, AudioReplacementEntry>> previousEntries,
                string reason)
            {
                OwnerId = ownerId ?? string.Empty;
                PreviousEntries = previousEntries;
                Reason = reason ?? string.Empty;
            }

            internal string OwnerId { get; }
            internal IReadOnlyList<KeyValuePair<string, AudioReplacementEntry>> PreviousEntries { get; }
            internal string Reason { get; }
        }

        internal sealed class AudioReplacementOwnerReloadResult
        {
            private AudioReplacementOwnerReloadResult(
                bool success,
                string status,
                string ownerId,
                long generation,
                int activeEntryCount,
                bool retainedPreviousGeneration,
                string message)
            {
                Success = success;
                Status = status ?? string.Empty;
                OwnerId = ownerId ?? string.Empty;
                Generation = generation;
                ActiveEntryCount = activeEntryCount;
                RetainedPreviousGeneration = retainedPreviousGeneration;
                Message = message ?? string.Empty;
            }

            internal bool Success { get; }
            internal string Status { get; }
            internal string OwnerId { get; }
            internal long Generation { get; }
            internal int ActiveEntryCount { get; }
            internal bool RetainedPreviousGeneration { get; }
            internal string Message { get; }

            internal static AudioReplacementOwnerReloadResult Committed(string ownerId, long generation, int activeEntryCount, string message)
            {
                return new AudioReplacementOwnerReloadResult(true, "committed", ownerId, generation, activeEntryCount, false, message);
            }

            internal static AudioReplacementOwnerReloadResult Rejected(string ownerId, string status, long generation, int activeEntryCount, bool retainedPreviousGeneration, string message)
            {
                return new AudioReplacementOwnerReloadResult(false, status, ownerId, generation, activeEntryCount, retainedPreviousGeneration, message);
            }

            internal static AudioReplacementOwnerReloadResult Removed(string ownerId, int removedEntryCount)
            {
                return new AudioReplacementOwnerReloadResult(true, "removed", ownerId, 0, 0, false, "Removed " + removedEntryCount.ToString(CultureInfo.InvariantCulture) + " audio replacement entries for inactive owner.");
            }
        }

        private sealed class AudioReplacementEntry
        {
            internal AudioReplacementEntry(string ownerId, AudioReplacementOptions options)
                : this(ownerId, options, SimpleSfxCategory, string.Empty, string.Empty, CodeModSource)
            {
            }

            internal AudioReplacementEntry(string ownerId, AudioReplacementOptions options, string category, string speciesId, string stage, string source)
            {
                OwnerId = ownerId;
                Options = options;
                Category = category;
                SpeciesId = speciesId;
                Stage = stage;
                Source = source;
                LoadStatus = options.Enabled ? "pending" : "disabled";
            }

            internal string OwnerId { get; }
            internal AudioReplacementOptions Options { get; }
            internal string Category { get; }
            internal string SpeciesId { get; }
            internal string Stage { get; }
            internal string Source { get; }
            internal bool LoadStarted { get; set; }
            internal object? Request { get; set; }
            internal object? AsyncOperation { get; set; }
            internal object? AudioClip { get; set; }
            internal object? AudioCallbackOwner { get; set; }
            internal object? PlatformAudioPlayer { get; set; }
            internal string LoadStatus { get; set; }
            internal string LoadFailureReason { get; set; } = string.Empty;
            internal string ReadyBackend { get; set; } = string.Empty;
            internal string ReadyDetail { get; set; } = string.Empty;
            internal string LastMessage { get; set; } = string.Empty;
            internal string LastNativeSoundEvent { get; set; } = string.Empty;
            internal bool LastPlayed { get; set; }
            internal bool LastSuppressed { get; set; }
            internal DateTimeOffset LastLoadAttemptAtUtc { get; set; } = DateTimeOffset.MinValue;
            internal DateTimeOffset LastPlayedAtUtc { get; set; } = DateTimeOffset.MinValue;
            internal bool IsReady => string.Equals(LoadStatus, ReadyStatus, StringComparison.OrdinalIgnoreCase) && (AudioClip != null || PlatformAudioPlayer != null);
        }

        private sealed class AnimalSoundContext
        {
            internal AnimalSoundContext(string speciesId, string stage, string expectedNativeSoundEvent, DateTimeOffset createdAtUtc)
            {
                SpeciesId = speciesId ?? string.Empty;
                Stage = stage ?? string.Empty;
                ExpectedNativeSoundEvent = expectedNativeSoundEvent ?? string.Empty;
                CreatedAtUtc = createdAtUtc;
            }

            internal string SpeciesId { get; }
            internal string Stage { get; }
            internal string ExpectedNativeSoundEvent { get; }
            internal DateTimeOffset CreatedAtUtc { get; }
        }

        [DataContract]
        private sealed class AudioReplacementDefinitionModel
        {
            [DataMember(Name = "id")]
            public string Id { get; set; } = string.Empty;

            [DataMember(Name = "category")]
            public string Category { get; set; } = string.Empty;

            [DataMember(Name = "speciesId")]
            public string SpeciesId { get; set; } = string.Empty;

            [DataMember(Name = "stage")]
            public string Stage { get; set; } = string.Empty;

            [DataMember(Name = "nativeSoundEvent")]
            public string NativeSoundEvent { get; set; } = string.Empty;

            [DataMember(Name = "file")]
            public string File { get; set; } = string.Empty;

            [DataMember(Name = "enabled")]
            public bool? EnabledValue { get; set; }

            [DataMember(Name = "suppressNativeWhenReady")]
            public bool? SuppressNativeWhenReadyValue { get; set; }

            [DataMember(Name = "volume")]
            public float? VolumeValue { get; set; }

            [DataMember(Name = "cooldownMilliseconds")]
            public int? CooldownMillisecondsValue { get; set; }

            [DataMember(Name = "verboseLogging")]
            public bool? VerboseLoggingValue { get; set; }

            public bool Enabled => EnabledValue ?? true;
            public bool SuppressNativeWhenReady => SuppressNativeWhenReadyValue ?? true;
            public float Volume => VolumeValue ?? 1f;
            public int CooldownMilliseconds => CooldownMillisecondsValue ?? 0;
            public bool VerboseLogging => VerboseLoggingValue ?? false;

            public void Normalize()
            {
                Id = Id ?? string.Empty;
                Category = Category ?? string.Empty;
                SpeciesId = SpeciesId ?? string.Empty;
                Stage = Stage ?? string.Empty;
                NativeSoundEvent = NativeSoundEvent ?? string.Empty;
                File = File ?? string.Empty;
            }
        }

        private sealed class PcmAudioReader
        {
            private readonly float[] samples;
            private readonly int sampleRate;
            private readonly int channels;
            private int position;

            internal PcmAudioReader(float[] samples, int sampleRate, int channels)
            {
                this.samples = samples;
                this.sampleRate = sampleRate;
                this.channels = channels;
            }

            internal Delegate? Callback { get; set; }

            internal float DurationSeconds
            {
                get
                {
                    if (sampleRate <= 0 || channels <= 0)
                        return 2f;

                    return Math.Max(0.1f, samples.Length / (float)(sampleRate * channels));
                }
            }

            public void Read(float[] data)
            {
                if (data == null)
                    return;

                int copied = 0;
                while (copied < data.Length && position < samples.Length)
                {
                    data[copied] = samples[position];
                    copied++;
                    position++;
                }

                while (copied < data.Length)
                {
                    data[copied] = 0f;
                    copied++;
                }
            }

            internal void Reset()
            {
                position = 0;
            }
        }
    }
}
