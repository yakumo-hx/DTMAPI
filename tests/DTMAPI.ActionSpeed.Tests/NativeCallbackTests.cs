using System;
using System.Reflection;
using Yuuka.DTMAPI.ActionSpeed;

namespace DTMAPI.UnitTests;

internal static class NativeCallbackTests
{
    private static readonly MethodInfo Outer = typeof(NativeCallbackTests).GetMethod(nameof(OuterOperation), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo Inner = typeof(NativeCallbackTests).GetMethod(nameof(InnerOperation), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static void OuterOperation() { }
    private static void InnerOperation() { }

    internal static int RunAll()
    {
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        // These call the real Prefix/Finalizer and Dispose in NativeActions.Lifetime.cs.
        NativeActions.EndForTest(NativeActions.BeginForTest(Outer));
        var owner = NewOwner();
        var replacement = NewOwner();
        try
        {
            owner.Install();
            var outer = NativeActions.BeginForTest(Outer);
            var inner = NativeActions.BeginForTest(Inner);
            Check(owner.CurrentOperation == ActionKind.Plant, "Inner native operation wins");
            NativeActions.EndForTest(outer);
            Check(owner.CurrentOperation == ActionKind.Plant, "Out-of-order receipt cannot overwrite a nested operation");
            NativeActions.EndForTest(inner);
            NativeActions.EndForTest(inner);
            Check(owner.CurrentOperation == ActionKind.Interact, "Inner receipt restores its caller once");
            NativeActions.EndForTest(outer);
            Check(owner.CurrentOperation == ActionKind.None, "Outer finalizer releases the operation");

            var beforeReset = NativeActions.BeginForTest(Outer);
            owner.Reset();
            var afterReset = NativeActions.BeginForTest(Inner);
            NativeActions.EndForTest(beforeReset);
            Check(owner.CurrentOperation == ActionKind.Plant, "A receipt from before Reset cannot change the new operation");
            NativeActions.EndForTest(afterReset);

            var beforeClose = NativeActions.BeginForTest(Outer);
            owner.ResetEffect = () => throw new InvalidOperationException("injected animation restore failure");
            owner.FailNextDispose = true;
            owner.UnpatchEffect = () =>
            {
                NativeActions.EndForTest(NativeActions.BeginForTest(Inner));
                Check(owner.CurrentOperation == ActionKind.None, "Callbacks reentered during unpatch are inactive");
            };
            AggregateException? failure = null;
            try { owner.Dispose(); } catch (AggregateException ex) { failure = ex; }
            Check(failure?.InnerExceptions.Count == 2 && owner.Disposals == 1,
                "Animation failure must not skip unpatch, and both errors must survive");
            NativeActions.EndForTest(NativeActions.BeginForTest(Outer));
            NativeActions.EndForTest(beforeClose);
            Check(owner.CurrentOperation == ActionKind.None, "Residual Prefix/Finalizer are harmless after failed cleanup");

            bool installationRefused = false;
            try { replacement.Install(); } catch (InvalidOperationException) { installationRefused = true; }
            Check(installationRefused, "Failed cleanup still owns the patch family until retry succeeds");
            replacement.Dispose();
            Check(replacement.Disposals == 0, "A rejected instance must not unpatch another instance");

            owner.ResetEffect = null;
            owner.UnpatchEffect = null;
            owner.Dispose();
            Check(owner.Disposals == 2, "Failed cleanup remains retryable");
            owner.Dispose();
            Check(owner.Disposals == 2, "Completed cleanup is idempotent");

            replacement = NewOwner();
            replacement.Install();
            var replacementScope = NativeActions.BeginForTest(Inner);
            NativeActions.EndForTest(beforeClose);
            owner.Dispose();
            Check(replacement.CurrentOperation == ActionKind.Plant && owner.Disposals == 2,
                "An old receipt or repeated Dispose cannot alter a replacement instance");
            NativeActions.EndForTest(replacementScope);
            replacement.ClassifyEffect = replacement.Dispose;
            NativeActions.EndForTest(NativeActions.BeginForTest(Outer));
            Check(replacement.CurrentOperation == ActionKind.None, "A classification returning after owner close cannot publish a scope");
            return checks;
        }
        finally
        {
            owner.ResetEffect = null; owner.UnpatchEffect = null; owner.FailNextDispose = false;
            replacement.ClassifyEffect = null;
            owner.Dispose(); replacement.Dispose();
        }
    }

    private static NativeActions NewOwner()
    {
        var owner = new NativeActions(() => new ActionConfig());
        owner.RegisterOperation(Outer, NativeAction.Fuel);
        owner.RegisterOperation(Inner, NativeAction.PlantSeed);
        return owner;
    }
}
