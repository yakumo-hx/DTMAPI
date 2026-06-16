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
        private static readonly HashSet<string> ReviewedNativeSoundEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PLAY_RESOURCE_PAPER_BOX"
        };

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

            if (!ReviewedNativeSoundEvents.Contains(normalized.NativeSoundEvent))
            {
                string message = "Native sound event is not reviewed for audio replacement: " + normalized.NativeSoundEvent + ".";
                states[owner.UniqueID] = new AudioReplacementState
                {
                    OwnerId = owner.UniqueID,
                    HookInstalled = hookInstalled,
                    LastMessage = message,
                    Status = "unsupported-event"
                };
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
                AudioReplacementEntry? conflict = entries.Values.FirstOrDefault(e =>
                    e.Options.Enabled &&
                    e.Options.SuppressNativeWhenReady &&
                    string.Equals(e.Options.NativeSoundEvent, normalized.NativeSoundEvent, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(MakeKey(e.OwnerId, e.Options.ReplacementId), key, StringComparison.OrdinalIgnoreCase));
                if (conflict != null)
                {
                    string message = "Suppressing replacement already registered for " + normalized.NativeSoundEvent + " by " + conflict.OwnerId + "/" + conflict.Options.ReplacementId + ".";
                    states[owner.UniqueID] = new AudioReplacementState
                    {
                        OwnerId = owner.UniqueID,
                        HookInstalled = hookInstalled,
                        LastMessage = message,
                        Status = "conflict"
                    };
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

            var entry = new AudioReplacementEntry(owner.UniqueID, normalized);
            entries[key] = entry;

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
            {
                if (entry.Options.Enabled &&
                    !entry.IsReady &&
                    (string.Equals(entry.LoadStatus, "pending", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.LoadStatus, "retry", StringComparison.OrdinalIgnoreCase)) &&
                    DateTimeOffset.UtcNow - entry.LastLoadAttemptAtUtc >= TimeSpan.FromSeconds(3))
                {
                    EnsureLoadStarted(entry);
                }

                PollLoad(entry);
            }
        }

        internal bool HandleNativeSoundEvent(string? eventName, object? emitter, object? eventCallback, bool waitEndOfFrame, ref bool nativeResult)
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

            if (emitter != null || eventCallback != null)
            {
                foreach (AudioReplacementEntry entry in candidates)
                    RecordEvent(entry, normalizedEvent, played: false, suppressed: false, "native Wwise emitter/callback semantics are not supported by this reviewed 2D replacement path; native sound allowed. emitter=" + (emitter != null) + " callback=" + (eventCallback != null) + " waitEndOfFrame=" + waitEndOfFrame.ToString(CultureInfo.InvariantCulture));
                return true;
            }

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
                entry.LastLoadAttemptAtUtc = DateTimeOffset.UtcNow;
                entry.LoadStarted = true;
                entry.LoadStatus = "loading";
                if (!TryCreatePcmWavClip(entry.Options.AudioPath, entry.Options.ReplacementId, out object? clip, out object? callbackOwner, out string message))
                {
                    if (IsTransientUnityClipLoadFailure(message))
                    {
                        MarkLoadRetry(entry, message);
                        return;
                    }

                    entry.LoadStatus = "failed";
                    entry.LoadFailureReason = message;
                    UpdateOwnerState(entry.OwnerId, message);
                    return;
                }

                entry.AudioClip = clip;
                entry.AudioCallbackOwner = callbackOwner;
                entry.LoadStatus = ReadyStatus;
                entry.LoadFailureReason = string.Empty;
                entry.LastMessage = "Local PCM WAV ready for " + entry.Options.NativeSoundEvent + ": " + entry.Options.AudioPath;
                UpdateOwnerState(entry.OwnerId, entry.LastMessage);
                runtime.RuntimeMonitor.Log("AudioReplacement local PCM WAV ready owner=" + entry.OwnerId +
                    " replacement=" + entry.Options.ReplacementId +
                    " event=" + entry.Options.NativeSoundEvent +
                    " path=" + entry.Options.AudioPath);
            }
            catch (Exception ex)
            {
                entry.LoadStatus = "failed";
                entry.LoadFailureReason = ex.GetType().Name + ": " + ex.Message;
                UpdateOwnerState(entry.OwnerId, "AudioReplacement load start failed: " + entry.LoadFailureReason);
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.AudioReplacement", "Failed to start local WAV load.", ex.ToString());
            }
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

            object? created = create.Invoke(null, createArguments);
            if (created == null)
            {
                message = "Unity AudioClip.Create returned null. overload=" + DescribeMethod(create);
                return false;
            }

            object? setResult = setData.Invoke(created, new object[] { samples, 0 });
            if (setResult is bool ok && !ok)
            {
                DestroyUnityObject(created, 0f);
                message = "Unity AudioClip.SetData returned false. overload=" + DescribeMethod(create) +
                    " frames=" + frameCount.ToString(CultureInfo.InvariantCulture) +
                    " channels=" + channels.ToString(CultureInfo.InvariantCulture) +
                    " samples=" + samples.Length.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            if (!HasAudioClipMetadata(created, out string createdMetadata))
            {
                DestroyUnityObject(created, 0f);
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
                entry.AsyncOperation = null;
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
            object? go = null;
            try
            {
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

        private static void CleanupEntry(AudioReplacementEntry entry)
        {
            DisposeRequest(entry);
            DestroyUnityObject(entry.AudioClip, 0f);
            entry.AudioClip = null;
            entry.AudioCallbackOwner = null;
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
            internal object? AudioCallbackOwner { get; set; }
            internal string LoadStatus { get; set; }
            internal string LoadFailureReason { get; set; } = string.Empty;
            internal string LastMessage { get; set; } = string.Empty;
            internal string LastNativeSoundEvent { get; set; } = string.Empty;
            internal bool LastPlayed { get; set; }
            internal bool LastSuppressed { get; set; }
            internal DateTimeOffset LastLoadAttemptAtUtc { get; set; } = DateTimeOffset.MinValue;
            internal DateTimeOffset LastPlayedAtUtc { get; set; } = DateTimeOffset.MinValue;
            internal bool IsReady => string.Equals(LoadStatus, ReadyStatus, StringComparison.OrdinalIgnoreCase) && AudioClip != null;
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
