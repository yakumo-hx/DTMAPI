using System;
using System.Reflection;
using DTMAPI.Abstractions;
using Yuuka.DTMAPI.AutoFishing;

namespace DTMAPI.UnitTests
{
    internal static class LifecycleTests
    {
        internal static void RunAll()
        {
            var entry = new ModEntry();
            var helper = DispatchProxy.Create<IDtmHelper, CleanupProxy>();
            ((CleanupProxy)(object)helper).Call = _ => throw new Exception("Inactive owner helper must not be accessed");
            var key = DispatchProxy.Create<IInputRegistration, CleanupProxy>();
            int attempts = 0;
            ((CleanupProxy)(object)key).Call = method =>
            {
                if (method.Name != "Dispose") throw new Exception("Unexpected input call");
                if (++attempts == 1) throw new InvalidOperationException("input release failed");
                return null;
            };
            var native = new NativeFishing(new AutoFishingConfig(), null!) { FailNextDispose = true };
            Set("helper", helper); Set("key", key); Set("fishing", native);
            try { entry.Dispose(); throw new Exception("Failures must be visible"); }
            catch (AggregateException ex) { if (ex.InnerExceptions.Count != 2) throw new Exception("Each failure must survive the aggregate"); }
            if (attempts != 1 || native.Attempts != 1) throw new Exception("A failure prevented independent cleanup");
            entry.Dispose(); entry.Dispose();
            if (attempts != 2 || native.Attempts != 2) throw new Exception("Failed releases must retry, completed releases must stop");
            void Set(string name, object value) => typeof(ModEntry).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(entry, value);
        }
    }
    public class CleanupProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> Call = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Call(targetMethod!);
    }
}

namespace Yuuka.DTMAPI.AutoFishing
{
    // Replace only native access; the real product Entry/Dispose and cleanup aggregator are linked.
    internal sealed class NativeFishing : IDisposable
    {
        internal int Attempts;
        internal bool FailNextDispose;
        internal NativeFishing(AutoFishingConfig value, IMonitor monitor) { }
        internal void Install() { }
        internal void Configure(AutoFishingConfig value) { }
        internal void Toggle() { }
        internal void Tick() { }
        internal void Stop(string reason) { }
        public void Dispose()
        {
            Attempts++;
            if (FailNextDispose) { FailNextDispose = false; throw new InvalidOperationException("motion restore failed"); }
        }
    }
}
