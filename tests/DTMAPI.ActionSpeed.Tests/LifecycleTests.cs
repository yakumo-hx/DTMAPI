using System;
using System.Reflection;
using DTMAPI.Abstractions;
using Yuuka.DTMAPI.ActionSpeed;

namespace DTMAPI.UnitTests
{
    internal static class LifecycleTests
    {
        public static int RunAll()
        {
            var entry = new ModEntry();
            var native = new NativeActions(() => new ActionConfig());
            native.Install();
            var inactiveHelper = DispatchProxy.Create<IDtmHelper, BoundaryProxy>();
            ((BoundaryProxy)(object)inactiveHelper).InvokeBoundary = _ => throw new InvalidOperationException("Owner is inactive");
            Set(entry, "helper", inactiveHelper); Set(entry, "native", native);
            entry.Dispose();
            if (native.Disposals != 1) throw new Exception("Inactive owner prevented native animation/patch cleanup");
            entry.Dispose();
            if (native.Disposals != 1) throw new Exception("Successful cleanup was repeated");
            native = new NativeActions(() => new ActionConfig()) { FailNextDispose = true };
            native.Install();
            Set(entry, "native", native);
            try { entry.Dispose(); } catch (InvalidOperationException) { }
            entry.Dispose();
            if (native.Disposals != 2) throw new Exception("Failed native restoration lost the product retry entry");
            entry.Dispose();
            if (native.Disposals != 2) throw new Exception("Successful retried cleanup was repeated");
            return 4;
        }
        private static void Set(object instance, string field, object value) =>
            instance.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);
    }

    public class BoundaryProxy : DispatchProxy
    {
        public Func<MethodInfo, object?> InvokeBoundary = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => InvokeBoundary(targetMethod!);
    }
}

// Only native effects are substituted. Dispose, activation and operation callbacks
// are compiled from NativeActions.Lifetime.cs, rather than replaced by a stand-in.
namespace Yuuka.DTMAPI.ActionSpeed
{
    internal sealed partial class NativeActions : IDisposable
    {
        internal int Disposals;
        internal bool FailNextDispose;
        internal Action? ResetEffect;
        internal Action? ClassifyEffect;
        internal Action? UnpatchEffect;
        internal NativeActions(Func<ActionConfig> settings) { }
        internal void Install() => Activate();
        internal void Reset() { scope.Reset(); ResetEffect?.Invoke(); }
        private ActionKind ClassifyOperation(NativeAction operation, object instance)
        { ClassifyEffect?.Invoke(); return ActionRules.Classify(operation); }
        private void Unpatch()
        {
            Disposals++;
            UnpatchEffect?.Invoke();
            if (FailNextDispose) { FailNextDispose = false; throw new InvalidOperationException("Motion restore failed"); }
        }
        internal ActionKind CurrentOperation => scope.Current;
        internal void RegisterOperation(MethodBase method, NativeAction action) => operations.Add(method, action);
        internal static ActionScope.Receipt BeginForTest(MethodBase method)
        { BeginOperation(new object(), method, out var receipt); return receipt; }
        internal static void EndForTest(ActionScope.Receipt receipt) => EndOperation(receipt);
    }
}
