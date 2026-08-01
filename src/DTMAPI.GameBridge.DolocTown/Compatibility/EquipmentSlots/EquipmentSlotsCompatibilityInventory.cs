using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        private IEnumerable<InventoryDebugItem> EnumerateInventoryDebugItems()
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = ReadStaticMember(dolocConfig, "Tables");
            object? itemTable = tables == null ? null : ReadMember(tables, "TbItem");
            object? dataList = itemTable == null ? null : ReadMember(itemTable, "DataList");
            foreach (object proto in EnumerateObjects(dataList))
            {
                string id = ReadStringMember(proto, "Id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                yield return new InventoryDebugItem
                {
                    Id = id,
                    DisplayName = FirstText(ReadStringMember(proto, "Title"), id)
                };
            }
        }
    }
}
