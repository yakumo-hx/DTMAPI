using System;
using System.Collections.Generic;
using System.Reflection;

namespace Yuuka.DTMAPI.ActionSpeed;

// This control path is shared by the real native adapter and its fault-boundary tests.
internal sealed partial class NativeActions
{
    private static NativeActions? installed;
    private static NativeActions? active
    { get { var owner = installed; return owner != null && owner.state == HookLifetime.Active ? owner : null; } }
    private enum HookLifetime { Created, Active, Stopping, Closed }
    private HookLifetime state;
    private readonly ActionScope scope = new ActionScope();
    private readonly Dictionary<MethodBase, NativeAction> operations = new Dictionary<MethodBase, NativeAction>();

    private void Activate()
    {
        if (installed != null || state != HookLifetime.Created) throw new InvalidOperationException("ActionSpeed native cleanup must finish before another installation.");
        state = HookLifetime.Active;
        installed = this;
    }

    private static void BeginOperation(object __instance, MethodBase __originalMethod, out ActionScope.Receipt __state)
    {
        __state = default;
        var self = active;
        if (self == null || !self.operations.TryGetValue(__originalMethod, out var operation)) return;
        var kind = self.ClassifyOperation(operation, __instance);
        if (ReferenceEquals(active, self)) __state = self.scope.Push(kind);
    }

    private static void EndOperation(ActionScope.Receipt __state) => __state.Restore();

    public void Dispose()
    {
        // Disable every residual callback before a native restore/unpatch can reenter it.
        if (state == HookLifetime.Closed) return;
        state = HookLifetime.Stopping;
        scope.Reset();
        Exception? restoreError = null;
        try { Reset(); } catch (Exception ex) { restoreError = ex; }
        try { if (ReferenceEquals(installed, this)) Unpatch(); }
        catch (Exception ex)
        {
            if (restoreError != null) throw new AggregateException(restoreError, ex);
            throw;
        }
        if (restoreError != null) throw restoreError;
        state = HookLifetime.Closed;
        if (ReferenceEquals(installed, this)) installed = null;
    }
}
