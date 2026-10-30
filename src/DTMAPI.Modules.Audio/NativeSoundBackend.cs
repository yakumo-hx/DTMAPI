using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using DTMAPI.AudioContent;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Audio
{
    internal sealed class NativeSoundBackend : IOwnedSoundBackend, IFileSoundBackend
    {
        private readonly Action<string> trace;
        private readonly Dictionary<Action<SoundEndReceipt>, EndTicket> callbacks = new Dictionary<Action<SoundEndReceipt>, EndTicket>();
        internal Action<AkCallbackType, AkCallbackInfo>? AdmittedCallback;
        private sealed class EndTicket
        {
            internal Action<SoundEndReceipt> End = null!;
            internal Action<AkCallbackType, AkCallbackInfo>? Native;
            internal uint Playing;
            internal int Delivered;
            internal void Receive(AkCallbackType type, AkCallbackInfo info)
            {
                if (type != AkCallbackType.AK_EndOfEvent || !(info is AkEventCallbackInfo receipt) || receipt.playingID == 0 ||
                    Playing != 0 && receipt.playingID != Playing || System.Threading.Interlocked.Exchange(ref Delivered, 1) != 0) return;
                End(new SoundEndReceipt(receipt.playingID, (uint)type));
                Native?.Invoke(type, info); // Native callback data never escapes this callback.
            }
        }
        private long request;
        internal long TemporaryBytes { get; private set; }
        internal NativeSoundBackend(Action<string> trace) { this.trace = trace; }
        private string? resolverRoot;
        public void AddMediaPath(string root)
        {
            root = Path.GetFullPath(root);
            if (resolverRoot != null)
            { if (resolverRoot != root) throw new InvalidOperationException("AudioResolverRootChanged"); return; }
            Check(AkSoundEngine.AddBasePath(root + Path.DirectorySeparatorChar), "Add owned streaming media root");
            resolverRoot = root; Trace("StreamResolver root=" + root + "; lifetime=engine");
        }
        private static readonly AkCallbackManager.BankCallback BankCallback = (bank, pointer, result, cookie) =>
            ((Action<SoundBankReceipt>)cookie)(new SoundBankReceipt(bank, result == AKRESULT.AK_Success, result.ToString()));
        public SoundBankReceipt LoadFile(string path, Action<SoundBankReceipt> completed)
        {
            if (!Available) return new SoundBankReceipt(0, false, UnavailableReason);
            var result = AkSoundEngine.LoadBank(path, BankCallback, completed, out uint bank);
            Trace("FileLoadSubmitted bank=" + bank + "; result=" + result + "; file=" + path);
            return new SoundBankReceipt(bank, result == AKRESULT.AK_Success, result.ToString());
        }
        private void Trace(string message) { try { trace(message); } catch { } }
        // Native compatibility belongs to this module's supported engine and
        // authored bank format. Normal playback does not scan game bundles.
        internal static string UnavailableReason { get; private set; } = "The current Wwise engine is not ready.";
        internal static bool EngineSupported
        {
            get
            {
                if (Application.platform != RuntimePlatform.WindowsPlayer || !AkSoundEngine.IsInitialized()) return false;
                if (AkSoundEngine.GetMajorMinorVersion() != ((2023u << 16) | 1u) || AkSoundEngine.GetSubminorBuildVersion() != 8367u)
                { UnavailableReason = "The short PCM format has not been verified for this Wwise engine."; return false; }
                return true;
            }
        }
        internal static bool Available => EngineSupported;
        internal static HashSet<uint> ReservedIds()
        {
            var ids = new HashSet<uint>();
            using (var stream = typeof(NativeSoundBackend).Assembly.GetManifestResourceStream("DTMAPI.NativeAudioIds.txt") ?? throw new InvalidOperationException("Native sound identity catalog is missing."))
            using (var reader = new StreamReader(stream))
                while (!reader.EndOfStream) ids.Add(uint.Parse(reader.ReadLine()!, CultureInfo.InvariantCulture));
            return ids;
        }
        private static void Check(AKRESULT result, string operation)
        { if (result != AKRESULT.AK_Success) throw new InvalidOperationException(operation + " failed (" + result + ")."); }
        public uint Load(byte[] bytes)
        {
            if (!Available) throw new InvalidOperationException(UnavailableReason);
            IntPtr allocation = Marshal.AllocHGlobal(bytes.Length + 15);
            TemporaryBytes = bytes.Length + 15;
            try
            {
                var aligned = new IntPtr((allocation.ToInt64() + 15) & ~15L);
                Marshal.Copy(bytes, 0, aligned, bytes.Length);
                Check(AkSoundEngine.LoadBankMemoryCopy(aligned, (uint)bytes.Length, out uint bank), "Load owned PCM bank");
                Trace("CopyLoaded bank=" + bank + "; bankBytes=" + bytes.Length + "; temporaryNativeBytes=" + TemporaryBytes);
                return bank;
            }
            finally { Marshal.FreeHGlobal(allocation); TemporaryBytes = 0; Trace("CopyTemporaryReleased; temporaryNativeBytes=0"); }
        }
        public void Unload(uint bankId)
        {
            if (!AkSoundEngine.IsInitialized()) throw new InvalidOperationException("AudioEngineEndedBeforeOwnedBankRelease");
            Check(AkSoundEngine.UnloadBank(bankId, IntPtr.Zero), "Unload owned PCM bank"); Trace("BankUnloaded bank=" + bankId);
        }
        public object CreateEmitter()
        { var emitter = new GameObject("DTMAPI.AudioVoice"); Object.DontDestroyOnLoad(emitter); return emitter; }
        public void Register(object emitter) => Check(AkSoundEngine.RegisterGameObj((GameObject)emitter), "Register owned emitter");
        public void Position(object emitter, AudioPosition position)
        {
            var gameObject = (GameObject)emitter;
            gameObject.transform.position = new Vector3(position.X, position.Y, position.Z);
            Check(AkSoundEngine.SetObjectPosition(gameObject, gameObject.transform), "Position owned emitter");
        }
        public void Gain(object emitter, float gain) => Check(AkSoundEngine.SetGameObjectOutputBusVolume(AkSoundEngine.GetAkGameObjectID((GameObject)emitter), ulong.MaxValue, gain), "Set owned emitter gain");
        private static readonly AkCallbackManager.EventCallback EndCallback = (cookie, type, info) =>
            ((EndTicket)cookie).Receive(type, info);
        public uint Play(uint eventId, object emitter, Action<SoundEndReceipt> ended)
        {
            long identity = ++request; uint playing = 0;
            var callback = new EndTicket { Native = AdmittedCallback, End = receipt => { ended(receipt); Trace("EndAcknowledged request=" + identity + "; playing=" + receipt.PlayingId + "; type=" + receipt.Type); } };
            callbacks.Add(ended, callback);
            playing = AkSoundEngine.PostEvent(eventId, (GameObject)emitter,
                (uint)(AkCallbackType.AK_EndOfEvent | AkCallbackType.AK_EnableGetSourcePlayPosition), EndCallback, callback);
            callback.Playing = playing;
            Trace("PostReturned request=" + identity + "; event=" + eventId + "; playing=" + playing + "; audible=unmeasured");
            return playing;
        }
        public void Pause(uint playingId, bool paused) => AkSoundEngine.ExecuteActionOnPlayingID(paused ? AkActionOnEventType.AkActionOnEventType_Pause : AkActionOnEventType.AkActionOnEventType_Resume, playingId);
        public void Stop(uint playingId, int transitionMs, int curve) { if (AkSoundEngine.IsInitialized()) AkSoundEngine.StopPlayingID(playingId, transitionMs, (AkCurveInterpolation)curve); Trace("StopRequested playing=" + playingId + "; transitionMs=" + transitionMs + "; curve=" + curve); }
        public void CancelCallback(Action<SoundEndReceipt> ended)
        {
            if (!callbacks.TryGetValue(ended, out var callback)) return;
            AkSoundEngine.CancelEventCallbackCookie(callback); callbacks.Remove(ended);
        }
        public void Unregister(object emitter)
        { if (AkSoundEngine.IsInitialized()) Check(AkSoundEngine.UnregisterGameObj((GameObject)emitter), "Unregister owned emitter"); }
        public void Destroy(object emitter) => Object.Destroy((GameObject)emitter);
    }
}
