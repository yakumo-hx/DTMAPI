using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.Modules.Audio;

internal static class ModuleChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        string root = Path.Combine(Path.GetTempPath(), "dtmapi-audio-module-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            int constructed = 0; bool alive = true;
            var driver = new Fake(); var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
            using var host = new AudioModuleFixture(() => { constructed++; return engine; }, _ => root);
            var api = host.Api(new Manifest("Owner"), () => { if (!alive) throw new ObjectDisposedException("Owner"); });
            check(constructed == 0 && driver.Empty, "Querying the typed API creates no backend or media work.");
            AudioRegistration Input() => new() { Resources = new[] { new AudioResource { Key = "r", Path = "sound.wav" } }, Events = new[] { new AudioEvent { Key = "e", Resource = "r" } } };
            throws(() => api.Register(Input()), "Missing package-root resource is rejected before backend creation.");
            check(constructed == 0, "Rejected input does not create demand.");
            File.WriteAllBytes(Path.Combine(root, "sound.wav"), wav());
            var first = api.Register(Input()); var sound = first.GetSound("e");
            var second = api.Register(Input());
            check(constructed == 1 && engine.Snapshot("Owner").RegistrationCount == 2, "Same Owner and local keys form independent registrations on one backend.");
            var consumer = new PendingConsumer();
            using var binding = ((IAudioSoundBinding)sound).Bind(consumer);
            throws(first.Dispose, "Audio withdrawal waits for an exact bound domain consumer.");
            check(consumer.Attempts == 1 && engine.Snapshot("Owner").RegistrationCount == 2, "Pending domain cleanup retains the media registration and its sibling.");
            check(first.GetSnapshot().State == AudioPreparationState.CleanupPending && second.GetSnapshot().State == AudioPreparationState.Registered,
                "A closing domain consumer projects CleanupPending without changing the sibling or native media state.");
            consumer.Ready = true;
            host.RetryPending();
            check(engine.Snapshot("Owner").RegistrationCount == 1, "The common cleanup driver releases completed consumers without a new registration or Owner exit.");
            first.Dispose();
            check(first.GetSnapshot().State == AudioPreparationState.Released, "The closing registration projects Released only after consumer and media cleanup complete.");
            throws(() => sound.Play(), "A released registration invalidates its retained sound handle.");
            check(second.GetSnapshot().State == AudioPreparationState.Registered, "Sibling registration survives release.");
            second.Dispose();
            var third = api.Register(Input());
            check(constructed == 1, "Successful zero-demand cleanup allows registration again on the same service.");
            alive = false;
            throws(() => third.GetSound("e").Play(), "Closed Owner guards retained API and sound handles.");
            check(host.ReleaseOwner("Owner") && engine.CountOwnerResources("Owner") == 0, "Owner exit drains every child contribution without reopening its guard.");
            third.Dispose();
            host.CloseAdmission();
            throws(() => api.Register(Input()), "Provider exit closes cached facades.");
        }
        finally { Directory.Delete(root, true); }
    }
    private sealed class PendingConsumer : IDisposable
    {
        internal bool Ready;
        internal int Attempts;
        public void Dispose() { Attempts++; if (!Ready) throw new InvalidOperationException("Native voice not ended."); }
    }
}
