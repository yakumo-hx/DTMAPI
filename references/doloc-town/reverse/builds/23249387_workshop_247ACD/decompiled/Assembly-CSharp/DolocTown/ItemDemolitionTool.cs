using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemDemolitionTool : Item
{
	public ItemDemolitionTool(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemDemolitionTool(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		Dismantle();
	}

	protected override void OnUseAsItem()
	{
		Dismantle();
	}

	private bool CheckCanDismantle(out Building building, bool useLog = true)
	{
		building = null;
		if (!DolocAPI.agent.IsCurrentStateSupportInteract)
		{
			return false;
		}
		if (!TryGetSelectedBuilding(out building))
		{
			return false;
		}
		if (building.proto.IsUnique && ((IBuildingHost)DolocAPI.CurrentRoom).CountBuilding(building.proto.Id) <= 1)
		{
			if (useLog)
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocUtils.Format(DolocConfig.StaticTexts.FarmbuilderErrBuildingCanNotRemove, building.Title));
			}
			return false;
		}
		return true;
	}

	private void Dismantle()
	{
		if (!CheckCanDismantle(out var building))
		{
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			building.Dismantle(delegate
			{
				CostSelf();
			});
		});
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		ShowCellTip(Vector2Int.zero, Vector2Int.one, flipWhenFaceLeft: true);
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = CheckCanDismantle(out var _, useLog: false);
	}
}
