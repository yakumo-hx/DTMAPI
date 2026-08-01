using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CustomAnimalAnimatorBridgeService
    {
        private const int SleepRenderFollowUpMaxFrames = 60;
        private const string FeatureHookId = "CustomAnimals.AnimatorBridge";
        private const string AiTemplateHookId = "CustomAnimals.AiTemplateBridge";
        private const string PngSpriteHookId = "CustomAnimals.PngSpriteBridge";
        private const string SleepWakeDiagnosticsHookId = "CustomAnimals.SleepWakeDiagnostics";
        private const string SleepTaskBoundaryHookId = "CustomAnimals.SleepTaskBoundary";
        private const string SchemaFileRelativePath = "Content/DTMAPI/custom-animals.json";
        private const int AnimatorCallbackDemand = 1 << 0;
        private const int AiTemplateCallbackDemand = 1 << 1;
        private const int PngSpriteCallbackDemand = 1 << 2;
        private const int DiagnosticCallbackDemand = 1 << 3;
        private const int SleepTaskCallbackDemand = 1 << 4;
        private readonly DtmApiRuntime runtime;
        private readonly object gate = new object();
        private Dictionary<string, CustomAnimalAnimatorRegistration> registrations = new Dictionary<string, CustomAnimalAnimatorRegistration>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CustomAnimalAiTemplateRegistration> aiTemplateRegistrations = new Dictionary<string, CustomAnimalAiTemplateRegistration>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CustomAnimalAnimatorRegistration> pngSpriteRegistrationsBySpecies = new Dictionary<string, CustomAnimalAnimatorRegistration>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, long> ownerDefinitionGenerations = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private ConditionalWeakTable<object, PngSpriteOverrideContext> pngSpriteContexts = new ConditionalWeakTable<object, PngSpriteOverrideContext>();
        private ConditionalWeakTable<object, SleepRenderFollowUpContext> sleepRenderFollowUps = new ConditionalWeakTable<object, SleepRenderFollowUpContext>();
        private readonly Dictionary<string, object?> controllerCache = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, object?> bundleCache = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Type?> aiDefaultStateTypeCache = new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> degradedReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> aiTemplateDegradedReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> pngSpriteDegradedReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> verifiedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> verifiedAiTemplates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> verifiedPngSpriteSpecies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> diagnosticWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> aiTemplateDiagnosticWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pngSpriteDiagnosticWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> sleepWakeStatusPublished = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> sleepTaskBoundaryEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> lastRenderDiagnosticSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> lastPlayAnimationDiagnosticSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private int observedPngSpriteContextCount;
        private int observedSleepRenderFollowUpCount;
        private string sleepTaskBoundaryDegradedReason = string.Empty;
        private long nextOwnerDefinitionGeneration;
        private int definitionCandidateBuildCount;
        private long contentProjectionBuildCountForTest;
        private int callbackDemandMask;
        private long retainedCallbackWorkCountForTest;
        private long optionalFileStatusCallCount;
        private Action<string>? preCommitFaultForTest;
        private Action<ContentRefreshDirtyBatch>? beforeGenerationCompleteForTest;
        private Action<string>? postCommitFaultForTest;
        private Type? runtimeAnimatorControllerType;
        private Type? assetBundleType;
        private MethodInfo? assetBundleLoadFromFile;
        private MethodInfo? assetBundleLoadAssetByNameAndType;
        private MethodInfo? dolocApiGetAssetDefinition;
        private Type? spriteOverrideHandlerType;
        private Type? spriteType;
        private PropertyInfo? dolocApiModManagerProperty;
        private MethodInfo? modManagerLoadSpriteFromFile;
        private MethodInfo? linearTaskWaitFramesMethod;

        public CustomAnimalAnimatorBridgeService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal int RegisteredKeyCount
        {
            get
            {
                lock (gate)
                    return registrations.Count;
            }
        }

        internal string RegisteredKeySummary
        {
            get
            {
                lock (gate)
                    return registrations.Count == 0
                        ? "none"
                        : string.Join(",", registrations.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray());
            }
        }

        internal string GetCustomAnimalAnimatorLifecycleSummary()
        {
            lock (gate)
            {
                return "registrations=" + registrations.Count.ToString(CultureInfo.InvariantCulture) +
                    ", aiTemplates=" + aiTemplateRegistrations.Count.ToString(CultureInfo.InvariantCulture) +
                    ", pngSprites=" + pngSpriteRegistrationsBySpecies.Count.ToString(CultureInfo.InvariantCulture) +
                    ", controllerCache=" + controllerCache.Count.ToString(CultureInfo.InvariantCulture) +
                    ", nonNullControllers=" + controllerCache.Values.Count(value => value != null).ToString(CultureInfo.InvariantCulture) +
                    ", bundleCache=" + bundleCache.Count.ToString(CultureInfo.InvariantCulture) +
                    ", nonNullBundles=" + bundleCache.Values.Count(value => value != null).ToString(CultureInfo.InvariantCulture) +
                    ", aiDefaultStateTypes=" + aiDefaultStateTypeCache.Count.ToString(CultureInfo.InvariantCulture) +
                    ", degraded=" + degradedReasons.Count.ToString(CultureInfo.InvariantCulture) +
                    ", aiTemplateDegraded=" + aiTemplateDegradedReasons.Count.ToString(CultureInfo.InvariantCulture) +
                    ", pngSpriteDegraded=" + pngSpriteDegradedReasons.Count.ToString(CultureInfo.InvariantCulture) +
                    ", warnings=" + diagnosticWarnings.Count.ToString(CultureInfo.InvariantCulture) +
                    ", pngSpriteContextsObserved=" + observedPngSpriteContextCount.ToString(CultureInfo.InvariantCulture) +
                    ", sleepFollowUpsObserved=" + observedSleepRenderFollowUpCount.ToString(CultureInfo.InvariantCulture) +
                    ", verifiedAnimatorKeys=" + verifiedKeys.Count.ToString(CultureInfo.InvariantCulture) +
                    ", verifiedAiTemplates=" + verifiedAiTemplates.Count.ToString(CultureInfo.InvariantCulture) +
                    ", verifiedPngSprites=" + verifiedPngSpriteSpecies.Count.ToString(CultureInfo.InvariantCulture);
            }
        }

        internal int RegisteredAiTemplateCount
        {
            get
            {
                lock (gate)
                    return aiTemplateRegistrations.Count;
            }
        }

        internal string RegisteredAiTemplateSummary
        {
            get
            {
                lock (gate)
                    return aiTemplateRegistrations.Count == 0
                        ? "none"
                        : string.Join(",", aiTemplateRegistrations.Values
                            .OrderBy(r => r.SpeciesId, StringComparer.OrdinalIgnoreCase)
                            .Select(r => r.SpeciesId + "->" + r.AiTemplate)
                            .ToArray());
            }
        }

        internal int RegisteredPngSpriteOverrideSpeciesCount
        {
            get
            {
                lock (gate)
                    return pngSpriteRegistrationsBySpecies.Count;
            }
        }

        internal string RegisteredPngSpriteOverrideSummary
        {
            get
            {
                lock (gate)
                    return pngSpriteRegistrationsBySpecies.Count == 0
                        ? "none"
                        : string.Join(",", pngSpriteRegistrationsBySpecies.Values
                            .OrderBy(r => r.SpeciesId, StringComparer.OrdinalIgnoreCase)
                            .Select(r => r.SpeciesId + ":" + r.TemplateSpritePrefix + "->" + r.CustomSpritePrefix)
                            .ToArray());
            }
        }

        internal int RegisteredDiagnosticSpeciesCount
        {
            get
            {
                lock (gate)
                    return BuildDiagnosticSpeciesSet().Count;
            }
        }

        internal string RegisteredDiagnosticSpeciesSummary
        {
            get
            {
                lock (gate)
                {
                    HashSet<string> species = BuildDiagnosticSpeciesSet();
                    return species.Count == 0
                        ? "none"
                        : string.Join(",", species.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray());
                }
            }
        }

        internal int RegisteredCustomAnimalSpeciesCount
        {
            get
            {
                lock (gate)
                    return BuildDiagnosticSpeciesSet().Count;
            }
        }

        internal string RegisteredCustomAnimalSpeciesSummary
        {
            get
            {
                lock (gate)
                {
                    HashSet<string> species = BuildDiagnosticSpeciesSet();
                    return species.Count == 0
                        ? "none"
                        : string.Join(",", species.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray());
                }
            }
        }

        internal void ClearSaveLifetimeState(string reason)
        {
            int pngContexts;
            int sleepFollowUps;
            int renderDiagnostics;
            int playDiagnostics;
            if (runtime.ResourceLifecycleCleanupEnabled)
            {
                lock (gate)
                {
                    pngContexts = observedPngSpriteContextCount;
                    sleepFollowUps = observedSleepRenderFollowUpCount;
                    renderDiagnostics = lastRenderDiagnosticSignatures.Count;
                    playDiagnostics = lastPlayAnimationDiagnosticSignatures.Count;
                    pngSpriteContexts = new ConditionalWeakTable<object, PngSpriteOverrideContext>();
                    sleepRenderFollowUps = new ConditionalWeakTable<object, SleepRenderFollowUpContext>();
                    lastRenderDiagnosticSignatures.Clear();
                    lastPlayAnimationDiagnosticSignatures.Clear();
                    observedPngSpriteContextCount = 0;
                    observedSleepRenderFollowUpCount = 0;
                }

                int cleared = pngContexts + sleepFollowUps + renderDiagnostics + playDiagnostics;
                runtime.ReleaseResourceLifecycle(
                    "PngSpriteOverrideContext",
                    "all",
                    "DTMAPI.GameBridge.CustomAnimals",
                    string.Empty,
                    ResourceLifetime.SaveLifetime,
                    ResourceOwnership.DtmapiOwned,
                    "clear-on-save-boundary",
                    ResourceLifecycleStatus.Cleaned);
                runtime.ReleaseResourceLifecycle(
                    "SleepRenderFollowUpContext",
                    "all",
                    "DTMAPI.GameBridge.CustomAnimals",
                    string.Empty,
                    ResourceLifetime.SaveLifetime,
                    ResourceOwnership.DtmapiOwned,
                    "clear-on-save-boundary",
                    ResourceLifecycleStatus.Cleaned);
                runtime.ObserveResourceCleanup(
                    "CustomAnimals",
                    reason ?? string.Empty,
                    cleared,
                    "state=pngSpriteContexts:" + pngContexts +
                    "; sleepRenderFollowUps:" + sleepFollowUps +
                    "; renderDiagnostics:" + renderDiagnostics +
                    "; playDiagnostics:" + playDiagnostics +
                    "; cleanupEnabled=true");
                if (cleared > 0)
                    runtime.RuntimeMonitor.Log("CustomAnimals.AnimatorBridge cleared SaveLifetime contexts reason=" + (reason ?? string.Empty) + " count=" + cleared + ".");
                return;
            }

            lock (gate)
            {
                pngContexts = observedPngSpriteContextCount;
                sleepFollowUps = observedSleepRenderFollowUpCount;
                renderDiagnostics = lastRenderDiagnosticSignatures.Count;
                playDiagnostics = lastPlayAnimationDiagnosticSignatures.Count;
            }

            runtime.ObserveResourceCleanup(
                "CustomAnimals",
                reason ?? string.Empty,
                0,
                "state=pngSpriteContexts:" + pngContexts +
                "; sleepRenderFollowUps:" + sleepFollowUps +
                "; renderDiagnostics:" + renderDiagnostics +
                "; playDiagnostics:" + playDiagnostics +
                "; cleanupEnabled=false");
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            int removed = 0;
            lock (gate)
            {
                string[] animatorKeys = registrations
                    .Where(pair => pair.Value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (string key in animatorKeys)
                {
                    registrations.Remove(key);
                    degradedReasons.Remove(key);
                    verifiedKeys.Remove(key);
                    removed++;
                }

                string[] aiSpecies = aiTemplateRegistrations
                    .Where(pair => pair.Value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (string species in aiSpecies)
                {
                    aiTemplateRegistrations.Remove(species);
                    aiTemplateDegradedReasons.Remove(species);
                    verifiedAiTemplates.Remove(species);
                    removed++;
                }

                string[] pngSpecies = pngSpriteRegistrationsBySpecies
                    .Where(pair => pair.Value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (string species in pngSpecies)
                {
                    pngSpriteRegistrationsBySpecies.Remove(species);
                    pngSpriteDegradedReasons.Remove(species);
                    verifiedPngSpriteSpecies.Remove(species);
                    removed++;
                }

                ownerDefinitionGenerations.Remove(ownerId);
                if (removed > 0)
                {
                    controllerCache.Clear();
                    bundleCache.Clear();
                    aiDefaultStateTypeCache.Clear();
                    pngSpriteContexts = new ConditionalWeakTable<object, PngSpriteOverrideContext>();
                    sleepRenderFollowUps = new ConditionalWeakTable<object, SleepRenderFollowUpContext>();
                    lastRenderDiagnosticSignatures.Clear();
                    lastPlayAnimationDiagnosticSignatures.Clear();
                    observedPngSpriteContextCount = 0;
                    observedSleepRenderFollowUpCount = 0;
                }
                RebuildCallbackDemandMaskUnsafe();
            }
            ReconcileOwnerDefinitionDemand(ownerId, "custom-animal owner cleanup " + (reason ?? string.Empty));
            return removed;
        }

        private void ReconcileOwnerDefinitionDemand(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            bool active;
            lock (gate)
            {
                active = registrations.Values.Any(value => value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) ||
                    aiTemplateRegistrations.Values.Any(value => value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) ||
                    pngSpriteRegistrationsBySpecies.Values.Any(value => value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            }
            GameBridgeDemandRoutes.SetOwnerDemand(
                runtime,
                GameBridgeDemandRoutes.CustomAnimalAnimatorBridge,
                ownerId,
                RuntimeDemandSourceType.ContentDefinition,
                RuntimeDemandLifetime.Owner,
                "committed-definitions",
                active,
                reason ?? string.Empty);
        }

        private void ReconcileOwnerDefinitionDemandBestEffort(string ownerId, long generation, string reason)
        {
            try
            {
                ReconcileOwnerDefinitionDemand(ownerId, reason);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, generation, ex);
            }
        }

        private void RecordPostCommitSideEffectFailure(string ownerId, long generation, Exception exception)
        {
            try
            {
                runtime.Diagnostics.RecordError(
                    ownerId ?? "DTMAPI.GameBridge.CustomAnimals",
                    "Custom animal generation committed, but a post-commit side effect failed; visible state and terminal receipt remain committed. generation=" + generation.ToString(CultureInfo.InvariantCulture) + ".",
                    exception.ToString());
            }
            catch
            {
            }
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            lock (gate)
            {
                return registrations.Values.Count(value => value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) +
                    aiTemplateRegistrations.Values.Count(value => value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) +
                    pngSpriteRegistrationsBySpecies.Values.Count(value => value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            }
        }

        internal int DefinitionCandidateBuildCountForTest => definitionCandidateBuildCount;
        internal long ContentProjectionBuildCountForTest => contentProjectionBuildCountForTest;

        internal long RetainedCallbackWorkCountForTest => retainedCallbackWorkCountForTest;

        internal long OptionalFileStatusCallCount => optionalFileStatusCallCount;

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

        internal bool HasPngSpriteCallbackDemand => HasCallbackDemand(PngSpriteCallbackDemand);

        internal bool HasDiagnosticCallbackDemand => HasCallbackDemand(DiagnosticCallbackDemand);

        internal bool HasSleepTaskCallbackDemand => HasCallbackDemand(SleepTaskCallbackDemand);

        private bool HasCallbackDemand(int demand) => (Volatile.Read(ref callbackDemandMask) & demand) != 0;

        private void RebuildCallbackDemandMaskUnsafe()
        {
            int mask = 0;
            if (registrations.Count > 0)
                mask |= AnimatorCallbackDemand;
            if (aiTemplateRegistrations.Count > 0)
                mask |= AiTemplateCallbackDemand;
            if (pngSpriteRegistrationsBySpecies.Count > 0)
                mask |= PngSpriteCallbackDemand;
            if (registrations.Count > 0 || aiTemplateRegistrations.Count > 0 || pngSpriteRegistrationsBySpecies.Count > 0)
                mask |= DiagnosticCallbackDemand | SleepTaskCallbackDemand;
            Volatile.Write(ref callbackDemandMask, mask);
        }

        internal bool HasRegisteredBehaviorDefinitions
        {
            get
            {
                lock (gate)
                {
                    return registrations.Count > 0 ||
                        aiTemplateRegistrations.Count > 0 ||
                        pngSpriteRegistrationsBySpecies.Count > 0;
                }
            }
        }

        internal long GetOwnerDefinitionGenerationForTest(string ownerId)
        {
            lock (gate)
                return ownerDefinitionGenerations.TryGetValue(ownerId ?? string.Empty, out long generation) ? generation : 0;
        }

        internal bool RefreshDefinitions(string reason, bool force)
        {
            if (force)
            {
                runtime.ContentRefreshGenerations.MarkDirty(
                    ContentRefreshDomains.CustomAnimals,
                    runtime.LoadedMods.Select(mod => mod.Manifest.UniqueID),
                    reason ?? "forced custom animal refresh");
            }

            if (!runtime.ContentRefreshGenerations.TryGetDirty(ContentRefreshDomains.CustomAnimals, out ContentRefreshDirtyBatch dirtyBatch))
                return false;

            bool generationCompleted = false;
            try
            {
            IReadOnlyList<DiscoveredMod> loadedMods = runtime.LoadedMods;
            contentProjectionBuildCountForTest++;
            DiscoveredMod[] enabledContentPacks = loadedMods.Where(IsEnabledContentPack).ToArray();
            var enabledByOwner = enabledContentPacks
                .GroupBy(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var ownerLoadOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < enabledContentPacks.Length; index++)
            {
                string ownerId = enabledContentPacks[index].Manifest.UniqueID;
                if (!ownerLoadOrder.ContainsKey(ownerId))
                    ownerLoadOrder[ownerId] = index;
            }
            var activeOwners = new HashSet<string>(enabledByOwner.Keys, StringComparer.OrdinalIgnoreCase);
            var targetOwners = new HashSet<string>(dirtyBatch.OwnerIds.Where(owner => !string.Equals(owner, "all", StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);
            bool refreshAll = dirtyBatch.OwnerIds.Count == 0 || dirtyBatch.OwnerIds.Any(owner => string.Equals(owner, "all", StringComparison.OrdinalIgnoreCase));
            var next = new Dictionary<string, CustomAnimalAnimatorRegistration>(StringComparer.OrdinalIgnoreCase);
            var nextAiTemplates = new Dictionary<string, CustomAnimalAiTemplateRegistration>(StringComparer.OrdinalIgnoreCase);
            var nextOwnerGenerations = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            lock (gate)
            {
                foreach (KeyValuePair<string, CustomAnimalAnimatorRegistration> pair in registrations)
                {
                    if (activeOwners.Contains(pair.Value.OwnerId))
                        next[pair.Key] = pair.Value;
                }
                foreach (KeyValuePair<string, CustomAnimalAiTemplateRegistration> pair in aiTemplateRegistrations)
                {
                    if (activeOwners.Contains(pair.Value.OwnerId))
                        nextAiTemplates[pair.Key] = pair.Value;
                }
                foreach (KeyValuePair<string, long> pair in ownerDefinitionGenerations)
                {
                    if (activeOwners.Contains(pair.Key))
                        nextOwnerGenerations[pair.Key] = pair.Value;
                    if (refreshAll || !activeOwners.Contains(pair.Key))
                        targetOwners.Add(pair.Key);
                }
            }
            if (refreshAll)
            {
                foreach (string ownerId in activeOwners)
                    targetOwners.Add(ownerId);
            }

            var duplicateKeys = new List<string>();
            var duplicateAiTemplates = new List<string>();
            var completions = new ContentRefreshCompletionCollector();
            var rejectionPublications = new List<CustomAnimalOwnerRejectionPublication>();
            long nextGeneration = nextOwnerDefinitionGeneration;
            foreach (string ownerId in targetOwners.OrderBy(owner => ownerLoadOrder.TryGetValue(owner, out int index) ? index : int.MaxValue).ThenBy(owner => owner, StringComparer.OrdinalIgnoreCase))
            {
                nextOwnerGenerations.TryGetValue(ownerId, out long previousGeneration);
                if (!enabledByOwner.TryGetValue(ownerId, out DiscoveredMod? mod))
                {
                    RemoveOwnerFromCandidate(next, nextAiTemplates, ownerId);
                    nextOwnerGenerations.Remove(ownerId);
                    completions.Add(new ContentRefreshCompletion(
                        ownerId,
                        ContentRefreshCompletionStatus.Removed,
                        previousGeneration,
                        0,
                        0,
                        "Owner is no longer active; definition generation removed."));
                    continue;
                }

                string schemaPath = Path.Combine(mod.RootPath, "Content", "DTMAPI", "custom-animals.json");
                optionalFileStatusCallCount++;
                if (!File.Exists(schemaPath))
                {
                    if (previousGeneration == 0)
                    {
                        completions.Add(new ContentRefreshCompletion(
                            ownerId,
                            ContentRefreshCompletionStatus.Unchanged,
                            0,
                            0,
                            0,
                            "Owner does not declare Content/DTMAPI/custom-animals.json."));
                        continue;
                    }

                    string failure = "Content/DTMAPI/custom-animals.json is missing.";
                    rejectionPublications.Add(new CustomAnimalOwnerRejectionPublication(ownerId, previousGeneration, dirtyBatch, failure));
                    completions.Add(new ContentRefreshCompletion(ownerId, ContentRefreshCompletionStatus.Rejected, previousGeneration, previousGeneration, previousGeneration, failure));
                    continue;
                }

                try
                {
                    definitionCandidateBuildCount++;
                    CustomAnimalDefinitionModel[] definitions = LoadDefinitionsCandidate(schemaPath);
                    ValidateDefinitions(definitions);
                    CustomAnimalAnimatorRegistration[] candidateRegistrations = BuildRegistrations(ownerId, mod.RootPath, definitions).ToArray();
                    CustomAnimalAiTemplateRegistration[] candidateAiTemplates = BuildAiTemplateRegistrations(ownerId, definitions).ToArray();
                    ValidateOwnerCandidate(ownerId, candidateRegistrations, candidateAiTemplates);

                    var withoutOwner = new Dictionary<string, CustomAnimalAnimatorRegistration>(next, StringComparer.OrdinalIgnoreCase);
                    var withoutOwnerAi = new Dictionary<string, CustomAnimalAiTemplateRegistration>(nextAiTemplates, StringComparer.OrdinalIgnoreCase);
                    RemoveOwnerFromCandidate(withoutOwner, withoutOwnerAi, ownerId);
                    string? conflict = FindOwnerCandidateConflict(candidateRegistrations, candidateAiTemplates, withoutOwner, withoutOwnerAi);
                    if (!string.IsNullOrWhiteSpace(conflict))
                        throw new InvalidDataException(conflict);

                    foreach (CustomAnimalAnimatorRegistration registration in candidateRegistrations)
                        withoutOwner[registration.AnimatorKey] = registration;
                    foreach (CustomAnimalAiTemplateRegistration registration in candidateAiTemplates)
                        withoutOwnerAi[registration.SpeciesId] = registration;
                    next = withoutOwner;
                    nextAiTemplates = withoutOwnerAi;
                    long committedGeneration = checked(++nextGeneration);
                    nextOwnerGenerations[ownerId] = committedGeneration;
                    completions.Add(new ContentRefreshCompletion(
                        ownerId,
                        ContentRefreshCompletionStatus.Committed,
                        previousGeneration,
                        committedGeneration,
                        committedGeneration,
                        "animators=" + candidateRegistrations.Length + "; aiTemplates=" + candidateAiTemplates.Length));
                }
                catch (Exception ex)
                {
                    string failure = ex.GetType().Name + ": " + ex.Message;
                    rejectionPublications.Add(new CustomAnimalOwnerRejectionPublication(ownerId, previousGeneration, dirtyBatch, failure));
                    completions.Add(new ContentRefreshCompletion(ownerId, ContentRefreshCompletionStatus.Rejected, previousGeneration, previousGeneration, previousGeneration, failure));
                }
            }

            string[] demandReconciliationOwners = targetOwners
                .OrderBy(owner => owner, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var nextPngSpriteRegistrations = new Dictionary<string, CustomAnimalAnimatorRegistration>(StringComparer.OrdinalIgnoreCase);
            foreach (CustomAnimalAnimatorRegistration registration in next.Values.Where(r => r.IsPngSpriteOverride))
            {
                if (!nextPngSpriteRegistrations.ContainsKey(registration.SpeciesId))
                    nextPngSpriteRegistrations[registration.SpeciesId] = registration;
            }

            var staleControllerKeys = new List<string>();
            var staleBundlePaths = new List<string>();
            // The generation receipt and the corresponding live definition snapshot
            // share one authority edge. Everything above is candidate construction;
            // validation rejects stale work before this publisher can run.
            beforeGenerationCompleteForTest?.Invoke(dirtyBatch);
            generationCompleted = runtime.ContentRefreshGenerations.CompleteWithAtomicCommit(
                dirtyBatch,
                completions,
                () =>
                {
                    preCommitFaultForTest?.Invoke("before-visible-snapshot-swap");
                    lock (gate)
                    {
                        registrations = next;
                        aiTemplateRegistrations = nextAiTemplates;
                        ownerDefinitionGenerations = nextOwnerGenerations;
                        pngSpriteRegistrationsBySpecies = nextPngSpriteRegistrations;
                        nextOwnerDefinitionGeneration = nextGeneration;
                        RebuildCallbackDemandMaskUnsafe();
                    }
                });
            if (!generationCompleted)
                throw new InvalidOperationException("Custom-animal content generation completion was rejected as stale.");

                try
                {
                    foreach (CustomAnimalOwnerRejectionPublication publication in rejectionPublications)
                        PublishOwnerGenerationRejectedBestEffort(publication);

                    // Diagnostic/cache pruning is post-commit hygiene. It must never be able
                    // to publish a candidate early or requeue the authoritative generation.
                    lock (gate)
                    {
                        foreach (string key in degradedReasons.Keys.Where(k => !registrations.ContainsKey(k)).ToArray())
                            degradedReasons.Remove(key);
                        foreach (string key in verifiedKeys.Where(k => !registrations.ContainsKey(k)).ToArray())
                            verifiedKeys.Remove(key);
                        foreach (string key in aiTemplateDegradedReasons.Keys.Where(k => !aiTemplateRegistrations.ContainsKey(k)).ToArray())
                            aiTemplateDegradedReasons.Remove(key);
                        foreach (string key in verifiedAiTemplates.Where(k => !aiTemplateRegistrations.ContainsKey(k)).ToArray())
                            verifiedAiTemplates.Remove(key);
                        foreach (string key in pngSpriteDegradedReasons.Keys.Where(k => !pngSpriteRegistrationsBySpecies.ContainsKey(k)).ToArray())
                            pngSpriteDegradedReasons.Remove(key);
                        foreach (string key in verifiedPngSpriteSpecies.Where(k => !pngSpriteRegistrationsBySpecies.ContainsKey(k)).ToArray())
                            verifiedPngSpriteSpecies.Remove(key);
                        HashSet<string> diagnosticSpecies = BuildDiagnosticSpeciesSet();
                        foreach (string key in lastRenderDiagnosticSignatures.Keys.Where(k => !diagnosticSpecies.Contains(ExtractDiagnosticSpeciesKey(k))).ToArray())
                            lastRenderDiagnosticSignatures.Remove(key);
                        foreach (string key in lastPlayAnimationDiagnosticSignatures.Keys.Where(k => !diagnosticSpecies.Contains(ExtractDiagnosticSpeciesKey(k))).ToArray())
                            lastPlayAnimationDiagnosticSignatures.Remove(key);
                        foreach (string key in sleepWakeStatusPublished.Where(k =>
                        {
                            int separator = k.IndexOf('|');
                            string species = separator < 0 ? k : k.Substring(0, separator);
                            return !diagnosticSpecies.Contains(species);
                        }).ToArray())
                        {
                            sleepWakeStatusPublished.Remove(key);
                        }

                        foreach (string key in sleepTaskBoundaryEvents.Where(k =>
                        {
                            int separator = k.IndexOf('|');
                            string species = separator < 0 ? k : k.Substring(0, separator);
                            return !string.Equals(species, "degraded", StringComparison.OrdinalIgnoreCase) && !diagnosticSpecies.Contains(species);
                        }).ToArray())
                        {
                            sleepTaskBoundaryEvents.Remove(key);
                        }

                        if (diagnosticSpecies.Count == 0)
                            sleepTaskBoundaryDegradedReason = string.Empty;

                        var nextControllerKeys = new HashSet<string>(next.Values.Where(r => !string.IsNullOrWhiteSpace(r.ControllerCacheKey)).Select(r => r.ControllerCacheKey), StringComparer.OrdinalIgnoreCase);
                        foreach (string key in controllerCache.Keys.Where(k => !nextControllerKeys.Contains(k)).ToArray())
                        {
                            staleControllerKeys.Add(key);
                            controllerCache.Remove(key);
                        }

                        var nextBundlePaths = new HashSet<string>(next.Values.Where(r => !string.IsNullOrWhiteSpace(r.BundlePath)).Select(r => r.BundlePath), StringComparer.OrdinalIgnoreCase);
                        foreach (string key in bundleCache.Keys.Where(k => !nextBundlePaths.Contains(k)).ToArray())
                        {
                            staleBundlePaths.Add(key);
                            bundleCache.Remove(key);
                        }
                    }

                    postCommitFaultForTest?.Invoke("terminal-receipt-and-visible-snapshot-committed");

                    foreach (CustomAnimalAnimatorRegistration registration in next.Values)
                    {
                        runtime.ObserveResourceLifecycle(
                            "CustomAnimalDefinition",
                            registration.SpeciesId,
                            registration.OwnerId,
                            string.IsNullOrWhiteSpace(registration.BundlePath) ? registration.FrameManifestPath : registration.BundlePath,
                            ResourceLifetime.TitleLifetime,
                            ResourceOwnership.DtmapiOwned,
                            ResourceLifecycleStatus.Declared,
                            "content-generation-rebuild-only");
                        runtime.ObserveResourceLifecycle(
                            "AnimatorRegistration",
                            registration.AnimatorKey,
                            registration.OwnerId,
                            string.IsNullOrWhiteSpace(registration.BundlePath) ? registration.FrameManifestPath : registration.BundlePath,
                            ResourceLifetime.TitleLifetime,
                            ResourceOwnership.DtmapiOwned,
                            ResourceLifecycleStatus.Declared,
                            "content-generation-rebuild-only");
                    }

                    foreach (CustomAnimalAiTemplateRegistration registration in nextAiTemplates.Values)
                    {
                        runtime.ObserveResourceLifecycle(
                            "AiTemplateRegistration",
                            registration.SpeciesId,
                            registration.OwnerId,
                            string.Empty,
                            ResourceLifetime.TitleLifetime,
                            ResourceOwnership.DtmapiOwned,
                            ResourceLifecycleStatus.Declared,
                            "content-generation-rebuild-only");
                    }

                    foreach (string key in staleControllerKeys)
                    {
                        runtime.ReleaseResourceLifecycle(
                            "AnimatorControllerCacheRef",
                            key,
                            "DTMAPI.GameBridge.CustomAnimals",
                            string.Empty,
                            ResourceLifetime.TitleLifetime,
                            ResourceOwnership.DtmapiOwned,
                            "drop-managed-ref-on-content-generation-replacement",
                            ResourceLifecycleStatus.DroppedManagedReference);
                    }

                    foreach (string path in staleBundlePaths)
                    {
                        runtime.ReleaseResourceLifecycle(
                            "AssetBundleCacheRef",
                            path,
                            "DTMAPI.GameBridge.CustomAnimals",
                            path,
                            ResourceLifetime.TitleLifetime,
                            ResourceOwnership.DtmapiOwned,
                            "drop-managed-ref-only-no-unload",
                            ResourceLifecycleStatus.DroppedManagedReference);
                    }

                    runtime.ObserveResourceRefresh("CustomAnimals", reason ?? string.Empty, ResourceLifecycleStatus.Rebuilt, next.Count + nextAiTemplates.Count);
                    if (duplicateKeys.Count > 0)
                    {
                        string details = string.Join("; ", duplicateKeys);
                        runtime.Diagnostics.RecordWarning("DTMAPI.GameBridge.CustomAnimals", "Duplicate custom animal animator keys were ignored.", details);
                        runtime.RuntimeMonitor.Log("CustomAnimals.AnimatorBridge duplicate keys ignored: " + details + ".", LogLevel.Warn);
                    }

                    if (duplicateAiTemplates.Count > 0)
                    {
                        string details = string.Join("; ", duplicateAiTemplates);
                        runtime.Diagnostics.RecordWarning("DTMAPI.GameBridge.CustomAnimals", "Duplicate custom animal AI template mappings were ignored.", details);
                        runtime.RuntimeMonitor.Log("CustomAnimals.AiTemplateBridge duplicate species mappings ignored: " + details + ".", LogLevel.Warn);
                    }

                    string pngProjection;
                    lock (gate)
                        pngProjection = FormatBoundedRefreshProjection(pngSpriteRegistrationsBySpecies.Keys, pngSpriteRegistrationsBySpecies.Count);
                    runtime.RuntimeMonitor.Log(BoundRefreshDiagnostic(
                        "CustomAnimals.AnimatorBridge refreshed reason=" + (reason ?? string.Empty) +
                        " registeredKeys=" + next.Count +
                        " keys=" + FormatBoundedRefreshProjection(next.Keys, next.Count) +
                        " aiTemplates=" + FormatBoundedRefreshProjection(nextAiTemplates.Values.Select(registration => registration.SpeciesId + "->" + registration.AiTemplate), nextAiTemplates.Count) +
                        " pngSpriteOverrides=" + pngProjection + "."));
                    runtime.ObserveLifecycleResourceEvent(
                        "CustomAnimals",
                        "AnimatorBridgeRefresh",
                        "all",
                        "custom-animal-animators",
                        "reason=" + (reason ?? string.Empty) + "; dirtyGeneration=" + dirtyBatch.Generation + "; registeredKeys=" + next.Count + "; aiTemplates=" + nextAiTemplates.Count);
                    return true;
                }
                finally
                {
                    foreach (string ownerId in demandReconciliationOwners)
                    {
                        ReconcileOwnerDefinitionDemandBestEffort(
                            ownerId,
                            dirtyBatch.Generation,
                            "custom-animal generation reconciled");
                    }
                }
            }
            catch (Exception ex)
            {
                if (!generationCompleted)
                {
                    runtime.ContentRefreshGenerations.AbandonAndRequeue(
                        dirtyBatch,
                        "CustomAnimals consumer failed: " + ex.GetType().Name + ": " + ex.Message);
                }
                throw;
            }
        }

        internal void PublishHookStatuses(
            bool animatorAssetTryLoadPatched,
            bool animalAIDefaultAnyStatePatched,
            bool animalOnRenderPatched = false,
            bool animalDebugSetAdultPatched = false,
            bool animalRendererOnRecyclePatched = false,
            bool spriteOverrideTryGetModOverrideSpritePatched = false,
            bool sleepWakeDiagnosticsPatched = false,
            bool sleepTaskBoundaryPatched = false)
        {
            int count;
            string keys;
            bool degraded;
            string degradedSummary;
            int aiTemplateCount;
            string aiTemplateSummary;
            bool aiTemplateDegraded;
            string aiTemplateDegradedSummary;
            int pngSpriteCount;
            string pngSpriteSummary;
            bool pngSpriteDegraded;
            string pngSpriteDegradedSummary;
            int diagnosticSpeciesCount;
            string diagnosticSpeciesSummary;
            bool sleepTaskBoundaryDegraded;
            string sleepTaskBoundaryReason;
            lock (gate)
            {
                count = registrations.Count;
                keys = registrations.Count == 0 ? "none" : string.Join(",", registrations.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray());
                degraded = degradedReasons.Count > 0;
                degradedSummary = degradedReasons.Count == 0
                    ? string.Empty
                    : string.Join("; ", degradedReasons.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => p.Key + "=" + p.Value).ToArray());
                aiTemplateCount = aiTemplateRegistrations.Count;
                aiTemplateSummary = aiTemplateRegistrations.Count == 0
                    ? "none"
                    : string.Join(",", aiTemplateRegistrations.Values.OrderBy(r => r.SpeciesId, StringComparer.OrdinalIgnoreCase).Select(r => r.SpeciesId + "->" + r.AiTemplate).ToArray());
                aiTemplateDegraded = aiTemplateDegradedReasons.Count > 0;
                aiTemplateDegradedSummary = aiTemplateDegradedReasons.Count == 0
                    ? string.Empty
                    : string.Join("; ", aiTemplateDegradedReasons.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => p.Key + "=" + p.Value).ToArray());
                pngSpriteCount = pngSpriteRegistrationsBySpecies.Count;
                pngSpriteSummary = pngSpriteRegistrationsBySpecies.Count == 0
                    ? "none"
                    : string.Join(",", pngSpriteRegistrationsBySpecies.Values.OrderBy(r => r.SpeciesId, StringComparer.OrdinalIgnoreCase).Select(r => r.SpeciesId + ":" + r.TemplateSpritePrefix + "->" + r.CustomSpritePrefix).ToArray());
                pngSpriteDegraded = pngSpriteDegradedReasons.Count > 0;
                pngSpriteDegradedSummary = pngSpriteDegradedReasons.Count == 0
                    ? string.Empty
                    : string.Join("; ", pngSpriteDegradedReasons.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => p.Key + "=" + p.Value).ToArray());
                HashSet<string> diagnosticSpecies = BuildDiagnosticSpeciesSet();
                diagnosticSpeciesCount = diagnosticSpecies.Count;
                diagnosticSpeciesSummary = diagnosticSpeciesCount == 0
                    ? "none"
                    : string.Join(",", diagnosticSpecies.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray());
                sleepTaskBoundaryDegraded = !string.IsNullOrWhiteSpace(sleepTaskBoundaryDegradedReason);
                sleepTaskBoundaryReason = sleepTaskBoundaryDegradedReason;
            }

            string status;
            string details;
            if (degraded)
            {
                status = "degraded";
                details = "AnimatorAsset.TryLoadAsset bridge is patched but at least one registered DTMAPI animal animator degraded: " + degradedSummary;
            }
            else if (animatorAssetTryLoadPatched && count > 0)
            {
                status = "verified";
                details = "AnimatorAsset.TryLoadAsset hook is patched; registered custom animal animator keys=" + keys + ".";
            }
            else if (animatorAssetTryLoadPatched)
            {
                status = "pending";
                details = "AnimatorAsset.TryLoadAsset hook is patched; waiting for enabled ContentPack custom-animals.json entries.";
            }
            else
            {
                status = "pending";
                details = "Waiting for DolocTown.AnimatorAsset.TryLoadAsset hook target; registered custom animal animator keys=" + keys + ".";
            }

            runtime.SetHookStatus(FeatureHookId, status, "DTMAPI.GameBridge.DolocTown CustomAnimalAnimatorBridgeFeature", details);

            string aiStatus;
            string aiDetails;
            if (aiTemplateDegraded)
            {
                aiStatus = "degraded";
                aiDetails = "AnimalAI.GetDefaultAnyState bridge is patched but at least one custom animal AI template mapping degraded: " + aiTemplateDegradedSummary;
            }
            else if (animalAIDefaultAnyStatePatched && aiTemplateCount > 0)
            {
                aiStatus = "verified";
                aiDetails = "AnimalAI.GetDefaultAnyState hook is patched; registered custom animal AI templates=" + aiTemplateSummary + ".";
            }
            else if (animalAIDefaultAnyStatePatched)
            {
                aiStatus = "pending";
                aiDetails = "AnimalAI.GetDefaultAnyState hook is patched; waiting for enabled ContentPack custom-animals.json aiTemplate entries.";
            }
            else
            {
                aiStatus = "pending";
                aiDetails = "Waiting for DolocTown.AnimalAI.GetDefaultAnyState hook target; registered custom animal AI templates=" + aiTemplateSummary + ".";
            }

            runtime.SetHookStatus(AiTemplateHookId, aiStatus, "DTMAPI.GameBridge.DolocTown CustomAnimalAnimatorBridgeFeature", aiDetails);

            bool pngHooksReady = animalOnRenderPatched && animalDebugSetAdultPatched && animalRendererOnRecyclePatched && spriteOverrideTryGetModOverrideSpritePatched;
            string pngStatus;
            string pngDetails;
            if (pngSpriteDegraded)
            {
                pngStatus = "degraded";
                pngDetails = "PNG SpriteOverride bridge is patched but at least one registered custom animal sprite mapping degraded: " + pngSpriteDegradedSummary;
            }
            else if (pngHooksReady && pngSpriteCount > 0)
            {
                pngStatus = "verified";
                pngDetails = "Animal render/recycle and SpriteOverrideHandler hooks are patched; registered PNG sprite override species=" + pngSpriteSummary + ".";
            }
            else if (pngHooksReady)
            {
                pngStatus = "pending";
                pngDetails = "PNG SpriteOverride hooks are patched; waiting for enabled ContentPack pngSpriteOverride entries.";
            }
            else
            {
                pngStatus = "pending";
                pngDetails = "Waiting for PNG SpriteOverride hook targets; registered PNG sprite override species=" + pngSpriteSummary + ".";
            }

            runtime.SetHookStatus(PngSpriteHookId, pngStatus, "DTMAPI.GameBridge.DolocTown CustomAnimalAnimatorBridgeFeature", pngDetails);

            string sleepWakeStatus;
            string sleepWakeDetails;
            if (sleepWakeDiagnosticsPatched && diagnosticSpeciesCount > 0)
            {
                sleepWakeStatus = "verified";
                sleepWakeDetails = "Animal sleep/wake diagnostic hooks are patched for custom animal species=" + diagnosticSpeciesSummary + ".";
            }
            else if (sleepWakeDiagnosticsPatched)
            {
                sleepWakeStatus = "pending";
                sleepWakeDetails = "Animal sleep/wake diagnostic hooks are patched; waiting for enabled ContentPack custom-animals.json entries.";
            }
            else
            {
                sleepWakeStatus = "pending";
                sleepWakeDetails = "Waiting for Animal.Sleep/WakeUp/CallToRoom/OnRender and AnimalRenderer.OnFell diagnostic hooks; registered custom animal species=" + diagnosticSpeciesSummary + ".";
            }

            runtime.SetHookStatus(SleepWakeDiagnosticsHookId, sleepWakeStatus, "DTMAPI.GameBridge.DolocTown CustomAnimalAnimatorBridgeFeature", sleepWakeDetails);

            string sleepTaskStatus;
            string sleepTaskDetails;
            if (sleepTaskBoundaryDegraded)
            {
                sleepTaskStatus = "degraded";
                sleepTaskDetails = "Custom animal sleep task boundary hooks are patched but degraded: " + sleepTaskBoundaryReason;
            }
            else if (sleepTaskBoundaryPatched && diagnosticSpeciesCount > 0)
            {
                sleepTaskStatus = "verified";
                sleepTaskDetails = "Custom animal sleep task boundary hooks are patched for species=" + diagnosticSpeciesSummary + ".";
            }
            else if (sleepTaskBoundaryPatched)
            {
                sleepTaskStatus = "pending";
                sleepTaskDetails = "Custom animal sleep task boundary hooks are patched; waiting for enabled ContentPack custom-animals.json entries.";
            }
            else
            {
                sleepTaskStatus = "pending";
                sleepTaskDetails = "Waiting for AnimalAI.MakeDecision_FreeTime and AnimalController.OnUpdate hooks; registered custom animal species=" + diagnosticSpeciesSummary + ".";
            }

            runtime.SetHookStatus(SleepTaskBoundaryHookId, sleepTaskStatus, "DTMAPI.GameBridge.DolocTown CustomAnimalAnimatorBridgeFeature", sleepTaskDetails);
        }

        internal AnimatorAssetLoadPrefixResult TryResolveRuntimeAnimatorController(string address)
        {
            if (!HasCallbackDemand(AnimatorCallbackDemand))
                return AnimatorAssetLoadPrefixResult.Unhandled();
            retainedCallbackWorkCountForTest++;
            if (!TryGetRegistration(address, out CustomAnimalAnimatorRegistration? registration) || registration == null)
                return AnimatorAssetLoadPrefixResult.Unhandled();

            if (registration.IsPngSpriteOverride)
            {
                object? templateController = TryLoadTemplateController(registration);
                if (templateController != null)
                {
                    MarkVerified(registration);
                    return AnimatorAssetLoadPrefixResult.HandledAsset(templateController);
                }

                MarkDegraded(registration, "pngSpriteOverride template controller missing " + registration.TemplateAnimatorKey);
                return AnimatorAssetLoadPrefixResult.HandledAsset(null);
            }

            object? controller = TryLoadBundleController(registration);
            if (controller != null)
            {
                MarkVerified(registration);
                return AnimatorAssetLoadPrefixResult.HandledAsset(controller);
            }

            object? fallback = TryLoadTemplateController(registration);
            if (fallback != null)
            {
                MarkDegraded(registration, "assetBundle unavailable; returned template fallback " + registration.TemplateAnimatorKey);
                return AnimatorAssetLoadPrefixResult.HandledAsset(fallback);
            }

            MarkDegraded(registration, "assetBundle unavailable and template fallback missing " + registration.TemplateAnimatorKey);
            return AnimatorAssetLoadPrefixResult.HandledAsset(null);
        }

        internal bool? CheckRuntimeAnimatorController(string address)
        {
            if (!HasCallbackDemand(AnimatorCallbackDemand))
                return null;
            retainedCallbackWorkCountForTest++;
            if (!TryGetRegistration(address, out CustomAnimalAnimatorRegistration? registration) || registration == null)
                return null;

            if (registration.IsPngSpriteOverride)
            {
                object? templateController = TryLoadTemplateController(registration);
                if (templateController != null)
                {
                    MarkVerified(registration);
                    return true;
                }

                MarkDegraded(registration, "pngSpriteOverride template controller missing during CheckAsset " + registration.TemplateAnimatorKey);
                return false;
            }

            object? controller = TryLoadBundleController(registration);
            if (controller != null)
            {
                MarkVerified(registration);
                return true;
            }

            object? fallback = TryLoadTemplateController(registration);
            if (fallback != null)
            {
                MarkDegraded(registration, "assetBundle unavailable during CheckAsset; template fallback exists " + registration.TemplateAnimatorKey);
                return true;
            }

            MarkDegraded(registration, "assetBundle unavailable during CheckAsset and template fallback missing " + registration.TemplateAnimatorKey);
            return false;
        }

        internal Type? TryResolveDefaultAnimalAIStateType(string speciesId, Type? originalResult)
        {
            if (!HasCallbackDemand(AiTemplateCallbackDemand))
                return originalResult;
            retainedCallbackWorkCountForTest++;
            string key = NormalizeKey(speciesId);
            CustomAnimalAiTemplateRegistration? registration;
            lock (gate)
                aiTemplateRegistrations.TryGetValue(key, out registration);
            if (registration == null)
                return originalResult;

            Type? stateType = ResolveAnimalAIDefaultStateType(registration.AiTemplate);
            if (stateType == null)
            {
                MarkAiTemplateDegraded(registration, "default state type missing for aiTemplate " + registration.AiTemplate);
                return originalResult;
            }

            MarkAiTemplateVerified(registration, stateType);
            return stateType;
        }

        internal void ApplyPngSpriteOverrideContext(object? animal)
        {
            if (!HasPngSpriteCallbackDemand || animal == null)
                return;
            retainedCallbackWorkCountForTest++;

            string species = ReadStringMember(animal, "protoName", "ProtoName");
            if (string.IsNullOrWhiteSpace(species))
                return;

            CustomAnimalAnimatorRegistration? registration;
            lock (gate)
                pngSpriteRegistrationsBySpecies.TryGetValue(species, out registration);
            if (registration == null)
                return;

            object? renderer = ReadMember(animal, "Renderer");
            if (renderer == null)
                return;

            object? handler = EnsureSpriteOverrideHandler(renderer);
            if (handler == null)
            {
                MarkPngSpriteDegraded(registration, "SpriteOverrideHandler unavailable for rendered animal");
                return;
            }

            var context = new PngSpriteOverrideContext(
                registration.OwnerId,
                registration.SpeciesId,
                registration.TemplateSpritePrefix,
                registration.CustomSpritePrefix,
                registration.FrameManifestPath);
            pngSpriteContexts.Remove(handler);
            pngSpriteContexts.Add(handler, context);
            lock (gate)
                observedPngSpriteContextCount++;
            runtime.ObserveResourceLifecycle(
                "PngSpriteOverrideContext",
                registration.SpeciesId,
                registration.OwnerId,
                registration.FrameManifestPath,
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "clear-on-save-boundary");
        }

        internal void RecordAnimalOnRenderDiagnostic(object? animal)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            RecordSleepWakeDiagnostic("Animal.OnRender", animal, "renderer=rendered", renderOnlyWhenSuspicious: true);
            TrackSleepRenderFollowUp(animal);
        }

        internal void RecordAnimalDebugSetAdultDiagnostic(object? animal, bool adult)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            RecordSleepWakeDiagnostic("Animal.DEBUG_SetAdult", animal, "adultArg=" + adult);
        }

        internal void RecordAnimalSleepDiagnostic(object? animal)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            RecordSleepWakeDiagnostic("Animal.Sleep", animal, "after=Sleep");
        }

        internal void RecordAnimalWakeUpDiagnostic(object? animal)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            RecordSleepWakeDiagnostic("Animal.WakeUp", animal, "after=WakeUp");
        }

        internal void RecordAnimalCallToRoomDiagnostic(object? animal, object? room, object? position)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            string targetRoom = DescribeRoom(room);
            string targetPosition = position?.ToString() ?? "unknown";
            RecordSleepWakeDiagnostic("Animal.CallToRoom", animal, "targetRoom=" + targetRoom + " targetPosition=" + targetPosition);
        }

        internal void RecordAnimalRendererOnFellPrefixDiagnostic(object? renderer, object? tool)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            object? animal = renderer == null ? null : ReadMember(renderer, "animal", "Animal");
            RecordSleepWakeDiagnostic("AnimalRenderer.OnFell.Prefix", animal, "tool=" + DescribeObject(tool));
        }

        internal void RecordAnimalRendererOnFellPostfixDiagnostic(object? renderer, bool result)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            object? animal = renderer == null ? null : ReadMember(renderer, "animal", "Animal");
            RecordSleepWakeDiagnostic("AnimalRenderer.OnFell.Postfix", animal, "result=" + result);
        }

        internal void RecordAnimalRendererPlayAnimationDiagnostic(object? renderer, string? animationName, bool force, float normalizedTime)
        {
            if (!HasDiagnosticCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            string animName = animationName ?? string.Empty;
            bool isSleepCommand = string.Equals(animName, "sleep", StringComparison.Ordinal);
            bool hasFollowUp = renderer != null && sleepRenderFollowUps.TryGetValue(renderer, out _);
            if (!isSleepCommand && !hasFollowUp)
                return;

            object? animal = renderer == null ? null : ReadMember(renderer, "animal", "Animal");
            if (!TryDescribeCustomAnimal(animal, out string species, out string snapshot, out string identityKey, out bool? isSleep, out _, out _, out string rendererState))
                return;
            if (isSleep != true && IsRendererInSleepState(rendererState) && !hasFollowUp)
                return;
            if (!isSleepCommand && isSleep != true && !hasFollowUp)
                return;

            string reason = isSleepCommand ? "sleep-command" : "sleeping-non-sleep-animation";
            string key = species + "|" + (string.IsNullOrWhiteSpace(identityKey) ? "unknown" : identityKey) + "|" + reason + "|" + animName;
            string signature = FormatNullableBool(isSleep) + "|" + rendererState;
            lock (gate)
            {
                if (lastPlayAnimationDiagnosticSignatures.ContainsKey(key))
                    return;

                lastPlayAnimationDiagnosticSignatures[key] = signature;
            }

            runtime.RuntimeMonitor.Log(
                "CustomAnimals.SleepWakeDiagnostics event=AnimalRenderer.PlayAnimation" +
                " species=" + species +
                " " + snapshot +
                " animName=" + animName +
                " force=" + force +
                " normalizedTime=" + normalizedTime.ToString("0.###", CultureInfo.InvariantCulture) +
                " reason=" + reason +
                " afterPlayRendererState=" + rendererState +
                ".");
        }

        internal void RecordAnimalRendererFixedUpdateDiagnostic(object? renderer)
        {
            if (!HasDiagnosticCallbackDemand || renderer == null)
                return;
            retainedCallbackWorkCountForTest++;

            SleepRenderFollowUpContext? followUp = null;
            lock (gate)
                if (sleepRenderFollowUps.TryGetValue(renderer, out SleepRenderFollowUpContext? pending))
                    followUp = pending;

            if (followUp == null)
                return;

            object? animal = ReadMember(renderer, "animal", "Animal");
            string rendererState = DescribeRendererState(renderer);
            if (!TryDescribeCustomAnimal(animal, out string species, out string snapshot, out _, out bool? isSleep, out string aiState, out string task, out _))
            {
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.SleepWakeDiagnostics event=AnimalRenderer.FixedUpdateAfterRender" +
                    " species=" + followUp.Species +
                    " animal=" + followUp.IdentityKey +
                    " rendererState=" + rendererState +
                    " onRenderState=" + followUp.RendererState +
                    " note=animal_context_missing_or_no_longer_custom.");
                RemoveSleepRenderFollowUp(renderer, "fixed-update-context-missing");
                return;
            }

            if (IsSettledSleepingRendererFollowUp(isSleep, aiState, task, rendererState))
            {
                RemoveSleepRenderFollowUp(renderer, "fixed-update-settled");
                return;
            }

            int frame;
            bool shouldLog;
            bool removeExpired;
            string signature = FormatNullableBool(isSleep) + "|" + aiState + "|" + task + "|" + NormalizeRendererStateForDiagnosticSignature(rendererState);
            lock (gate)
            {
                frame = followUp.AdvanceFrame();
                shouldLog = followUp.ShouldLog(frame, signature);
                removeExpired = frame >= SleepRenderFollowUpMaxFrames || isSleep != true;
            }
            if (removeExpired)
                RemoveSleepRenderFollowUp(renderer, "fixed-update-expired");

            if (shouldLog)
            {
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.SleepWakeDiagnostics event=AnimalRenderer.FixedUpdateFollowUp" +
                    " species=" + species +
                    " " + snapshot +
                    " followUpFrame=" + frame.ToString(CultureInfo.InvariantCulture) +
                    " followUpReason=" + followUp.Reason +
                    " onRenderAiState=" + followUp.AiState +
                    " onRenderTask=" + followUp.Task +
                    " onRenderRendererState=" + followUp.RendererState +
                    " fixedUpdateRendererState=" + rendererState +
                    ".");
            }
        }

        internal void ClearPngSpriteOverrideContext(object? renderer)
        {
            if (!HasPngSpriteCallbackDemand || renderer == null)
                return;
            retainedCallbackWorkCountForTest++;

            object? handler = GetSpriteOverrideHandler(renderer, addIfMissing: false);
            if (handler != null)
            {
                if (pngSpriteContexts.TryGetValue(handler, out PngSpriteOverrideContext? context))
                {
                    runtime.ReleaseResourceLifecycle(
                        "PngSpriteOverrideContext",
                        context.SpeciesId,
                        context.OwnerId,
                        context.FrameManifestPath,
                        ResourceLifetime.SaveLifetime,
                        ResourceOwnership.DtmapiOwned,
                        "clear-on-renderer-release",
                        ResourceLifecycleStatus.Cleaned);
                }

                pngSpriteContexts.Remove(handler);
            }

            RemoveSleepRenderFollowUp(renderer, "clear-on-renderer-release");
        }

        private void RemoveSleepRenderFollowUp(object renderer, string releasePolicy)
        {
            SleepRenderFollowUpContext? followUp = null;
            lock (gate)
            {
                if (sleepRenderFollowUps.TryGetValue(renderer, out SleepRenderFollowUpContext? existing))
                    followUp = existing;
                sleepRenderFollowUps.Remove(renderer);
            }

            if (followUp == null)
                return;

            runtime.ReleaseResourceLifecycle(
                "SleepRenderFollowUpContext",
                followUp.Species,
                "DTMAPI.GameBridge.CustomAnimals",
                string.Empty,
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.DtmapiOwned,
                releasePolicy ?? "clear-on-renderer-release",
                ResourceLifecycleStatus.Cleaned);
        }

        internal PngSpriteOverrideResult TryResolvePngSpriteOverride(object? handler, string oldSpriteName, object? originalSprite)
        {
            if (!HasPngSpriteCallbackDemand)
                return PngSpriteOverrideResult.Unhandled();
            retainedCallbackWorkCountForTest++;
            if (handler == null || !pngSpriteContexts.TryGetValue(handler, out PngSpriteOverrideContext? context))
                return PngSpriteOverrideResult.Unhandled();

            CustomAnimalAnimatorRegistration? registration;
            lock (gate)
                pngSpriteRegistrationsBySpecies.TryGetValue(context.SpeciesId, out registration);
            if (registration == null)
            {
                runtime.ReleaseResourceLifecycle(
                    "PngSpriteOverrideContext",
                    context.SpeciesId,
                    context.OwnerId,
                    context.FrameManifestPath,
                    ResourceLifetime.SaveLifetime,
                    ResourceOwnership.DtmapiOwned,
                    "registration-missing",
                    ResourceLifecycleStatus.Cleaned);
                pngSpriteContexts.Remove(handler);
                return PngSpriteOverrideResult.Unhandled();
            }

            string customSpriteName = MapPngSpriteName(context.TemplateSpritePrefix, context.CustomSpritePrefix, oldSpriteName);
            if (string.IsNullOrWhiteSpace(customSpriteName))
                return PngSpriteOverrideResult.Unhandled();

            if (!TryLoadModSpriteFromFile(customSpriteName, out object? sprite) || sprite == null)
            {
                MarkPngSpriteDegraded(registration, "missing mapped sprite " + customSpriteName + " for " + oldSpriteName);
                return PngSpriteOverrideResult.NoOverride();
            }

            MarkPngSpriteVerified(context, oldSpriteName, customSpriteName);
            return PngSpriteOverrideResult.OverrideSprite(sprite);
        }

        internal AnimalAITaskPrefixResult TryPreventSleepingFreeTimeDecision(object? state)
        {
            if (!HasSleepTaskCallbackDemand)
                return AnimalAITaskPrefixResult.Continue();
            retainedCallbackWorkCountForTest++;
            object? animal = ReadMember(state, "animal", "Animal");
            if (!ShouldApplySleepTaskBoundary(animal, out string species, out string snapshot))
                return AnimalAITaskPrefixResult.Continue();

            object? waitTask = CreateSleepBoundaryWaitTask(5);
            if (waitTask == null)
            {
                MarkSleepTaskBoundaryDegraded("LinearTask.WaitFrames unavailable for sleeping FreeTime guard");
                return AnimalAITaskPrefixResult.Continue();
            }

            RecordSleepTaskBoundaryEvent("FreeTimeDecisionGuard", species, snapshot, "replacement=WaitFrames(5)");
            return AnimalAITaskPrefixResult.UseTask(waitTask);
        }

        internal void EnforceSleepTaskBoundaryAfterAnimalControllerUpdate(object? controller)
        {
            if (!HasSleepTaskCallbackDemand)
                return;
            retainedCallbackWorkCountForTest++;
            object? animal = ReadMember(controller, "animal", "Animal");
            if (!ShouldApplySleepTaskBoundary(animal, out string species, out string snapshot))
                return;

            string task = DescribeTypeMember(controller, "CurrentTaskType");
            if (!IsMovementAnimalTaskName(task))
                return;

            object? result = InvokeInstanceMethod(controller, "StopTask");
            RecordSleepTaskBoundaryEvent("StopSleepingMoveTask", species, snapshot, "stoppedTask=" + task + " result=" + FormatLogValue(result));
        }

        internal static bool ShouldApplySleepTaskBoundaryForTest(bool isRegisteredCustomAnimal, bool? isSleep, string dayPeriod)
        {
            return ShouldApplySleepTaskBoundaryCore(isRegisteredCustomAnimal, isSleep, dayPeriod);
        }

        internal static bool IsMovementAnimalTaskNameForTest(string taskTypeName)
        {
            return IsMovementAnimalTaskName(taskTypeName);
        }

        internal static IReadOnlyList<string> BuildRegistrationSummariesForTest(string ownerId, string rootPath, string json)
        {
            CustomAnimalDefinitionModel[] definitions = ReadDefinitions(json);
            return BuildRegistrations(ownerId, rootPath, definitions)
                .Select(r => r.AnimatorKey + "|" + r.Stage + "|" + r.BundlePath + "|" + r.AssetName + "|" + r.TemplateAnimatorKey)
                .ToArray();
        }

        internal static string MapPngSpriteNameForTest(string templateSpritePrefix, string customSpritePrefix, string oldSpriteName)
        {
            return MapPngSpriteName(templateSpritePrefix, customSpritePrefix, oldSpriteName);
        }

        internal static IReadOnlyList<string> BuildAiTemplateSummariesForTest(string ownerId, string json)
        {
            CustomAnimalDefinitionModel[] definitions = ReadDefinitions(json);
            return BuildAiTemplateRegistrations(ownerId, definitions)
                .Select(r => r.SpeciesId + "|" + r.AiTemplate)
                .ToArray();
        }

        internal static IReadOnlyList<string> BuildDiagnosticSpeciesSummariesForTest(string ownerId, string rootPath, string json)
        {
            CustomAnimalDefinitionModel[] definitions = ReadDefinitions(json);
            var species = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CustomAnimalAnimatorRegistration registration in BuildRegistrations(ownerId, rootPath, definitions))
            {
                if (!string.IsNullOrWhiteSpace(registration.SpeciesId))
                    species.Add(registration.SpeciesId);
            }

            foreach (CustomAnimalAiTemplateRegistration registration in BuildAiTemplateRegistrations(ownerId, definitions))
            {
                if (!string.IsNullOrWhiteSpace(registration.SpeciesId))
                    species.Add(registration.SpeciesId);
            }

            return species.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private bool TryGetRegistration(string address, out CustomAnimalAnimatorRegistration? registration)
        {
            string key = NormalizeKey(address);
            lock (gate)
                return registrations.TryGetValue(key, out registration);
        }

        private static CustomAnimalDefinitionModel[] LoadDefinitionsCandidate(string schemaPath)
        {
            string json = File.ReadAllText(schemaPath, Encoding.UTF8);
            return ReadDefinitions(json);
        }

        private static void ValidateDefinitions(IEnumerable<CustomAnimalDefinitionModel> definitions)
        {
            int index = 0;
            foreach (CustomAnimalDefinitionModel definition in definitions ?? Array.Empty<CustomAnimalDefinitionModel>())
            {
                if (definition == null)
                    throw new InvalidDataException("Definition row " + index + " is null.");
                definition.Normalize();
                if (string.IsNullOrWhiteSpace(definition.SpeciesId))
                    throw new InvalidDataException("Definition row " + index + " requires speciesId.");

                bool hasAdultKey = !string.IsNullOrWhiteSpace(definition.AdultAnimatorKey);
                bool hasChildKey = !string.IsNullOrWhiteSpace(definition.ChildAnimatorKey);
                if (string.Equals(definition.AnimatorMode, "assetBundle", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(definition.AnimatorBundle))
                        throw new InvalidDataException("Definition row " + index + " assetBundle mode requires animatorBundle.");
                    if (hasAdultKey != !string.IsNullOrWhiteSpace(definition.AdultAnimatorAsset))
                        throw new InvalidDataException("Definition row " + index + " adultAnimatorKey/adultAnimatorAsset must be supplied together.");
                    if (hasChildKey != !string.IsNullOrWhiteSpace(definition.ChildAnimatorAsset))
                        throw new InvalidDataException("Definition row " + index + " childAnimatorKey/childAnimatorAsset must be supplied together.");
                    if (!hasAdultKey && !hasChildKey)
                        throw new InvalidDataException("Definition row " + index + " assetBundle mode requires an adult or child animator mapping.");
                }
                else if (string.Equals(definition.AnimatorMode, "pngSpriteOverride", StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasAdultKey && !hasChildKey)
                        throw new InvalidDataException("Definition row " + index + " pngSpriteOverride mode requires an adult or child animator key.");
                    if (string.IsNullOrWhiteSpace(definition.TemplateSpritePrefix) || string.IsNullOrWhiteSpace(definition.CustomSpritePrefix))
                        throw new InvalidDataException("Definition row " + index + " pngSpriteOverride mode requires templateSpritePrefix and customSpritePrefix.");
                }

                index++;
            }
        }

        private static void ValidateOwnerCandidate(
            string ownerId,
            IEnumerable<CustomAnimalAnimatorRegistration> candidateRegistrations,
            IEnumerable<CustomAnimalAiTemplateRegistration> candidateAiTemplates)
        {
            string[] duplicateAnimatorKeys = candidateRegistrations
                .GroupBy(registration => registration.AnimatorKey, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateAnimatorKeys.Length > 0)
                throw new InvalidDataException("Owner " + ownerId + " has duplicate animator keys: " + string.Join(",", duplicateAnimatorKeys) + ".");

            string[] duplicateAiSpecies = candidateAiTemplates
                .GroupBy(registration => registration.SpeciesId, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateAiSpecies.Length > 0)
                throw new InvalidDataException("Owner " + ownerId + " has duplicate AI template species: " + string.Join(",", duplicateAiSpecies) + ".");
        }

        private static string FindOwnerCandidateConflict(
            IEnumerable<CustomAnimalAnimatorRegistration> candidateRegistrations,
            IEnumerable<CustomAnimalAiTemplateRegistration> candidateAiTemplates,
            IReadOnlyDictionary<string, CustomAnimalAnimatorRegistration> existingRegistrations,
            IReadOnlyDictionary<string, CustomAnimalAiTemplateRegistration> existingAiTemplates)
        {
            foreach (CustomAnimalAnimatorRegistration registration in candidateRegistrations)
            {
                if (existingRegistrations.TryGetValue(registration.AnimatorKey, out CustomAnimalAnimatorRegistration? existing))
                {
                    return "Animator key " + registration.AnimatorKey + " conflicts with owner " + existing.OwnerId + ".";
                }
            }

            foreach (CustomAnimalAiTemplateRegistration registration in candidateAiTemplates)
            {
                if (existingAiTemplates.TryGetValue(registration.SpeciesId, out CustomAnimalAiTemplateRegistration? existing))
                {
                    return "AI template species " + registration.SpeciesId + " conflicts with owner " + existing.OwnerId + ".";
                }
            }

            return string.Empty;
        }

        private static void RemoveOwnerFromCandidate(
            IDictionary<string, CustomAnimalAnimatorRegistration> candidateRegistrations,
            IDictionary<string, CustomAnimalAiTemplateRegistration> candidateAiTemplates,
            string ownerId)
        {
            foreach (string key in candidateRegistrations
                .Where(pair => pair.Value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToArray())
            {
                candidateRegistrations.Remove(key);
            }

            foreach (string key in candidateAiTemplates
                .Where(pair => pair.Value.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToArray())
            {
                candidateAiTemplates.Remove(key);
            }
        }

        private void PublishOwnerGenerationRejectedBestEffort(CustomAnimalOwnerRejectionPublication publication)
        {
            string ownerId = publication.OwnerId;
            long lastGoodGeneration = publication.LastGoodGeneration;
            ContentRefreshDirtyBatch dirtyBatch = publication.DirtyBatch;
            string failure = publication.Failure;
            string message = "Custom animal owner generation rejected owner=" + ownerId +
                " dirtyGeneration=" + dirtyBatch.Generation.ToString(CultureInfo.InvariantCulture) +
                " retained=" + (lastGoodGeneration > 0).ToString(CultureInfo.InvariantCulture) +
                " lastGood=" + lastGoodGeneration.ToString(CultureInfo.InvariantCulture) +
                " reasons=" + dirtyBatch.ReasonSummary +
                " failure=" + failure + ".";
            try
            {
                runtime.Diagnostics.RecordWarning(ownerId, "Custom animal generation rejected; last-good generation retained.", message);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, dirtyBatch.Generation, ex);
            }

            try
            {
                runtime.RuntimeMonitor.Log(message, LogLevel.Warn);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, dirtyBatch.Generation, ex);
            }

            try
            {
                runtime.ObserveLifecycleResourceEvent(
                    "CustomAnimals",
                    "OwnerGenerationRejected",
                    ownerId,
                    lastGoodGeneration.ToString(CultureInfo.InvariantCulture),
                    "dirtyGeneration=" + dirtyBatch.Generation + "; retained=" + (lastGoodGeneration > 0) + "; failure=" + failure);
            }
            catch (Exception ex)
            {
                RecordPostCommitSideEffectFailure(ownerId, dirtyBatch.Generation, ex);
            }
        }

        private static CustomAnimalDefinitionModel[] ReadDefinitions(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json ?? string.Empty);
            using (var stream = new MemoryStream(bytes))
            {
                var serializer = new DataContractJsonSerializer(typeof(CustomAnimalDefinitionModel[]));
                object? result = serializer.ReadObject(stream);
                return result as CustomAnimalDefinitionModel[] ?? Array.Empty<CustomAnimalDefinitionModel>();
            }
        }

        private static IReadOnlyList<CustomAnimalAnimatorRegistration> BuildRegistrations(string ownerId, string rootPath, IEnumerable<CustomAnimalDefinitionModel> definitions)
        {
            var registrations = new List<CustomAnimalAnimatorRegistration>();
            foreach (CustomAnimalDefinitionModel definition in definitions ?? Array.Empty<CustomAnimalDefinitionModel>())
            {
                definition.Normalize();
                string bundlePath = ResolvePackPath(rootPath, definition.AnimatorBundle);
                string templateSpecies = string.IsNullOrWhiteSpace(definition.TemplateSpeciesId) ? definition.SpeciesId : definition.TemplateSpeciesId;
                if (string.Equals(definition.AnimatorMode, "pngSpriteOverride", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(definition.TemplateSpritePrefix) || string.IsNullOrWhiteSpace(definition.CustomSpritePrefix))
                        continue;

                    string frameManifestPath = string.IsNullOrWhiteSpace(definition.FrameManifest)
                        ? string.Empty
                        : ResolvePackPath(rootPath, definition.FrameManifest);
                    if (!string.IsNullOrWhiteSpace(definition.AdultAnimatorKey))
                    {
                        registrations.Add(new CustomAnimalAnimatorRegistration(
                            ownerId,
                            definition.SpeciesId,
                            "adult",
                            definition.AdultAnimatorKey,
                            string.Empty,
                            string.Empty,
                            "game_anim_animal_" + templateSpecies,
                            "pngSpriteOverride",
                            frameManifestPath,
                            definition.TemplateSpritePrefix,
                            definition.CustomSpritePrefix));
                    }

                    if (!string.IsNullOrWhiteSpace(definition.ChildAnimatorKey))
                    {
                        registrations.Add(new CustomAnimalAnimatorRegistration(
                            ownerId,
                            definition.SpeciesId,
                            "child",
                            definition.ChildAnimatorKey,
                            string.Empty,
                            string.Empty,
                            "game_anim_animal_" + templateSpecies + "_child",
                            "pngSpriteOverride",
                            frameManifestPath,
                            definition.TemplateSpritePrefix,
                            definition.CustomSpritePrefix));
                    }

                    continue;
                }

                if (!string.Equals(definition.AnimatorMode, "assetBundle", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.IsNullOrWhiteSpace(definition.AnimatorBundle))
                    continue;

                if (!string.IsNullOrWhiteSpace(definition.AdultAnimatorKey) && !string.IsNullOrWhiteSpace(definition.AdultAnimatorAsset))
                {
                    registrations.Add(new CustomAnimalAnimatorRegistration(
                        ownerId,
                        definition.SpeciesId,
                        "adult",
                        definition.AdultAnimatorKey,
                        bundlePath,
                        definition.AdultAnimatorAsset,
                        "game_anim_animal_" + templateSpecies,
                        "assetBundle",
                        string.Empty,
                        string.Empty,
                        string.Empty));
                }

                if (!string.IsNullOrWhiteSpace(definition.ChildAnimatorKey) && !string.IsNullOrWhiteSpace(definition.ChildAnimatorAsset))
                {
                    registrations.Add(new CustomAnimalAnimatorRegistration(
                        ownerId,
                        definition.SpeciesId,
                        "child",
                        definition.ChildAnimatorKey,
                        bundlePath,
                        definition.ChildAnimatorAsset,
                        "game_anim_animal_" + templateSpecies + "_child",
                        "assetBundle",
                        string.Empty,
                        string.Empty,
                        string.Empty));
                }
            }

            return registrations;
        }

        private static IReadOnlyList<CustomAnimalAiTemplateRegistration> BuildAiTemplateRegistrations(string ownerId, IEnumerable<CustomAnimalDefinitionModel> definitions)
        {
            var registrations = new List<CustomAnimalAiTemplateRegistration>();
            foreach (CustomAnimalDefinitionModel definition in definitions ?? Array.Empty<CustomAnimalDefinitionModel>())
            {
                definition.Normalize();
                string aiTemplate = string.IsNullOrWhiteSpace(definition.AiTemplate)
                    ? definition.TemplateSpeciesId
                    : definition.AiTemplate;
                if (string.IsNullOrWhiteSpace(definition.SpeciesId) || string.IsNullOrWhiteSpace(aiTemplate))
                    continue;
                if (string.Equals(definition.SpeciesId, aiTemplate, StringComparison.OrdinalIgnoreCase))
                    continue;

                registrations.Add(new CustomAnimalAiTemplateRegistration(ownerId, definition.SpeciesId, aiTemplate));
            }

            return registrations;
        }

        private object? TryLoadBundleController(CustomAnimalAnimatorRegistration registration)
        {
            string cacheKey = registration.ControllerCacheKey;
            lock (gate)
            {
                if (controllerCache.TryGetValue(cacheKey, out object? cached))
                    return cached;
            }

            object? controller = LoadBundleController(registration);
            lock (gate)
                controllerCache[cacheKey] = controller;
            if (controller != null)
            {
                runtime.ObserveResourceLifecycle(
                    "AnimatorControllerCacheRef",
                    cacheKey,
                    registration.OwnerId,
                    registration.BundlePath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    ResourceLifecycleStatus.Acquired,
                    "drop-managed-ref-only-no-destroy");
            }

            return controller;
        }

        private object? LoadBundleController(CustomAnimalAnimatorRegistration registration)
        {
            if (!ResolveUnityAssetTypes())
            {
                MarkDegraded(registration, "Unity AssetBundle/RuntimeAnimatorController types unavailable");
                return null;
            }

            optionalFileStatusCallCount++;
            if (!File.Exists(registration.BundlePath))
            {
                MarkDegraded(registration, "bundle missing " + registration.BundlePath);
                return null;
            }

            object? bundle = LoadAssetBundle(registration);
            if (bundle == null)
                return null;

            try
            {
                object? controller = assetBundleLoadAssetByNameAndType!.Invoke(bundle, new object?[] { registration.AssetName, runtimeAnimatorControllerType });
                if (controller != null && runtimeAnimatorControllerType!.IsInstanceOfType(controller))
                {
                    runtime.ObserveResourceLifecycle(
                        "RuntimeAnimatorController",
                        registration.AssetName,
                        registration.OwnerId,
                        registration.BundlePath,
                        ResourceLifetime.TitleLifetime,
                        ResourceOwnership.DtmapiOwned,
                        ResourceLifecycleStatus.Ready,
                        "report-only-title-asset-no-destroy");
                    return controller;
                }

                MarkDegraded(registration, "controller asset missing " + registration.AssetName + " in " + registration.BundlePath);
                return null;
            }
            catch (Exception ex)
            {
                MarkDegraded(registration, "controller asset load failed " + registration.AssetName + ": " + Unwrap(ex).GetType().Name + ": " + Unwrap(ex).Message);
                return null;
            }
        }

        private object? LoadAssetBundle(CustomAnimalAnimatorRegistration registration)
        {
            lock (gate)
            {
                if (bundleCache.TryGetValue(registration.BundlePath, out object? cached))
                    return cached;
            }

            object? bundle = null;
            try
            {
                bundle = assetBundleLoadFromFile!.Invoke(null, new object?[] { registration.BundlePath });
                if (bundle == null)
                    MarkDegraded(registration, "AssetBundle.LoadFromFile returned null " + registration.BundlePath);
            }
            catch (Exception ex)
            {
                Exception unwrapped = Unwrap(ex);
                MarkDegraded(registration, "AssetBundle.LoadFromFile failed " + registration.BundlePath + ": " + unwrapped.GetType().Name + ": " + unwrapped.Message);
            }

            lock (gate)
                bundleCache[registration.BundlePath] = bundle;
            if (bundle != null)
            {
                runtime.ObserveResourceLifecycle(
                    "AssetBundleCacheRef",
                    registration.BundlePath,
                    registration.OwnerId,
                    registration.BundlePath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    ResourceLifecycleStatus.Acquired,
                    "drop-managed-ref-only-no-unload");
                runtime.ObserveResourceLifecycle(
                    "AssetBundle",
                    registration.BundlePath,
                    registration.OwnerId,
                    registration.BundlePath,
                    ResourceLifetime.TitleLifetime,
                    ResourceOwnership.DtmapiOwned,
                    ResourceLifecycleStatus.Ready,
                    "report-only-title-asset-no-unload");
            }

            return bundle;
        }

        private object? TryLoadTemplateController(CustomAnimalAnimatorRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(registration.TemplateAnimatorKey))
                return null;
            if (!ResolveRuntimeAnimatorControllerType() || !ResolveDolocApiGetAsset())
                return null;

            try
            {
                MethodInfo method = dolocApiGetAssetDefinition!.MakeGenericMethod(runtimeAnimatorControllerType!);
                object? controller = method.Invoke(null, new object?[] { registration.TemplateAnimatorKey, false });
                if (controller != null)
                {
                    runtime.ObserveResourceLifecycle(
                        "TemplateAnimatorController",
                        registration.TemplateAnimatorKey,
                        registration.OwnerId,
                        string.Empty,
                        ResourceLifetime.TitleLifetime,
                        ResourceOwnership.BorrowedNative,
                        ResourceLifecycleStatus.Ready,
                        "borrowed-native-never-release");
                }

                return controller;
            }
            catch (Exception ex)
            {
                Exception unwrapped = Unwrap(ex);
                MarkDegraded(registration, "template fallback load failed " + registration.TemplateAnimatorKey + ": " + unwrapped.GetType().Name + ": " + unwrapped.Message);
                return null;
            }
        }

        private Type? ResolveAnimalAIDefaultStateType(string aiTemplate)
        {
            string normalized = NormalizeKey(aiTemplate);
            if (normalized.Length == 0)
                return null;

            lock (gate)
            {
                if (aiDefaultStateTypeCache.TryGetValue(normalized, out Type? cached))
                    return cached;
            }

            Type? resolved = FindType(GetAnimalAIDefaultStateTypeName(normalized));
            lock (gate)
                aiDefaultStateTypeCache[normalized] = resolved;
            return resolved;
        }

        private static string GetAnimalAIDefaultStateTypeName(string aiTemplate)
        {
            string normalized = NormalizeKey(aiTemplate);
            if (string.Equals(normalized, "chicken", StringComparison.OrdinalIgnoreCase))
                return "DolocTown.AnimalAI+Chicken_FreeTimeState, Assembly-CSharp";
            if (string.Equals(normalized, "slime", StringComparison.OrdinalIgnoreCase))
                return "DolocTown.AnimalAI+Slime_FreeTimeState, Assembly-CSharp";
            if (string.Equals(normalized, "goat", StringComparison.OrdinalIgnoreCase))
                return "DolocTown.AnimalAI+Goat_FreeTimeState, Assembly-CSharp";
            if (string.Equals(normalized, "marsh_pangolin", StringComparison.OrdinalIgnoreCase))
                return "DolocTown.AnimalAI+MarshPangolin_FreeTimeState, Assembly-CSharp";
            return "DolocTown.AnimalAI+Normal_FreeTimeState, Assembly-CSharp";
        }

        private bool ResolveUnityAssetTypes()
        {
            if (runtimeAnimatorControllerType != null && assetBundleType != null && assetBundleLoadFromFile != null && assetBundleLoadAssetByNameAndType != null)
                return true;

            ResolveRuntimeAnimatorControllerType();
            assetBundleType ??= FindType(
                "UnityEngine.AssetBundle, UnityEngine.AssetBundleModule",
                "UnityEngine.AssetBundle, UnityEngine",
                "UnityEngine.AssetBundle, UnityEngine.CoreModule");
            assetBundleLoadFromFile = assetBundleType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "LoadFromFile" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
            assetBundleLoadAssetByNameAndType = assetBundleType?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return m.Name == "LoadAsset" &&
                        parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(Type);
                });

            return runtimeAnimatorControllerType != null && assetBundleType != null && assetBundleLoadFromFile != null && assetBundleLoadAssetByNameAndType != null;
        }

        private bool ResolveRuntimeAnimatorControllerType()
        {
            if (runtimeAnimatorControllerType != null)
                return true;

            runtimeAnimatorControllerType = FindType(
                "UnityEngine.RuntimeAnimatorController, UnityEngine.AnimationModule",
                "UnityEngine.RuntimeAnimatorController, UnityEngine",
                "UnityEngine.RuntimeAnimatorController, UnityEngine.CoreModule");
            return runtimeAnimatorControllerType != null;
        }

        private bool ResolveDolocApiGetAsset()
        {
            if (dolocApiGetAssetDefinition != null)
                return true;

            Type? dolocApiType = FindType("DolocAPI, Assembly-CSharp");
            dolocApiGetAssetDefinition = dolocApiType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetAsset" || !m.IsGenericMethodDefinition || m.GetGenericArguments().Length != 1)
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(bool);
                });
            return dolocApiGetAssetDefinition != null;
        }

        private bool ResolveSpriteOverrideTypes()
        {
            if (spriteOverrideHandlerType != null)
                return true;

            spriteOverrideHandlerType = FindType("DolocTown.SpriteOverrideHandler, Assembly-CSharp");
            return spriteOverrideHandlerType != null;
        }

        private bool ResolveModSpriteLoader()
        {
            if (spriteType != null && dolocApiModManagerProperty != null && modManagerLoadSpriteFromFile != null)
                return true;

            spriteType ??= FindType(
                "UnityEngine.Sprite, UnityEngine.CoreModule",
                "UnityEngine.Sprite, UnityEngine");
            Type? dolocApiType = FindType("DolocAPI, Assembly-CSharp");
            dolocApiModManagerProperty ??= dolocApiType?.GetProperty("modManager", BindingFlags.Public | BindingFlags.Static);
            Type? modManagerType = dolocApiModManagerProperty?.PropertyType ?? FindType("DolocTown.Config.ModManager, Assembly-CSharp");
            modManagerLoadSpriteFromFile = modManagerType?.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "LoadSpriteFromFile" || m.ReturnType != typeof(bool))
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType.IsByRef &&
                        parameters[1].ParameterType.GetElementType() == spriteType;
                });
            return spriteType != null && dolocApiModManagerProperty != null && modManagerLoadSpriteFromFile != null;
        }

        private object? EnsureSpriteOverrideHandler(object renderer)
        {
            return GetSpriteOverrideHandler(renderer, addIfMissing: true);
        }

        private object? GetSpriteOverrideHandler(object renderer, bool addIfMissing)
        {
            if (!ResolveSpriteOverrideTypes())
                return null;

            object? gameObject = ReadMember(renderer, "gameObject");
            if (gameObject == null)
                return null;

            MethodInfo? getComponent = gameObject.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return m.Name == "GetComponent" && !m.IsGenericMethod && parameters.Length == 1 && parameters[0].ParameterType == typeof(Type);
                });
            object? handler = null;
            try
            {
                handler = getComponent?.Invoke(gameObject, new object?[] { spriteOverrideHandlerType });
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.Log("CustomAnimals.PngSpriteBridge GetComponent(SpriteOverrideHandler) failed: " + Unwrap(ex).Message + ".", LogLevel.Warn);
            }

            if (handler == null && addIfMissing)
            {
                MethodInfo? addComponent = gameObject.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return m.Name == "AddComponent" && !m.IsGenericMethod && parameters.Length == 1 && parameters[0].ParameterType == typeof(Type);
                    });
                try
                {
                    handler = addComponent?.Invoke(gameObject, new object?[] { spriteOverrideHandlerType });
                }
                catch (Exception ex)
                {
                    runtime.RuntimeMonitor.Log("CustomAnimals.PngSpriteBridge AddComponent(SpriteOverrideHandler) failed: " + Unwrap(ex).Message + ".", LogLevel.Warn);
                    return null;
                }
            }

            if (handler != null)
                SetBoolFieldOrProperty(handler, "onlyOverrideAnimatedSprites", true);
            return handler;
        }

        private bool TryLoadModSpriteFromFile(string spriteName, out object? sprite)
        {
            sprite = null;
            if (!ResolveModSpriteLoader())
                return false;

            try
            {
                object? modManager = dolocApiModManagerProperty!.GetValue(null);
                if (modManager == null)
                    return false;

                object?[] args = { spriteName, null };
                bool loaded = modManagerLoadSpriteFromFile!.Invoke(modManager, args) is bool value && value;
                sprite = args[1];
                if (loaded && sprite != null)
                {
                    runtime.ObserveResourceLifecycle(
                        "PngSprite",
                        spriteName,
                        "DTMAPI.GameBridge.CustomAnimals",
                        spriteName,
                        ResourceLifetime.TitleLifetime,
                        ResourceOwnership.BorrowedNative,
                        ResourceLifecycleStatus.Ready,
                        "borrowed-native-never-release");
                }

                return loaded;
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.Log("CustomAnimals.PngSpriteBridge LoadSpriteFromFile failed sprite=" + spriteName + ": " + Unwrap(ex).GetType().Name + ": " + Unwrap(ex).Message + ".", LogLevel.Warn);
                return false;
            }
        }

        private void RecordSleepWakeDiagnostic(string eventName, object? animal, string details, bool renderOnlyWhenSuspicious = false)
        {
            if (!TryDescribeCustomAnimal(animal, out string species, out string snapshot, out string identityKey, out bool? isSleep, out string aiState, out string task, out string rendererState))
                return;
            if (renderOnlyWhenSuspicious && !ShouldLogRenderDiagnostic(species, identityKey, isSleep, aiState, task, rendererState))
                return;

            string message = "CustomAnimals.SleepWakeDiagnostics event=" + eventName +
                " species=" + species +
                " " + snapshot +
                (string.IsNullOrWhiteSpace(details) ? string.Empty : " " + details) +
                ".";
            runtime.RuntimeMonitor.Log(message);
            bool publishStatus;
            lock (gate)
                publishStatus = sleepWakeStatusPublished.Add(species + "|" + eventName);
            if (publishStatus)
            {
                runtime.SetHookStatus(
                    SleepWakeDiagnosticsHookId + "." + species,
                    "observed",
                    "Animal sleep/wake diagnostics",
                    message);
            }
        }

        private void TrackSleepRenderFollowUp(object? animal)
        {
            if (!TryDescribeCustomAnimal(animal, out string species, out _, out string identityKey, out bool? isSleep, out string aiState, out string task, out string rendererState))
                return;
            if (!ShouldTrackSleepRenderFollowUp(isSleep, aiState, rendererState))
                return;

            object? renderer = ReadMember(animal, "Renderer");
            if (renderer == null)
                return;

            string reason = IsRendererInSleepState(rendererState) ? "ai_not_sleep_state" : "renderer_not_sleep";
            var context = new SleepRenderFollowUpContext(species, identityKey, aiState, task, rendererState, reason);
            RemoveSleepRenderFollowUp(renderer, "replace-follow-up");
            lock (gate)
            {
                sleepRenderFollowUps.Add(renderer, context);
                observedSleepRenderFollowUpCount++;
            }
            runtime.ObserveResourceLifecycle(
                "SleepRenderFollowUpContext",
                species,
                "DTMAPI.GameBridge.CustomAnimals",
                string.Empty,
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "clear-on-save-boundary");
        }

        private bool ShouldLogRenderDiagnostic(string species, string identityKey, bool? isSleep, string aiState, string task, string rendererState)
        {
            if (isSleep != true)
                return false;
            if (string.IsNullOrWhiteSpace(aiState) ||
                string.Equals(aiState, "Normal_SleepState", StringComparison.Ordinal) ||
                string.Equals(aiState, "无状态", StringComparison.Ordinal))
            {
                return false;
            }

            string key = species + "|" + (string.IsNullOrWhiteSpace(identityKey) ? "unknown" : identityKey);
            string signature = FormatNullableBool(isSleep) + "|" + aiState + "|" + task + "|" + rendererState;
            lock (gate)
            {
                if (lastRenderDiagnosticSignatures.TryGetValue(key, out string? previous) &&
                    string.Equals(previous, signature, StringComparison.Ordinal))
                {
                    return false;
                }

                lastRenderDiagnosticSignatures[key] = signature;
                return true;
            }
        }

        private static bool ShouldTrackSleepRenderFollowUp(bool? isSleep, string aiState, string rendererState)
        {
            if (isSleep != true)
                return false;

            if (!IsRendererInSleepState(rendererState))
                return true;

            return !string.Equals(aiState, "Normal_SleepState", StringComparison.Ordinal) &&
                !string.Equals(aiState, "无状态", StringComparison.Ordinal);
        }

        private static bool IsRendererInSleepState(string rendererState)
        {
            return !string.IsNullOrWhiteSpace(rendererState) &&
                rendererState.IndexOf("animState=sleep", StringComparison.Ordinal) >= 0;
        }

        private static bool IsFollowUpSampleFrame(int frame)
        {
            return frame == 1 || frame == 10 || frame == 30 || frame == SleepRenderFollowUpMaxFrames;
        }

        private static bool IsSettledSleepingRendererFollowUp(bool? isSleep, string aiState, string task, string rendererState)
        {
            if (isSleep != true || !IsRendererInSleepState(rendererState) || !IsSleepWaitTaskName(task))
                return false;

            return string.IsNullOrWhiteSpace(aiState) ||
                string.Equals(aiState, "Normal_SleepState", StringComparison.Ordinal) ||
                string.Equals(aiState, "无状态", StringComparison.Ordinal);
        }

        private static bool IsSleepWaitTaskName(string task)
        {
            return string.Equals(task, "RedSaw.AI.LinearTask.LinearTaskWaitInt", StringComparison.Ordinal) ||
                string.Equals(task, "none", StringComparison.Ordinal);
        }

        private static string NormalizeRendererStateForDiagnosticSignature(string rendererState)
        {
            if (string.IsNullOrWhiteSpace(rendererState))
                return string.Empty;

            string[] parts = rendererState.Split(',');
            const string animStatePrefix = "animState=";
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (!part.StartsWith(animStatePrefix, StringComparison.Ordinal))
                    continue;

                string animState = part.Substring(animStatePrefix.Length);
                int timeSeparator = animState.IndexOf('@');
                if (timeSeparator >= 0)
                    animState = animState.Substring(0, timeSeparator);
                parts[i] = animStatePrefix + animState;
            }

            return string.Join(",", parts);
        }

        internal static bool IsSettledSleepingRendererFollowUpForTest(bool? isSleep, string aiState, string task, string rendererState)
        {
            return IsSettledSleepingRendererFollowUp(isSleep, aiState, task, rendererState);
        }

        internal static string NormalizeRendererStateForDiagnosticSignatureForTest(string rendererState)
        {
            return NormalizeRendererStateForDiagnosticSignature(rendererState);
        }

        private bool ShouldApplySleepTaskBoundary(object? animal, out string species, out string snapshot)
        {
            species = string.Empty;
            snapshot = string.Empty;
            if (animal == null)
                return false;

            species = ReadStringMember(animal, "protoName", "ProtoName");
            if (string.IsNullOrWhiteSpace(species) || !IsRegisteredCustomAnimalSpecies(species))
                return false;

            bool? isSleep = ReadBoolMember(animal, "isSleep", "IsSleep");
            if (!ShouldApplySleepTaskBoundaryCore(isRegisteredCustomAnimal: true, isSleep, ReadCurrentDayPeriod()))
                return false;

            if (!TryDescribeCustomAnimal(animal, out species, out snapshot, out _, out _, out _, out _, out _))
                snapshot = "animal=unknown sleep=" + FormatNullableBool(isSleep) + " time=" + DescribeGameTime();

            return true;
        }

        private static bool ShouldApplySleepTaskBoundaryCore(bool isRegisteredCustomAnimal, bool? isSleep, string dayPeriod)
        {
            return isRegisteredCustomAnimal &&
                isSleep == true &&
                string.Equals((dayPeriod ?? string.Empty).Trim(), "Night", StringComparison.OrdinalIgnoreCase);
        }

        private object? CreateSleepBoundaryWaitTask(int frames)
        {
            MethodInfo? waitFrames = ResolveLinearTaskWaitFramesMethod();
            if (waitFrames == null)
                return null;

            try
            {
                return waitFrames.Invoke(null, new object[] { frames });
            }
            catch (Exception ex)
            {
                MarkSleepTaskBoundaryDegraded("LinearTask.WaitFrames invocation failed: " + Unwrap(ex).GetType().Name + ": " + Unwrap(ex).Message);
                return null;
            }
        }

        private MethodInfo? ResolveLinearTaskWaitFramesMethod()
        {
            if (linearTaskWaitFramesMethod != null)
                return linearTaskWaitFramesMethod;

            Type? linearTaskType = FindType("RedSaw.AI.LinearTask.LinearTask, Assembly-CSharp");
            if (linearTaskType == null)
                return null;

            linearTaskWaitFramesMethod = linearTaskType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!string.Equals(method.Name, "WaitFrames", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(int);
                });
            return linearTaskWaitFramesMethod;
        }

        private void RecordSleepTaskBoundaryEvent(string eventName, string species, string snapshot, string details)
        {
            string eventKey = species + "|" + eventName + "|" + details;
            bool first;
            lock (gate)
                first = sleepTaskBoundaryEvents.Add(eventKey);
            if (!first)
                return;

            string message = "CustomAnimals.SleepTaskBoundary event=" + eventName +
                " species=" + species +
                " " + snapshot +
                (string.IsNullOrWhiteSpace(details) ? string.Empty : " " + details) +
                ".";
            runtime.RuntimeMonitor.Log(message);
            runtime.SetHookStatus(
                SleepTaskBoundaryHookId + "." + species,
                "observed",
                "Custom animal sleep task boundary",
                message);
        }

        private void MarkSleepTaskBoundaryDegraded(string reason)
        {
            string messageReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
            bool first;
            lock (gate)
            {
                sleepTaskBoundaryDegradedReason = messageReason;
                first = sleepTaskBoundaryEvents.Add("degraded|" + messageReason);
            }

            if (first)
                runtime.RuntimeMonitor.Log("CustomAnimals.SleepTaskBoundary degraded reason=" + messageReason + ".", LogLevel.Warn);

            runtime.SetHookStatus(
                SleepTaskBoundaryHookId,
                "degraded",
                "Custom animal sleep task boundary",
                "Custom animal sleep task boundary degraded: " + messageReason + ".");
        }

        private static bool IsMovementAnimalTaskName(string taskTypeName)
        {
            string value = taskTypeName ?? string.Empty;
            return string.Equals(value, "DolocTown.AnimalMove", StringComparison.Ordinal) ||
                string.Equals(value, "DolocTown.AnimalJump", StringComparison.Ordinal) ||
                string.Equals(value, "DolocTown.AnimalEnterRoom", StringComparison.Ordinal);
        }

        private static string ReadCurrentDayPeriod()
        {
            try
            {
                Type? dolocApiType = FindType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApiType, "archiveHandle", "ArchiveHandle");
                object? dayPeriod = ReadMember(archive, "CurrentDayPeriodType");
                return dayPeriod?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool TryDescribeCustomAnimal(object? animal, out string species, out string snapshot)
        {
            return TryDescribeCustomAnimal(animal, out species, out snapshot, out _, out _, out _, out _, out _);
        }

        private bool TryDescribeCustomAnimal(object? animal, out string species, out string snapshot, out string identityKey, out bool? isSleep, out string aiState, out string task, out string rendererState)
        {
            species = string.Empty;
            snapshot = string.Empty;
            identityKey = string.Empty;
            isSleep = null;
            aiState = string.Empty;
            task = string.Empty;
            rendererState = string.Empty;
            if (animal == null)
                return false;

            species = ReadStringMember(animal, "protoName", "ProtoName");
            if (string.IsNullOrWhiteSpace(species) || !IsRegisteredCustomAnimalSpecies(species))
                return false;

            object? data = ReadMember(animal, "data", "Data");
            bool? isAdult = ReadBoolMember(data, "isAdult", "IsAdult");
            bool? isChild = ReadBoolMember(data, "isChild", "IsChild");
            isSleep = ReadBoolMember(animal, "isSleep", "IsSleep");
            bool? isRender = ReadBoolMember(animal, "IsRender", "isRender");
            bool? isPassingTime = ReadBoolMember(animal, "isPassingTime", "IsPassingTime");
            object? currentRoom = ReadMember(animal, "currentRoom", "CurrentRoom");
            object? homeRoom = ReadMember(animal, "homeRoom", "HomeRoom");
            object? controller = ReadMember(animal, "controller", "Controller");
            aiState = ReadStringMember(controller, "CurrentStateName");
            task = DescribeTypeMember(controller, "CurrentTaskType");
            object? renderer = ReadMember(animal, "Renderer");
            rendererState = DescribeRendererState(renderer);
            identityKey = GetRuntimeObjectKey(animal);

            snapshot =
                "animal=" + DescribeAnimalIdentity(animal, data, renderer) +
                "sleep=" + FormatNullableBool(isSleep) +
                " render=" + FormatNullableBool(isRender) +
                " passingTime=" + FormatNullableBool(isPassingTime) +
                " stage=" + DescribeStage(isAdult, isChild) +
                " room=" + DescribeRoom(currentRoom) +
                " home=" + DescribeRoom(homeRoom) +
                " aiState=" + aiState +
                " task=" + task +
                (renderer == null ? string.Empty : " rendererState=" + rendererState) +
                " time=" + DescribeGameTime();
            return true;
        }

        private static string DescribeAnimalIdentity(object animal, object? data, object? renderer)
        {
            var parts = new List<string>
            {
                "ref=" + GetRuntimeObjectKey(animal)
            };
            AddLogPart(parts, "index", ReadMember(animal, "index", "Index"));
            AddLogPart(parts, "dataIdx", ReadMember(animal, "dataIdx"));
            AddLogPart(parts, "customName", ReadMember(animal, "customName", "CustomName"));
            AddLogPart(parts, "title", ReadMember(animal, "Title", "title"));
            AddLogPart(parts, "cell", ReadMember(animal, "positionCell", "PositionCell"));
            AddLogPart(parts, "ws", ReadMember(animal, "PositionWSOfCell", "positionWSOfCell"));
            AddLogPart(parts, "adult", ReadBoolMember(data, "isAdult", "IsAdult"));
            AddLogPart(parts, "child", ReadBoolMember(data, "isChild", "IsChild"));

            if (renderer != null)
            {
                parts.Add("rendererRef=" + GetRuntimeObjectKey(renderer));
                object? transform = ReadMember(renderer, "transform", "Transform");
                AddLogPart(parts, "rendererPos", ReadMember(transform, "position", "Position"));
            }

            return string.Join(",", parts.ToArray()) + " ";
        }

        private static string DescribeRendererState(object? renderer)
        {
            if (renderer == null)
                return "none";

            var parts = new List<string>();
            object? animator = ReadMember(renderer, "_animator");
            object? animatorController = ReadMember(animator, "runtimeAnimatorController", "RuntimeAnimatorController");
            AddLogPart(parts, "animController", ReadMember(animatorController, "name", "Name"));
            AddLogPart(parts, "animState", DescribeAnimatorState(animator));
            AddLogPart(parts, "moving", ReadBoolMember(renderer, "_isMoving"));
            AddLogPart(parts, "eating", ReadBoolMember(renderer, "_isEating"));
            AddLogPart(parts, "jumping", ReadBoolMember(renderer, "_isJumping"));
            AddLogPart(parts, "jumpReady", ReadBoolMember(renderer, "_isJumpingReady"));
            AddLogPart(parts, "faceRight", ReadBoolMember(renderer, "FaceRight"));
            object? sprite = ReadMember(renderer, "Sprite");
            AddLogPart(parts, "sprite", ReadMember(sprite, "name", "Name"));
            return parts.Count == 0 ? "unknown" : string.Join(",", parts.ToArray());
        }

        private static string DescribeAnimatorState(object? animator)
        {
            if (animator == null)
                return "none";

            object? stateInfo = InvokeInstanceMethod(animator, "GetCurrentAnimatorStateInfo", 0);
            if (stateInfo == null)
                return "unknown";

            string stateName = FindAnimatorStateName(stateInfo);
            string normalizedTime = FormatLogValue(ReadMember(stateInfo, "normalizedTime", "NormalizedTime"));
            return string.IsNullOrWhiteSpace(normalizedTime) ? stateName : stateName + "@" + normalizedTime;
        }

        private static string FindAnimatorStateName(object stateInfo)
        {
            string[] candidateNames =
            {
                "sleep",
                "idle",
                "eat",
                "jump_ready",
                "jump",
                "move"
            };

            foreach (string candidate in candidateNames)
            {
                object? result = InvokeInstanceMethod(stateInfo, "IsName", candidate);
                if (result is bool isMatch && isMatch)
                    return candidate;
            }

            return "unknown";
        }

        private static void AddLogPart(List<string> parts, string name, object? value)
        {
            string formatted = FormatLogValue(value);
            if (!string.IsNullOrWhiteSpace(formatted))
                parts.Add(name + "=" + formatted);
        }

        private static string GetRuntimeObjectKey(object instance)
        {
            uint value = unchecked((uint)RuntimeHelpers.GetHashCode(instance));
            return "0x" + value.ToString("X8", CultureInfo.InvariantCulture);
        }

        private static string FormatLogValue(object? value)
        {
            if (value == null)
                return string.Empty;
            if (value is string text)
                return SanitizeLogValue(text);
            if (value is bool boolean)
                return boolean ? "true" : "false";
            if (value is IFormattable formattable && value.GetType().IsPrimitive)
                return SanitizeLogValue(formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty);
            return SanitizeLogValue(value.ToString() ?? string.Empty);
        }

        private static string SanitizeLogValue(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            if (trimmed.Length == 0)
                return string.Empty;

            var builder = new StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
                builder.Append(char.IsWhiteSpace(c) ? '_' : c);
            return builder.ToString();
        }

        private bool IsRegisteredCustomAnimalSpecies(string speciesId)
        {
            string key = NormalizeKey(speciesId);
            lock (gate)
            {
                if (aiTemplateRegistrations.ContainsKey(key) || pngSpriteRegistrationsBySpecies.ContainsKey(key))
                    return true;

                foreach (CustomAnimalAnimatorRegistration registration in registrations.Values)
                {
                    if (string.Equals(registration.SpeciesId, key, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private HashSet<string> BuildDiagnosticSpeciesSet()
        {
            var species = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CustomAnimalAiTemplateRegistration registration in aiTemplateRegistrations.Values)
            {
                if (!string.IsNullOrWhiteSpace(registration.SpeciesId))
                    species.Add(registration.SpeciesId);
            }

            foreach (CustomAnimalAnimatorRegistration registration in registrations.Values)
            {
                if (!string.IsNullOrWhiteSpace(registration.SpeciesId))
                    species.Add(registration.SpeciesId);
            }

            foreach (CustomAnimalAnimatorRegistration registration in pngSpriteRegistrationsBySpecies.Values)
            {
                if (!string.IsNullOrWhiteSpace(registration.SpeciesId))
                    species.Add(registration.SpeciesId);
            }

            return species;
        }

        private static bool? ReadBoolMember(object? instance, params string[] names)
        {
            if (instance == null)
                return null;

            object? value = ReadMember(instance, names);
            return value is bool boolean ? boolean : null;
        }

        private static string DescribeStage(bool? isAdult, bool? isChild)
        {
            if (isAdult == true)
                return "adult";
            if (isChild == true)
                return "child";
            if (isAdult == false)
                return "young";
            return "unknown";
        }

        private static string FormatNullableBool(bool? value)
        {
            return value.HasValue ? (value.Value ? "true" : "false") : "unknown";
        }

        private static string DescribeTypeMember(object? instance, params string[] names)
        {
            if (instance == null)
                return "none";

            object? value = ReadMember(instance, names);
            if (value == null)
                return "none";
            if (value is Type type)
                return type.FullName ?? type.Name;
            return value.GetType().FullName ?? value.GetType().Name;
        }

        private static string DescribeRoom(object? room)
        {
            if (room == null)
                return "none";

            string value = ReadStringMember(room, "protoName", "ProtoName", "id", "Id", "roomName", "RoomName", "name", "Name");
            if (!string.IsNullOrWhiteSpace(value))
                return value;
            return room.GetType().Name;
        }

        private static string DescribeObject(object? value)
        {
            if (value == null)
                return "none";

            string id = ReadStringMember(value, "protoName", "ProtoName", "id", "Id", "name", "Name");
            string typeName = value.GetType().Name;
            return string.IsNullOrWhiteSpace(id) ? typeName : typeName + ":" + id;
        }

        private static string DescribeGameTime()
        {
            try
            {
                Type? dolocApiType = FindType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApiType, "archiveHandle", "ArchiveHandle");
                if (archive == null)
                    return "unknown";

                object? dateNow = ReadMember(archive, "DateNow", "dateNow");
                object? hour = dateNow == null ? null : ReadMember(dateNow, "Hour", "hour");
                object? minute = dateNow == null ? null : ReadMember(dateNow, "Minute", "minute");
                object? dayPeriod = ReadMember(archive, "CurrentDayPeriodType");
                string clock = hour == null
                    ? "hour=unknown"
                    : "hour=" + hour + ":" + (minute == null ? "00" : minute.ToString());
                return clock + " period=" + (dayPeriod?.ToString() ?? "unknown");
            }
            catch
            {
                return "unknown";
            }
        }

        private static string MapPngSpriteName(string templateSpritePrefix, string customSpritePrefix, string oldSpriteName)
        {
            string template = (templateSpritePrefix ?? string.Empty).Trim();
            string custom = (customSpritePrefix ?? string.Empty).Trim();
            string source = (oldSpriteName ?? string.Empty).Trim();
            if (template.Length == 0 || custom.Length == 0 || source.Length == 0)
                return string.Empty;

            string prefix = template + "_";
            if (!source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string suffix = source.Substring(prefix.Length);
            if (suffix.IndexOf("_jump_ready_", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int jumpIndex = suffix.IndexOf("_jump_ready_", StringComparison.OrdinalIgnoreCase);
                return custom + "_" + suffix.Substring(0, jumpIndex) + "_jump_0";
            }

            if (suffix.EndsWith("_jump_ready", StringComparison.OrdinalIgnoreCase))
                return custom + "_" + suffix.Substring(0, suffix.Length - "_jump_ready".Length) + "_jump_0";

            return custom + "_" + ReplaceOrdinalIgnoreCase(suffix, "child_", "young_");
        }

        private static string ReplaceOrdinalIgnoreCase(string text, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldValue))
                return text ?? string.Empty;

            int index = text.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return text;

            var builder = new StringBuilder();
            int start = 0;
            while (index >= 0)
            {
                builder.Append(text, start, index - start);
                builder.Append(newValue);
                start = index + oldValue.Length;
                index = text.IndexOf(oldValue, start, StringComparison.OrdinalIgnoreCase);
            }

            builder.Append(text, start, text.Length - start);
            return builder.ToString();
        }

        private void MarkVerified(CustomAnimalAnimatorRegistration registration)
        {
            bool first;
            lock (gate)
            {
                degradedReasons.Remove(registration.AnimatorKey);
                first = verifiedKeys.Add(registration.AnimatorKey);
            }

            if (first)
            {
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.AnimatorBridge verified key=" + registration.AnimatorKey +
                    " species=" + registration.SpeciesId +
                    " stage=" + registration.Stage +
                    " mode=" + registration.AnimatorMode +
                    " source=" + registration.ControllerSourceDetails + ".");
            }

            runtime.SetHookStatus(
                FeatureHookId + "." + registration.AnimatorKey,
                "verified",
                registration.ControllerStatusSource,
                "Resolved custom animal animator controller species=" + registration.SpeciesId + " stage=" + registration.Stage + " source=" + registration.ControllerSourceDetails + ".");
        }

        private void MarkDegraded(CustomAnimalAnimatorRegistration registration, string reason)
        {
            bool firstWarning;
            lock (gate)
            {
                degradedReasons[registration.AnimatorKey] = reason;
                firstWarning = diagnosticWarnings.Add(registration.AnimatorKey + "|" + reason);
            }

            if (firstWarning)
            {
                runtime.Diagnostics.RecordWarning(registration.OwnerId, "Custom animal animator bridge degraded.", "key=" + registration.AnimatorKey + ", species=" + registration.SpeciesId + ", stage=" + registration.Stage + ", reason=" + reason);
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.AnimatorBridge degraded key=" + registration.AnimatorKey +
                    " species=" + registration.SpeciesId +
                    " stage=" + registration.Stage +
                    " reason=" + reason + ".",
                    LogLevel.Warn);
            }

            runtime.SetHookStatus(
                FeatureHookId + "." + registration.AnimatorKey,
                "degraded",
                registration.ControllerStatusSource,
                "Resolved through fallback or failed custom controller species=" + registration.SpeciesId + " stage=" + registration.Stage + " reason=" + reason + ".");
        }

        private void MarkPngSpriteVerified(PngSpriteOverrideContext context, string oldSpriteName, string customSpriteName)
        {
            bool first;
            bool degraded;
            lock (gate)
            {
                first = verifiedPngSpriteSpecies.Add(context.SpeciesId);
                degraded = pngSpriteDegradedReasons.ContainsKey(context.SpeciesId);
            }

            if (first)
            {
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.PngSpriteBridge verified species=" + context.SpeciesId +
                    " oldSprite=" + oldSpriteName +
                    " customSprite=" + customSpriteName +
                    " prefix=" + context.TemplateSpritePrefix + "->" + context.CustomSpritePrefix + ".");

                if (!degraded)
                {
                    runtime.SetHookStatus(
                        PngSpriteHookId + "." + context.SpeciesId,
                        "verified",
                        "SpriteOverrideHandler PNG frame mapping",
                        "Mapped first template sprite " + oldSpriteName + " to custom sprite " + customSpriteName + " for species=" + context.SpeciesId + ".");
                }
            }
        }

        private void MarkPngSpriteDegraded(CustomAnimalAnimatorRegistration registration, string reason)
        {
            bool firstWarning;
            lock (gate)
            {
                pngSpriteDegradedReasons[registration.SpeciesId] = reason;
                firstWarning = pngSpriteDiagnosticWarnings.Add(registration.SpeciesId + "|" + reason);
            }

            if (firstWarning)
            {
                runtime.Diagnostics.RecordWarning(registration.OwnerId, "Custom animal PNG sprite override bridge degraded.", "species=" + registration.SpeciesId + ", reason=" + reason);
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.PngSpriteBridge degraded species=" + registration.SpeciesId +
                    " reason=" + reason + ".",
                    LogLevel.Warn);
            }

            runtime.SetHookStatus(
                PngSpriteHookId + "." + registration.SpeciesId,
                "degraded",
                "SpriteOverrideHandler PNG frame mapping",
                "Custom animal PNG sprite override failed species=" + registration.SpeciesId + " reason=" + reason + ".");
        }

        private void MarkAiTemplateVerified(CustomAnimalAiTemplateRegistration registration, Type stateType)
        {
            bool first;
            lock (gate)
            {
                aiTemplateDegradedReasons.Remove(registration.SpeciesId);
                first = verifiedAiTemplates.Add(registration.SpeciesId);
            }

            if (first)
            {
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.AiTemplateBridge verified species=" + registration.SpeciesId +
                    " aiTemplate=" + registration.AiTemplate +
                    " stateType=" + (stateType.FullName ?? stateType.Name) + ".");
            }

            runtime.SetHookStatus(
                AiTemplateHookId + "." + registration.SpeciesId,
                "verified",
                "AnimalAI default-state template mapping",
                "Mapped custom animal species=" + registration.SpeciesId + " to aiTemplate=" + registration.AiTemplate + " stateType=" + (stateType.FullName ?? stateType.Name) + ".");
        }

        private void MarkAiTemplateDegraded(CustomAnimalAiTemplateRegistration registration, string reason)
        {
            bool firstWarning;
            lock (gate)
            {
                aiTemplateDegradedReasons[registration.SpeciesId] = reason;
                firstWarning = aiTemplateDiagnosticWarnings.Add(registration.SpeciesId + "|" + reason);
            }

            if (firstWarning)
            {
                runtime.Diagnostics.RecordWarning(registration.OwnerId, "Custom animal AI template bridge degraded.", "species=" + registration.SpeciesId + ", aiTemplate=" + registration.AiTemplate + ", reason=" + reason);
                runtime.RuntimeMonitor.Log(
                    "CustomAnimals.AiTemplateBridge degraded species=" + registration.SpeciesId +
                    " aiTemplate=" + registration.AiTemplate +
                    " reason=" + reason + ".",
                    LogLevel.Warn);
            }

            runtime.SetHookStatus(
                AiTemplateHookId + "." + registration.SpeciesId,
                "degraded",
                "AnimalAI default-state template mapping",
                "Custom animal AI template mapping failed species=" + registration.SpeciesId + " aiTemplate=" + registration.AiTemplate + " reason=" + reason + ".");
        }

        private static bool IsEnabledContentPack(DiscoveredMod mod)
        {
            return mod != null &&
                mod.OfficialEnabled &&
                mod.Manifest != null &&
                string.Equals(mod.Manifest.Type, "ContentPack", StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeKey(string address)
        {
            return (address ?? string.Empty).Trim();
        }

        private static string ExtractDiagnosticSpeciesKey(string key)
        {
            string value = key ?? string.Empty;
            int separator = value.IndexOf('|');
            return separator < 0 ? value : value.Substring(0, separator);
        }

        private static string ResolvePackPath(string rootPath, string relativePath)
        {
            string root = Path.GetFullPath(rootPath ?? string.Empty);
            string relative = (relativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(root, relative));
            string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) && !string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
                return Path.Combine(root, Path.GetFileName(relative));
            return full;
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
                            return property.GetValue(instance);
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

        private static object? ReadStaticMember(Type? type, params string[] names)
        {
            if (type == null)
                return null;

            foreach (string name in names)
            {
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property != null)
                {
                    try
                    {
                        return property.GetValue(null);
                    }
                    catch
                    {
                    }
                }

                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    try
                    {
                        return field.GetValue(null);
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        private static object? InvokeInstanceMethod(object? instance, string name, params object[] arguments)
        {
            if (instance == null)
                return null;

            Type type = instance.GetType();
            Type[] argumentTypes = arguments.Select(argument => argument?.GetType() ?? typeof(object)).ToArray();
            MethodInfo? method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, argumentTypes, null);
            if (method == null)
            {
                method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(candidate =>
                    {
                        if (!string.Equals(candidate.Name, name, StringComparison.Ordinal))
                            return false;
                        ParameterInfo[] parameters = candidate.GetParameters();
                        if (parameters.Length != arguments.Length)
                            return false;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (arguments[i] != null && !parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                                return false;
                        }

                        return true;
                    });
            }

            if (method == null)
                return null;

            try
            {
                return method.Invoke(instance, arguments);
            }
            catch
            {
                return null;
            }
        }

        private static string ReadStringMember(object? instance, params string[] names)
        {
            return ReadMember(instance, names) as string ?? string.Empty;
        }

        private static void SetBoolFieldOrProperty(object instance, string name, bool value)
        {
            Type type = instance.GetType();
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(instance, value);
                return;
            }

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.PropertyType == typeof(bool) && property.CanWrite)
                property.SetValue(instance, value);
        }

        private static Type? FindType(params string[] assemblyQualifiedNames)
        {
            foreach (string assemblyQualifiedName in assemblyQualifiedNames)
            {
                Type? type = Type.GetType(assemblyQualifiedName);
                if (type != null)
                    return type;

                string typeName = assemblyQualifiedName;
                int comma = assemblyQualifiedName.IndexOf(',');
                if (comma >= 0)
                    typeName = assemblyQualifiedName.Substring(0, comma).Trim();

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        type = assembly.GetType(typeName, throwOnError: false);
                        if (type != null)
                            return type;
                    }
                    catch
                    {
                    }
                }
            }

            return null;
        }

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException target && target.InnerException != null
                ? target.InnerException
                : exception;
        }

        private sealed class CustomAnimalOwnerRejectionPublication
        {
            public CustomAnimalOwnerRejectionPublication(
                string ownerId,
                long lastGoodGeneration,
                ContentRefreshDirtyBatch dirtyBatch,
                string failure)
            {
                OwnerId = ownerId ?? string.Empty;
                LastGoodGeneration = lastGoodGeneration;
                DirtyBatch = dirtyBatch;
                Failure = failure ?? string.Empty;
            }

            public string OwnerId { get; }
            public long LastGoodGeneration { get; }
            public ContentRefreshDirtyBatch DirtyBatch { get; }
            public string Failure { get; }
        }

        [DataContract]
        private sealed class CustomAnimalDefinitionModel
        {
            [DataMember(Name = "speciesId")]
            public string SpeciesId { get; set; } = string.Empty;

            [DataMember(Name = "templateSpeciesId")]
            public string TemplateSpeciesId { get; set; } = string.Empty;

            [DataMember(Name = "aiTemplate")]
            public string AiTemplate { get; set; } = string.Empty;

            [DataMember(Name = "animatorMode")]
            public string AnimatorMode { get; set; } = string.Empty;

            [DataMember(Name = "adultAnimatorKey")]
            public string AdultAnimatorKey { get; set; } = string.Empty;

            [DataMember(Name = "childAnimatorKey")]
            public string ChildAnimatorKey { get; set; } = string.Empty;

            [DataMember(Name = "animatorBundle")]
            public string AnimatorBundle { get; set; } = string.Empty;

            [DataMember(Name = "adultAnimatorAsset")]
            public string AdultAnimatorAsset { get; set; } = string.Empty;

            [DataMember(Name = "childAnimatorAsset")]
            public string ChildAnimatorAsset { get; set; } = string.Empty;

            [DataMember(Name = "frameManifest")]
            public string FrameManifest { get; set; } = string.Empty;

            [DataMember(Name = "templateSpritePrefix")]
            public string TemplateSpritePrefix { get; set; } = string.Empty;

            [DataMember(Name = "customSpritePrefix")]
            public string CustomSpritePrefix { get; set; } = string.Empty;

            public void Normalize()
            {
                SpeciesId = (SpeciesId ?? string.Empty).Trim();
                TemplateSpeciesId = (TemplateSpeciesId ?? string.Empty).Trim();
                AiTemplate = (AiTemplate ?? string.Empty).Trim();
                AnimatorMode = (AnimatorMode ?? string.Empty).Trim();
                AdultAnimatorKey = (AdultAnimatorKey ?? string.Empty).Trim();
                ChildAnimatorKey = (ChildAnimatorKey ?? string.Empty).Trim();
                AnimatorBundle = (AnimatorBundle ?? string.Empty).Trim();
                AdultAnimatorAsset = (AdultAnimatorAsset ?? string.Empty).Trim();
                ChildAnimatorAsset = (ChildAnimatorAsset ?? string.Empty).Trim();
                FrameManifest = (FrameManifest ?? string.Empty).Trim();
                TemplateSpritePrefix = (TemplateSpritePrefix ?? string.Empty).Trim();
                CustomSpritePrefix = (CustomSpritePrefix ?? string.Empty).Trim();
            }
        }

        private static string FormatBoundedRefreshProjection(IEnumerable<string> values, int totalCount)
        {
            const int sampleCapacity = 16;
            var sample = new List<string>(sampleCapacity);
            foreach (string raw in values ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;
                string value = raw.Length <= 96 ? raw : raw.Substring(0, 96);
                int index = sample.BinarySearch(value, StringComparer.OrdinalIgnoreCase);
                if (index < 0)
                    index = ~index;
                if (index >= sampleCapacity)
                    continue;
                sample.Insert(index, value);
                if (sample.Count > sampleCapacity)
                    sample.RemoveAt(sampleCapacity);
            }
            int omitted = Math.Max(0, totalCount - sample.Count);
            string projection = sample.Count == 0 ? "none" : string.Join(",", sample);
            return projection + "[total=" + totalCount + ";omitted=" + omitted + "]";
        }

        private static string BoundRefreshDiagnostic(string value)
        {
            const int maxLength = 4096;
            string normalized = value ?? string.Empty;
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        private sealed class CustomAnimalAnimatorRegistration
        {
            public CustomAnimalAnimatorRegistration(
                string ownerId,
                string speciesId,
                string stage,
                string animatorKey,
                string bundlePath,
                string assetName,
                string templateAnimatorKey,
                string animatorMode,
                string frameManifestPath,
                string templateSpritePrefix,
                string customSpritePrefix)
            {
                OwnerId = ownerId ?? string.Empty;
                SpeciesId = speciesId ?? string.Empty;
                Stage = stage ?? string.Empty;
                AnimatorKey = NormalizeKey(animatorKey);
                BundlePath = bundlePath ?? string.Empty;
                AssetName = assetName ?? string.Empty;
                TemplateAnimatorKey = templateAnimatorKey ?? string.Empty;
                AnimatorMode = NormalizeKey(animatorMode);
                FrameManifestPath = frameManifestPath ?? string.Empty;
                TemplateSpritePrefix = templateSpritePrefix ?? string.Empty;
                CustomSpritePrefix = customSpritePrefix ?? string.Empty;
                ControllerCacheKey = AnimatorKey + "|" + BundlePath + "|" + AssetName + "|" + AnimatorMode + "|" + TemplateAnimatorKey;
            }

            public string OwnerId { get; }

            public string SpeciesId { get; }

            public string Stage { get; }

            public string AnimatorKey { get; }

            public string BundlePath { get; }

            public string AssetName { get; }

            public string TemplateAnimatorKey { get; }

            public string AnimatorMode { get; }

            public string FrameManifestPath { get; }

            public string TemplateSpritePrefix { get; }

            public string CustomSpritePrefix { get; }

            public string ControllerCacheKey { get; }

            public bool IsPngSpriteOverride => string.Equals(AnimatorMode, "pngSpriteOverride", StringComparison.OrdinalIgnoreCase);

            public string ControllerStatusSource => IsPngSpriteOverride ? "pngSpriteOverride template RuntimeAnimatorController" : "AssetBundle RuntimeAnimatorController";

            public string ControllerSourceDetails => IsPngSpriteOverride
                ? "template=" + TemplateAnimatorKey + " spritePrefix=" + TemplateSpritePrefix + "->" + CustomSpritePrefix
                : "bundle=" + BundlePath + " asset=" + AssetName;
        }

        private sealed class CustomAnimalAiTemplateRegistration
        {
            public CustomAnimalAiTemplateRegistration(string ownerId, string speciesId, string aiTemplate)
            {
                OwnerId = ownerId ?? string.Empty;
                SpeciesId = NormalizeKey(speciesId);
                AiTemplate = NormalizeKey(aiTemplate);
            }

            public string OwnerId { get; }

            public string SpeciesId { get; }

            public string AiTemplate { get; }
        }

        private sealed class PngSpriteOverrideContext
        {
            public PngSpriteOverrideContext(string ownerId, string speciesId, string templateSpritePrefix, string customSpritePrefix, string frameManifestPath)
            {
                OwnerId = ownerId ?? string.Empty;
                SpeciesId = NormalizeKey(speciesId);
                TemplateSpritePrefix = templateSpritePrefix ?? string.Empty;
                CustomSpritePrefix = customSpritePrefix ?? string.Empty;
                FrameManifestPath = frameManifestPath ?? string.Empty;
            }

            public string OwnerId { get; }

            public string SpeciesId { get; }

            public string TemplateSpritePrefix { get; }

            public string CustomSpritePrefix { get; }

            public string FrameManifestPath { get; }
        }

        private sealed class SleepRenderFollowUpContext
        {
            public SleepRenderFollowUpContext(string species, string identityKey, string aiState, string task, string rendererState, string reason)
            {
                Species = species ?? string.Empty;
                IdentityKey = identityKey ?? string.Empty;
                AiState = aiState ?? string.Empty;
                Task = task ?? string.Empty;
                RendererState = rendererState ?? string.Empty;
                Reason = reason ?? string.Empty;
            }

            public string Species { get; }

            public string IdentityKey { get; }

            public string AiState { get; }

            public string Task { get; }

            public string RendererState { get; }

            public string Reason { get; }

            private int FrameCount { get; set; }

            private string LastSignature { get; set; } = string.Empty;

            public int AdvanceFrame()
            {
                FrameCount++;
                return FrameCount;
            }

            public bool ShouldLog(int frame, string signature)
            {
                if (!string.Equals(LastSignature, signature, StringComparison.Ordinal))
                {
                    LastSignature = signature ?? string.Empty;
                    return true;
                }

                return IsFollowUpSampleFrame(frame);
            }
        }
    }
}
