using System;
using System.Collections.Generic;
using System.Diagnostics;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Audio
{
    internal sealed partial class NativeModule : IAudioBackend, IAudioDemandBackend, IAudioStreamingBackend, IAudioPreparationBackend
    {
        private static NativeModule? current;
        private readonly IRuntimeModuleContext host;
        private readonly IDtmRuntimeContext context;
        private readonly IDtmScheduler scheduler;
        private readonly Harmony harmony = new Harmony("DTMAPI.Audio.Native");
        private readonly AudioEngine engine;
        private readonly NativeSoundBackend driver;
        private readonly MusicRequests music;
        private readonly WaitingMusic waitingMusic;
        private NativeMusicCancellation? musicCancellation;
        private bool musicHooks, idleRequested;
        private readonly Action<Action> authorizePatches;
        private IDisposable? contextSubscription;
        private IDtmScheduledWork? update;
        private bool disposed;
        private bool demanded;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private long nextTick;
        public NativeModule(IRuntimeModuleContext host)
        {
            if (current != null) throw new InvalidOperationException("Audio module already active.");
            this.host = host;
            this.authorizePatches = host.AuthorizePatches;
            context = host.Runtime;
            scheduler = host.Scheduler;
            driver = new NativeSoundBackend(Log);
            engine = new AudioEngine(driver, NativeSoundBackend.ReservedIds(), Log);
            music = new MusicRequests(Log);
            waitingMusic = new WaitingMusic(music, Log);
            current = this;
            try
            {
                SetDemand(true);
            }
            catch
            {
                update?.TryCancel();
                contextSubscription?.Dispose(); UnpatchPlayback(); current = null; throw;
            }
        }
        public void SetDemand(bool active)
        {
            RequireThread();
            idleRequested = !active;
            // A borrowed native voice still has our completion guard. Keep its
            // pump until End; withdrawing an Audio consumer cannot stall BgmUpdate.
            if (!active && (music.HasNativeCurrent || music.HasPendingCancellation || waitingMusic.HasWaiting)) return;
            if (active == demanded) return;
            if (!active)
            {
                update?.TryCancel(); update = null;
                contextSubscription?.Dispose(); contextSubscription = null;
                UnpatchPlayback(); demanded = false; musicHooks = false;
                Log("Idle; hooks=0; tick=0; media=0; voices=0"); return;
            }
            demanded = true;
            try
            {
                var target = AccessTools.Method(typeof(WwiseSoundManager), "InternalPostSoundEvent", new[] { typeof(string), typeof(GameObject), typeof(AkCallbackManager.EventCallback), typeof(bool) });
                if (target == null || target.ReturnType != typeof(bool)) throw new MissingMethodException("AudioNativeSignatureUnsupported");
                authorizePatches(() => {
                    harmony.Patch(target, transpiler: new HarmonyMethod(typeof(NativeModule), nameof(AfterAdmission)));
                    harmony.Patch(AccessTools.Method(typeof(WwiseSoundManager), "StopAll", Type.EmptyTypes),
                        prefix: new HarmonyMethod(typeof(NativeModule), nameof(StopOwnedSfx)));
                });
                contextSubscription = context.Subscribe(OnContext);
                QueueTick();
                Log("Activated; hook=InternalPostSoundEvent; input=PCM16");
            }
            catch { SetDemand(false); throw; }
        }

        private void UnpatchPlayback()
        {
            foreach (string name in new[] { "InternalPostSoundEvent", "PlayBGMInternal", "PauseBgm", "StopBgm", "BgmUpdate", "StopAll" })
            {
                var method = AccessTools.Method(typeof(WwiseSoundManager), name);
                if (method != null) harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
            }

        }
        public void ConfigureStreaming(string cacheDirectory) { RequireThread(); engine.ConfigureStreaming(cacheDirectory); }
        public void ConfigureDecoder(string executable) { RequireThread(); engine.ConfigureDecoder(executable); }
        private void Log(string message) => host.Monitor.Log("Audio " + message);
        private void RequireThread()
        {
            if (!context.IsMainThread) throw new InvalidOperationException("AudioWrongThread");
            if (disposed) throw new ObjectDisposedException(nameof(NativeModule));
        }
        private static readonly System.Reflection.MethodInfo NativePost = AccessTools.Method(typeof(AkSoundEngine), "PostEvent",
            new[] { typeof(string), typeof(GameObject), typeof(uint), typeof(AkCallbackManager.EventCallback), typeof(object) });
        private static readonly System.Reflection.PropertyInfo NativeAgent = AccessTools.Property(typeof(WwiseSoundManager), "agentObj");
        private static IEnumerable<CodeInstruction> AfterAdmission(IEnumerable<CodeInstruction> instructions) =>
            NativeAdmissionPatch.Redirect(instructions,
                AccessTools.Method(typeof(WwiseSoundManager), "TryOccupySoundGroup", new[] { typeof(string) }),
                NativePost, AccessTools.Method(typeof(NativeModule), nameof(PostAdmitted)));
        private static void PostAdmitted(string eventName, GameObject emitter, uint flags,
            AkCallbackManager.EventCallback callback, object cookie, AkCallbackManager.EventCallback requestedCallback)
        {
            var module = current;
            if (module != null && module.context.IsMainThread && module.musicHooks)
            {
                if (requestedCallback?.Target is NativeMusicCallback guarded)
                { module.PostMusicAdmitted(eventName, emitter, flags, callback, cookie, guarded); return; }
                string stop = (eventName ?? "").ToUpperInvariant();
                if (stop == "STOP_BGM_FADEOUT" || stop == "STOP_BGM_IMMEDIATE")
                    try { module.music.Stop(DolocAPI.Sound, stop == "STOP_BGM_FADEOUT"); }
                    catch (Exception ex) { module.Log("MusicStopPending reason=" + ex.GetBaseException().Message); }
            }
            if (!TryReplaceAdmitted(eventName ?? "", requestedCallback == null ? null : callback, cookie))
                AkSoundEngine.PostEvent(eventName, emitter, flags, callback, cookie);
        }
        private static void StopOwnedSfx() => current?.engine.StopSfx();
        private sealed class NativeMusicCallback
        {
            internal readonly NativeModule Module;
            internal readonly MusicRequests.Request Request;
            internal uint PlayingId;
            internal bool Attempted;
            internal NativeMusicCallback(NativeModule module, MusicRequests.Request request) { Module = module; Request = request; }
            internal void End(object cookie, AkCallbackType type, AkCallbackInfo info)
            {
                if (type == AkCallbackType.AK_EndOfEvent && info is AkEventCallbackInfo receipt)
                    Module.music.Observe(Request, new SoundEndReceipt(receipt.playingID, (uint)type));
            }
        }
        private static readonly System.Reflection.MethodInfo MusicTarget = AccessTools.Method(typeof(WwiseSoundManager), "PlayBGMInternal", new[] { typeof(string) });
        private static readonly System.Reflection.MethodInfo MusicNativePost = AccessTools.Method(typeof(WwiseSoundManager), "PostSoundEvent",
            new[] { typeof(string), typeof(AkCallbackManager.EventCallback), typeof(bool) });
        private static IEnumerable<CodeInstruction> MusicRequestPatch(IEnumerable<CodeInstruction> instructions) =>
            NativeMusicPatch.Redirect(instructions, MusicNativePost, AccessTools.Method(typeof(NativeModule), nameof(PostMusicRequest)));
        private void UpdateMusicHooks()
        {
            if (musicHooks || !engine.HasMusic) return;
            if (MusicTarget == null || MusicTarget.ReturnType != typeof(void) || MusicNativePost == null)
                throw new MissingMethodException("AudioMusicNativeSignatureUnsupported");
            var pause = AccessTools.Method(typeof(WwiseSoundManager), "PauseBgm", new[] { typeof(bool) });
            var stop = AccessTools.Method(typeof(WwiseSoundManager), "StopBgm", new[] { typeof(bool) });
            if (pause == null || stop == null) throw new MissingMethodException("AudioMusicStopSignatureUnsupported");
            musicCancellation = new NativeMusicCancellation(typeof(WwiseSoundManager));
            authorizePatches(() => {
                harmony.Patch(MusicTarget, transpiler: new HarmonyMethod(typeof(NativeModule), nameof(MusicRequestPatch)));
                harmony.Patch(pause, prefix: new HarmonyMethod(typeof(NativeModule), nameof(CancelMusicRequest)));
                harmony.Patch(stop, prefix: new HarmonyMethod(typeof(NativeModule), nameof(CancelMusicRequest)));
                harmony.Patch(AccessTools.Method(typeof(WwiseSoundManager), "BgmUpdate", new[] { typeof(float) }), prefix: new HarmonyMethod(typeof(NativeModule), nameof(AllowMusicUpdate)));
            });
            musicHooks = true;
        }
        private static void CancelMusicRequest(WwiseSoundManager __instance) => current?.music.Cancel(__instance);
        private static bool AllowMusicUpdate(WwiseSoundManager __instance) => current?.waitingMusic.IsWaiting(__instance) != true;
        private static bool PostMusicRequest(WwiseSoundManager manager, string logical, AkCallbackManager.EventCallback complete, bool ignoreOccupy)
        {
            var module = current;
            if (module == null || !module.context.IsMainThread) return manager.PostSoundEvent(logical, complete, ignoreOccupy);
            var mapped = module.engine.CaptureMusicEvent(logical, out string owner);
            if (mapped != null)
                return module.waitingMusic.Post(manager, owner, logical, receipt => complete(logical, AkCallbackType.AK_EndOfEvent, null!), mapped,
                    () => module.musicCancellation!.Resume(manager, logical), () => module.musicCancellation!.Release(manager, logical));
            return module.music.Post(manager, logical, receipt => complete(logical, AkCallbackType.AK_EndOfEvent, null!), request => {
                var guarded = new NativeMusicCallback(module, request);
                bool result = manager.PostSoundEvent(logical, guarded.End, ignoreOccupy);
                return result ? guarded.PlayingId : 0;
            }, () => module.musicCancellation!.Release(manager, logical));
        }
        private void PostMusicAdmitted(string eventName, GameObject emitter, uint flags, AkCallbackManager.EventCallback nativeCallback,
            object cookie, NativeMusicCallback guarded)
        {
            var request = guarded.Request;
            // Also rejects a delayed native bank-load continuation belonging to
            // a cancelled request; readiness never grants a fresh request ID.
            if (guarded.Attempted || !music.Eligible(request)) return;
            guarded.Attempted = true;
            var voice = engine.TryPlayMusic(eventName.ToUpperInvariant(), receipt => music.Observe(request, receipt),
                () => music.Withdraw(request), out string detail);
            if (voice != null) { request.Voice = voice; guarded.PlayingId = voice.PlayingId; }
            else guarded.PlayingId = AkSoundEngine.PostEvent(eventName, emitter, flags, nativeCallback, cookie);
            Log("MusicPost request=" + request.Id + "; logical=" + request.Logical + "; playing=" + guarded.PlayingId +
                "; route=" + (voice == null ? "Native" : "P") + "; " + detail);
        }
        private static bool TryReplaceAdmitted(string eventName, AkCallbackManager.EventCallback? callback, object cookie)
        {
            var module = current;
            if (module == null || module.disposed || !module.demanded) return false;
            string nativeEvent = (eventName ?? "").ToUpperInvariant();
            if (!module.context.IsMainThread) return false;
            try
            {
                // A two-dimensional replacement owns its emitter. Forward the
                // original native wrapper so its logical cookie is preserved.
                var previous = module.driver.AdmittedCallback;
                bool started; string detail;
                try
                {
                    module.driver.AdmittedCallback = callback == null ? null : (Action<AkCallbackType, AkCallbackInfo>)((type, info) => callback(cookie, type, info));
                    started = module.engine.TryReplace(nativeEvent, module.clock.ElapsedMilliseconds, out detail);
                }
                finally { module.driver.AdmittedCallback = previous; }
                module.Log((started ? "PostStarted" : "Fallback") + " nativeEvent=" + eventName + "; " + detail + "; audible=unmeasured");
                return started;
            }
            catch (Exception ex) { module.Log("Fallback event=" + eventName + "; reason=" + ex.GetBaseException().Message); }
            return false;
        }
        private static bool NativeParentReady()
        {
            if (DolocAPI.Sound == null) return false;
            return AccessTools.Field(typeof(WwiseSoundManager), "loadedSoundBanks").GetValue(DolocAPI.Sound) is HashSet<string> banks && banks.Contains("SFX");
        }
        private void QueueTick() => update = scheduler.NextTick(Drive);
        private void Drive()
        {
            if (disposed) return;
            Tick();
            if (!disposed && demanded) QueueTick();
        }
        private void Tick()
        {
            if (disposed || clock.ElapsedMilliseconds < nextTick) return;
            nextTick = clock.ElapsedMilliseconds + 25;
            try
            {
                bool ready = context.Snapshot.IsWorldReady && NativeSoundBackend.Available && NativeParentReady();
                try { engine.Tick(ready, NativeSoundBackend.UnavailableReason); }
                finally { waitingMusic.Pump(); music.Pump(); }

                if (idleRequested && !music.HasNativeCurrent && !music.HasPendingCancellation) SetDemand(false);
            }
            catch (Exception ex) { nextTick = clock.ElapsedMilliseconds + 1000; Log("TickCleanupPending reason=" + ex.GetBaseException().Message); }
        }
        private void OnContext(DtmRuntimeContextSnapshot snapshot)
        {
            if (snapshot.Phase == DtmRuntimePhase.ReturningToTitle || snapshot.Phase == DtmRuntimePhase.ShuttingDown)
            { music.RetireSession(); waitingMusic.Clear(); engine.RetireWorld(); }
        }
        public IAudioRegistration Register(string owner, int loadIndex, AudioRegistration input, Func<string, IAudioSourceReference> source, Action guard)
        {
            RequireThread(); var registration = engine.Register(owner, loadIndex, input, source, guard);
            try { UpdateMusicHooks(); }
            catch { registration.Dispose(); throw; }
            return new NativeRegistration(this, registration, owner);
        }
        private sealed class NativeRegistration : IAudioRegistration
        {
            private readonly NativeModule module;
            private readonly IAudioRegistration value;
            private readonly string owner;
            internal NativeRegistration(NativeModule module, IAudioRegistration value, string owner) { this.module = module; this.value = value; this.owner = owner; }
            public AudioRegistrationSnapshot GetSnapshot() => value.GetSnapshot();
            public IAudioSound GetSound(string key) { module.RequireThread(); return value.GetSound(key); }
            public void RetryPreparation() { module.RequireThread(); value.RetryPreparation(); module.waitingMusic.Retry(owner); }
            public void Dispose()
            {
                if (module.disposed) return;
                module.RequireThread();
                try { value.Dispose(); } finally { module.waitingMusic.Pump(); module.music.Pump(); }
            }
        }
        public AudioSnapshot Snapshot(string owner) { RequireThread(); var snapshot = engine.Snapshot(owner); snapshot.TemporaryNativeBytes = driver.TemporaryBytes; return snapshot; }
        public void RetryPreparation(string owner) { RequireThread(); engine.RetryPreparation(owner); waitingMusic.Retry(owner); }
        public int CountOwnerResources(string owner) => engine.CountOwnerResources(owner);
        public int RemoveOwner(string owner)
        {
            RequireThread(); int count = CountOwnerResources(owner); var errors = new List<Exception>();
            try { engine.RemoveOwner(owner); } catch (Exception ex) { errors.Add(ex); }
            finally { waitingMusic.Pump(); music.Pump(); }
            if (errors.Count != 0) throw new AggregateException("AudioOwnerCleanupPending", errors);
            return count;
        }
        public void Dispose()
        {
            if (disposed) return;
            RequireThread();
            if ((music.HasNativeCurrent || music.HasPendingCancellation || waitingMusic.HasWaiting) && context.Snapshot.IsWorldReady) throw new InvalidOperationException("AudioNativeMusicCompletionPending");
            music.RetireSession(); waitingMusic.Clear();
            // Shutdown may have no next Unity tick. Drain our own late file
            // tickets while the engine still exists; never use engine death as
            // a substitute for a matching callback and successful unload.
            Exception? pending = null;
            long drainUntil = clock.ElapsedMilliseconds + 1000;
            do
            {
                try { engine.Dispose(); pending = null; break; }
                catch (Exception ex) { pending = ex; }
                if (!AkSoundEngine.IsInitialized()) break;
                AkSoundEngine.RenderAudio(); AkCallbackManager.PostCallbacks();
                System.Threading.Thread.Sleep(2);
            } while (context.Snapshot.Phase == DtmRuntimePhase.ShuttingDown && clock.ElapsedMilliseconds < drainUntil);
            if (pending != null) throw pending; // Core retains this participant for retry.
            contextSubscription?.Dispose(); contextSubscription = null;
            update?.TryCancel(); update = null; UnpatchPlayback();
            current = null; disposed = true; Log("Drained; hooks=0; tick=0; media=0; voices=0");
        }
    }
}
