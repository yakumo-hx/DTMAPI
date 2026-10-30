using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.UI;
using HarmonyLib;

namespace DTMAPI.MoreSaves
{
    internal sealed class MoreSavesPagingRuntime
    {
        internal const string HarmonyOwner = "DTMAPI.MoreSavesMod.Native";
        private static MoreSavesPagingRuntime? current;
        private readonly MoreSavesNativeRuntime capacity;
        private readonly Func<string, string, string> translate;
        private readonly IMonitor monitor;
        private readonly Harmony harmony = new Harmony(HarmonyOwner);
        private readonly List<MethodInfo> targets = new List<MethodInfo>();
        private readonly Dictionary<GameDataUiState, MoreSavesNativePage> pages = new Dictionary<GameDataUiState, MoreSavesNativePage>();
        private bool suspended;
        internal int InstalledPatchCount => targets.Count;

        internal MoreSavesPagingRuntime(MoreSavesNativeRuntime capacity, IMonitor monitor, Func<string, string, string> translate)
        { this.capacity = capacity; this.monitor = monitor; this.translate = translate; }

        internal bool OwnsNative => !suspended && capacity.LeaseHeld && capacity.NativeSlotCount == MoreSavesNativeRuntime.ExpandedSlotCount &&
            MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.ProductOwner);

        internal void Install()
        {
            if (current != null) throw new InvalidOperationException("MoreSaves paging already has an owner.");
            current = this;
            try
            {
                Patch("Show", nameof(Show));
                Patch("Hide", nameof(Close));
                Patch("Unregister", nameof(Close));
                Patch("OnDataSlotSelect", nameof(Select));
                Patch("get_currentSlot", nameof(CurrentSlot));
                Patch("OnUiUpdate", nameof(Update));
                Patch("OnConfirm", nameof(Confirm));
                Patch("OnDelete", nameof(Delete));
                Patch("OnDuplicate", nameof(Duplicate));
            }
            catch { Unpatch(); current = null; throw; }
        }

        private void Patch(string method, string callback)
        {
            MethodInfo target = AccessTools.DeclaredMethod(typeof(GameDataUiState), method) ?? throw new MissingMethodException(typeof(GameDataUiState).FullName, method);
            if (Harmony.GetPatchInfo(target)?.Owners.Contains(HarmonyOwner) == true)
                throw new InvalidOperationException("MoreSaves found an existing exact Hook owner.");
            targets.Add(target);
            harmony.Patch(target, prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(MoreSavesPagingRuntime), callback)));
        }

        internal void Suspend()
        {
            suspended = true;
            foreach (MoreSavesNativePage page in pages.Values) page.Suspend();
        }

        internal void RestoreAndDispose()
        {
            // Capacity restoration must succeed before the original panel sees the source again.
            foreach (MoreSavesNativePage page in pages.Values) page.RestoreNative();
            Unpatch();
            pages.Clear();
            if (ReferenceEquals(current, this)) current = null;
            monitor.Log("MoreSaves paging removed; original panel and exact Hook owner restored.");
        }

        private void Unpatch()
        {
            foreach (MethodInfo target in targets)
            {
                harmony.Unpatch(target, HarmonyPatchType.All, HarmonyOwner);
                if (Harmony.GetPatchInfo(target)?.Owners.Contains(HarmonyOwner) == true)
                    throw new InvalidOperationException("MoreSaves could not remove its exact Hook owner.");
            }
            targets.Clear();
        }

        private static MoreSavesNativePage? Find(GameDataUiState state) =>
            current != null && current.pages.TryGetValue(state, out MoreSavesNativePage page) && !page.Restoring ? page : null;

        private static bool Show(GameDataUiState __instance)
        {
            if (current == null || !current.OwnsNative) return true;
            MoreSavesNativePage? page = Find(__instance);
            if (page == null)
            {
                page = new MoreSavesNativePage(__instance, () => current != null && current.OwnsNative, current.translate, current.monitor);
                current.pages.Add(__instance, page);
            }
            page.Show();
            return false;
        }

        private static void Close(GameDataUiState __instance) => Find(__instance)?.Close();
        private static bool Select(GameDataUiState __instance, int index)
        { MoreSavesNativePage? page = Find(__instance); if (page == null) return true; page.Session.SelectCell(index); return false; }
        private static bool CurrentSlot(GameDataUiState __instance, ref GameDataSlot __result)
        { MoreSavesNativePage? page = Find(__instance); if (page == null) return true; __result = page.Panel.GetSlot(page.Session.Cell); return false; }
        private static bool Update(GameDataUiState __instance)
        { MoreSavesNativePage? page = Find(__instance); if (page == null) return true; page.UpdateInput(); return false; }
        private static bool Confirm(GameDataUiState __instance)
        { MoreSavesNativePage? page = Find(__instance); if (page == null) return true; page.Session.Confirm(); return false; }
        private static bool Delete(GameDataUiState __instance)
        { MoreSavesNativePage? page = Find(__instance); if (page == null) return true; page.Session.Delete(); return false; }
        private static bool Duplicate(GameDataUiState __instance)
        { MoreSavesNativePage? page = Find(__instance); if (page == null) return true; page.Session.Duplicate(); return false; }
    }
}
