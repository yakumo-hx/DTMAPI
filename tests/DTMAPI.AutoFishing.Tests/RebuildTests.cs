using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Yuuka.DTMAPI.AutoFishing;

namespace DTMAPI.UnitTests
{
    internal static class RebuildTests
    {
        public static void RunAll()
        {
            // Inspect the compiled product inputs, not comments or unrelated repository files.
            using (var stream = File.OpenRead(typeof(AutoFishingConfig).Assembly.Location))
            using (var pe = new PEReader(stream))
            {
                var metadata = pe.GetMetadataReader();
                Check(!metadata.TypeReferences.Select(metadata.GetTypeReference).Any(type =>
                    metadata.GetString(type.Namespace) == "DTMAPI.Abstractions" && metadata.GetString(type.Name) == "IFishingAutomationApi"),
                    "current AutoFishing product does not consume frozen fishing automation");
            }
            LifecycleTests.RunAll();
            MotionRecoveryTests.RunAll();
            var config = new AutoFishingConfig();
            Check(!config.InstantBite && !config.SkipMiniGame && !config.FastAnimations && config.CastChargeRatio == 0, "ordinary native defaults");
            config.CastChargeRatio = .65f; config.AnimationMultiplier = 5;
            config.InstantBite = true; config.Normalize();
            Check(!config.FastAnimations && !config.SkipMiniGame && config.CastChargeRatio == .65f && config.AnimationMultiplier == 5, "independent settings");
            config.CastChargeRatio = float.NaN; config.AnimationMultiplier = float.PositiveInfinity; config.Normalize();
            Check(config.CastChargeRatio == 0 && config.AnimationMultiplier == 3, "non-finite config recovery");
            config.CastChargeRatio = 2; config.AnimationMultiplier = -2; config.Normalize();
            Check(config.CastChargeRatio == 1 && config.AnimationMultiplier == 1, "range validation");
            config.AnimationMultiplier = 10; config.Normalize();
            Check(config.AnimationMultiplier == 5, "candidate promise is limited to the accepted maximum");
            var notes = new NoteInputPolicy();
            Check(!notes.Read(NoteKind.Bonus, 1, .9f, 1, 2).Press, "red gap before bonus");
            Check(notes.Read(NoteKind.Bonus, 1, 1, 1, 2).Press, "bonus starts inclusively");
            Check(!notes.Read(NoteKind.Bonus, 1, 1.1f, 1, 2).Press, "one press per yellow note");
            Check(notes.Read(NoteKind.Bonus, 2, 3, 3, 4).Press, "adjacent bonus needs its own edge");
            Check(!notes.Read(NoteKind.Stable, 3, 4.9f, 5, 6).Hold, "red gap releases stable input");
            Check(notes.Read(NoteKind.Stable, 3, 5, 5, 6).Hold && notes.Read(NoteKind.Stable, 3, 5.1f, 5, 6).Hold, "green continuously held");
            Check(!notes.Read(NoteKind.Stable, 3, 6.001f, 5, 6).Hold, "green ends without leaked hold");
            Check(!notes.Read(NoteKind.Delay, 4, 7, 6, 8).Hold, "delay is neutral");
            notes.Reset();
            Check(notes.Read(NoteKind.Bonus, 2, 3, 3, 4).Press, "same controller reused by a new minigame");
            var loop = new FishingLoop();
            loop.Enable(FishingPhase.Idle, 10);
            Check(loop.Observe(FishingPhase.Idle, 10), "hotkey starts first cast");
            loop.Started();
            Check(!loop.Observe(FishingPhase.Ready, 10) && loop.Enabled, "one outstanding cast");
            Check(!loop.Observe(FishingPhase.Wait, 20) && loop.Enabled, "ordinary wait remains enabled");
            Check(!loop.Observe(FishingPhase.Pull, 30) && loop.Enabled, "wait for native settlement");
            loop.Completed(30);
            Check(!loop.Observe(FishingPhase.Idle, 30.1f) && loop.Observe(FishingPhase.Idle, 30.2f), "bounded recast interval after success or ordinary failure");
            loop.Started();
            Check(!loop.Observe(FishingPhase.Idle, 31) && !loop.Enabled, "unreceipted native idle is interruption");
            loop.Enable(FishingPhase.Wait, 32); loop.Stop();
            Check(!loop.Observe(FishingPhase.Pull, 33) && !loop.Observe(FishingPhase.Idle, 34), "movement-caused failed pull never rearms");
            loop.Enable(FishingPhase.Idle, 35);
            Check(!loop.Observe(FishingPhase.Other, 35.1f) && !loop.Enabled, "native movement between casts stops loop");
            loop.Enable(FishingPhase.Other, 40);
            Check(!loop.Enabled, "cannot enable during incompatible native action");
            Console.WriteLine("AutoFishing policy, inactive-owner cleanup and native motion recovery assertions passed.");
        }
        private static void Check(bool condition, string label) { if (!condition) throw new Exception(label); }
    }
}
