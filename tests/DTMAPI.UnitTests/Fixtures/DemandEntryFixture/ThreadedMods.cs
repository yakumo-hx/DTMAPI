using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using DTMAPI.Abstractions;

namespace DTMAPI.DemandEntryFixture
{
    public sealed class ThreadedFailureMod : DtmMod, IDisposable
    {
        public static Action<MethodInfo>? OnPatch;
        public static int EntryThread, LoadThread, LoadCallbackThread, DisposeThread;
        public static bool Disposed;
        public static int InstalledPrefixes;
        public override void Entry(IDtmHelper helper)
        {
            EntryThread = Thread.CurrentThread.ManagedThreadId;
            Assembly? dependency = null;
            Exception? failure = null;
            var worker = new Thread(() =>
            {
                try { LoadThread = Thread.CurrentThread.ManagedThreadId; dependency = LoadPayload(); }
                catch (Exception ex) { failure = ex; }
            }) { IsBackground = true };
            AssemblyLoadEventHandler observed = (_, args) =>
            {
                if (args.LoadedAssembly.GetName().Name == "DTMAPI.DemandPayloadFixture") LoadCallbackThread = Thread.CurrentThread.ManagedThreadId;
            };
            AppDomain.CurrentDomain.AssemblyLoad += observed;
            try
            {
                worker.Start();
                if (!worker.Join(TimeSpan.FromSeconds(15))) throw new TimeoutException("Background dependency load blocked while Entry waited.");
            }
            finally { AppDomain.CurrentDomain.AssemblyLoad -= observed; }
            if (failure != null) throw failure;
            Install(dependency!.GetType("DTMAPI.DemandPayloadFixture.Payload", true)!.GetMethod("Prefix")!);
            throw new InvalidOperationException("Expected failure after background dependency load.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Assembly LoadPayload() => Assembly.Load("DTMAPI.DemandPayloadFixture");
        public void Dispose()
        {
            DisposeThread = Thread.CurrentThread.ManagedThreadId;
            var dependency = Assembly.Load("DTMAPI.DemandCallerFixture");
            Install(dependency.GetType("DTMAPI.DemandCallerFixture.CleanupPatch", true)!.GetMethod("Prefix")!);
            Disposed = true;
        }
        public static void Install(MethodInfo prefix)
        {
            if (OnPatch != null) { OnPatch(prefix); return; }
            // The same fixture runs against the game's real Harmony on Mono.
            var harmony = Type.GetType("HarmonyLib.Harmony, 0Harmony", true)!;
            var harmonyMethod = harmony.Assembly.GetType("HarmonyLib.HarmonyMethod", true)!;
            var patch = harmony.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 6);
            patch.Invoke(Activator.CreateInstance(harmony, "DependencyProbe." + prefix.DeclaringType!.FullName),
                new[] { (object)typeof(ThreadedFailureMod).GetMethod(nameof(Target))!, Activator.CreateInstance(harmonyMethod, prefix), null, null, null, null });
            object info = harmony.GetMethod("GetPatchInfo")!.Invoke(null, new object[] { typeof(ThreadedFailureMod).GetMethod(nameof(Target))! })!;
            var prefixes = (System.Collections.IEnumerable)(info.GetType().GetProperty("Prefixes")?.GetValue(info) ?? info.GetType().GetField("Prefixes")!.GetValue(info))!;
            bool found = prefixes.Cast<object>().Any(p => Equals(p.GetType().GetProperty("PatchMethod")!.GetValue(p), prefix));
            if (!found) throw new InvalidOperationException("The actual Harmony prefix was not installed.");
            InstalledPrefixes++;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Target() => 7;
    }
    public sealed class SharedMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            if (Manifest.UniqueID.EndsWith("Failed", StringComparison.Ordinal)) throw new InvalidOperationException("Expected shared Entry failure.");
            ThreadedFailureMod.Install(typeof(SharedMod).GetMethod(nameof(Prefix))!);
        }
        public static void Prefix() { }
    }
    public sealed class SharedDependencyMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            // An alias forces a real request even when the library is resident.
            // Both packages resolve the provider's same file on Unity Mono.
            var dependency = Assembly.Load("SharedDependencyAlias");
            if (Manifest.UniqueID.EndsWith("Failed", StringComparison.Ordinal)) throw new InvalidOperationException("Expected shared dependency Entry failure.");
            ThreadedFailureMod.Install(dependency.GetType("DTMAPI.DemandPayloadFixture.Payload", true)!.GetMethod("Prefix")!);
        }
    }

}
