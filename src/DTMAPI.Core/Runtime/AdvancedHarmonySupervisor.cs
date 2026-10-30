using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.Core.Runtime
{

    internal sealed class AdvancedHarmonyPatchRecord
    {
        public AdvancedHarmonyPatchRecord(string owner, string target, string patchKind, string patchMethod, string patchModuleMvid, MethodBase? targetMethod = null, MethodInfo? patch = null)
        {
            Owner = owner ?? string.Empty;
            Target = target ?? string.Empty;
            PatchKind = patchKind ?? string.Empty;
            PatchMethod = patchMethod ?? string.Empty;
            PatchModuleMvid = patchModuleMvid ?? string.Empty;
            TargetMethod = targetMethod;
            Patch = patch;
        }

        public string Owner { get; }
        public string Target { get; }
        public string PatchKind { get; }
        public string PatchMethod { get; }
        public string PatchModuleMvid { get; }
        internal MethodBase? TargetMethod { get; }
        internal MethodInfo? Patch { get; }
        public string Format() => "owner=" + Owner + "; kind=" + PatchKind + "; target=" + Target + "; patch=" + PatchMethod;

        internal bool SameContribution(AdvancedHarmonyPatchRecord other) =>
            Owner == other.Owner && PatchKind == other.PatchKind &&
            TargetMethod != null && Patch != null &&
            Equals(TargetMethod, other.TargetMethod) && Equals(Patch, other.Patch);

        internal bool Targets(MethodBase target) =>
            target != null && Target.Equals(MethodIdentity(target), StringComparison.Ordinal);

        internal static string MethodIdentity(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            string prefix = ModuleMvid(method.Module);
            try
            {
                return prefix + ":" + method.MetadataToken.ToString("X8", CultureInfo.InvariantCulture);
            }
            catch
            {
                return prefix + ":dynamic:" + (method.DeclaringType?.FullName ?? string.Empty) + "." + method.Name + ":" + RuntimeHelpers.GetHashCode(method).ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string ModuleMvid(Module module)
        {
            try
            {
                return module.ModuleVersionId.ToString("D", CultureInfo.InvariantCulture);
            }
            catch
            {
                return module.Name ?? string.Empty;
            }
        }
    }

    internal sealed class AdvancedHarmonySnapshot
    {
        public AdvancedHarmonySnapshot(bool available, IReadOnlyList<AdvancedHarmonyPatchRecord> patches, string failure)
        {
            Available = available;
            Patches = patches ?? Array.Empty<AdvancedHarmonyPatchRecord>();
            Failure = failure ?? string.Empty;
        }

        public bool Available { get; }
        public IReadOnlyList<AdvancedHarmonyPatchRecord> Patches { get; }
        public string Failure { get; }

        public static AdvancedHarmonySnapshot Captured(params AdvancedHarmonyPatchRecord[] patches) =>
            new AdvancedHarmonySnapshot(true, patches ?? Array.Empty<AdvancedHarmonyPatchRecord>(), string.Empty);

        public static AdvancedHarmonySnapshot Unavailable(string failure) =>
            new AdvancedHarmonySnapshot(false, Array.Empty<AdvancedHarmonyPatchRecord>(), failure);
    }

    internal readonly struct AdvancedHarmonyUnpatchResult
    {
        public AdvancedHarmonyUnpatchResult(bool success, int removed, int remaining, string details)
        {
            Success = success;
            Removed = Math.Max(0, removed);
            Remaining = Math.Max(0, remaining);
            Details = details ?? string.Empty;
        }

        public bool Success { get; }
        public int Removed { get; }
        public int Remaining { get; }
        public string Details { get; }
    }

    internal interface IAdvancedHarmonyInspector
    {
        AdvancedHarmonySnapshot Capture();
        AdvancedHarmonyUnpatchResult UnpatchPatches(IReadOnlyList<AdvancedHarmonyPatchRecord> patches);
    }

    internal sealed class ReflectionAdvancedHarmonyInspector : IAdvancedHarmonyInspector
    {
        public AdvancedHarmonySnapshot Capture()
        {
            try
            {
                Type? harmonyType = FindHarmonyType();
                if (harmonyType == null) return AdvancedHarmonySnapshot.Captured();
                MethodInfo getAllPatchedMethods = RequireStaticMethod(harmonyType, "GetAllPatchedMethods", 0);
                MethodInfo getPatchInfo = RequireStaticMethod(harmonyType, "GetPatchInfo", 1);
                var records = new List<AdvancedHarmonyPatchRecord>();
                object? patchedMethods = getAllPatchedMethods.Invoke(null, null);
                if (!(patchedMethods is IEnumerable methods))
                    throw new InvalidOperationException("Harmony.GetAllPatchedMethods did not return an enumerable.");

                foreach (object? value in methods)
                {
                    if (!(value is MethodBase target))
                        continue;
                    object? patchInfo = getPatchInfo.Invoke(null, new object[] { target });
                    if (patchInfo == null)
                        continue;
                    AddPatchKind(records, patchInfo, target, "Prefixes", "prefix");
                    AddPatchKind(records, patchInfo, target, "Postfixes", "postfix");
                    AddPatchKind(records, patchInfo, target, "Transpilers", "transpiler");
                    AddPatchKind(records, patchInfo, target, "Finalizers", "finalizer");
                    AddPatchKind(records, patchInfo, target, "ILManipulators", "ilmanipulator");
                }
                return new AdvancedHarmonySnapshot(true, records, string.Empty);
            }
            catch (Exception ex)
            {
                Exception failure = Unwrap(ex);
                return AdvancedHarmonySnapshot.Unavailable(failure.GetType().Name + ": " + failure.Message);
            }
        }

        public AdvancedHarmonyUnpatchResult UnpatchPatches(IReadOnlyList<AdvancedHarmonyPatchRecord> patches)
        {
            if (patches.Count == 0) return new AdvancedHarmonyUnpatchResult(true, 0, 0, "No attributed patches.");
            int remaining = patches.Count;
            var failures = new List<string>();
            try
            {
                Type harmonyType = FindHarmonyType() ?? throw new TypeLoadException("Harmony is no longer available for the observed patches.");
                MethodInfo? unpatch = harmonyType.GetMethod("Unpatch", new[] { typeof(MethodBase), typeof(MethodInfo) });
                if (unpatch == null)
                    return new AdvancedHarmonyUnpatchResult(false, 0, remaining, "Harmony method-specific cleanup unavailable; restart required.");
                MethodInfo getPatchInfo = RequireStaticMethod(harmonyType, "GetPatchInfo", 1);
                object instance = Activator.CreateInstance(harmonyType, "DTMAPI.Cleanup")!;
                foreach (var group in patches.Where(p => p.TargetMethod != null && p.Patch != null).GroupBy(p => new { p.TargetMethod, p.Patch }))
                {
                    // Unpatch(target, method) removes every use of that method on
                    // the target. Preserve it if any other contribution shares it.
                    try
                    {
                        var current = ReadTarget(getPatchInfo, group.Key.TargetMethod!);
                        if (current.Any(p => Equals(p.Patch, group.Key.Patch) && !patches.Any(p.SameContribution))) continue;
                        unpatch.Invoke(unpatch.IsStatic ? null : instance, new object[] { group.Key.TargetMethod!, group.Key.Patch! });
                    }
                    catch (Exception ex) { failures.Add(Unwrap(ex).Message); }
                }
                remaining = patches.Count(p => p.TargetMethod == null || p.Patch == null);
                foreach (var target in patches.Where(p => p.TargetMethod != null && p.Patch != null).Select(p => p.TargetMethod!).Distinct())
                {
                    var expected = patches.Where(p => Equals(p.TargetMethod, target) && p.Patch != null).ToArray();
                    try
                    {
                        var current = ReadTarget(getPatchInfo, target);
                        remaining += expected.Count(p => current.Any(p.SameContribution));
                    }
                    catch (Exception ex)
                    {
                        remaining += expected.Length;
                        failures.Add(Unwrap(ex).Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Exception failure = Unwrap(ex);
                return new AdvancedHarmonyUnpatchResult(false, 0, remaining, failure.GetType().Name + ": " + failure.Message);
            }
            return new AdvancedHarmonyUnpatchResult(remaining == 0 && failures.Count == 0, Math.Max(0, patches.Count - remaining), remaining,
                "Method-specific Harmony cleanup; remaining=" + remaining + ". " + string.Join("; ", failures));
        }

        private static List<AdvancedHarmonyPatchRecord> ReadTarget(MethodInfo getPatchInfo, MethodBase target)
        {
            var records = new List<AdvancedHarmonyPatchRecord>();
            object? info = getPatchInfo.Invoke(null, new object[] { target });
            if (info == null) return records;
            AddPatchKind(records, info, target, "Prefixes", "prefix");
            AddPatchKind(records, info, target, "Postfixes", "postfix");
            AddPatchKind(records, info, target, "Transpilers", "transpiler");
            AddPatchKind(records, info, target, "Finalizers", "finalizer");
            AddPatchKind(records, info, target, "ILManipulators", "ilmanipulator");
            return records;
        }

        private static void AddPatchKind(List<AdvancedHarmonyPatchRecord> records, object patchInfo, MethodBase target, string propertyName, string kind)
        {
            object? collection = ReadMember(patchInfo, propertyName);
            if (!(collection is IEnumerable patches))
                return;
            foreach (object? patch in patches)
            {
                if (patch == null)
                    continue;
                string owner = Convert.ToString(ReadMember(patch, "owner") ?? ReadMember(patch, "Owner"), CultureInfo.InvariantCulture) ?? string.Empty;
                MethodInfo? patchMethod = ReadMember(patch, "PatchMethod") as MethodInfo ?? ReadMember(patch, "patch") as MethodInfo;
                records.Add(new AdvancedHarmonyPatchRecord(
                    owner,
                    AdvancedHarmonyPatchRecord.MethodIdentity(target),
                    kind,
                    patchMethod == null ? "unknown" : AdvancedHarmonyPatchRecord.MethodIdentity(patchMethod),
                    patchMethod == null ? string.Empty : ModuleMvid(patchMethod.Module), target, patchMethod));
            }
        }

        private static object? ReadMember(object value, string name)
        {
            Type type = value.GetType();
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(value, null);
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(value);
        }

        private static string ModuleMvid(Module module)
        {
            try
            {
                return module.ModuleVersionId.ToString("D", CultureInfo.InvariantCulture);
            }
            catch
            {
                return module.Name ?? string.Empty;
            }
        }

        private static Type? FindHarmonyType()
        {
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("HarmonyLib.Harmony", throwOnError: false, ignoreCase: false))
                .FirstOrDefault(candidate => candidate != null);
            return type;
        }

        private static MethodInfo RequireStaticMethod(Type type, string name, int parameterCount)
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SingleOrDefault(method => method.Name.Equals(name, StringComparison.Ordinal) && method.GetParameters().Length == parameterCount)
                ?? throw new MissingMethodException(type.FullName, name);
        }

        private static int CountOwner(AdvancedHarmonySnapshot snapshot, string owner) =>
            snapshot.Patches.Count(patch => patch.Owner.Equals(owner, StringComparison.Ordinal));

        private static Exception Unwrap(Exception exception) =>
            exception is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : exception;
    }

    internal sealed class AdvancedHarmonySupervisor : IModOwnerCleanupParticipant
    {
        private readonly Dictionary<string, OwnerState> states = new Dictionary<string, OwnerState>(StringComparer.OrdinalIgnoreCase);
        private IAdvancedHarmonyInspector inspector;
        public AdvancedHarmonySupervisor(IAdvancedHarmonyInspector? inspector = null) { this.inspector = inspector ?? new ReflectionAdvancedHarmonyInspector(); }
        public string ParticipantId => "DTMAPI.Core.AdvancedHarmonySupervisor";

        public void ConfigureInspectorForTests(IAdvancedHarmonyInspector value)
        {
            if (states.Count != 0) throw new InvalidOperationException("Cannot change the inspector while owners are registered.");
            inspector = value ?? throw new ArgumentNullException(nameof(value));
        }

        public void BeginLoad(string ownerId, ManagedModClassification classification)
        {
            if (classification == null || !classification.IsCodeMod) return;
            if (states.ContainsKey(ownerId)) throw new InvalidOperationException("Owner already active: " + ownerId);
            states.Add(ownerId, new OwnerState(ownerId, classification.ExpectedHarmonyOwner));
        }

        internal void RegisterRuntimeModule(string owner) => states.Add(owner, new OwnerState(owner, owner) { EntryCompleted = true });

        // Observation can arrive before Entry or through normal CLR/custom loads.
        // Its absence is an attribution gap, never a cold-start permission check.
        internal void BindAssembly(string ownerId, Assembly assembly)
        {
            if (states.TryGetValue(ownerId, out var state)) state.Assemblies.Add(assembly);
        }
        public void BindEntryAssembly(string ownerId, Assembly entryAssembly) => BindAssembly(ownerId, entryAssembly);
        public void BeginEntry(string ownerId, ManagedModClassification classification, Assembly entryAssembly)
        { BeginLoad(ownerId, classification); BindEntryAssembly(ownerId, entryAssembly); }
        public void CompleteEntry(string ownerId)
        { if (states.TryGetValue(ownerId, out var state)) state.EntryCompleted = true; }

        internal object ActivateInternalModule(string ownerId, Assembly implementation, Func<object> activate)
        {
            if (!states.TryGetValue(ownerId, out var state) || !state.EntryCompleted)
                throw new InvalidOperationException("module-owner-inactive: " + ownerId);
            state.Assemblies.Add(implementation);
            // This snapshot bounds a partial module activation rollback. Ordinary
            // Mod Entry takes no global before/after snapshots.
            AdvancedHarmonySnapshot before = inspector.Capture();
            try { return activate(); }
            catch
            {
                AdvancedHarmonySnapshot after = inspector.Capture();
                if (before.Available && after.Available)
                {
                    var additions = after.Patches.Where(p => !before.Patches.Any(p.SameContribution) && IsExclusive(state, p)).ToArray();
                    inspector.UnpatchPatches(additions);
                }
                throw;
            }
        }

        // Explicit observation only: naming and multiple Harmony IDs are not violations.
        public IReadOnlyList<AdvancedHarmonyAuditIssue> AuditActiveOwners()
        {
            var current = inspector.Capture();
            return current.Available ? Array.Empty<AdvancedHarmonyAuditIssue>() : new[] { new AdvancedHarmonyAuditIssue("", "harmony-not-checked", current.Failure) };
        }

        public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
        {
            if (!states.TryGetValue(ownerId, out var state)) return ModOwnerCleanupParticipantResult.None("No Harmony ownership observations for this owner.");
            if (state.Assemblies.Count == 0)
            {
                states.Remove(ownerId);
                return ModOwnerCleanupParticipantResult.None("No managed assembly was bound for this owner.");
            }
            AdvancedHarmonySnapshot current = inspector.Capture();
            if (!current.Available)
                return new ModOwnerCleanupParticipantResult(0, 0, 1, "Harmony cleanup not checked; restart required: " + current.Failure);
            var attributed = current.Patches.Where(p => IsExclusive(state, p)).ToArray();
            int unknown = current.Patches.Count(p => Owns(state, p) && !IsExclusive(state, p) ||
                p.Patch == null && p.Owner.Equals(state.OwnerHint, StringComparison.Ordinal));
            AdvancedHarmonyUnpatchResult cleanup = inspector.UnpatchPatches(attributed);
            int remaining = cleanup.Remaining + unknown;
            int failures = cleanup.Success && remaining == 0 ? 0 : 1;
            if (failures == 0) states.Remove(ownerId);
            return new ModOwnerCleanupParticipantResult(cleanup.Removed, remaining, failures,
                "code=" + (failures == 0 ? "advanced-harmony-cleanup-complete" : "advanced-harmony-cleanup-failed") +
                "; unknown=" + unknown + "; restart=" + (failures == 0 ? "assembly-lifetime-policy" : "required-incomplete-harmony-cleanup") + "; " + cleanup.Details);
        }

        private bool IsExclusive(OwnerState state, AdvancedHarmonyPatchRecord patch) =>
            Owns(state, patch) && !states.Values.Any(other => !ReferenceEquals(other, state) && Owns(other, patch));
        private static bool Owns(OwnerState state, AdvancedHarmonyPatchRecord patch)
        {
            return patch.Patch != null && state.Assemblies.Contains(patch.Patch.Module.Assembly);
        }
        private sealed class OwnerState
        {
            internal OwnerState(string id, string hint) { Id = id; OwnerHint = hint; }
            internal string Id { get; }
            internal string OwnerHint { get; }
            internal bool EntryCompleted { get; set; }
            internal HashSet<Assembly> Assemblies { get; } = new HashSet<Assembly>();
        }
    }
    internal sealed class AdvancedHarmonyAuditIssue
    {
        public AdvancedHarmonyAuditIssue(string ownerId, string code, string details)
        {
            OwnerId = ownerId ?? string.Empty;
            Code = code ?? string.Empty;
            Details = details ?? string.Empty;
        }

        public string OwnerId { get; }
        public string Code { get; }
        public string Details { get; }
    }
}
