using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;
using HarmonyLib;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class HarmonyModPatchCleanup : IModPatchCleanup
    {
        public ModOwnerCleanupParticipantResult RemovePatches(
            IReadOnlyCollection<Assembly> exclusive,
            IReadOnlyCollection<Assembly> shared,
            string ownerHint)
        {
            if (exclusive.Count == 0 && shared.Count == 0)
                return ModOwnerCleanupParticipantResult.None("No managed assembly was bound for this Mod.");

            var harmony = new Harmony("DTMAPI.Cleanup");
            int removed = 0;
            var failures = new List<string>();
            var initial = Harmony.GetAllPatchedMethods().ToArray()
                .SelectMany(target => ReadPatches(Harmony.GetPatchInfo(target))
                    .Select(p => (Target: target, p.Kind, p.Patch))).ToArray();
            var attributed = initial.Where(p => p.Patch.PatchMethod != null &&
                exclusive.Contains(p.Patch.PatchMethod.Module.Assembly)).ToArray();
            int remaining = initial.Count(p => p.Patch.PatchMethod != null && shared.Contains(p.Patch.PatchMethod.Module.Assembly) ||
                p.Patch.PatchMethod == null && ownerHint.Length != 0 && p.Patch.owner.Equals(ownerHint, StringComparison.Ordinal));
            foreach (var target in attributed.GroupBy(p => p.Target))
            {
                foreach (MethodInfo method in target.Select(p => p.Patch.PatchMethod).Distinct())
                {
                    // Unpatch(target, method) removes every use of that method.
                    // Preserve a contribution added while another patch was being rebuilt.
                    var current = ReadPatches(Harmony.GetPatchInfo(target.Key));
                    if (current.Any(p => Equals(p.Patch.PatchMethod, method) && !target.Any(a => SameContribution(p, (a.Kind, a.Patch)))))
                        continue;
                    try
                    {
                        // Rebuilding a patched method can call a Mod's transpiler.
                        harmony.Unpatch(target.Key, method);
                    }
                    catch (Exception error)
                    {
                        failures.Add(target.Key.DeclaringType?.FullName + "." + target.Key.Name + ": " + error.Message);
                    }
                }
            }
            // Rebuilding a later target may install a patch on an earlier one.
            // Check the complete initial contribution set after every unpatch has run.
            foreach (var target in attributed.GroupBy(p => p.Target))
            {
                var retained = ReadPatches(Harmony.GetPatchInfo(target.Key)).ToArray();
                int stillInstalled = target.Count(p => retained.Any(a => SameContribution((p.Kind, p.Patch), a)));
                removed += target.Count() - stillInstalled;
                remaining += stillInstalled;
            }
            int failed = remaining == 0 && failures.Count == 0 ? 0 : 1;
            return new ModOwnerCleanupParticipantResult(removed, remaining, failed,
                "Harmony patches removed=" + removed + "; remaining=" + remaining +
                (failed == 0 ? "." : "; restart required. " + string.Join("; ", failures)));
        }

        private static IEnumerable<(HarmonyPatchType Kind, Patch Patch)> ReadPatches(Patches? patches)
        {
            if (patches == null)
                yield break;
            foreach (Patch patch in patches.Prefixes) yield return (HarmonyPatchType.Prefix, patch);
            foreach (Patch patch in patches.Postfixes) yield return (HarmonyPatchType.Postfix, patch);
            foreach (Patch patch in patches.Transpilers) yield return (HarmonyPatchType.Transpiler, patch);
            foreach (Patch patch in patches.Finalizers) yield return (HarmonyPatchType.Finalizer, patch);
            foreach (Patch patch in patches.ILManipulators) yield return (HarmonyPatchType.ILManipulator, patch);
        }

        private static bool SameContribution((HarmonyPatchType Kind, Patch Patch) left, (HarmonyPatchType Kind, Patch Patch) right) =>
            left.Kind == right.Kind && left.Patch.owner == right.Patch.owner && Equals(left.Patch.PatchMethod, right.Patch.PatchMethod);
    }
}
