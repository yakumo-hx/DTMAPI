using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void OptionalServicesEnforceOwnerLifetimeAndCleanupRetry()
        {
            bool active = true;
            int made = 0;
            Action check = () => { if (!active) throw new InvalidOperationException("inactive owner"); };
            var scope = new OwnerServiceScope(check, () => { }, (type, guard) =>
                type == typeof(IServiceProbe) ? new ServiceProbe(guard, ++made) : null);
            var helper = new DtmHelper(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, scope);
            IServiceProbe first = helper.GetRequiredService<IServiceProbe>("0.7.0", "0.7.0");
            Assert(ReferenceEquals(first, helper.GetOptionalService<IServiceProbe>()) && made == 1, "An owner must reuse its service instance.");
            Assert(helper.GetOptionalService<object>() == null, "Assignable object must not bypass the exact contract allowlist.");
            try { helper.GetRequiredService<IDisposable>("0.7.0", "0.7.0"); throw new Exception("missing service accepted"); }
            catch (NotSupportedException ex) { Assert(ex.Message.Contains("Runtime >= 0.7.0") && ex.Message.Contains("API target >= 0.7.0"), "Missing service must name both minimum targets."); }
            active = false;
            AssertThrows(() => first.Use(), "Held service must enforce owner validity on each operation.");
            active = true;
            var probe = (ServiceProbe)first;
            probe.OnDispose = () =>
            {
                AssertThrows(() => helper.GetOptionalService<IServiceProbe>(), "Dispose reentry must not reacquire a service.");
                scope.Close();
            };
            probe.FailDisposeOnce = true;
            var failed = scope.Close();
            Assert(failed.FailureCount == 1 && failed.RemainingResources == 1, "Failed dispose must remain available for retry.");
            AssertThrows(() => first.Use(), "Service reference must stay closed even if disposal fails.");
            AssertThrows(() => helper.GetOptionalService<object>(), "A closed scope must reject queries including unknown types.");
            var retried = scope.Close();
            Assert(retried.RemainingResources == 0 && retried.FailureCount == 0 && probe.DisposeCount == 2, "Cleanup retry must release the failed resource exactly once more.");
            scope.Close();
            Assert(probe.DisposeCount == 2, "Successful cleanup is idempotent.");

            var other = new OwnerServiceScope(() => { }, () => { }, (type, guard) =>
                type == typeof(IServiceProbe) ? new ServiceProbe(guard, 2) : null);
            IServiceProbe otherInstance = other.GetService<IServiceProbe>()!;
            Assert(!ReferenceEquals(first, otherInstance) && otherInstance.Use() == 2, "Closing owner A must not close owner B.");
            other.Close();

            var manager = new OwnerServiceManager();
            var a = manager.Create("Owner.A", () => { }, () => { });
            var b = manager.Create("Owner.B", () => { }, () => { });
            Assert(a.GetService<IServiceProbe>() == null, "Internal test service must not be available in the production catalog.");
            manager.RemoveOwner("owner.a", ModOwnerCleanupReason.EntryFailed);
            AssertThrows(() => a.GetService<IServiceProbe>(), "Entry failure closes the original helper scope.");
            Assert(b.GetService<IServiceProbe>() == null, "Cleanup must isolate owner IDs.");
            var replacement = manager.Create("Owner.A", () => { }, () => { });
            Assert(!ReferenceEquals(a, replacement), "A new owner activation must use a new scope.");
            AssertThrows(() => a.GetService<IServiceProbe>(), "Old helper must not revive when the same owner ID reactivates.");
            manager.RemoveOwner("Owner.A", ModOwnerCleanupReason.RuntimeShutdown);
            manager.RemoveOwner("Owner.B", ModOwnerCleanupReason.RuntimeShutdown);
        }

        private static void OptionalServicesHandleConstructionReentryAndThreadBoundary()
        {
            OwnerServiceScope? scope = null;
            ServiceProbe? constructed = null;
            scope = new OwnerServiceScope(() => { }, () => { }, (type, guard) =>
            {
                AssertThrows(() => scope!.GetService<IServiceProbe>(), "Recursive construction must be rejected.");
                var result = scope!.Close();
                Assert(result.RemainingResources == 1, "In-flight construction must retain the cleanup participant.");
                constructed = new ServiceProbe(guard, 3);
                return constructed;
            });
            AssertThrows(() => scope.GetService<IServiceProbe>(), "Closure during construction cannot return a live service.");
            Assert(scope.Close().RemainingResources == 0 && constructed!.DisposeCount == 1, "A resource returned after reentrant close must still be cleaned up.");
            var wrongThread = new OwnerServiceScope(() => { }, () => throw new InvalidOperationException("wrong thread"), (type, guard) => throw new Exception("factory must not run"));
            AssertThrows(() => wrongThread.GetService<IServiceProbe>(), "Query must enforce the existing Runtime thread boundary before factory execution.");
        }

        private interface IServiceProbe { int Use(); }
        private sealed class ServiceProbe : IServiceProbe, IDisposable
        {
            private readonly Action guard;
            private readonly int value;
            public Action? OnDispose;
            public bool FailDisposeOnce;
            public int DisposeCount;
            public ServiceProbe(Action guard, int value) { this.guard = guard; this.value = value; }
            public int Use() { guard(); return value; }
            public void Dispose()
            {
                DisposeCount++;
                OnDispose?.Invoke();
                if (FailDisposeOnce) { FailDisposeOnce = false; throw new InvalidOperationException("retry disposal"); }
            }
        }
    }
}
