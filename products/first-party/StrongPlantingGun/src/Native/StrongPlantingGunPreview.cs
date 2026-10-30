using System;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed partial class StrongPlantingGunNativeRuntime
    {
        internal bool RefreshMaterialPreview(object gun)
        {
            if (!callbackAttached) return true;
            // The native tip is shared. A retained callback from a previous gun or
            // save must not query an incomplete world or recolor another gun's tip.
            if (!IsCurrentPreviewGun(gun)) return false;
            object? tip = reflection.ReadMember(gun, "cellTip");
            try
            {
                object inventory = reflection.ReadMember(gun, "inventory") ??
                    throw new MissingMemberException("ItemFarmingGun.inventory");
                bool valid = false;
                // Native InitCellTip owns size, facing and anchor. Only the material
                // predicate changes, using the same live qualification as execution.
                foreach (object target in ReadUniqueEquipments(gun))
                {
                    if (FindMaterial(gun, inventory, target, ItemKind.None, out _, out _) == null) continue;
                    valid = true;
                    break;
                }
                if (tip == null || !reflection.TryWriteBool(tip, "CellTipValid", valid))
                    throw new MissingMemberException("ItemFarmingGun.cellTip.CellTipValid");
            }
            catch (Exception ex)
            {
                if (tip != null) reflection.TryWriteBool(tip, "CellTipValid", false);
                if (logGate.ShouldLog(true, "preview-error", false))
                    monitor.Log("StrongPlantingGun preview unavailable: " + ex,
                        global::DTMAPI.Abstractions.LogLevel.Error);
            }
            return false;
        }
    }
}
