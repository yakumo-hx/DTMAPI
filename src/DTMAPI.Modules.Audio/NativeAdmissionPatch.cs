using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace DTMAPI.Modules.Audio
{
    // Redirect only the two existing, admitted native Posts. The native method
    // still owns the gate, bank readiness and registered-emitter resolution.
    internal static class NativeAdmissionPatch
    {
        // Label is outside the SDK's netstandard2.0 reference surface. Inspect
        // existing Harmony labels as opaque values; never create or rewire one.
        private static readonly FieldInfo LabelField = typeof(CodeInstruction).GetField("labels") ??
            throw new MissingFieldException("Harmony CodeInstruction.labels");
        private static IList Labels(CodeInstruction code) => (IList)LabelField.GetValue(code)!;
        internal static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions,
            MethodInfo occupy, MethodInfo post, MethodInfo replace)
        {
            var code = instructions.ToList();
            int[] calls = code.Select((c, i) => c.Calls(occupy) ? i : -1).Where(i => i >= 0).ToArray();
            if (calls.Length != 1) throw new InvalidOperationException("AudioNativeAdmissionUnsupported: gate count.");
            int gate = calls[0];
            bool Branch(CodeInstruction c) => c.opcode == OpCodes.Brtrue || c.opcode == OpCodes.Brtrue_S;
            if (gate + 3 >= code.Count || !Branch(code[gate + 1]) ||
                !code[gate + 2].IsLdarg(4) || !Branch(code[gate + 3]) ||
                code[gate + 1].operand == null || code[gate + 3].operand == null)
                throw new InvalidOperationException("AudioNativeAdmissionUnsupported: occupancy/ignoreOccupy branches.");
            int entry = code.FindIndex(c => Labels(c).Contains(code[gate + 1].operand));
            // HarmonyX assigns one label per branch, even when both branches
            // share a destination. Compare destinations, never label identity.
            if (entry < 0 || entry != code.FindIndex(c => Labels(c).Contains(code[gate + 3].operand)))
                throw new InvalidOperationException("AudioNativeAdmissionUnsupported: admission destinations.");
            // A rejected call returns false before either Post.
            if (entry <= gate + 4 || code[entry - 2].opcode != OpCodes.Ldc_I4_0 ||
                code[entry - 1].opcode != OpCodes.Ret || code.Take(entry + 1).Any(c => c.blocks.Count != 0))
                throw new InvalidOperationException("AudioNativeAdmissionUnsupported: rejection/exception boundary.");
            int[] posts = code.Select((c, i) => c.Calls(post) ? i : -1).Where(i => i >= 0).ToArray();
            if (posts.Length != 2 || posts.Any(i => i <= entry || i + 1 >= code.Count ||
                code[i + 1].opcode != OpCodes.Pop || Labels(code[i + 1]).Count != 0 || code[i + 1].blocks.Count != 0))
                throw new InvalidOperationException("AudioNativeAdmissionUnsupported: admitted Post/result shape.");
            foreach (int i in posts.Reverse())
            {
                // The original closure always supplies a callback wrapper, even
                // when the caller requested none. Pass the original argument to
                // distinguish that case; never swallow a requested callback.
                code[i].opcode = OpCodes.Ldarg_3; code[i].operand = null;
                code[i + 1].opcode = OpCodes.Call; code[i + 1].operand = replace;
            }
            return code;
        }
    }
}
