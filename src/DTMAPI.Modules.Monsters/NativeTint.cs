using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly IReadOnlyList<string> TintOverrideFields = Array.AsReadOnly(new[] { "defense", "hurtDuration", "tint" });
        private sealed class NativeTintState
        {
            internal SpriteRenderer Renderer = null!;
            internal Color Original, Applied;
        }
        private readonly Dictionary<string, string> nativeTints = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<MonsterController, NativeTintState> nativeTintStates = new Dictionary<MonsterController, NativeTintState>();

        private static bool CanTintNative(Template template) => template.Prefab != null && template.Prefab.GetComponent<SpriteRenderer>() != null;

        private void RefreshNativeTints()
        {
            var desired = nativeOverrides.TintTargets.ToDictionary(id => id, id => nativeOverrides.ResolveTint(id)!, StringComparer.Ordinal);
            // Diagnostic reads rebuild arbitration too. Only a changed effective tint needs
            // a scene scan for already-running native monsters; there is no frame polling.
            if (desired.Count == nativeTints.Count && desired.All(p => nativeTints.TryGetValue(p.Key, out var value) && value == p.Value)) return;
            ClearNativeTints();
            nativeTints.Clear();
            foreach (var pair in desired) nativeTints.Add(pair.Key, pair.Value);
            if (nativeTints.Count == 0) return;
            foreach (var controller in Object.FindObjectsOfType<MonsterController>()) ApplyNativeTint(controller);
        }

        private void ApplyNativeTint(MonsterController controller)
        {
            if (!Ready || controller == null || controller.Monster == null || nativeTintStates.ContainsKey(controller)) return;
            string id = controller.Monster.proto.Name;
            // A new definition has its own native identity even when it shares this template.
            if (bindings.ContainsKey(id) || !nativeTints.TryGetValue(id, out var text)) return;
            var renderer = controller.Renderer?.SpriteRenderer;
            if (renderer == null || !ColorUtility.TryParseHtmlString(text, out var tint)) return;
            var state = new NativeTintState { Renderer = renderer, Original = renderer.color };
            state.Applied = state.Original * tint;
            nativeTintStates.Add(controller, state);
            renderer.color = state.Applied;
        }

        private void RestoreNativeTint(MonsterController controller)
        {
            if (!nativeTintStates.TryGetValue(controller, out var state)) return;
            // Do not overwrite a later direct renderer write by another system.
            if (state.Renderer != null && state.Renderer.color == state.Applied) state.Renderer.color = state.Original;
            nativeTintStates.Remove(controller);
        }

        private void ClearNativeTints()
        {
            foreach (var controller in nativeTintStates.Keys.ToArray()) RestoreNativeTint(controller);
        }
    }
}
