using System;
using DTMAPI.Abstractions;

namespace Samples.OwnedCleanup
{
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            var demo = new CleanupDemo(helper.GetRequiredService<IDtmOwnedResources>("0.6.2", "0.6.2"));
            var commands = helper.GetRequiredService<IDtmCommands>("0.6.2", "0.6.2");
            commands.Register("close", c => c.WriteLine(demo.Close()), description: "Close both resources; one restoration fails once.");
            commands.Register("retry", c => c.WriteLine(demo.Close()), description: "Retry the failed restoration.");
            helper.Input.RegisterKeybind("close", "F9", DtmInputScope.Title);
            helper.Events.Input.KeybindPressed += (_, e) =>
            {
                if (e.OwnerId == helper.ModManifest.UniqueID && e.KeybindId == "close")
                    helper.Monitor.Log(demo.Close());
            };
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => helper.Monitor.Log(demo.Close());
        }
    }

    // Managed demonstration effects. A real Advanced Mod supplies its exact
    // native restore/unpatch actions here; resource ownership cannot infer them.
    internal sealed class CleanupDemo
    {
        private readonly IDtmOwnedResource motion;
        private readonly IDtmOwnedResource hooks;
        internal double Rate { get; private set; } = 1;
        internal bool CallbacksActive { get; private set; }
        internal bool HookAttached { get; private set; }
        internal int RestoreAttempts { get; private set; }
        internal int UnpatchAttempts { get; private set; }

        internal CleanupDemo(IDtmOwnedResources resources)
        {
            motion = resources.Register(new Release(() =>
            {
                if (++RestoreAttempts == 1) throw new InvalidOperationException("Demonstration: restoration fails once.");
                Rate = 1;
            }));
            hooks = resources.Register(new Release(() =>
            {
                CallbacksActive = false; // Residual callbacks must go dormant before unpatch.
                UnpatchAttempts++;
                HookAttached = false;
            }));
            Rate = 3;
            HookAttached = CallbacksActive = true;
        }

        internal string Close()
        {
            // Token.Dispose records Failed without throwing, so both requests run.
            // The same tokens retain failures and skip already completed releases.
            hooks.Dispose();
            motion.Dispose();
            return "hooks=" + hooks.Status + "; motion=" + motion.Status + "; rate=" + Rate + "; restoreAttempts=" + RestoreAttempts;
        }

        private sealed class Release : IDisposable
        {
            private readonly Action action;
            internal Release(Action action) { this.action = action; }
            public void Dispose() => action();
        }
    }
}
