using System;
using System.Collections.Generic;
using System.Threading;
using DTMAPI.AudioContent;

namespace DTMAPI.Modules.Audio
{
    // Copied at the native boundary; never retains AkCallbackInfo or an emitter.
    internal readonly struct SoundEndReceipt
    {
        internal const uint EndOfEvent = 1;
        internal readonly uint PlayingId, Type;
        internal SoundEndReceipt(uint playingId, uint type) { PlayingId = playingId; Type = type; }
    }
    internal interface IOwnedSoundBackend
    {
        uint Load(byte[] bytes);
        void Unload(uint bankId);
        object CreateEmitter();
        void Register(object emitter);
        void Gain(object emitter, float gain);
        void Position(object emitter, AudioPosition position);
        uint Play(uint eventId, object emitter, Action<SoundEndReceipt> ended);
        void Pause(uint playingId, bool paused);
        void Stop(uint playingId, int transitionMs, int curve);
        void CancelCallback(Action<SoundEndReceipt> ended);
        void Unregister(object emitter);
        void Destroy(object emitter);
    }
    internal readonly struct SoundBankReceipt
    {
        internal readonly uint BankId;
        internal readonly bool Success;
        internal readonly string Result;
        internal SoundBankReceipt(uint bankId, bool success, string result) { BankId = bankId; Success = success; Result = result; }
    }
    internal interface IFileSoundBackend
    {
        void AddMediaPath(string root);
        SoundBankReceipt LoadFile(string path, Action<SoundBankReceipt> completed);
    }
    // The bank retains all voices, including partial starts and asynchronous stops.
    // No instance or definition can lose its final cleanup handle on a failure.
    internal sealed class OwnedSoundBank
    {
        internal sealed class Voice
        {
            private readonly OwnedSoundBank bank;
            internal readonly Action Callback;
            internal readonly Action<SoundEndReceipt> ReceiptCallback;
            private readonly Action<SoundEndReceipt>? observer;
            private readonly Action? invalidate;
            private readonly object receiptGate = new object();
            private SoundEndReceipt? earlyEnd;
            private bool postReturned;
            internal object? Emitter;
            internal uint PlayingId;
            internal bool Registered, Stopping, StopSent, CallbackCancelled, Paused;
            internal bool IsMusic;
            private int transitionMs, curve = 3;
            private int ended;
            internal bool Ended { get => Volatile.Read(ref ended) != 0; set => Volatile.Write(ref ended, value ? 1 : 0); }
            internal float Gain = float.NaN;
            private AudioPosition? position;
            internal Voice(OwnedSoundBank bank, Action<SoundEndReceipt>? observer, Action? invalidate)
            {
                this.bank = bank; this.observer = observer; this.invalidate = invalidate;
                ReceiptCallback = Receive; Callback = () => Receive(new SoundEndReceipt(PlayingId, SoundEndReceipt.EndOfEvent));
            }
            private void Receive(SoundEndReceipt receipt)
            {
                if (receipt.Type != SoundEndReceipt.EndOfEvent) return;
                lock (receiptGate)
                {
                    if (!postReturned) earlyEnd = receipt;
                    else if (receipt.PlayingId == PlayingId && PlayingId != 0) Ended = true;
                }
                observer?.Invoke(receipt);
            }
            internal void AcceptPost(uint playingId)
            {
                lock (receiptGate)
                {
                    PlayingId = playingId; postReturned = true;
                    if (playingId != 0 && earlyEnd?.PlayingId == playingId) Ended = true;
                    earlyEnd = null;
                }
            }
            internal void Invalidate() => invalidate?.Invoke();
            internal void SetGain(float gain)
            {
                if (float.IsNaN(gain) || float.IsInfinity(gain) || gain < 0) throw new ArgumentOutOfRangeException(nameof(gain));
                if (Ended || Stopping || Gain == gain) return;
                bank.backend.Gain(Emitter!, gain); Gain = gain;
            }
            internal void Pause(bool paused)
            {
                if (Ended || Stopping || PlayingId == 0 || Paused == paused) return;
                bank.backend.Pause(PlayingId, paused); Paused = paused;
            }
            internal void SetPosition(AudioPosition value)
            {
                if (Ended || Stopping || position.HasValue && position.Value.Equals(value)) return;
                bank.backend.Position(Emitter!, value); position = value;
            }
            internal void Stop() => Stop(0, 3);
            internal void Stop(int transitionMs, int curve)
            {
                Invalidate();
                // An immediate stop upgrades a fade. Repeated fades neither
                // restart the native envelope nor postpone its termination.
                if (!Stopping || this.transitionMs != 0 && transitionMs == 0)
                { this.transitionMs = transitionMs; this.curve = curve; StopSent = false; }
                Stopping = true;
                RetryStop();
            }
            internal void RetryStop()
            {
                if (PlayingId == 0) Ended = true;
                if (!Ended && !StopSent) { bank.backend.Stop(PlayingId, transitionMs, curve); StopSent = true; }
            }
            internal void Finish()
            {
                if (!Ended) throw new InvalidOperationException("A playing voice still owns its callback and emitter.");
                if (!CallbackCancelled) { bank.backend.CancelCallback(ReceiptCallback); CallbackCancelled = true; }
                if (Registered) { bank.backend.Unregister(Emitter!); Registered = false; }
                if (Emitter != null) { bank.backend.Destroy(Emitter); Emitter = null; }
                bank.voices.Remove(this);
            }
        }
        private readonly IOwnedSoundBackend backend;
        private readonly List<Voice> voices = new List<Voice>();
        private readonly uint bankId;
        private byte[]? bytes;
        private uint loadedId;
        private bool loaded, released;
        private readonly string? file;
        private IDisposable? filePin;
        private readonly object loadGate = new object();
        private SoundBankReceipt? loadReceipt;
        private uint submittedId;
        private bool submitted, loadPending;
        internal string LoadError { get; private set; } = "";
        internal bool Loading => loadPending;
        internal bool Released => released;
        internal int VoiceCount => voices.Count;
        internal long ManagedBytes => bytes?.LongLength ?? 0;
        internal bool Loaded => loaded && LoadError.Length == 0;
        internal IReadOnlyList<Voice> Voices => voices;
        internal OwnedSoundBank(IOwnedSoundBackend backend, byte[] bytes, uint bankId)
        { this.backend = backend; this.bytes = bytes; this.bankId = bankId; }
        internal OwnedSoundBank(IOwnedSoundBackend backend, string file, IDisposable pin)
        { this.backend = backend; this.file = file; filePin = pin; }
        private void ReceiveLoad(SoundBankReceipt receipt)
        { lock (loadGate) { if (!loadReceipt.HasValue) loadReceipt = receipt; } }
        private void AdvanceLoad()
        {
            if (!loadPending) return;
            SoundBankReceipt? receipt;
            lock (loadGate) receipt = loadReceipt;
            if (!receipt.HasValue) return;
            loadPending = false;
            if (receipt.Value.Success)
            {
                loaded = true; loadedId = receipt.Value.BankId;
                if (loadedId != submittedId) LoadError = "AudioBankCallbackIdentityMismatch";
            }
            else LoadError = "AudioBankLoadFailed: " + receipt.Value.Result;
        }
        internal void Load()
        {
            if (released) throw new ObjectDisposedException(nameof(OwnedSoundBank));
            if (loaded) return;
            if (file != null)
            {
                if (submitted) return;
                // Register this ticket and pin before submitting; even an early
                // callback only writes its primitive mailbox. Never cancel I/O
                // by removing a Bank callback cookie.
                submitted = true; loadPending = true;
                var result = ((IFileSoundBackend)backend).LoadFile(file, ReceiveLoad);
                submittedId = result.BankId;
                if (!result.Success) { loadPending = false; LoadError = "AudioBankSubmitFailed: " + result.Result; }
                return;
            }
            loadedId = backend.Load(bytes!); loaded = true; bytes = null;
            if (loadedId != bankId) throw new InvalidOperationException("The loaded bank identity differs from its owned input.");
        }
        internal Voice Play(uint eventId, float gain, bool paused, AudioPosition? position = null,
            Action<SoundEndReceipt>? ended = null, Action? invalidate = null)
        {
            if (released) throw new ObjectDisposedException(nameof(OwnedSoundBank));
            if (float.IsNaN(gain) || float.IsInfinity(gain) || gain < 0) throw new ArgumentOutOfRangeException(nameof(gain));
            Load();
            if (!Loaded) throw new InvalidOperationException(LoadError.Length == 0 ? "AudioBankLoading" : LoadError);
            var voice = new Voice(this, ended, invalidate); voices.Add(voice);
            try
            {
                voice.Emitter = backend.CreateEmitter();
                voice.Registered = true; // Include a partially failed native registration in compensation.
                backend.Register(voice.Emitter);
                if (position.HasValue) voice.SetPosition(position.Value);
                voice.SetGain(gain);
                voice.AcceptPost(backend.Play(eventId, voice.Emitter, voice.ReceiptCallback));
                if (voice.PlayingId == 0) throw new InvalidOperationException("Wwise returned an invalid playing ID.");
                voice.Pause(paused); return voice;
            }
            catch
            {
                voice.Stop();
                if (voice.Ended) voice.Finish();
                throw;
            }
        }
        internal void Tick(bool paused, bool preservePause = false)
        {
            AdvanceLoad();
            List<Exception>? errors = null;
            for (int i = voices.Count - 1; i >= 0; i--)
            {
                var voice = voices[i];
                try
                {
                    if (voice.Stopping) voice.RetryStop();
                    else if (!preservePause) voice.Pause(paused);
                    if (voice.Ended) voice.Finish();
                }
                catch (Exception error) { (errors ?? (errors = new List<Exception>())).Add(error); }
            }
            if (errors != null) throw new AggregateException("Owned sound voice cleanup requires retry.", errors);
        }
        internal void Release()
        {
            if (released) return;
            AdvanceLoad();
            if (loadPending) throw new InvalidOperationException("AudioBankLoadPending: retain ticket and file pin until the native callback.");
            foreach (var voice in voices) voice.Invalidate();
            // Synchronous unloading of our own sound structures terminates their
            // voices. It does not unload borrowed buses or any other bank.
            if (loaded) { backend.Unload(loadedId); loaded = false; }
            foreach (var voice in voices) voice.Ended = true;
            Tick(false);
            filePin?.Dispose(); filePin = null;
            bytes = null; released = true;
        }
    }
}
