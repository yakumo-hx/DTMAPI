using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DTMAPI.Modules.Audio
{
    internal static class NativeMusicPatch
    {
        internal static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, MethodInfo nativePost, MethodInfo requestPost)
        {
            var code = instructions.ToList();
            var calls = code.Where(c => c.Calls(nativePost)).ToArray();
            if (calls.Length != 1) throw new InvalidOperationException("AudioMusicRequestSignatureUnsupported: expected one native music Post.");
            calls[0].opcode = System.Reflection.Emit.OpCodes.Call; calls[0].operand = requestPost;
            return code;
        }
    }
}
