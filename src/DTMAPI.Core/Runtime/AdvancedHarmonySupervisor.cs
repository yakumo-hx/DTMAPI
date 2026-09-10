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
    internal sealed class AdvancedHarmonyViolationException : InvalidOperationException
    {
        public AdvancedHarmonyViolationException(string code, string message)
            : base((code ?? "advanced-harmony-violation") + ": " + (message ?? string.Empty))
        {
            Code = code ?? "advanced-harmony-violation";
        }

        public string Code { get; }
    }

    internal sealed class AdvancedHarmonyPatchRecord
    {
        public AdvancedHarmonyPatchRecord(string owner, string target, string patchKind, string patchMethod, string patchModuleMvid)
        {
            Owner = owner ?? string.Empty;
            Target = target ?? string.Empty;
            PatchKind = patchKind ?? string.Empty;
            PatchMethod = patchMethod ?? string.Empty;
            PatchModuleMvid = patchModuleMvid ?? string.Empty;
        }

        public string Owner { get; }
        public string Target { get; }
        public string PatchKind { get; }
        public string PatchMethod { get; }
        public string PatchModuleMvid { get; }
        public string Key => Owner + "\u001f" + Target + "\u001f" + PatchKind + "\u001f" + PatchMethod;
        public string Format() => "owner=" + Owner + "; kind=" + PatchKind + "; target=" + Target + "; patch=" + PatchMethod;

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
        AdvancedHarmonyUnpatchResult UnpatchOwner(string harmonyOwner);
    }

    internal sealed class ReflectionAdvancedHarmonyInspector : IAdvancedHarmonyInspector
    {
        public AdvancedHarmonySnapshot Capture()
        {
            try
            {
                Type harmonyType = FindHarmonyType();
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
                }
                return new AdvancedHarmonySnapshot(true, records, string.Empty);
            }
            catch (Exception ex)
            {
                Exception failure = Unwrap(ex);
                return AdvancedHarmonySnapshot.Unavailable(failure.GetType().Name + ": " + failure.Message);
            }
        }

        public AdvancedHarmonyUnpatchResult UnpatchOwner(string harmonyOwner)
        {
            if (string.IsNullOrWhiteSpace(harmonyOwner))
                return new AdvancedHarmonyUnpatchResult(false, 0, 0, "Canonical Harmony owner is empty.");
            AdvancedHarmonySnapshot before = Capture();
            if (!before.Available)
                return new AdvancedHarmonyUnpatchResult(false, 0, 0, "Pre-cleanup capture failed: " + before.Failure);
            int beforeCount = CountOwner(before, harmonyOwner);
            try
            {
                Type harmonyType = FindHarmonyType();
                MethodInfo? unpatch = harmonyType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .Where(method => method.Name.Equals("UnpatchAll", StringComparison.Ordinal))
                    .SingleOrDefault(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                    });
                if (unpatch == null)
                    return new AdvancedHarmonyUnpatchResult(false, 0, beforeCount, "Exact owner-scoped Harmony.UnpatchAll(string) is unavailable; global cleanup was refused.");
                object? instance = null;
                if (!unpatch.IsStatic)
                {
                    ConstructorInfo constructor = harmonyType.GetConstructor(new[] { typeof(string) })
                        ?? throw new MissingMethodException(harmonyType.FullName, ".ctor(string)");
                    instance = constructor.Invoke(new object[] { harmonyOwner });
                }
                unpatch.Invoke(instance, new object[] { harmonyOwner });
            }
            catch (Exception ex)
            {
                Exception failure = Unwrap(ex);
                return new AdvancedHarmonyUnpatchResult(false, 0, beforeCount, failure.GetType().Name + ": " + failure.Message);
            }

            AdvancedHarmonySnapshot after = Capture();
            if (!after.Available)
                return new AdvancedHarmonyUnpatchResult(false, 0, beforeCount, "Post-cleanup capture failed: " + after.Failure);
            int remaining = CountOwner(after, harmonyOwner);
            int removed = Math.Max(0, beforeCount - remaining);
            return new AdvancedHarmonyUnpatchResult(
                remaining == 0,
                removed,
                remaining,
                "Harmony owner-scoped cleanup owner=" + harmonyOwner + "; before=" + beforeCount.ToString(CultureInfo.InvariantCulture) +
                "; removed=" + removed.ToString(CultureInfo.InvariantCulture) + "; remaining=" + remaining.ToString(CultureInfo.InvariantCulture) + ".");
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
                    patchMethod == null ? string.Empty : ModuleMvid(patchMethod.Module)));
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

        private static Type FindHarmonyType()
        {
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType("HarmonyLib.Harmony", throwOnError: false, ignoreCase: false))
                .FirstOrDefault(candidate => candidate != null);
            return type ?? throw new TypeLoadException("HarmonyLib.Harmony is not loaded; Advanced Harmony supervision is unavailable.");
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
        private readonly Dictionary<string, AdvancedHarmonyOwnerState> states =
            new Dictionary<string, AdvancedHarmonyOwnerState>(StringComparer.OrdinalIgnoreCase);
        private IAdvancedHarmonyInspector inspector;
        private AdvancedHarmonySnapshot? lastAcceptedSnapshot;

        public AdvancedHarmonySupervisor(IAdvancedHarmonyInspector? inspector = null)
        {
            this.inspector = inspector ?? new ReflectionAdvancedHarmonyInspector();
        }

        public string ParticipantId => "DTMAPI.Core.AdvancedHarmonySupervisor";

        public void ConfigureInspectorForTests(IAdvancedHarmonyInspector value)
        {
            if (states.Count != 0)
                throw new InvalidOperationException("Advanced Harmony inspector cannot change after supervision begins.");
            inspector = value ?? throw new ArgumentNullException(nameof(value));
            lastAcceptedSnapshot = null;
        }

        public void BeginLoad(string ownerId, ManagedModClassification classification)
        {
            if (classification == null || !classification.IsAdvanced)
                return;
            if (states.ContainsKey(ownerId))
                throw Violation("advanced-harmony-owner-mismatch", "Advanced owner supervision already exists for " + ownerId + ".");
            AdvancedHarmonySnapshot before = RequireSnapshot();
            string expectedOwner = classification.ExpectedHarmonyOwner;
            if (classification.DeclarationProvenance == "sdk-native-contract-v1-verified"
                || classification.DeclarationProvenance == "sdk-native-contract-v2-verified")
                DTMAPI.Internal.Authoring.NativePackageContract.RequireOwner(ownerId, expectedOwner);
            else if (!expectedOwner.StartsWith("dtmapi.mod.", StringComparison.Ordinal) || expectedOwner.Length == "dtmapi.mod.".Length)
                throw Violation("advanced-harmony-owner-mismatch", "Canonical Harmony owner must use the non-empty dtmapi.mod.<uniqueid> namespace.");
            if (before.Patches.Any(patch => patch.Owner.Equals(expectedOwner, StringComparison.Ordinal)))
                throw Violation("advanced-harmony-owner-mismatch", "Canonical Harmony owner already has patches before Entry: " + expectedOwner + ".");
            states.Add(ownerId, new AdvancedHarmonyOwnerState(ownerId, expectedOwner, string.Empty, before));
            lastAcceptedSnapshot = before;
        }

        public void BindEntryAssembly(string ownerId, Assembly entryAssembly)
        {
            if (!states.TryGetValue(ownerId, out AdvancedHarmonyOwnerState state))
                throw Violation("advanced-harmony-owner-mismatch", "Advanced owner load supervision was not established before Assembly.LoadFrom for " + ownerId + ".");
            if (state.EntryModuleMvid.Length > 0)
                throw Violation("advanced-harmony-owner-mismatch", "Advanced entry assembly was already bound for " + ownerId + ".");
            try
            {
                state.EntryModuleMvid = entryAssembly.ManifestModule.ModuleVersionId.ToString("D", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw Violation("advanced-entry-identity-mismatch", "Advanced entry Module MVID could not be read after load: " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        public void BeginEntry(string ownerId, ManagedModClassification classification, Assembly entryAssembly)
        {
            BeginLoad(ownerId, classification);
            BindEntryAssembly(ownerId, entryAssembly);
        }

        public void CompleteEntry(string ownerId)
        {
            if (!states.TryGetValue(ownerId, out AdvancedHarmonyOwnerState state))
                return;
            if (state.EntryModuleMvid.Length == 0)
                throw Violation("advanced-entry-identity-mismatch", "Advanced entry assembly was not bound before Entry completion for " + ownerId + ".");
            AdvancedHarmonySnapshot after = RequireSnapshot();
            IReadOnlyList<AdvancedHarmonyPatchRecord> additions = Added(state.EntrySnapshot, after);
            state.EntryCompleted = true;
            state.LastObservedSnapshot = after;
            TrackUnowned(state, additions);

            AdvancedHarmonyPatchRecord[] wrongOwner = additions
                .Where(patch => !patch.Owner.Equals(state.ExpectedHarmonyOwner, StringComparison.Ordinal))
                .ToArray();
            if (wrongOwner.Length > 0)
                throw Violation("advanced-harmony-owner-mismatch", "Entry added patch(es) outside canonical owner " + state.ExpectedHarmonyOwner + ": " + Format(wrongOwner) + ".");

            string[] duplicates = additions
                .GroupBy(patch => patch.Key, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.First().Format())
                .ToArray();
            if (duplicates.Length > 0)
                throw Violation("advanced-harmony-duplicate-patch", "Entry added duplicate canonical patches: " + string.Join(" | ", duplicates) + ".");

            foreach (IGrouping<string, AdvancedHarmonyPatchRecord> group in additions.GroupBy(patch => patch.Key, StringComparer.Ordinal))
                state.AllowedReactivationPatchCounts[group.Key] = group.Count();
            lastAcceptedSnapshot = after;
        }

        public IReadOnlyList<AdvancedHarmonyAuditIssue> AuditActiveOwners()
        {
            AdvancedHarmonyOwnerState[] active = states.Values.Where(state => state.EntryCompleted).ToArray();
            if (active.Length == 0)
                return Array.Empty<AdvancedHarmonyAuditIssue>();
            AdvancedHarmonySnapshot current = inspector.Capture();
            if (!current.Available)
            {
                return active.Select(state => new AdvancedHarmonyAuditIssue(
                    state.OwnerId,
                    "advanced-harmony-unavailable",
                    "Harmony patch snapshot failed during late audit: " + current.Failure + "."))
                    .ToArray();
            }
            AdvancedHarmonySnapshot baseline = lastAcceptedSnapshot ?? current;
            IReadOnlyList<AdvancedHarmonyPatchRecord> additions = Added(baseline, current);
            lastAcceptedSnapshot = current;
            if (additions.Count == 0)
                return Array.Empty<AdvancedHarmonyAuditIssue>();
            Dictionary<string, int> currentPatchCounts = current.Patches
                .GroupBy(patch => patch.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

            var byOwner = new Dictionary<string, List<AdvancedHarmonyPatchRecord>>(StringComparer.OrdinalIgnoreCase);
            foreach (AdvancedHarmonyPatchRecord patch in additions)
            {
                AdvancedHarmonyOwnerState? state = active.FirstOrDefault(candidate => patch.Owner.Equals(candidate.ExpectedHarmonyOwner, StringComparison.Ordinal));
                if (state == null && patch.PatchModuleMvid.Length > 0)
                {
                    AdvancedHarmonyOwnerState[] moduleOwners = active
                        .Where(candidate => candidate.EntryModuleMvid.Equals(patch.PatchModuleMvid, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (moduleOwners.Length == 1)
                        state = moduleOwners[0];
                }
                if (state == null)
                    // The snapshot is process-global. A patch that matches neither a
                    // canonical Advanced owner nor an Advanced entry-module MVID stays
                    // owned by its Platform/Strict/External sibling and is not an
                    // Advanced violation or cleanup candidate.
                    continue;
                if (patch.Owner.Equals(state.ExpectedHarmonyOwner, StringComparison.Ordinal) &&
                    state.AllowedReactivationPatchCounts.TryGetValue(patch.Key, out int allowedCount) &&
                    currentPatchCounts.TryGetValue(patch.Key, out int currentCount) &&
                    currentCount <= allowedCount)
                {
                    // ProductNative may remove its exact Entry-observed patch set while
                    // suspended at title and restore those same identities on save
                    // re-entry. This is a lifecycle reactivation, not a new late patch.
                    state.LastObservedSnapshot = current;
                    continue;
                }
                if (!byOwner.TryGetValue(state.OwnerId, out List<AdvancedHarmonyPatchRecord>? ownerPatches))
                {
                    ownerPatches = new List<AdvancedHarmonyPatchRecord>();
                    byOwner.Add(state.OwnerId, ownerPatches);
                }
                ownerPatches.Add(patch);
                TrackUnowned(state, new[] { patch });
                state.LastObservedSnapshot = current;
            }

            var issues = byOwner.Select(pair => new AdvancedHarmonyAuditIssue(
                pair.Key,
                "advanced-harmony-late-owner-drift",
                "Advanced owner added Harmony patch(es) after Entry: " + Format(pair.Value) + "."))
                .ToList();
            return issues;
        }

        public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
        {
            if (!states.TryGetValue(ownerId, out AdvancedHarmonyOwnerState state))
                return ModOwnerCleanupParticipantResult.None("No Advanced Harmony supervision state for owner.");

            AdvancedHarmonySnapshot current = inspector.Capture();
            if (current.Available && !state.EntryCompleted)
            {
                IReadOnlyList<AdvancedHarmonyPatchRecord> additions = Added(state.EntrySnapshot, current);
                TrackUnowned(state, additions);
            }
            AdvancedHarmonyUnpatchResult cleanup = inspector.UnpatchOwner(state.ExpectedHarmonyOwner);
            AdvancedHarmonySnapshot after = inspector.Capture();
            int unownedRemaining = after.Available
                ? CountKeys(after, state.UnownedPatchKeys)
                : state.UnownedPatchKeys.Count;
            int remaining = Math.Max(cleanup.Remaining, unownedRemaining);
            int failureCount = cleanup.Success && after.Available && remaining == 0 ? 0 : 1;
            string details = "code=" + (failureCount == 0 ? "advanced-harmony-cleanup-complete" : "advanced-harmony-cleanup-failed") +
                "; expectedOwner=" + state.ExpectedHarmonyOwner +
                "; unownedObserved=" + state.UnownedPatchKeys.Count.ToString(CultureInfo.InvariantCulture) +
                "; unownedRemaining=" + unownedRemaining.ToString(CultureInfo.InvariantCulture) +
                "; capture=" + (after.Available ? "ok" : after.Failure) +
                "; restart=" + (failureCount == 0 ? "assembly-lifetime-policy" : "required-incomplete-harmony-cleanup") +
                "; " + cleanup.Details;
            if (failureCount == 0)
                states.Remove(ownerId);
            if (after.Available)
                lastAcceptedSnapshot = after;
            return new ModOwnerCleanupParticipantResult(cleanup.Removed, remaining, failureCount, details);
        }

        private AdvancedHarmonySnapshot RequireSnapshot()
        {
            AdvancedHarmonySnapshot snapshot = inspector.Capture();
            if (!snapshot.Available)
                throw Violation("advanced-harmony-unavailable", "Harmony patch snapshot failed: " + snapshot.Failure);
            return snapshot;
        }

        private static IReadOnlyList<AdvancedHarmonyPatchRecord> Added(AdvancedHarmonySnapshot before, AdvancedHarmonySnapshot after)
        {
            var remaining = before.Patches
                .GroupBy(patch => patch.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            var additions = new List<AdvancedHarmonyPatchRecord>();
            foreach (AdvancedHarmonyPatchRecord patch in after.Patches)
            {
                if (remaining.TryGetValue(patch.Key, out int count) && count > 0)
                {
                    remaining[patch.Key] = count - 1;
                    continue;
                }
                additions.Add(patch);
            }
            return additions;
        }

        private static void TrackUnowned(AdvancedHarmonyOwnerState state, IEnumerable<AdvancedHarmonyPatchRecord> additions)
        {
            foreach (AdvancedHarmonyPatchRecord patch in additions)
            {
                if (!patch.Owner.Equals(state.ExpectedHarmonyOwner, StringComparison.Ordinal))
                    state.UnownedPatchKeys.Add(patch.Key);
            }
        }

        private static int CountKeys(AdvancedHarmonySnapshot snapshot, IReadOnlyCollection<string> keys)
        {
            if (keys.Count == 0)
                return 0;
            var lookup = new HashSet<string>(keys, StringComparer.Ordinal);
            return snapshot.Patches.Count(patch => lookup.Contains(patch.Key));
        }

        private static string Format(IEnumerable<AdvancedHarmonyPatchRecord> patches) =>
            string.Join(" | ", patches.Take(16).Select(patch => patch.Format()));

        private static AdvancedHarmonyViolationException Violation(string code, string message) =>
            new AdvancedHarmonyViolationException(code, message);

        private sealed class AdvancedHarmonyOwnerState
        {
            public AdvancedHarmonyOwnerState(string ownerId, string expectedHarmonyOwner, string entryModuleMvid, AdvancedHarmonySnapshot entrySnapshot)
            {
                OwnerId = ownerId;
                ExpectedHarmonyOwner = expectedHarmonyOwner;
                EntryModuleMvid = entryModuleMvid;
                EntrySnapshot = entrySnapshot;
                LastObservedSnapshot = entrySnapshot;
            }

            public string OwnerId { get; }
            public string ExpectedHarmonyOwner { get; }
            public string EntryModuleMvid { get; set; }
            public AdvancedHarmonySnapshot EntrySnapshot { get; }
            public AdvancedHarmonySnapshot LastObservedSnapshot { get; set; }
            public bool EntryCompleted { get; set; }
            public Dictionary<string, int> AllowedReactivationPatchCounts { get; } =
                new Dictionary<string, int>(StringComparer.Ordinal);
            public HashSet<string> UnownedPatchKeys { get; } = new HashSet<string>(StringComparer.Ordinal);
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
