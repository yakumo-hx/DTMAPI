using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    internal sealed class WorkshopService : IWorkshopHelper
    {
        private IWorkshopModInfo[] publication = Array.Empty<IWorkshopModInfo>();

        public void SetMods(IEnumerable<DiscoveredMod> discovered)
        {
            IWorkshopModInfo[] candidate = (discovered ?? Array.Empty<DiscoveredMod>())
                .Select(m => (IWorkshopModInfo)new WorkshopModInfo(m))
                .ToArray();
            Volatile.Write(ref publication, candidate);
        }

        public IReadOnlyList<IWorkshopModInfo> GetOfficialMods() => Volatile.Read(ref publication).ToArray();
        public IReadOnlyList<IWorkshopModInfo> GetDtmApiMods() => Volatile.Read(ref publication).Where(m => !string.IsNullOrWhiteSpace(m.UniqueID)).ToArray();
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
        private readonly DiagnosticsService diagnostics;
        private DiscoveredMod[] activeOwners = Array.Empty<DiscoveredMod>();
        private ContentAssetInfo[]? assets;

        public ContentQueryService(DiagnosticsService diagnostics) => this.diagnostics = diagnostics;

        internal void SetOwners(DiscoveredMod[] owners)
        {
            activeOwners = owners;
            assets = null;
        }

        private ContentAssetInfo[] GetAssets()
        {
            if (assets != null)
                return assets;

            var found = new List<ContentAssetInfo>();
            foreach (DiscoveredMod mod in activeOwners)
            {
                try
                {
                    foreach (string path in Directory.GetFiles(mod.RootPath, "*.*", SearchOption.AllDirectories))
                    {
                        string extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
                        if (extension != "json" && extension != "png" && extension != "txt" && extension != "csv")
                            continue;
                        string relative = path.Substring(mod.RootPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        found.Add(new ContentAssetInfo(extension, relative, mod.Manifest.UniqueID, path));
                    }
                }
                catch (Exception ex)
                {
                    diagnostics.RecordError(mod.Manifest.UniqueID, "无法读取 Mod 包资源。", mod.RootPath + ": " + ex.Message);
                }
            }
            assets = found.ToArray();
            return assets;
        }

        public int RemoveOwner(string ownerId)
        {
            activeOwners = activeOwners.Where(mod => !mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (assets == null)
                return 0;
            int removed = assets.Count(asset => asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            assets = assets.Where(asset => !asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)).ToArray();
            return removed;
        }

        public int CountOwnerResources(string ownerId) =>
            assets?.Count(asset => asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) ?? 0;

        public IReadOnlyList<IContentAssetInfo> FindAssets(string contentType) =>
            GetAssets().Where(asset => asset.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)).Cast<IContentAssetInfo>().ToArray();

        public IReadOnlyList<string> GetKnownContentTypes() =>
            GetAssets().Select(asset => asset.ContentType).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();

        public bool TryReadTextAsset(string relativePath, out string text)
        {
            ContentAssetInfo? asset = GetAssets().FirstOrDefault(value => value.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
            text = string.Empty;
            if (asset == null)
                return false;
            try
            {
                text = File.ReadAllText(asset.SourcePath);
                return true;
            }
            catch (Exception ex)
            {
                diagnostics.RecordError(asset.SourceModId, "无法读取 Mod 包资源。", asset.SourcePath + ": " + ex.Message);
                return false;
            }
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

    internal readonly struct InputButtonSample
    {
        public InputButtonSample(string button, bool isDownNow, bool pressedEdge, bool releasedEdge)
        {
            Button = DtmButton.Normalize(button);
            IsDownNow = isDownNow;
            PressedEdge = pressedEdge;
            ReleasedEdge = releasedEdge;
        }

        public string Button { get; }
        public bool IsDownNow { get; }
        public bool PressedEdge { get; }
        public bool ReleasedEdge { get; }
    }

    internal enum InputAudienceMode
    {
        Normal,
        OwnerModal,
        PlatformModal
    }

    internal readonly struct InputAudienceSnapshot : IEquatable<InputAudienceSnapshot>
    {
        public InputAudienceSnapshot(InputAudienceMode mode, string ownerId, DtmInputScope effectiveScope, long overlaySessionSequence)
        {
            Mode = mode;
            OwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
            EffectiveScope = effectiveScope;
            OverlaySessionSequence = overlaySessionSequence;
        }

        public InputAudienceMode Mode { get; }
        public string OwnerId { get; }
        public DtmInputScope EffectiveScope { get; }
        public long OverlaySessionSequence { get; }

        public bool Equals(InputAudienceSnapshot other) =>
            Mode == other.Mode &&
            EffectiveScope == other.EffectiveScope &&
            OverlaySessionSequence == other.OverlaySessionSequence &&
            OwnerId.Equals(other.OwnerId, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => obj is InputAudienceSnapshot other && Equals(other);
        public override int GetHashCode() =>
            ((int)Mode * 397) ^
            ((int)EffectiveScope * 31) ^
            OverlaySessionSequence.GetHashCode() ^
            StringComparer.OrdinalIgnoreCase.GetHashCode(OwnerId);

        public static InputAudienceSnapshot Normal(DtmInputScope scope) =>
            new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, scope, 0);
    }

    internal readonly struct InputButtonDispatch
    {
        public InputButtonDispatch(string button, string targetOwnerId)
        {
            Button = button;
            TargetOwnerId = targetOwnerId;
        }

        public string Button { get; }
        public string TargetOwnerId { get; }
        public bool IsBroadcast => TargetOwnerId.Length == 0;
    }

    internal readonly struct InputReleaseDispatch
    {
        public InputReleaseDispatch(string button, IReadOnlyList<string> recipientOwnerIds, string targetOwnerId)
        {
            Button = button;
            RecipientOwnerIds = recipientOwnerIds;
            TargetOwnerId = targetOwnerId;
        }

        public string Button { get; }
        public IReadOnlyList<string> RecipientOwnerIds { get; }
        public string TargetOwnerId { get; }
        public bool IsSettlement => RecipientOwnerIds.Count > 0;
        public bool IsBroadcast => !IsSettlement && TargetOwnerId.Length == 0;
    }

    internal readonly struct InputKeybindReleaseDispatch
    {
        public InputKeybindReleaseDispatch(InputKeybindDispatch keybind, IReadOnlyList<string> recipientOwnerIds, string targetOwnerId)
        {
            Keybind = keybind;
            RecipientOwnerIds = recipientOwnerIds;
            TargetOwnerId = targetOwnerId;
        }

        public InputKeybindDispatch Keybind { get; }
        public IReadOnlyList<string> RecipientOwnerIds { get; }
        public string TargetOwnerId { get; }
        public bool IsSettlement => RecipientOwnerIds.Count > 0;
    }

    internal sealed partial class InputService : IInputHelper
    {
        private const string GlobalOwnerId = "DTMAPI.Legacy.GlobalInput";
        private const int LocalSnapshotDormantFrameRetention = 2;
        private readonly Dictionary<string, InputRegistrationState> registrationsByKey = new Dictionary<string, InputRegistrationState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> registrationKeysByOwner = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> registrationKeysByButton = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LocalSnapshotOwnerState> localSnapshotOwners = new Dictionary<string, LocalSnapshotOwnerState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> activeLocalSnapshotOwnerCountsByButton = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> down = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> released = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> suppressedByOwner = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindDown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindPressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindReleased = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IReadOnlyList<string>> buttonPressRecipients = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PressedKeybind> pressedKeybinds = new Dictionary<string, PressedKeybind>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> settlementReleasedByOwnerButton = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> settlementReleasedButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> settlementReleasedKeybinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> requiresNeutralByOwner = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> keybindRequiresNeutral = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private InputAudienceSnapshot? frameAudience;
        private InputAudienceSnapshot? lastCompletedAudience;
        private int registryVersion;
        private int cachedButtonsVersion = -1;
        private DtmInputScope cachedButtonsScope = DtmInputScope.Gameplay;
        private string[] cachedButtons = Array.Empty<string>();
        private int cachedActiveRegistrationsVersion = -1;
        private DtmInputScope cachedActiveRegistrationsScope = DtmInputScope.Gameplay;
        private readonly List<InputRegistrationState> cachedActiveRegistrations = new List<InputRegistrationState>();
        private readonly List<string> sampleButtonsBuffer = new List<string>();
        private readonly HashSet<string> sampleButtonsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> sampledFrameButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> activeRegistrationKeysFrame = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> inactiveRegistrationKeysFrame = new List<string>();
        private readonly List<LocalSnapshotPruneEntry> localSnapshotPruneEntries = new List<LocalSnapshotPruneEntry>();
        private readonly List<string> localSnapshotEmptyOwners = new List<string>();
        private readonly List<string> localSnapshotButtonsBuffer = new List<string>();
        private string[] cachedLocalSnapshotButtons = Array.Empty<string>();
        private bool localSnapshotButtonsCacheDirty;
        private long inputFrameGeneration;
        private int localSnapshotVersion;
        private int cachedButtonsLocalSnapshotVersion = -1;

        public IInputHelper CreateOwnerBound(string ownerId, Action? ensureOwnerActive = null)
        {
            return new OwnerBoundInputHelper(this, ownerId, ensureOwnerActive ?? (() => { }));
        }

        public void RegisterButton(string button)
        {
            RegisterButtonForOwner(GlobalOwnerId, button);
        }

        public void UnregisterButton(string button)
        {
            UnregisterButtonForOwner(GlobalOwnerId, button);
        }

        public IInputRegistration RegisterKeybind(string id, string keybindText, DtmInputScope scope = DtmInputScope.Gameplay)
        {
            return RegisterKeybindForOwner(GlobalOwnerId, id, DtmKeybindList.Parse(keybindText), scope);
        }

        public IInputRegistration RegisterKeybind(string id, DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay)
        {
            return RegisterKeybindForOwner(GlobalOwnerId, id, keybinds, scope);
        }

        public IReadOnlyList<string> GetRegisteredButtons()
        {
            return registrationsByKey.Values
                .SelectMany(registration => registration.Keybinds.GetButtonIds())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        internal bool HasOwnerLegacyButtonRegistration(string ownerId, string button)
        {
            string normalizedOwner = NormalizeOwner(ownerId);
            string normalizedButton = NormalizeButton(button);
            if (normalizedButton.Length == 0)
                return false;

            string key = MakeRegistrationKey(normalizedOwner, "legacy:" + normalizedButton);
            return registrationsByKey.TryGetValue(key, out InputRegistrationState registration) &&
                !registration.Disposed &&
                !registration.DispatchKeybindEvents &&
                registration.ButtonIds.Any(candidate => candidate.Equals(normalizedButton, StringComparison.OrdinalIgnoreCase));
        }

        internal bool HasOwnerTypedKeybindForButton(string ownerId, string button)
        {
            string normalizedOwner = NormalizeOwner(ownerId);
            string normalizedButton = NormalizeButton(button);
            if (normalizedButton.Length == 0 || !registrationKeysByOwner.TryGetValue(normalizedOwner, out HashSet<string> ownerKeys))
                return false;

            foreach (string key in ownerKeys)
            {
                if (registrationsByKey.TryGetValue(key, out InputRegistrationState registration) &&
                    !registration.Disposed &&
                    registration.DispatchKeybindEvents &&
                    registration.ButtonIds.Any(candidate => candidate.Equals(normalizedButton, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            return false;
        }

        public IReadOnlyList<string> GetButtonsToSample(InputAudienceSnapshot requestedAudience)
        {
            InputAudienceSnapshot audience = FreezeAudience(requestedAudience);
            string[] activeButtons = audience.Mode == InputAudienceMode.Normal
                ? GetCachedActiveButtons(audience.EffectiveScope)
                : Array.Empty<string>();

            sampleButtonsBuffer.Clear();
            sampleButtonsSet.Clear();
            foreach (string button in down)
            {
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }

            if (audience.Mode == InputAudienceMode.Normal)
            {
                foreach (string button in activeButtons)
                {
                    if (sampleButtonsSet.Add(button))
                        sampleButtonsBuffer.Add(button);
                }
            }
            else if (audience.Mode == InputAudienceMode.OwnerModal)
            {
                foreach (InputRegistrationState registration in registrationsByKey.Values)
                {
                    if (!IsRegistrationEligible(registration, audience))
                        continue;
                    AddButtonsToSample(registration.ButtonIds);
                }
            }

            foreach (HashSet<string> ownerButtons in requiresNeutralByOwner.Values)
            {
                foreach (string button in ownerButtons)
                {
                    if (sampleButtonsSet.Add(button))
                        sampleButtonsBuffer.Add(button);
                }
            }

            foreach (PressedKeybind ledger in pressedKeybinds.Values)
                AddButtonsToSample(ledger.ButtonIds);

            foreach (string button in buttonPressRecipients.Keys)
            {
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }

            sampleButtonsBuffer.Sort(StringComparer.OrdinalIgnoreCase);
            return sampleButtonsBuffer;
        }

        public InputFrameResult RecordFrame(
            InputAudienceSnapshot requestedAudience,
            IReadOnlyList<InputButtonSample> buttonSamples,
            Func<InputButtonDispatch, IReadOnlyList<string>> dispatchButtonPressed,
            Action<InputReleaseDispatch> dispatchButtonReleased,
            Func<InputKeybindDispatch, string, IReadOnlyList<string>> dispatchKeybindPressed,
            Action<InputKeybindReleaseDispatch> dispatchKeybindReleased)
        {
            InputAudienceSnapshot audience = FreezeAudience(requestedAudience);
            RearmRestoredRegistrations(audience, buttonSamples);
            int sampled = 0;
            int pressedCount = 0;
            int releasedCount = 0;
            int keybindPressedCount = 0;
            int keybindReleasedCount = 0;
            sampledFrameButtons.Clear();

            for (int sampleIndex = 0; sampleIndex < buttonSamples.Count; sampleIndex++)
            {
                InputButtonSample sample = buttonSamples[sampleIndex];
                string button = sample.Button;
                if (button.Length == 0 || !sampledFrameButtons.Add(button))
                    continue;

                sampled++;
                bool isDownNow = sample.IsDownNow;
                bool wasDown = down.Contains(button);
                bool pressedNow = sample.PressedEdge || (isDownNow && !wasDown);
                bool releasedNow = sample.ReleasedEdge || (!isDownNow && wasDown);

                if (isDownNow)
                    down.Add(button);
                else
                    down.Remove(button);

                if (pressedNow)
                {
                    pressed.Add(button);
                    if (TryResolveButtonDispatch(button, audience, out string targetOwnerId) &&
                        !IsButtonSuppressedForDispatch(button, audience, targetOwnerId))
                    {
                        IReadOnlyList<string> receipt = dispatchButtonPressed(new InputButtonDispatch(button, targetOwnerId));
                        if (receipt.Count > 0)
                            buttonPressRecipients[button] = receipt;
                        pressedCount++;
                    }
                }

                if (releasedNow)
                {
                    released.Add(button);
                    ClearNeutralRequirementsForReleasedButton(button);
                }

                if (releasedNow && buttonPressRecipients.TryGetValue(button, out IReadOnlyList<string> buttonRecipientOwnerIds))
                {
                    dispatchButtonReleased.Invoke(new InputReleaseDispatch(button, buttonRecipientOwnerIds, string.Empty));
                    for (int ownerIndex = 0; ownerIndex < buttonRecipientOwnerIds.Count; ownerIndex++)
                        settlementReleasedByOwnerButton.Add(MakeOwnerButtonKey(buttonRecipientOwnerIds[ownerIndex], button));
                    settlementReleasedButtons.Add(button);
                    buttonPressRecipients.Remove(button);
                    releasedCount++;
                }
                else if (releasedNow && wasDown &&
                    TryResolveButtonDispatch(button, audience, out string releaseTargetOwnerId) &&
                    !IsButtonSuppressedForDispatch(button, audience, releaseTargetOwnerId))
                {
                    dispatchButtonReleased.Invoke(new InputReleaseDispatch(button, Array.Empty<string>(), releaseTargetOwnerId));
                    releasedCount++;
                }
            }

            inactiveRegistrationKeysFrame.Clear();
            foreach (KeyValuePair<string, PressedKeybind> pair in pressedKeybinds)
            {
                EvaluateKeybindState(pair.Value.Keybinds, out bool ledgerDown, out _, out _);
                if (ledgerDown)
                    continue;
                dispatchKeybindReleased.Invoke(new InputKeybindReleaseDispatch(
                    pair.Value.ToDispatch(),
                    pair.Value.RecipientOwnerIds,
                    string.Empty));
                settlementReleasedKeybinds.Add(pair.Key);
                inactiveRegistrationKeysFrame.Add(pair.Key);
                keybindReleasedCount++;
            }
            foreach (string settledKey in inactiveRegistrationKeysFrame)
                pressedKeybinds.Remove(settledKey);

            activeRegistrationKeysFrame.Clear();
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                bool eligible = IsRegistrationEligible(registration, audience);
                if (!eligible)
                {
                    if (keybindDown.Remove(registration.Key))
                        MarkRegistrationRequiresNeutral(registration);
                    continue;
                }

                activeRegistrationKeysFrame.Add(registration.Key);
                if (!registration.Keybinds.IsBound)
                    continue;

                EvaluateKeybindState(registration.Keybinds, out bool isDownNow, out bool isPressedNow, out bool isReleasedNow);
                bool requiresNeutral = keybindRequiresNeutral.Contains(registration.Key);
                if (requiresNeutral && !isDownNow)
                {
                    keybindRequiresNeutral.Remove(registration.Key);
                    ClearOwnerNeutralRequirements(registration.OwnerId, registration.Keybinds);
                    requiresNeutral = false;
                }

                bool wasDown = keybindDown.Contains(registration.Key);
                bool visibleDown = isDownNow && !requiresNeutral;
                if (visibleDown)
                    keybindDown.Add(registration.Key);
                else
                    keybindDown.Remove(registration.Key);

                bool pressedDispatched = false;
                if (!requiresNeutral &&
                    (isPressedNow || (isDownNow && !wasDown)) &&
                    registration.DispatchKeybindEvents &&
                    !IsSuppressedForOwner(registration.OwnerId, registration.Keybinds, audience))
                {
                    keybindPressed.Add(registration.Key);
                    var keybindDispatch = new InputKeybindDispatch(
                        registration.OwnerId,
                        registration.Id,
                        registration.Keybinds,
                        FindTriggerButton(registration.Keybinds));
                    string targetOwnerId = audience.Mode == InputAudienceMode.OwnerModal ? audience.OwnerId : string.Empty;
                    IReadOnlyList<string> receipt = dispatchKeybindPressed(keybindDispatch, targetOwnerId);
                    if (receipt.Count > 0)
                    {
                        pressedKeybinds[registration.Key] = new PressedKeybind(
                            registration.OwnerId,
                            registration.Id,
                            registration.Keybinds,
                            keybindDispatch.TriggerButton,
                            receipt);
                    }
                    keybindPressedCount++;
                    pressedDispatched = true;
                }

                if (!isDownNow && (isReleasedNow || wasDown) && (wasDown || pressedDispatched) &&
                    !pressedKeybinds.ContainsKey(registration.Key) &&
                    !settlementReleasedKeybinds.Contains(registration.Key))
                {
                    keybindReleased.Add(registration.Key);
                    if (registration.DispatchKeybindEvents && !IsSuppressedForOwner(registration.OwnerId, registration.Keybinds, audience))
                    {
                        string targetOwnerId = audience.Mode == InputAudienceMode.OwnerModal ? audience.OwnerId : string.Empty;
                        dispatchKeybindReleased.Invoke(new InputKeybindReleaseDispatch(new InputKeybindDispatch(
                            registration.OwnerId,
                            registration.Id,
                            registration.Keybinds,
                            FindTriggerButton(registration.Keybinds)), Array.Empty<string>(), targetOwnerId));
                        keybindReleasedCount++;
                    }
                }
            }

            inactiveRegistrationKeysFrame.Clear();
            foreach (string key in keybindDown)
            {
                if (!activeRegistrationKeysFrame.Contains(key))
                    inactiveRegistrationKeysFrame.Add(key);
            }

            foreach (string inactiveKey in inactiveRegistrationKeysFrame)
            {
                keybindDown.Remove(inactiveKey);
            }

            return new InputFrameResult(sampled, pressedCount, releasedCount, keybindPressedCount, keybindReleasedCount);
        }

        public int RemoveOwner(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return 0;

            ownerId = ownerId.Trim();
            registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> registrationKeys);

            string[] removedKeys = registrationKeys?.ToArray() ?? Array.Empty<string>();
            foreach (string registrationKey in removedKeys)
                DisposeRegistration(registrationKey);

            int removedSnapshotButtons = RemoveLocalSnapshotOwner(ownerId);
            DropOwnerFrameState(ownerId);
            return removedKeys.Length + removedSnapshotButtons;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId = NormalizeOwner(ownerId);
            int registrations = registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> registrationKeys)
                ? registrationKeys.Count
                : 0;
            int localWatches = localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState localOwner)
                ? localOwner.Buttons.Count
                : 0;
            return registrations + localWatches;
        }

        public void ClearFrame()
        {
            if (frameAudience.HasValue)
                lastCompletedAudience = frameAudience.Value;
            pressed.Clear();
            released.Clear();
            suppressed.Clear();
            suppressedByOwner.Clear();
            keybindPressed.Clear();
            keybindReleased.Clear();
            settlementReleasedByOwnerButton.Clear();
            settlementReleasedButtons.Clear();
            settlementReleasedKeybinds.Clear();
            frameAudience = null;
            CommitLocalSnapshotRequests();
        }

        public void ClearTransientState()
        {
            MarkAllRegistrationsRequiresNeutral();
            pressed.Clear();
            released.Clear();
            suppressed.Clear();
            suppressedByOwner.Clear();
            keybindDown.Clear();
            keybindPressed.Clear();
            keybindReleased.Clear();
            settlementReleasedByOwnerButton.Clear();
            settlementReleasedButtons.Clear();
            settlementReleasedKeybinds.Clear();
            frameAudience = null;
            lastCompletedAudience = null;
            ClearLocalSnapshotState();
        }

        public DtmButtonState GetState(DtmButton button) => GetStateForOwner(GlobalOwnerId, button);
        public bool IsDown(string button) => IsDownForOwner(GlobalOwnerId, DtmButton.Parse(button));
        public bool IsDown(DtmButton button) => IsDownForOwner(GlobalOwnerId, button);
        public bool WasPressed(string button) => WasPressedForOwner(GlobalOwnerId, DtmButton.Parse(button));
        public bool WasPressed(DtmButton button) => WasPressedForOwner(GlobalOwnerId, button);
        public bool WasReleased(string button) => WasReleasedForOwner(GlobalOwnerId, DtmButton.Parse(button));
        public bool WasReleased(DtmButton button) => WasReleasedForOwner(GlobalOwnerId, button);
        public bool IsKeybindDown(string id) => TryResolveRegistrationKey(id, out string key) && keybindDown.Contains(key);
        public bool WasKeybindPressed(string id) => TryResolveRegistrationKey(id, out string key) && keybindPressed.Contains(key);
        private bool IsKeybindDownForOwner(string ownerId, string id) => keybindDown.Contains(MakeRegistrationKey(ownerId, id));
        private bool WasKeybindPressedForOwner(string ownerId, string id) => keybindPressed.Contains(MakeRegistrationKey(ownerId, id));
        public void Suppress(string button) => SuppressForOwner(GlobalOwnerId, button);

        private void SuppressForOwner(string ownerId, string button)
        {
            string normalized = NormalizeButton(button);
            if (normalized.Length == 0)
                return;
            InputAudienceSnapshot audience = GetQueryAudience();
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return;
            if (audience.Mode == InputAudienceMode.OwnerModal)
            {
                if (!audience.OwnerId.Equals(NormalizeOwner(ownerId), StringComparison.OrdinalIgnoreCase))
                    return;
                suppressedByOwner.Add(MakeOwnerButtonKey(ownerId, normalized));
                return;
            }
            suppressed.Add(normalized);
        }

        internal bool IsSuppressed(string button)
        {
            string normalized = NormalizeButton(button);
            InputAudienceSnapshot audience = GetQueryAudience();
            return suppressed.Contains(normalized) ||
                (audience.Mode == InputAudienceMode.OwnerModal &&
                 suppressedByOwner.Contains(MakeOwnerButtonKey(audience.OwnerId, normalized)));
        }

        private void EvaluateKeybindState(DtmKeybindList keybinds, out bool isDownNow, out bool isPressedNow, out bool isReleasedNow)
        {
            isDownNow = false;
            isPressedNow = false;
            isReleasedNow = false;
            for (int keybindIndex = 0; keybindIndex < keybinds.KeybindCount; keybindIndex++)
            {
                DtmKeybind keybind = keybinds.GetKeybindAt(keybindIndex);
                bool chordDown = true;
                bool chordPressedEligible = true;
                bool chordReleasedEligible = true;
                bool chordPressed = false;
                bool chordReleased = false;
                for (int index = 0; index < keybind.ButtonCount; index++)
                {
                    string button = keybind.GetButtonAt(index).Id;
                    bool buttonDown = ContainsPhysicalState(down, button);
                    bool buttonPressed = ContainsPhysicalState(pressed, button);
                    bool buttonReleased = ContainsPhysicalState(released, button);
                    chordDown &= buttonDown;
                    chordPressed |= buttonPressed;
                    chordReleased |= buttonReleased;
                    if (!buttonPressed && !buttonDown && !buttonReleased)
                        chordPressedEligible = false;
                    if (!buttonReleased && !buttonDown && !buttonPressed)
                        chordReleasedEligible = false;
                }

                isDownNow |= chordDown;
                isPressedNow |= chordPressed && chordPressedEligible;
                isReleasedNow |= chordReleased && chordReleasedEligible;
            }
        }

        private static bool ContainsPhysicalState(HashSet<string> states, string button)
        {
            if (states.Contains(button))
                return true;
            if (button.Equals("Control", StringComparison.OrdinalIgnoreCase))
                return states.Contains("LeftControl") || states.Contains("RightControl");
            if (button.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                return states.Contains("LeftShift") || states.Contains("RightShift");
            if (button.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                return states.Contains("LeftAlt") || states.Contains("RightAlt");
            return false;
        }

        public IReadOnlyList<string> GetSuppressedButtons() => GetSuppressedButtonsForOwner(GlobalOwnerId);

        private IReadOnlyList<string> GetSuppressedButtonsForOwner(string ownerId)
        {
            InputAudienceSnapshot audience = GetQueryAudience();
            if (audience.Mode != InputAudienceMode.OwnerModal ||
                !audience.OwnerId.Equals(NormalizeOwner(ownerId), StringComparison.OrdinalIgnoreCase))
                return suppressed.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();

            string prefix = NormalizeOwner(ownerId) + "\n";
            return suppressed
                .Concat(suppressedByOwner
                    .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(key => key.Substring(prefix.Length)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string NormalizeButton(string button)
        {
            return DtmButton.Normalize(button);
        }

        private void RegisterButtonForOwner(string ownerId, string button)
        {
            string normalized = NormalizeButton(button);
            ownerId = NormalizeOwner(ownerId);
            if (normalized.Length == 0)
                return;

            RegisterKeybindForOwner(ownerId, "legacy:" + normalized, new DtmKeybindList(new[] { new DtmKeybind(new[] { DtmButton.Parse(normalized) }) }), DtmInputScope.Gameplay, dispatchKeybindEvents: false);
        }

        private void UnregisterButtonForOwner(string ownerId, string button)
        {
            string normalized = NormalizeButton(button);
            ownerId = NormalizeOwner(ownerId);
            if (normalized.Length == 0)
                return;

            DisposeRegistration(MakeRegistrationKey(ownerId, "legacy:" + normalized));
        }

        private DtmButtonState GetStateForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return new DtmButtonState(
                button,
                IsOwnerVisibleButtonState(ownerId, button, down, includeSettlementRelease: false),
                IsOwnerVisibleButtonState(ownerId, button, pressed, includeSettlementRelease: false),
                IsOwnerVisibleButtonState(ownerId, button, released, includeSettlementRelease: true));
        }

        private bool IsDownForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return IsOwnerVisibleButtonState(ownerId, button, down, includeSettlementRelease: false);
        }

        private bool WasPressedForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return IsOwnerVisibleButtonState(ownerId, button, pressed, includeSettlementRelease: false);
        }

        private bool WasReleasedForOwner(string ownerId, DtmButton button)
        {
            ObserveLocalSnapshotButtonForOwnerIfAllowed(ownerId, button);
            return IsOwnerVisibleButtonState(ownerId, button, released, includeSettlementRelease: true);
        }

        private bool IsOwnerVisibleButtonState(string ownerId, DtmButton button, HashSet<string> physicalState, bool includeSettlementRelease)
        {
            if (!button.IsBound)
                return false;
            ownerId = NormalizeOwner(ownerId);
            if (includeSettlementRelease && ContainsPhysicalState(settlementReleasedButtons, button.Id))
            {
                foreach (string releasedButton in settlementReleasedButtons)
                {
                    if (PhysicalButtonsConflict(button.Id, releasedButton) &&
                        settlementReleasedByOwnerButton.Contains(MakeOwnerButtonKey(ownerId, releasedButton)))
                        return true;
                }
                return false;
            }

            InputAudienceSnapshot audience = GetQueryAudience();
            if (!IsButtonVisibleToOwner(ownerId, button.Id, audience))
                return false;
            if (RequiresNeutral(ownerId, button.Id))
                return false;
            if (IsSuppressedForOwner(ownerId, button.Id, audience))
                return false;
            return ContainsPhysicalState(physicalState, button.Id);
        }

        private void ObserveLocalSnapshotButtonForOwnerIfAllowed(string ownerId, DtmButton button)
        {
            InputAudienceSnapshot audience = GetQueryAudience();
            if (audience.Mode != InputAudienceMode.Normal)
                return;
            if (OwnerHasRegistrationForButton(ownerId, button.Id))
                return;
            ObserveLocalSnapshotButtonForOwner(ownerId, button);
        }

        private bool OwnerHasRegistrationForButton(string ownerId, string button)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> ownerKeys))
                return false;
            foreach (string registrationKey in ownerKeys)
            {
                if (registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration) &&
                    !registration.Disposed && RegistrationContainsPhysicalButton(registration, button))
                    return true;
            }
            return false;
        }

        private void ObserveLocalSnapshotButtonForOwner(string ownerId, DtmButton button)
        {
            if (!button.IsBound)
                return;

            ownerId = NormalizeOwner(ownerId);
            if (!localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState owner))
            {
                owner = new LocalSnapshotOwnerState();
                localSnapshotOwners[ownerId] = owner;
            }

            if (!owner.Buttons.TryGetValue(button.Id, out LocalSnapshotButtonWatch watch))
            {
                watch = new LocalSnapshotButtonWatch();
                owner.Buttons[button.Id] = watch;
            }
            watch.LastObservedGeneration = inputFrameGeneration;
        }

        private string[] GetLocalSnapshotButtons()
        {
            if (!localSnapshotButtonsCacheDirty)
                return cachedLocalSnapshotButtons;

            localSnapshotButtonsBuffer.Clear();
            foreach (KeyValuePair<string, int> pair in activeLocalSnapshotOwnerCountsByButton)
            {
                if (pair.Value > 0)
                    localSnapshotButtonsBuffer.Add(pair.Key);
            }
            localSnapshotButtonsBuffer.Sort(StringComparer.OrdinalIgnoreCase);
            cachedLocalSnapshotButtons = localSnapshotButtonsBuffer.Count == 0
                ? Array.Empty<string>()
                : localSnapshotButtonsBuffer.ToArray();
            localSnapshotButtonsCacheDirty = false;

            return cachedLocalSnapshotButtons;
        }

        private void CommitLocalSnapshotRequests()
        {
            long observedGeneration = inputFrameGeneration;
            long nextGeneration = observedGeneration + 1;
            localSnapshotPruneEntries.Clear();
            localSnapshotEmptyOwners.Clear();

            foreach (KeyValuePair<string, LocalSnapshotOwnerState> ownerPair in localSnapshotOwners)
            {
                foreach (KeyValuePair<string, LocalSnapshotButtonWatch> buttonPair in ownerPair.Value.Buttons)
                {
                    LocalSnapshotButtonWatch watch = buttonPair.Value;
                    bool shouldBeActive = watch.LastObservedGeneration == observedGeneration;
                    if (watch.Active != shouldBeActive)
                    {
                        watch.Active = shouldBeActive;
                        AdjustActiveLocalSnapshotButton(buttonPair.Key, shouldBeActive ? 1 : -1);
                    }

                    if (!shouldBeActive && nextGeneration - watch.LastObservedGeneration > LocalSnapshotDormantFrameRetention)
                        localSnapshotPruneEntries.Add(new LocalSnapshotPruneEntry(ownerPair.Key, buttonPair.Key));
                }
            }

            foreach (LocalSnapshotPruneEntry entry in localSnapshotPruneEntries)
            {
                if (!localSnapshotOwners.TryGetValue(entry.OwnerId, out LocalSnapshotOwnerState owner) ||
                    !owner.Buttons.TryGetValue(entry.Button, out LocalSnapshotButtonWatch watch) ||
                    watch.Active ||
                    nextGeneration - watch.LastObservedGeneration <= LocalSnapshotDormantFrameRetention)
                    continue;

                owner.Buttons.Remove(entry.Button);

                if (owner.Buttons.Count == 0)
                    localSnapshotEmptyOwners.Add(entry.OwnerId);
            }

            foreach (string ownerId in localSnapshotEmptyOwners)
            {
                if (localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState owner) && owner.Buttons.Count == 0)
                    localSnapshotOwners.Remove(ownerId);
            }

            inputFrameGeneration = nextGeneration;
        }

        private int RemoveLocalSnapshotOwner(string ownerId)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState owner))
                return 0;

            int removed = owner.Buttons.Count;
            foreach (KeyValuePair<string, LocalSnapshotButtonWatch> pair in owner.Buttons)
            {
                if (pair.Value.Active)
                    AdjustActiveLocalSnapshotButton(pair.Key, -1);
            }
            localSnapshotOwners.Remove(ownerId);
            return removed;
        }

        private void AdjustActiveLocalSnapshotButton(string button, int delta)
        {
            activeLocalSnapshotOwnerCountsByButton.TryGetValue(button, out int current);
            int updated = current + delta;
            bool membershipChanged = current <= 0 || updated <= 0;
            if (updated <= 0)
                activeLocalSnapshotOwnerCountsByButton.Remove(button);
            else
                activeLocalSnapshotOwnerCountsByButton[button] = updated;

            if (membershipChanged)
            {
                localSnapshotVersion++;
                localSnapshotButtonsCacheDirty = true;
            }
        }

        private void ClearLocalSnapshotState()
        {
            bool hadActiveButtons = activeLocalSnapshotOwnerCountsByButton.Count > 0;
            localSnapshotOwners.Clear();
            activeLocalSnapshotOwnerCountsByButton.Clear();
            localSnapshotPruneEntries.Clear();
            localSnapshotEmptyOwners.Clear();
            cachedLocalSnapshotButtons = Array.Empty<string>();
            localSnapshotButtonsCacheDirty = false;
            inputFrameGeneration++;
            if (hadActiveButtons)
                localSnapshotVersion++;
        }

        private IInputRegistration RegisterKeybindForOwner(string ownerId, string id, DtmKeybindList keybinds, DtmInputScope scope, bool dispatchKeybindEvents = true, Action? ensureOwnerActive = null)
        {
            ownerId = NormalizeOwner(ownerId);
            id = NormalizeRegistrationId(id);
            string key = MakeRegistrationKey(ownerId, id);
            if (!registrationsByKey.TryGetValue(key, out InputRegistrationState registration))
            {
                registration = new InputRegistrationState(this, ownerId, id, ensureOwnerActive ?? (() => { }));
                registrationsByKey[key] = registration;
            }
            else if (ensureOwnerActive != null)
            {
                registration.EnsureOwnerActive = ensureOwnerActive;
            }

            registration.DispatchKeybindEvents = dispatchKeybindEvents;
            UpdateRegistration(registration, keybinds ?? DtmKeybindList.None, scope);
            return registration.Handle;
        }

        private void UpdateRegistration(InputRegistrationState registration, DtmKeybindList keybinds, DtmInputScope scope)
        {
            bool wasRegistered = !registration.Disposed && registration.Keybinds.IsBound;
            string[] previousButtonIds = registration.ButtonIds;
            bool unchanged = !registration.Disposed &&
                registration.Scope == scope &&
                registration.Keybinds.Equals(keybinds);
            if (unchanged)
                return;

            IReadOnlyList<string> computedButtonIds = keybinds.GetButtonIds();
            string[] newButtonIds = computedButtonIds as string[] ?? computedButtonIds.ToArray();
            RemoveIndexes(registration);
            registration.Keybinds = keybinds;
            registration.ButtonIds = newButtonIds;
            registration.Scope = scope;
            registration.Disposed = false;
            keybindDown.Remove(registration.Key);
            keybindPressed.Remove(registration.Key);
            keybindReleased.Remove(registration.Key);
            if (wasRegistered)
            {
                MarkOwnerRequiresNeutral(registration.OwnerId, previousButtonIds);
                MarkOwnerRequiresNeutral(registration.OwnerId, newButtonIds);
                keybindRequiresNeutral.Add(registration.Key);
            }
            AddIndexes(registration);
            registryVersion++;
        }

        private void DisposeRegistration(string registrationKey)
        {
            if (!registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration))
                return;

            RemoveIndexes(registration);
            registrationsByKey.Remove(registrationKey);
            keybindDown.Remove(registrationKey);
            keybindPressed.Remove(registrationKey);
            keybindReleased.Remove(registrationKey);
            keybindRequiresNeutral.Remove(registrationKey);
            pressedKeybinds.Remove(registrationKey);
            registryVersion++;
            registration.Disposed = true;
        }

        private void AddIndexes(InputRegistrationState registration)
        {
            if (!registrationKeysByOwner.TryGetValue(registration.OwnerId, out HashSet<string> ownerKeys))
            {
                ownerKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                registrationKeysByOwner[registration.OwnerId] = ownerKeys;
            }
            ownerKeys.Add(registration.Key);

            foreach (string button in registration.ButtonIds)
            {
                if (!registrationKeysByButton.TryGetValue(button, out HashSet<string> buttonKeys))
                {
                    buttonKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    registrationKeysByButton[button] = buttonKeys;
                }
                buttonKeys.Add(registration.Key);
            }
        }

        private void RemoveIndexes(InputRegistrationState registration)
        {
            if (registrationKeysByOwner.TryGetValue(registration.OwnerId, out HashSet<string> ownerKeys))
            {
                ownerKeys.Remove(registration.Key);
                if (ownerKeys.Count == 0)
                    registrationKeysByOwner.Remove(registration.OwnerId);
            }

            foreach (string button in registration.ButtonIds)
            {
                    if (registrationKeysByButton.TryGetValue(button, out HashSet<string> buttonKeys))
                {
                    buttonKeys.Remove(registration.Key);
                    if (buttonKeys.Count == 0)
                    {
                        registrationKeysByButton.Remove(button);
                    }
                }
            }
        }

        private string[] GetCachedActiveButtons(DtmInputScope currentScope)
        {
            if (cachedButtonsVersion == registryVersion &&
                cachedButtonsLocalSnapshotVersion == localSnapshotVersion &&
                cachedButtonsScope == currentScope)
                return cachedButtons;

            cachedButtonsVersion = registryVersion;
            cachedButtonsLocalSnapshotVersion = localSnapshotVersion;
            cachedButtonsScope = currentScope;
            sampleButtonsBuffer.Clear();
            sampleButtonsSet.Clear();
            foreach (InputRegistrationState registration in GetCachedActiveRegistrations(currentScope))
            {
                foreach (string button in registration.ButtonIds)
                {
                    if (sampleButtonsSet.Add(button))
                        sampleButtonsBuffer.Add(button);
                }
            }

            foreach (string button in GetLocalSnapshotButtons())
            {
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }

            sampleButtonsBuffer.Sort(StringComparer.OrdinalIgnoreCase);
            cachedButtons = sampleButtonsBuffer.ToArray();
            return cachedButtons;
        }

        private sealed class LocalSnapshotOwnerState
        {
            public Dictionary<string, LocalSnapshotButtonWatch> Buttons { get; } = new Dictionary<string, LocalSnapshotButtonWatch>(StringComparer.OrdinalIgnoreCase);
        }

        private sealed class LocalSnapshotButtonWatch
        {
            public long LastObservedGeneration { get; set; }

            public bool Active { get; set; }
        }

        private readonly struct LocalSnapshotPruneEntry
        {
            public LocalSnapshotPruneEntry(string ownerId, string button)
            {
                OwnerId = ownerId;
                Button = button;
            }

            public string OwnerId { get; }

            public string Button { get; }
        }

        private List<InputRegistrationState> GetCachedActiveRegistrations(DtmInputScope currentScope)
        {
            if (cachedActiveRegistrationsVersion == registryVersion && cachedActiveRegistrationsScope == currentScope)
                return cachedActiveRegistrations;

            cachedActiveRegistrationsVersion = registryVersion;
            cachedActiveRegistrationsScope = currentScope;
            cachedActiveRegistrations.Clear();
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (!registration.Disposed && ScopeMatches(registration.Scope, currentScope))
                    cachedActiveRegistrations.Add(registration);
            }
            return cachedActiveRegistrations;
        }

        private InputAudienceSnapshot FreezeAudience(InputAudienceSnapshot requestedAudience)
        {
            if (frameAudience.HasValue)
                return frameAudience.Value;

            ApplyAudienceEligibilityBoundary(requestedAudience);
            frameAudience = requestedAudience;
            return requestedAudience;
        }

        private InputAudienceSnapshot GetQueryAudience()
        {
            if (frameAudience.HasValue)
                return frameAudience.GetValueOrDefault();
            if (lastCompletedAudience.HasValue)
                return lastCompletedAudience.GetValueOrDefault();
            return InputAudienceSnapshot.Normal(DtmInputScope.Gameplay);
        }

        private void ApplyAudienceEligibilityBoundary(InputAudienceSnapshot audience)
        {
            if (!lastCompletedAudience.HasValue)
                return;

            InputAudienceSnapshot previous = lastCompletedAudience.Value;
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                bool wasEligible = IsRegistrationEligible(registration, previous);
                bool isEligible = IsRegistrationEligible(registration, audience);
                if (wasEligible && !isEligible)
                    MarkRegistrationRequiresNeutral(registration);
            }

            if (previous.Mode == InputAudienceMode.Normal && audience.Mode != InputAudienceMode.Normal)
            {
                foreach (KeyValuePair<string, LocalSnapshotOwnerState> ownerPair in localSnapshotOwners)
                {
                    foreach (string button in ownerPair.Value.Buttons.Keys)
                        MarkOwnerRequiresNeutral(ownerPair.Key, button);
                }
            }
        }

        private void RearmRestoredRegistrations(InputAudienceSnapshot audience, IReadOnlyList<InputButtonSample> samples)
        {
            if (!lastCompletedAudience.HasValue || lastCompletedAudience.Value.Equals(audience))
                return;

            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (IsRegistrationEligible(registration, lastCompletedAudience.Value) ||
                    !IsRegistrationEligible(registration, audience))
                    continue;

                // A blocked registration may have stopped sampling after a release. A held
                // sample must become neutral; an actual new edge keeps normal press semantics.
                for (int index = 0; index < samples.Count; index++)
                {
                    InputButtonSample sample = samples[index];
                    if (!sample.IsDownNow || sample.PressedEdge || !RegistrationContainsPhysicalButton(registration, sample.Button))
                        continue;
                    MarkRegistrationRequiresNeutral(registration);
                    break;
                }
            }
        }

        private bool IsRegistrationEligible(
            InputRegistrationState registration,
            InputAudienceSnapshot audience)
        {
            if (registration.Disposed)
                return false;
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return false;
            if (audience.Mode == InputAudienceMode.OwnerModal &&
                !registration.OwnerId.Equals(audience.OwnerId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (audience.Mode == InputAudienceMode.OwnerModal && registration.Scope == DtmInputScope.Gameplay)
                return false;
            return ScopeMatches(registration.Scope, audience.EffectiveScope);
        }

        private bool TryResolveButtonDispatch(
            string button,
            InputAudienceSnapshot audience,
            out string targetOwnerId)
        {
            targetOwnerId = string.Empty;
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return false;

            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (!IsRegistrationEligible(registration, audience) ||
                    !RegistrationContainsPhysicalButton(registration, button) ||
                    RequiresNeutral(registration.OwnerId, button))
                    continue;
                if (audience.Mode == InputAudienceMode.OwnerModal)
                    targetOwnerId = audience.OwnerId;
                return true;
            }
            return false;
        }

        private bool IsButtonVisibleToOwner(string ownerId, string button, InputAudienceSnapshot audience)
        {
            if (audience.Mode == InputAudienceMode.PlatformModal)
                return false;
            if (audience.Mode == InputAudienceMode.OwnerModal &&
                !audience.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (registrationKeysByOwner.TryGetValue(ownerId, out HashSet<string> ownerKeys))
            {
                foreach (string registrationKey in ownerKeys)
                {
                    if (registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration) &&
                        IsRegistrationEligible(registration, audience) &&
                        RegistrationContainsPhysicalButton(registration, button))
                        return true;
                }
            }

            if (audience.Mode != InputAudienceMode.Normal ||
                !localSnapshotOwners.TryGetValue(ownerId, out LocalSnapshotOwnerState localOwner) ||
                !localOwner.Buttons.TryGetValue(button, out LocalSnapshotButtonWatch watch))
                return false;
            return watch.Active || watch.LastObservedGeneration == inputFrameGeneration;
        }

        private static bool RegistrationContainsPhysicalButton(InputRegistrationState registration, string physicalButton)
        {
            foreach (string registeredButton in registration.ButtonIds)
            {
                if (PhysicalButtonsConflict(registeredButton, physicalButton))
                    return true;
            }
            return false;
        }

        private void AddButtonsToSample(IReadOnlyList<string> buttonIds)
        {
            for (int index = 0; index < buttonIds.Count; index++)
            {
                string button = buttonIds[index];
                if (sampleButtonsSet.Add(button))
                    sampleButtonsBuffer.Add(button);
            }
        }

        private bool IsButtonSuppressedForDispatch(string button, InputAudienceSnapshot audience, string targetOwnerId)
        {
            if (suppressed.Contains(button))
                return true;
            return audience.Mode == InputAudienceMode.OwnerModal &&
                targetOwnerId.Length > 0 &&
                suppressedByOwner.Contains(MakeOwnerButtonKey(targetOwnerId, button));
        }

        private bool IsSuppressedForOwner(string ownerId, string button, InputAudienceSnapshot audience)
        {
            if (suppressed.Contains(button))
                return true;
            return audience.Mode == InputAudienceMode.OwnerModal &&
                suppressedByOwner.Contains(MakeOwnerButtonKey(ownerId, button));
        }

        private bool IsSuppressedForOwner(string ownerId, DtmKeybindList keybinds, InputAudienceSnapshot audience)
        {
            foreach (DtmKeybind keybind in keybinds.Keybinds)
            {
                foreach (DtmButton button in keybind.Buttons)
                {
                    if (DtmButton.MatchesPhysicalState(button.Id, candidate => IsSuppressedForOwner(ownerId, candidate, audience)))
                        return true;
                }
            }
            return false;
        }

        private void MarkRegistrationRequiresNeutral(InputRegistrationState registration)
        {
            keybindRequiresNeutral.Add(registration.Key);
            MarkOwnerRequiresNeutral(registration.OwnerId, registration.ButtonIds);
        }

        private void MarkAllRegistrationsRequiresNeutral()
        {
            foreach (InputRegistrationState registration in registrationsByKey.Values)
                MarkRegistrationRequiresNeutral(registration);
            foreach (KeyValuePair<string, LocalSnapshotOwnerState> ownerPair in localSnapshotOwners)
            {
                foreach (string button in ownerPair.Value.Buttons.Keys)
                    MarkOwnerRequiresNeutral(ownerPair.Key, button);
            }
        }

        private void MarkOwnerRequiresNeutral(string ownerId, IReadOnlyList<string> buttonIds)
        {
            for (int index = 0; index < buttonIds.Count; index++)
                MarkOwnerRequiresNeutral(ownerId, buttonIds[index]);
        }

        private void MarkOwnerRequiresNeutral(string ownerId, string button)
        {
            ownerId = NormalizeOwner(ownerId);
            button = NormalizeButton(button);
            if (button.Length == 0)
                return;
            if (!requiresNeutralByOwner.TryGetValue(ownerId, out HashSet<string> ownerButtons))
            {
                ownerButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                requiresNeutralByOwner[ownerId] = ownerButtons;
            }
            ownerButtons.Add(button);
        }

        private bool RequiresNeutral(string ownerId, string button)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!requiresNeutralByOwner.TryGetValue(ownerId, out HashSet<string> ownerButtons))
                return false;
            foreach (string requiredButton in ownerButtons)
            {
                if (PhysicalButtonsConflict(requiredButton, button))
                    return true;
            }
            return false;
        }

        private void ClearNeutralRequirementsForReleasedButton(string releasedButton)
        {
            localSnapshotEmptyOwners.Clear();
            foreach (KeyValuePair<string, HashSet<string>> ownerPair in requiresNeutralByOwner)
            {
                inactiveRegistrationKeysFrame.Clear();
                foreach (string requiredButton in ownerPair.Value)
                {
                    if (PhysicalButtonsConflict(requiredButton, releasedButton) &&
                        !ContainsPhysicalState(down, requiredButton))
                        inactiveRegistrationKeysFrame.Add(requiredButton);
                }
                foreach (string neutralButton in inactiveRegistrationKeysFrame)
                    ownerPair.Value.Remove(neutralButton);
                if (ownerPair.Value.Count == 0)
                    localSnapshotEmptyOwners.Add(ownerPair.Key);
            }
            foreach (string emptyOwner in localSnapshotEmptyOwners)
                requiresNeutralByOwner.Remove(emptyOwner);

            inactiveRegistrationKeysFrame.Clear();
            foreach (string registrationKey in keybindRequiresNeutral)
            {
                if (!registrationsByKey.TryGetValue(registrationKey, out InputRegistrationState registration))
                {
                    inactiveRegistrationKeysFrame.Add(registrationKey);
                    continue;
                }

                bool anyDown = false;
                for (int index = 0; index < registration.ButtonIds.Length; index++)
                {
                    if (!ContainsPhysicalState(down, registration.ButtonIds[index]))
                        continue;
                    anyDown = true;
                    break;
                }
                if (!anyDown)
                    inactiveRegistrationKeysFrame.Add(registrationKey);
            }
            foreach (string neutralRegistration in inactiveRegistrationKeysFrame)
                keybindRequiresNeutral.Remove(neutralRegistration);
        }

        private void ClearOwnerNeutralRequirements(string ownerId, DtmKeybindList keybinds)
        {
            ownerId = NormalizeOwner(ownerId);
            if (!requiresNeutralByOwner.TryGetValue(ownerId, out HashSet<string> ownerButtons))
                return;
            foreach (string button in keybinds.GetButtonIds())
            {
                if (!ContainsPhysicalState(down, button))
                    ownerButtons.Remove(button);
            }
            if (ownerButtons.Count == 0)
                requiresNeutralByOwner.Remove(ownerId);
        }

        private void DropOwnerFrameState(string ownerId)
        {
            ownerId = NormalizeOwner(ownerId);
            requiresNeutralByOwner.Remove(ownerId);
            suppressedByOwner.RemoveWhere(key => key.StartsWith(ownerId + "\n", StringComparison.OrdinalIgnoreCase));
            settlementReleasedByOwnerButton.RemoveWhere(key => key.StartsWith(ownerId + "\n", StringComparison.OrdinalIgnoreCase));

            foreach (string button in buttonPressRecipients.Keys.ToArray())
            {
                IReadOnlyList<string> filtered = FilterRecipients(buttonPressRecipients[button], ownerId);
                if (filtered.Count > 0)
                    buttonPressRecipients[button] = filtered;
                else
                    buttonPressRecipients.Remove(button);
            }
            foreach (string key in pressedKeybinds.Keys.ToArray())
            {
                PressedKeybind ledger = pressedKeybinds[key];
                if (ledger.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                {
                    pressedKeybinds.Remove(key);
                    continue;
                }
                IReadOnlyList<string> filtered = FilterRecipients(ledger.RecipientOwnerIds, ownerId);
                if (filtered.Count > 0)
                    pressedKeybinds[key] = ledger.WithRecipients(filtered);
                else
                    pressedKeybinds.Remove(key);
            }
        }

        private static IReadOnlyList<string> FilterRecipients(IReadOnlyList<string> receipt, string removedOwnerId)
        {
            string[] remaining = receipt
                .Where(owner => !owner.Equals(removedOwnerId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return remaining;
        }

        private static string MakeOwnerButtonKey(string ownerId, string button) =>
            NormalizeOwner(ownerId) + "\n" + NormalizeButton(button);

        private static bool PhysicalButtonsConflict(string left, string right)
        {
            if (left.Equals(right, StringComparison.OrdinalIgnoreCase))
                return true;
            return BelongsToModifierFamily(left, right, "Control", "LeftControl", "RightControl") ||
                BelongsToModifierFamily(left, right, "Shift", "LeftShift", "RightShift") ||
                BelongsToModifierFamily(left, right, "Alt", "LeftAlt", "RightAlt");
        }

        private static bool BelongsToModifierFamily(
            string left,
            string right,
            string generic,
            string leftPhysical,
            string rightPhysical)
        {
            bool leftGeneric = left.Equals(generic, StringComparison.OrdinalIgnoreCase);
            bool rightGeneric = right.Equals(generic, StringComparison.OrdinalIgnoreCase);
            if (leftGeneric)
            {
                return rightGeneric ||
                    right.Equals(leftPhysical, StringComparison.OrdinalIgnoreCase) ||
                    right.Equals(rightPhysical, StringComparison.OrdinalIgnoreCase);
            }
            if (!rightGeneric)
                return false;
            return left.Equals(leftPhysical, StringComparison.OrdinalIgnoreCase) ||
                left.Equals(rightPhysical, StringComparison.OrdinalIgnoreCase);
        }

        private string FindTriggerButton(DtmKeybindList keybinds)
        {
            string fallback = string.Empty;
            foreach (DtmKeybind keybind in keybinds.Keybinds)
            {
                foreach (DtmButton button in keybind.Buttons)
                {
                    if (fallback.Length == 0)
                        fallback = button.Id;
                    if (DtmButton.MatchesPhysicalState(button.Id, pressed.Contains) ||
                        DtmButton.MatchesPhysicalState(button.Id, released.Contains))
                        return button.Id;
                }
            }
            return fallback;
        }

        private bool TryResolveRegistrationKey(string id, out string key)
        {
            id = NormalizeRegistrationId(id);
            key = MakeRegistrationKey(GlobalOwnerId, id);
            if (registrationsByKey.ContainsKey(key) || keybindDown.Contains(key) || keybindPressed.Contains(key))
                return true;
            foreach (InputRegistrationState registration in registrationsByKey.Values)
            {
                if (registration.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
                {
                    key = registration.Key;
                    return true;
                }
            }
            return false;
        }

        private static bool ScopeMatches(DtmInputScope registrationScope, DtmInputScope currentScope)
        {
            if (registrationScope == DtmInputScope.Always)
                return true;
            if (registrationScope == DtmInputScope.SaveLoaded)
                return currentScope == DtmInputScope.SaveLoaded || currentScope == DtmInputScope.Gameplay;
            return registrationScope == currentScope;
        }

        private static string MakeRegistrationKey(string ownerId, string id)
        {
            return NormalizeOwner(ownerId) + "\n" + NormalizeRegistrationId(id);
        }

        private static string NormalizeRegistrationId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? "default" : id.Trim();
        }

        private static string NormalizeOwner(string ownerId)
        {
            return string.IsNullOrWhiteSpace(ownerId) ? "unknown" : ownerId.Trim();
        }

        private sealed class PressedKeybind
        {
            public PressedKeybind(
                string ownerId,
                string keybindId,
                DtmKeybindList keybinds,
                string triggerButton,
                IReadOnlyList<string> receipt)
            {
                OwnerId = ownerId;
                KeybindId = keybindId;
                Keybinds = keybinds;
                TriggerButton = triggerButton;
                RecipientOwnerIds = receipt;
                IReadOnlyList<string> buttonIds = keybinds.GetButtonIds();
                ButtonIds = buttonIds as string[] ?? buttonIds.ToArray();
            }

            public string OwnerId { get; }
            public string KeybindId { get; }
            public DtmKeybindList Keybinds { get; }
            public string TriggerButton { get; }
            public IReadOnlyList<string> RecipientOwnerIds { get; }
            public string[] ButtonIds { get; }

            public InputKeybindDispatch ToDispatch() =>
                new InputKeybindDispatch(OwnerId, KeybindId, Keybinds, TriggerButton);

            public PressedKeybind WithRecipients(IReadOnlyList<string> receipt) =>
                new PressedKeybind(OwnerId, KeybindId, Keybinds, TriggerButton, receipt);
        }

        private sealed class InputRegistrationState
        {
            public InputRegistrationState(InputService input, string ownerId, string id, Action ensureOwnerActive)
            {
                OwnerId = ownerId;
                Id = id;
                Key = MakeRegistrationKey(ownerId, id);
                EnsureOwnerActive = ensureOwnerActive;
                Handle = new InputRegistrationHandle(input, this);
            }

            public string OwnerId { get; }
            public string Id { get; }
            public string Key { get; }
            public DtmKeybindList Keybinds { get; set; } = DtmKeybindList.None;
            public string[] ButtonIds { get; set; } = Array.Empty<string>();
            public DtmInputScope Scope { get; set; } = DtmInputScope.Gameplay;
            public bool DispatchKeybindEvents { get; set; } = true;
            public bool Disposed { get; set; }
            public Action EnsureOwnerActive { get; set; }
            public InputRegistrationHandle Handle { get; }
        }

        private sealed class InputRegistrationHandle : IInputRegistration
        {
            private readonly InputService input;
            private readonly InputRegistrationState state;

            public InputRegistrationHandle(InputService input, InputRegistrationState state)
            {
                this.input = input;
                this.state = state;
            }

            public string Id => state.Id;
            public string OwnerId => state.OwnerId;
            public DtmKeybindList Keybinds => state.Keybinds;
            public DtmInputScope Scope => state.Scope;
            public bool IsDisposed => state.Disposed;
            public void Update(string keybindText, DtmInputScope scope = DtmInputScope.Gameplay) => Update(DtmKeybindList.Parse(keybindText), scope);
            public void Update(DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay)
            {
                if (state.Disposed)
                    throw new ObjectDisposedException("InputRegistration");
                state.EnsureOwnerActive();
                input.UpdateRegistration(state, keybinds ?? DtmKeybindList.None, scope);
            }

            public void Dispose()
            {
                if (state.Disposed)
                    return;
                input.DisposeRegistration(state.Key);
            }
        }

        private sealed class OwnerBoundInputHelper : IInputHelper
        {
            private readonly InputService input;
            private readonly string ownerId;
            private readonly Action ensureOwnerActive;

            public OwnerBoundInputHelper(InputService input, string ownerId, Action ensureOwnerActive)
            {
                this.input = input;
                this.ownerId = NormalizeOwner(ownerId);
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public void RegisterButton(string button) { ensureOwnerActive(); input.RegisterButtonForOwner(ownerId, button); }
            public void UnregisterButton(string button) { input.UnregisterButtonForOwner(ownerId, button); }
            public IInputRegistration RegisterKeybind(string id, string keybindText, DtmInputScope scope = DtmInputScope.Gameplay) { ensureOwnerActive(); return input.RegisterKeybindForOwner(ownerId, id, DtmKeybindList.Parse(keybindText), scope, ensureOwnerActive: ensureOwnerActive); }
            public IInputRegistration RegisterKeybind(string id, DtmKeybindList keybinds, DtmInputScope scope = DtmInputScope.Gameplay) { ensureOwnerActive(); return input.RegisterKeybindForOwner(ownerId, id, keybinds, scope, ensureOwnerActive: ensureOwnerActive); }
            public IReadOnlyList<string> GetRegisteredButtons() { ensureOwnerActive(); return input.GetRegisteredButtons(); }
            public DtmButtonState GetState(DtmButton button) { ensureOwnerActive(); return input.GetStateForOwner(ownerId, button); }
            public bool IsDown(string button) { ensureOwnerActive(); return input.IsDownForOwner(ownerId, DtmButton.Parse(button)); }
            public bool IsDown(DtmButton button) { ensureOwnerActive(); return input.IsDownForOwner(ownerId, button); }
            public bool WasPressed(string button) { ensureOwnerActive(); return input.WasPressedForOwner(ownerId, DtmButton.Parse(button)); }
            public bool WasPressed(DtmButton button) { ensureOwnerActive(); return input.WasPressedForOwner(ownerId, button); }
            public bool WasReleased(string button) { ensureOwnerActive(); return input.WasReleasedForOwner(ownerId, DtmButton.Parse(button)); }
            public bool WasReleased(DtmButton button) { ensureOwnerActive(); return input.WasReleasedForOwner(ownerId, button); }
            public bool IsKeybindDown(string id) { ensureOwnerActive(); return input.IsKeybindDownForOwner(ownerId, id); }
            public bool WasKeybindPressed(string id) { ensureOwnerActive(); return input.WasKeybindPressedForOwner(ownerId, id); }
            public void Suppress(string button) { ensureOwnerActive(); input.SuppressForOwner(ownerId, button); }
            public IReadOnlyList<string> GetSuppressedButtons() { ensureOwnerActive(); return input.GetSuppressedButtonsForOwner(ownerId); }
        }
    }

    internal readonly struct InputFrameResult
    {
        public InputFrameResult(int sampledButtons, int pressedEvents, int releasedEvents, int keybindPressedEvents, int keybindReleasedEvents)
        {
            SampledButtons = sampledButtons;
            PressedEvents = pressedEvents;
            ReleasedEvents = releasedEvents;
            KeybindPressedEvents = keybindPressedEvents;
            KeybindReleasedEvents = keybindReleasedEvents;
        }

        public int SampledButtons { get; }
        public int PressedEvents { get; }
        public int ReleasedEvents { get; }
        public int KeybindPressedEvents { get; }
        public int KeybindReleasedEvents { get; }
    }

    internal sealed class InputKeybindDispatch
    {
        public InputKeybindDispatch(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            OwnerId = ownerId;
            KeybindId = keybindId;
            Keybinds = keybinds;
            TriggerButton = triggerButton;
        }

        public string OwnerId { get; }
        public string KeybindId { get; }
        public DtmKeybindList Keybinds { get; }
        public string TriggerButton { get; }
    }

    public enum DtmOverlayPage
    {
        Status,
        Mods,
        Config,
        Errors,
        Hooks,
        Features,
        Logs
    }

    public sealed partial class UiRuntimeService : IUiHelper
    {
        private readonly Func<string> exportLogs;
        private readonly Action<string> opened;
        private readonly Action<string> closed;
        private readonly Action<string, string, string>? recordError;
        private readonly Action<string, LogLevel>? log;

        public UiRuntimeService(
            Func<string> exportLogs,
            Action<string> opened,
            Action<string> closed,
            Action<string, string, string>? recordError = null,
            Action<string, LogLevel>? log = null)
        {
            this.exportLogs = exportLogs;
            this.opened = opened;
            this.closed = closed;
            this.recordError = recordError;
            this.log = log;
        }

        internal IUiHelper CreateOwnerBound(string ownerId, Action ensureRuntimeThread, Action ensureOwnerActive)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner id is required.", nameof(ownerId));
            return new OwnerBoundUiHelper(
                this,
                ownerId,
                ensureRuntimeThread ?? throw new ArgumentNullException(nameof(ensureRuntimeThread)),
                ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive)));
        }

        public bool IsOpen { get; private set; }
        public DtmOverlayPage CurrentPage { get; private set; } = DtmOverlayPage.Status;
        public string ActiveMenuId { get; private set; } = string.Empty;
        internal string ActiveMenuOwnerId { get; private set; } = string.Empty;
        internal long OverlaySessionSequence { get; private set; }
        public string LastExportPath { get; private set; } = string.Empty;
        public string? RequestedConfigUniqueId { get; private set; }
        public string InputContext { get; private set; } = "Unknown";
        public bool CanDrawOverlay { get; private set; } = true;
        public bool GameplayHotkeysAllowed { get; private set; } = true;
        public string DrawBoundaryReason { get; private set; } = string.Empty;
        public bool BlocksGameplayHotkeys => IsOpen || !GameplayHotkeysAllowed;
        internal DtmManagerRuntimeModelProvider? ManagerModelProvider { get; set; }
        internal DtmManagerViewModel? CurrentManagerModel { get; private set; }
        internal DtmManagerReportExportResult? LastManagerReportExport { get; private set; }
        internal DtmManagerCopySummaryResult? LastManagerSummaryCopy { get; private set; }
        internal string LastManagerRefreshError { get; private set; } = string.Empty;

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
            string menuId = string.IsNullOrWhiteSpace(ActiveMenuId) ? "DTMAPI." + CurrentPage : ActiveMenuId;
            IsOpen = false;
            ActiveMenuId = string.Empty;
            ActiveMenuOwnerId = string.Empty;
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
        public void OpenFeatureStatusPage() => Open(DtmOverlayPage.Features);
        public void OpenLogsPage() => Open(DtmOverlayPage.Logs);
        public void SetPage(DtmOverlayPage page) => Open(page);

        public void OpenCustomMenu(string menuId)
        {
            bool wasOpen = IsOpen;
            OverlaySessionSequence++;
            ActiveMenuId = string.IsNullOrWhiteSpace(menuId) ? "DTMAPI.Custom" : menuId.Trim();
            ActiveMenuOwnerId = string.Empty;
            IsOpen = true;
            if (!wasOpen)
                opened(ActiveMenuId);
        }

        internal bool TryOpenOwnerBoundCustomMenu(string menuId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                return false;
            return OpenOwnerBoundModal(menuId, ownerId, () => { }).Session != null;
        }

        internal void OpenOwnerBoundCustomMenu(
            string menuId,
            string ownerId)
        {
            if (!TryOpenOwnerBoundCustomMenu(menuId, ownerId))
            {
                throw new InvalidOperationException(
                    "Another owner already holds the DTMAPI modal token.");
            }
        }

        internal bool IsOwnerBoundCustomMenuOpen(
            string menuId,
            string ownerId)
        {
            return IsOpen &&
                ActiveMenuId.Equals(
                    (menuId ?? string.Empty).Trim(),
                    StringComparison.OrdinalIgnoreCase) &&
                ActiveMenuOwnerId.Equals(
                    (ownerId ?? string.Empty).Trim(),
                    StringComparison.OrdinalIgnoreCase);
        }

        internal bool CloseOwnerBoundCustomMenu(
            string menuId,
            string ownerId)
        {
            if (!IsOwnerBoundCustomMenuOpen(menuId, ownerId))
                return false;
            Close();
            return true;
        }

        public void SetUiContext(string inputContext, bool canDrawOverlay, bool gameplayHotkeysAllowed, string reason)
        {
            InputContext = string.IsNullOrWhiteSpace(inputContext) ? "Unknown" : inputContext;
            CanDrawOverlay = canDrawOverlay;
            GameplayHotkeysAllowed = gameplayHotkeysAllowed;
            DrawBoundaryReason = reason ?? string.Empty;
        }

        public string ExportLogs()
        {
            try
            {
                if (ManagerModelProvider != null)
                {
                    LastManagerReportExport = ManagerModelProvider.ExportReportAndRefresh();
                    CurrentManagerModel = LastManagerReportExport.RefreshedModel;
                    LastExportPath = LastManagerReportExport.ExportedReportPath;
                }
                else
                {
                    LastExportPath = exportLogs() ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                LastExportPath = string.Empty;
                LastManagerReportExport = DtmManagerReportExportResult.FromFailure(ex, CurrentManagerModel);
                RecordManagerUiFailure("ExportLogs", ex);
            }

            CurrentPage = DtmOverlayPage.Logs;
            Open(DtmOverlayPage.Logs);
            return LastExportPath;
        }

        private void Open(DtmOverlayPage page)
        {
            bool wasOpen = IsOpen;
            OverlaySessionSequence++;
            CurrentPage = page;
            ActiveMenuId = "DTMAPI." + page;
            ActiveMenuOwnerId = string.Empty;
            IsOpen = true;
            RefreshDtmManagerModel();
            if (!wasOpen)
                opened(ActiveMenuId);
        }

        internal void RefreshDtmManagerModel()
        {
            if (ManagerModelProvider == null)
                return;

            try
            {
                CurrentManagerModel = ManagerModelProvider.GetCurrentModel();
                LastManagerRefreshError = string.Empty;
            }
            catch (Exception ex)
            {
                LastManagerRefreshError = ex.GetType().Name + ": " + ex.Message;
                RecordManagerUiFailure("RefreshDtmManagerModel", ex);
            }
        }

        internal DtmManagerCopySummaryResult CopyManagerSummary(Func<string, bool> copyText)
        {
            string summary = ManagerPageRowFormatter.FormatStatusSummary(CurrentManagerModel, LastManagerRefreshError, LastManagerReportExport);
            try
            {
                bool copied = copyText != null && copyText(summary);
                LastManagerSummaryCopy = copied
                    ? DtmManagerCopySummaryResult.FromCopied(summary)
                    : DtmManagerCopySummaryResult.FromUnavailable(summary, "Clipboard unavailable.");
            }
            catch (Exception ex)
            {
                LastManagerSummaryCopy = DtmManagerCopySummaryResult.FromUnavailable(summary, ex.GetType().Name + ": " + ex.Message);
            }

            if (LastManagerSummaryCopy.Copied)
                log?.Invoke("Manager UI Copy Summary copied.", LogLevel.Info);
            else
                log?.Invoke("Manager UI Copy Summary unavailable; summary text retained in runtime log. " + LastManagerSummaryCopy.Text, LogLevel.Warn);

            return LastManagerSummaryCopy;
        }

        private void RecordManagerUiFailure(string operation, Exception exception)
        {
            string message = "Manager UI " + operation + " failed.";
            recordError?.Invoke("DTMAPI.ManagerUI", message, exception.ToString());
            log?.Invoke(message + " " + exception.GetType().Name + ": " + exception.Message, LogLevel.Warn);
        }

        private sealed class OwnerBoundUiHelper :
            IUiHelper, IUiModalSessions
        {
            private readonly UiRuntimeService inner;
            private readonly string ownerId;
            private readonly Action ensureRuntimeThread;
            private readonly Action ensureOwnerActive;

            public OwnerBoundUiHelper(
                UiRuntimeService inner,
                string ownerId,
                Action ensureRuntimeThread,
                Action ensureOwnerActive)
            {
                this.inner = inner;
                this.ownerId = ownerId;
                this.ensureRuntimeThread = ensureRuntimeThread;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public bool TryOpenModal(string menuId)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.TryOpenOwnerBoundCustomMenu(
                    menuId,
                    ownerId);
            }

            public DtmModalOpenResult OpenModal(string menuId)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.OpenOwnerBoundModal(menuId, ownerId, ensureRuntimeThread);
            }

            public bool IsModalOpen(string menuId)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.IsOwnerBoundCustomMenuOpen(
                    menuId,
                    ownerId);
            }

            public bool CloseModal(string menuId)
            {
                ensureRuntimeThread();
                return inner.CloseOwnerBoundCustomMenu(
                    menuId,
                    ownerId);
            }

            public void OpenDtmApiStatusPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenDtmApiStatusPage();
            }

            public void OpenModListPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenModListPage();
            }

            public void OpenConfigPage(string? uniqueId = null)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenConfigPage(uniqueId);
            }

            public void OpenErrorPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenErrorPage();
            }

            public void OpenHookStatusPage()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.OpenHookStatusPage();
            }

            public string ExportLogs()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.ExportLogs();
            }
        }
    }
}
