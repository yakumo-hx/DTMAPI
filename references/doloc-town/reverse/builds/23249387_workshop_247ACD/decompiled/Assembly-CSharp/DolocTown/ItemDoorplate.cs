using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDoorplate : Item
{
	public ItemDoorplate(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemDoorplate(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		TryChangeBuildingName();
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		TryChangeBuildingName();
	}

	private void TryChangeBuildingName()
	{
		if (!DolocAPI.agent.IsCurrentStateSupportUseItem)
		{
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			if (!TryGetSelectedBuilding(out var building, includeInhouseRoom: true))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrNoBuildingHere);
			}
			else
			{
				building.InvokeChangeNameInputBox(delegate
				{
					CostSelf();
				});
			}
		});
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		if (DolocAPI.IsPlayerInFarmScene)
		{
			ShowCellTip(Vector2Int.zero, Vector2Int.one, flipWhenFaceLeft: true);
		}
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = TryGetSelectedBuilding(out var _, includeInhouseRoom: true);
	}
}
