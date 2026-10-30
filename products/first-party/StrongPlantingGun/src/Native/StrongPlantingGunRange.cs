using System;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed partial class StrongPlantingGunNativeRuntime
    {
        internal int Range { get; private set; } = StrongPlantingGunConfig.DefaultRange;
        private bool rangeTipTouched;

        internal void ApplyRange(double value)
        {
            int next = StrongPlantingGunConfig.Normalize(value);
            if (next == Range) return;
            CancelToolBatch("range changed", refreshTip: false);
            Range = next;
            RefreshCurrentRangePreview();
        }

        private bool IsCurrentPreviewGun(object gun) =>
            callbackAttached && batchContext?.Snapshot.IsWorldReady == true &&
            ReferenceEquals(NativeValue("SelectedItem"), gun) &&
            NativeValue("CurrentRoom") != null && NativeValue("archiveHandle") != null &&
            NativeValue("agent") != null;

        internal bool CanInitializePreview(object gun) => !callbackAttached || IsCurrentPreviewGun(gun);

        // InitCellTip remains the native owner of Offset, facing and Show. Only
        // its one Area read is adapted; the shared prototype is never written.
        internal object GetPreviewArea(object function)
        {
            object native = reflection.ReadMember(function, "Area") ?? throw new MissingMemberException("ItemFunctionFarmingGun.Area");
            rangeTipTouched = true;
            return Activator.CreateInstance(native.GetType(), Range, Range)!;
        }

        private void ObservePreviewContext()
        {
            ObserveToolBatch();
            if (batchContext?.Snapshot.IsWorldReady == true) RefreshCurrentRangePreview();
            else HideOwnedRangePreview();
        }

        private void RefreshCurrentRangePreview()
        {
            object? selected = NativeValue("SelectedItem");
            if (selected != null && IsTypeOrBase(selected.GetType(), "DolocTown.ItemFarmingGun") && IsCurrentPreviewGun(selected))
                InvokeNoArg(selected, "InitCellTip");
        }

        private void HideOwnedRangePreview()
        {
            if (!rangeTipTouched) return;
            object? ui = NativeValue("uiSystem");
            object? basic = ui == null ? null : reflection.ReadMember(ui, "basicTip");
            object? tip = basic == null ? null : reflection.ReadMember(basic, "AgentCellTip");
            object? target = tip == null ? null : (reflection.ReadMember(tip, "updateCallBack") as Action)?.Target;
            if (target != null && IsTypeOrBase(target.GetType(), "DolocTown.ItemFarmingGun"))
                InvokeNoArg(target, "HideCellTip");
        }

        private void RestoreRangePreview()
        {
            if (!rangeTipTouched) return;
            HideOwnedRangePreview();
            object? selected = NativeValue("SelectedItem");
            // Called after exact-owner unpatch. Native Init now reads its original
            // Area, restoring the selected gun without recoloring another item.
            if (batchContext?.Snapshot.IsWorldReady == true && NativeValue("CurrentRoom") != null &&
                NativeValue("archiveHandle") != null && NativeValue("agent") != null &&
                selected != null && IsTypeOrBase(selected.GetType(), "DolocTown.ItemFarmingGun"))
                InvokeNoArg(selected, "InitCellTip");
            rangeTipTouched = false;
        }
    }
}
