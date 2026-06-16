using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AudioReplacementService : IAudioReplacementApi
    {
        private const string ReadyStatus = "ready";
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, AudioReplacementEntry> entries = new Dictionary<string, AudioReplacementEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AudioReplacementState> states = new Dictionary<string, AudioReplacementState>(StringComparer.OrdinalIgnoreCase);
        private bool hookInstalled;

        public AudioReplacementService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal void SetHookInstalled(bool installed)
        {
            hookInstalled = installed;
            UpdateAllOwnerStates("hook-installed=" + installed.ToString(CultureInfo.InvariantCulture));
        }

        public AudioReplacementRegisterResult RegisterReplacement(IManifest owner, AudioReplacementOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            AudioReplacementOptions normalized = NormalizeOptions(options);
            string replacementId = normalized.ReplacementId;
            string key = MakeKey(owner.UniqueID, replacementId);
            var entry = new AudioReplacementEntry(owner.UniqueID, normalized);
            entries[key] = entry;

            if (normalized.Enabled)
                EnsureLoadStarted(entry);

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
                Message = state.LastMessage
            };

            runtime.RuntimeMonitor.Log("AudioReplacement register owner=" + owner.UniqueID +
                " replacement=" + replacementId +
                " event=" + normalized.NativeSoundEvent +
                " enabled=" + normalized.Enabled +
                " hook=" + hookInstalled +
                " preload=" + entry.LoadStatus +
                " path=" + normalized.AudioPath);
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

        internal void Update()
        {
            foreach (AudioReplacementEntry entry in entries.Values.ToArray())
                PollLoad(entry);
        }

        internal bool HandleNativeSoundEvent(string? eventName, ref bool nativeResult)
        {
            string normalizedEvent = NormalizeEventName(eventName);
            if (normalizedEvent.Length == 0)
                return true;

            AudioReplacementEntry[] candidates = entries.Values
                .Where(e => e.Options.Enabled && string.Equals(e.Options.NativeSoundEvent, normalizedEvent, StringComparison.OrdinalIgnoreCase))
                .OrderBy(e => e.OwnerId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.Options.ReplacementId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (candidates.Length == 0)
                return true;

            foreach (AudioReplacementEntry entry in candidates)
            {
                if (!entry.IsReady)
                {
                    EnsureLoadStarted(entry);
                    RecordEvent(entry, normalizedEvent, played: false, suppressed: false, "replacement not ready; native sound allowed. loadStatus=" + entry.LoadStatus);
                    continue;
                }

                if (entry.Options.CooldownMilliseconds > 0 && DateTimeOffset.UtcNow - entry.LastPlayedAtUtc < TimeSpan.FromMilliseconds(entry.Options.CooldownMilliseconds))
                {
                    RecordEvent(entry, normalizedEvent, played: false, suppressed: false, "replacement cooldown active; native sound allowed.");
                    continue;
                }

                bool played = TryPlay(entry, out string playMessage);
                bool suppress = played && entry.Options.SuppressNativeWhenReady;
                RecordEvent(entry, normalizedEvent, played, suppress, playMessage);
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

        private void EnsureLoadStarted(AudioReplacementEntry entry)
        {
            if (entry.LoadStarted || entry.IsReady)
                return;

            if (!File.Exists(entry.Options.AudioPath))
            {
                entry.LoadStatus = "failed";
                entry.LoadFailureReason = "Audio file missing: " + entry.Options.AudioPath;
                UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                return;
            }

            try
            {
                Type? multimedia = FindType("UnityEngine.Networking.UnityWebRequestMultimedia");
                Type? audioType = FindType("UnityEngine.AudioType");
                Type? requestType = FindType("UnityEngine.Networking.UnityWebRequest");
                if (multimedia == null || audioType == null || requestType == null)
                {
                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = "UnityWebRequest audio types unavailable.";
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                object wav = Enum.Parse(audioType, "WAV");
                MethodInfo? getAudioClip = multimedia.GetMethod("GetAudioClip", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), audioType }, null);
                if (getAudioClip == null)
                {
                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = "UnityWebRequestMultimedia.GetAudioClip unavailable.";
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                string uri = new Uri(entry.Options.AudioPath).AbsoluteUri;
                object? request = getAudioClip.Invoke(null, new object[] { uri, wav });
                MethodInfo? sendWebRequest = request?.GetType().GetMethod("SendWebRequest", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                object? operation = sendWebRequest?.Invoke(request, null);
                if (request == null || operation == null)
                {
                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = "UnityWebRequest did not start.";
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                entry.LoadStarted = true;
                entry.Request = request;
                entry.AsyncOperation = operation;
                entry.LoadStatus = "loading";
                entry.LastMessage = "Loading local WAV from " + entry.Options.AudioPath;
                UpdateOwnerState(entry.OwnerId, entry.LastMessage);
            }
            catch (Exception ex)
            {
                entry.LoadStatus = "failed";
                entry.LoadFailureReason = ex.GetType().Name + ": " + ex.Message;
                UpdateOwnerState(entry.OwnerId, "AudioReplacement load start failed: " + entry.LoadFailureReason);
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to start local WAV load.", ex.ToString());
            }
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
                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = "UnityWebRequest failed result=" + result + " error=" + error;
                    DisposeRequest(entry);
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                Type? downloadHandlerAudioClip = FindType("UnityEngine.Networking.DownloadHandlerAudioClip");
                MethodInfo? getContent = downloadHandlerAudioClip?.GetMethod("GetContent", BindingFlags.Public | BindingFlags.Static);
                object? clip = getContent?.Invoke(null, new[] { entry.Request });
                if (clip == null)
                {
                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = "DownloadHandlerAudioClip.GetContent returned null.";
                    DisposeRequest(entry);
                    UpdateOwnerState(entry.OwnerId, entry.LoadFailureReason);
                    return;
                }

                entry.AudioClip = clip;
                entry.LoadStatus = ReadyStatus;
                entry.LoadFailureReason = string.Empty;
                entry.LastMessage = "Local WAV ready for " + entry.Options.NativeSoundEvent + ": " + entry.Options.AudioPath;
                DisposeRequest(entry);
                UpdateOwnerState(entry.OwnerId, entry.LastMessage);
                runtime.RuntimeMonitor.Log("AudioReplacement local WAV ready owner=" + entry.OwnerId +
                    " replacement=" + entry.Options.ReplacementId +
                    " event=" + entry.Options.NativeSoundEvent +
                    " path=" + entry.Options.AudioPath);
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
            try
            {
                if (entry.AudioClip == null)
                {
                    message = "replacement clip missing; native sound allowed.";
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

                object? go = Activator.CreateInstance(gameObjectType, "DTMAPI.AudioReplacement." + entry.Options.ReplacementId);
                MethodInfo? addComponent = gameObjectType.GetMethod("AddComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
                object? source = addComponent?.Invoke(go, new object[] { audioSourceType });
                if (go == null || source == null)
                {
                    message = "AudioSource creation failed; native sound allowed.";
                    return false;
                }

                SetProperty(source, "playOnAwake", false);
                SetProperty(source, "spatialBlend", 0f);
                SetProperty(source, "volume", (float)entry.Options.Volume);
                SetProperty(source, "clip", entry.AudioClip);
                MethodInfo? play = audioSourceType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                play?.Invoke(source, null);

                float length = Math.Max(0.1f, ReadFloat(entry.AudioClip, "length", 2f));
                MethodInfo? destroy = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "Destroy" && m.GetParameters().Length == 2);
                destroy?.Invoke(null, new object[] { go, length + 0.25f });

                entry.LastPlayedAtUtc = DateTimeOffset.UtcNow;
                message = "AudioReplacement played event=" + entry.Options.NativeSoundEvent +
                    " replacement=" + entry.Options.ReplacementId +
                    " suppressNative=" + entry.Options.SuppressNativeWhenReady +
                    " path=" + entry.Options.AudioPath;
                return true;
            }
            catch (Exception ex)
            {
                message = "AudioReplacement playback failed: " + ex.GetType().Name + ": " + ex.Message + "; native sound allowed.";
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to play local replacement audio.", ex.ToString());
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
                played ? "verified" : (hookInstalled ? "experimental" : "pending"),
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
            AudioReplacementEntry[] ownerEntries = entries.Values.Where(e => string.Equals(e.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)).ToArray();
            bool configured = ownerEntries.Length > 0;
            bool enabled = ownerEntries.Any(e => e.Options.Enabled);
            bool ready = ownerEntries.Any(e => e.IsReady);
            AudioReplacementEntry? last = ownerEntries.OrderByDescending(e => e.LastPlayedAtUtc).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.LastNativeSoundEvent)) ?? ownerEntries.FirstOrDefault();
            states[ownerId ?? string.Empty] = new AudioReplacementState
            {
                OwnerId = ownerId ?? string.Empty,
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
            if (!states.TryGetValue(ownerId, out AudioReplacementState state))
            {
                UpdateOwnerState(ownerId, entries.Values.Any(e => string.Equals(e.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)) ? "Audio replacement registered." : "No audio replacement registered.");
                state = states.TryGetValue(ownerId, out AudioReplacementState? updated) ? updated : new AudioReplacementState { OwnerId = ownerId, Status = "not-configured", LastMessage = "No audio replacement registered." };
            }
            return state;
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

        private static string MakeKey(string ownerId, string replacementId)
        {
            return (ownerId ?? string.Empty).Trim() + "::" + (replacementId ?? string.Empty).Trim();
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

        private static void SetProperty(object target, string propertyName, object value)
        {
            target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.SetValue(target, value, null);
        }

        private sealed class AudioReplacementEntry
        {
            internal AudioReplacementEntry(string ownerId, AudioReplacementOptions options)
            {
                OwnerId = ownerId;
                Options = options;
                LoadStatus = options.Enabled ? "pending" : "disabled";
            }

            internal string OwnerId { get; }
            internal AudioReplacementOptions Options { get; }
            internal bool LoadStarted { get; set; }
            internal object? Request { get; set; }
            internal object? AsyncOperation { get; set; }
            internal object? AudioClip { get; set; }
            internal string LoadStatus { get; set; }
            internal string LoadFailureReason { get; set; } = string.Empty;
            internal string LastMessage { get; set; } = string.Empty;
            internal string LastNativeSoundEvent { get; set; } = string.Empty;
            internal bool LastPlayed { get; set; }
            internal bool LastSuppressed { get; set; }
            internal DateTimeOffset LastPlayedAtUtc { get; set; } = DateTimeOffset.MinValue;
            internal bool IsReady => string.Equals(LoadStatus, ReadyStatus, StringComparison.OrdinalIgnoreCase) && AudioClip != null;
        }
    }
}
