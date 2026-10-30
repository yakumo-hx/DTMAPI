using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Core.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Services;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void PublicReflectionUsesOptionalServiceAndIndependentOwnerClosure()
        {
            int thread = Thread.CurrentThread.ManagedThreadId;
            Action runtimeThread = () => { if (Thread.CurrentThread.ManagedThreadId != thread) throw new InvalidOperationException("runtime thread"); };
            var owners = new OwnerServiceManager();
            var a = owners.Create("reflection.a", () => { }, runtimeThread, ensureReflectionOwnerAlive: () => { });
            var b = owners.Create("reflection.b", () => { }, runtimeThread, ensureReflectionOwnerAlive: () => { });
            var helper = new DtmHelper(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, a);
            IReflectionHelper reflection = helper.GetReflection();
            Assert(ReferenceEquals(reflection, helper.GetOptionalService<IReflectionHelper>()), "Reflection uses the existing exact optional service slot.");
            var oldHelper = new DtmHelper(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);
            ReflectionReject<NotSupportedException>(() => oldHelper.GetReflection());
            var target = new ReflectionChild();
            var heldA = reflection.GetProperty<string>(target, "Text");
            var heldB = b.GetService<IReflectionHelper>()!.GetProperty<string>(target, "Text");
            Task.Run(() =>
            {
                using var method = reflection.GetMethod(target, "Overload", new[] { typeof(int) });
                Assert(method.Invoke<string>(8) == "int:8", "Acquired reflection service uses the caller thread for ordinary managed objects.");
                ReflectionReject<InvalidOperationException>(() => helper.GetReflection());
            }).GetAwaiter().GetResult();
            var result = owners.RemoveOwner("reflection.a", ModOwnerCleanupReason.Unload);
            Assert(result.RemainingResources == 0 && result.FailureCount == 0, "Owner participant closes reflection service.");
            ReflectionReject<ObjectDisposedException>(() => heldA.GetValue());
            ReflectionReject<ObjectDisposedException>(() => reflection.GetField<int>(target, "hidden"));
            Assert(heldB.GetValue() == "text", "Closing owner A leaves owner B functional.");
            owners.RemoveOwner("reflection.b", ModOwnerCleanupReason.RuntimeShutdown);
            ReflectionReject<ObjectDisposedException>(() => heldB.GetValue());
        }

        private static void ReflectionUsesExactSignaturesAndPreservesErrors()
        {
            using var scope = new ReflectionScope();
            var target = new ReflectionChild();
            Assert(scope.Field<int>(target, "hidden").GetValue() == 2, "Most-derived hidden field must win.");
            Assert(scope.Field<string>(target, "inheritedPrivate").GetValue() == "base", "Private base field must resolve.");
            Assert(scope.Method(target, "Overload", typeof(int)).Invoke<string>(3) == "int:3", "Exact base overload is still eligible beneath a derived overload.");
            Assert(scope.Method(target, "Overload", typeof(string)).Invoke<string>("x") == "string:x", "Exact derived overload.");
            Assert(!scope.TryMethod(target, "Overload", new[] { typeof(long) }, out var missingMethod) && missingMethod == null, "No implicit numeric overload resolution.");
            Assert(!scope.TryProperty<string>(target, "Missing", out var missing) && missing == null, "Only absence returns false/null.");
            Assert(scope.Property<string?>(target, "Null").GetValue() == null, "A legitimate null value is successful.");
            ReflectionReject<InvalidCastException>(() => scope.TryField<string>(target, "hidden", out _));
            ReflectionReject<NotSupportedException>(() => scope.TryProperty<int>(target, "Item", out _));
            ReflectionReject<NotSupportedException>(() => scope.TryProperty<int>(target, "WriteOnly", out _));
            ReflectionReject<AmbiguousMatchException>(() => scope.TryProperty<int>(new ReflectionAmbiguous(), "Item", out _));
            ReflectionReject<MissingFieldException>(() => scope.StaticField<int>(typeof(ReflectionChild), "hidden"));
            Assert(scope.StaticField<string>(typeof(ReflectionBase), "StaticText").GetValue() == "static", "Static lookup remains separate.");

            var wide = scope.Property<object>(target, "Text");
            wide.SetValue("changed");
            Assert(target.Text == "changed", "Assignable reference write.");
            ReflectionReject<ArgumentException>(() => wide.SetValue(new object()));
            Assert(target.Text == "changed", "Rejected write must not modify target.");
            var readOnly = scope.Field<int>(target, "ReadOnly");
            Assert(!readOnly.CanWrite && readOnly.GetValue() == 7, "Readonly fields remain readable.");
            ReflectionReject<NotSupportedException>(() => readOnly.SetValue(8));
            var constant = scope.StaticField<int>(typeof(ReflectionBase), "Constant");
            Assert(!constant.CanWrite && constant.GetValue() == 9, "Constants are read-only.");
            ReflectionReject<NotSupportedException>(() => constant.SetValue(1));
            ReflectionReject<NotSupportedException>(() => scope.Method(target, "Generic"));
            ReflectionReject<NotSupportedException>(() => scope.Method(target, "Ref", typeof(int).MakeByRefType()));

            var count = scope.Method(target, "Count", typeof(int));
            ReflectionReject<InvalidCastException>(() => count.Invoke<long>(1));
            ReflectionReject<NotSupportedException>(() => count.Invoke(1));
            ReflectionReject<ArgumentException>(() => count.Invoke<int>(1L));
            ReflectionReject<ArgumentException>(() => count.Invoke<int>(new object?[] { null }));
            ReflectionReject<ArgumentException>(() => count.Invoke<int>());
            Assert(target.Calls == 0, "Type/count/return validation happens before executing target.");
            Assert(count.Invoke<int>(4) == 4 && target.Calls == 1, "Exact invocation executes once.");
            ReflectionReject<ArgumentException>(() => scope.Method(target, "Optional", typeof(int)).Invoke<int>());
            ReflectionReject<ArgumentException>(() => scope.Method(target, "Params", typeof(string[])).Invoke<int>("a", "b"));
            Assert(scope.Method(target, "Params", typeof(string[])).Invoke<int>(new object?[] { new[] { "a", "b" } }) == 2, "Explicit params array is an ordinary exact argument.");
            scope.Method(target, "VoidCall").Invoke();
            Assert(target.Calls == 2, "Declared void method executes through void Invoke.");
            try { scope.Method(target, "ThrowTarget").Invoke(); throw new Exception("Target exception was swallowed."); }
            catch (TargetInvocationException ex)
            { Assert(ex.InnerException is ApplicationException && ex.InnerException.StackTrace!.Contains("ThrowTarget"), "Target failure preserves original exception and target stack."); }
            object boxed = new ReflectionStruct { Value = 6 };
            Assert(scope.Field<int>(boxed, "Value").GetValue() == 6, "Boxed value read.");
            ReflectionReject<NotSupportedException>(() => scope.Field<int>(boxed, "Value").SetValue(3));
            ReflectionReject<NotSupportedException>(() => scope.Method(boxed, "Increment").Invoke());
        }

        private static void ReflectionMetadataUsesTypeIdentityAndBoundedConcurrentCache()
        {
            string fullName = "Same.Namespace.NativeShape";
            Type first = ReflectionDynamicType(fullName, 11, 1);
            using var scope = new ReflectionScope();
            Assert(!scope.TryStaticField<int>(first, "LaterOnly", out _), "Missing metadata before another assembly loads.");
            Type second = ReflectionDynamicType(fullName, 22, 600);
            Assert(first.FullName == second.FullName && first.Assembly != second.Assembly, "Fixture requires equal names and distinct CLR type identities.");
            Assert(scope.StaticField<int>(first, "F0").GetValue() == 11 && scope.StaticField<int>(second, "F0").GetValue() == 22, "Cache must never alias same FullName across assemblies.");
            Assert(scope.StaticField<int>(second, "LaterOnly").GetValue() == 22, "Late loaded type is not poisoned by another type's Missing result.");
            Parallel.For(0, 600, i =>
            {
                using var concurrent = new ReflectionScope();
                concurrent.StaticField<int>(second, "F" + i).GetValue();
            });
            Assert(ReflectionMetadata.Count <= ReflectionMetadata.Capacity, "Successful metadata cache is bounded under concurrency.");
            Assert(scope.StaticField<int>(first, "F0").GetValue() == 11, "Eviction and re-resolution preserve exact identity.");
        }

        private static void ReflectionScopeClosesBindingsAndReleasesTargetRoots()
        {
            var target = new ReflectionChild();
            int thread = Thread.CurrentThread.ManagedThreadId;
            using (var scope = new ReflectionScope(() =>
            { if (Thread.CurrentThread.ManagedThreadId != thread) throw new InvalidOperationException("wrong thread"); }))
            {
                var held = scope.Property<string>(target, "Text");
                Task.Run(() => ReflectionReject<InvalidOperationException>(() => held.GetValue())).GetAwaiter().GetResult();
                held.Dispose();
                ReflectionReject<ObjectDisposedException>(() => held.GetValue());
                var after = scope.Property<string>(target, "Text");
                scope.Dispose();
                ReflectionReject<ObjectDisposedException>(() => after.GetValue());
                ReflectionReject<ObjectDisposedException>(() => scope.Property<string>(target, "Text"));
            }
            ReflectionScope? reentrant = null;
            reentrant = new ReflectionScope(() => reentrant!.Dispose());
            ReflectionReject<ObjectDisposedException>(() => reentrant.Property<string>(target, "Text"));
            var fixture = ReflectionRootsFixture();
            fixture.Binding.Dispose();
            for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
            Assert(!fixture.Target.IsAlive && fixture.Owner.IsAlive, "Disposing one wrapper releases its target while the scope and guard owner remain alive.");
            fixture.Scope.Dispose();
            for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
            Assert(!fixture.Target.IsAlive && !fixture.Owner.IsAlive, "Held disposed bindings and metadata cache must not retain target or guard owner.");
            GC.KeepAlive(fixture.Binding);
            GC.KeepAlive(fixture.Scope);
            using var liveScope = new ReflectionScope();
            WeakReference abandoned = ReflectionAbandonedFixture(liveScope);
            for (int i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
            Assert(!abandoned.IsAlive, "An active scope's weak registry cannot retain an abandoned wrapper's target.");
            GC.KeepAlive(liveScope);
            using var entered = new ManualResetEventSlim();
            using var resume = new ManualResetEventSlim();
            using var concurrentScope = new ReflectionScope();
            var concurrentTarget = new ReflectionInFlight(entered, resume);
            var inFlight = concurrentScope.Property<int>(concurrentTarget, "Value");
            Task<int> call = Task.Run(() => inFlight.GetValue());
            try
            {
                Assert(entered.Wait(TimeSpan.FromSeconds(3)), "Target call must enter before closure.");
                concurrentScope.Dispose();
                ReflectionReject<ObjectDisposedException>(() => inFlight.GetValue());
            }
            finally { resume.Set(); }
            Assert(call.GetAwaiter().GetResult() == 42, "An admitted call finishes using its local target after concurrent owner closure.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference ReflectionAbandonedFixture(ReflectionScope scope)
        {
            var target = new ReflectionChild();
            scope.Property<string>(target, "Text");
            return new WeakReference(target);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (ReflectionScope Scope, ReflectionValue<string> Binding, WeakReference Target, WeakReference Owner) ReflectionRootsFixture()
        {
            var owner = new ReflectionChild();
            var target = new ReflectionChild();
            var scope = new ReflectionScope(() => GC.KeepAlive(owner));
            return (scope, scope.Property<string>(target, "Text"), new WeakReference(target), new WeakReference(owner));
        }
        private static Type ReflectionDynamicType(string name, int value, int fieldCount)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ReflectionFixture" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            TypeBuilder builder = assembly.DefineDynamicModule("Main").DefineType(name, TypeAttributes.Public);
            for (int i = 0; i < fieldCount; i++) builder.DefineField("F" + i, typeof(int), FieldAttributes.Public | FieldAttributes.Static);
            if (fieldCount > 1) builder.DefineField("LaterOnly", typeof(int), FieldAttributes.Public | FieldAttributes.Static);
            Type type = builder.CreateType()!;
            type.GetField("F0")!.SetValue(null, value);
            type.GetField("LaterOnly")?.SetValue(null, value);
            return type;
        }
        private static void ReflectionReject<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Reflection expected " + typeof(T).Name + ".");
        }
        private class ReflectionBase
        {
            private readonly string inheritedPrivate = "base";
            protected int hidden = 1;
            public static string StaticText = "static";
            public readonly int ReadOnly = 7;
            public const int Constant = 9;
            public string Text { get; private set; } = "text";
            public string? Null => null;
            public int Calls;
            public string Overload(int value) => "int:" + value;
            public int Count(int value) { Calls++; return value; }
            public int Optional(int value = 8) => value;
            public int Params(params string[] values) => values.Length;
            public void VoidCall() { Calls++; }
            public T Generic<T>() => default!;
            public void Ref(ref int value) { value++; }
            public int this[int index] => index;
            public int WriteOnly { set { Calls = value; } }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void ThrowTarget() => throw new ApplicationException("target failure");
            public override string ToString() => inheritedPrivate + hidden;
        }
        private sealed class ReflectionChild : ReflectionBase
        {
            private new int hidden = 2;
            public string Overload(string value) => "string:" + value;
            public override string ToString() => base.ToString() + hidden;
        }
        private struct ReflectionStruct
        {
            public int Value;
            public void Increment() { Value++; }
        }
        private sealed class ReflectionAmbiguous
        {
            public int this[int index] => index;
            public int this[string index] => index.Length;
        }
        private sealed class ReflectionInFlight
        {
            private readonly ManualResetEventSlim entered, resume;
            internal ReflectionInFlight(ManualResetEventSlim entered, ManualResetEventSlim resume)
            { this.entered = entered; this.resume = resume; }
            public int Value
            {
                get
                {
                    entered.Set();
                    if (!resume.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Closure blocked on executing target.");
                    return 42;
                }
            }
        }
    }
}
