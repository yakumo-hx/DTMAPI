using System;
using System.Reflection;
using DolocTown;
using DolocTown.GameData;
using DolocTown.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentHooks : IDisposable
    {
        private const string Owner = "DTMAPI.MoreEquipmentSlotsMod.Native";
        private readonly EquipmentGame game;
        private readonly Harmony harmony = new Harmony(Owner);
        private static EquipmentGame? current;

        internal EquipmentHooks(EquipmentGame game) => this.game = game;

        internal void Install()
        {
            current = game;
            Patch(typeof(AgentEquipmentManager), "ReloadParams", nameof(Reload), Type.EmptyTypes);
            Patch(typeof(AgentEquipmentManager), "TryGetShieldItem", nameof(Shield),
                new[] { typeof(IAgentEquipmentShieldItem).MakeByRefType() });
            Patch(typeof(AccessoriesBar), "RenderPassiveItems", nameof(Render), new[] { typeof(Sprite[]) });
            Patch(typeof(AccessoriesBar), "get_allSelectablesArray", nameof(Selectables), Type.EmptyTypes);
            Patch(typeof(AccessoriesBar), "ClearCallBack", nameof(Hide), Type.EmptyTypes);
        }

        private void Patch(Type type, string method, string callback, Type[] arguments)
        {
            MethodInfo target = AccessTools.Method(type, method, arguments) ??
                throw new MissingMethodException(type.FullName, method);
            harmony.Patch(target, postfix: new HarmonyMethod(typeof(EquipmentHooks), callback));
        }

        private static void Reload(AgentEquipmentManager __instance) =>
            current?.HandleGameCall(() => current.ApplyDefense(__instance));

        private static void Shield(ref IAgentEquipmentShieldItem __0, ref bool __result)
        {
            if (__result) return;
            IAgentEquipmentShieldItem? shield = current?.ExtraShield;
            if (shield == null) return;
            __0 = shield;
            __result = true;
        }

        private static void Render(AccessoriesBar __instance, Sprite[] __0) =>
            current?.HandleGameCall(() => current.Render(__instance, __0.Length));

        private static void Selectables(AccessoriesBar __instance, ref Selectable[] __result)
        {
            EquipmentGame? game = current;
            if (game == null) return;
            Selectable[] result = __result;
            game.HandleGameCall(() => result = game.Selectables(__instance, result));
            __result = result;
        }

        private static void Hide(AccessoriesBar __instance) =>
            current?.HandleGameCall(() => current.Hide(__instance));

        public void Dispose()
        {
            if (ReferenceEquals(current, game)) current = null;
            harmony.UnpatchSelf();
        }
    }
}
