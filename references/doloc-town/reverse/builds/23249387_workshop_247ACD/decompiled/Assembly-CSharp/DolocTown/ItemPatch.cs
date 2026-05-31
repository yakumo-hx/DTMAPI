using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemPatch : Item
{
	private ItemFunctionPatch FunctionPatch => (ItemFunctionPatch)base.proto.Function;

	public ItemPatch(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemPatch(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		Repair();
	}

	private void Repair()
	{
		if (DolocAPI.agent.IsCurrentStateSupportUseItem && TryGetSelectedBuilding(out var building, includeInhouseRoom: true) && !building.IsIntact && building.CanPatch)
		{
			DolocAPI.agent._Interact(delegate
			{
				float healingAmount = FunctionPatch.HealingAmount;
				building.Repair(healingAmount);
				CostSelf();
			});
		}
	}

	public override string GetExtraInfo1()
	{
		return DolocUtils.Format(DolocConfig.StaticTexts.ItemPatchValueFormat, FunctionPatch.HealingAmount.ToString("F0").Colored(DolocUiColor.TEXTCOLOR_STD));
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
		base.cellTip.CellTipValid = TryGetSelectedBuilding(out var building, includeInhouseRoom: true) && !building.IsIntact;
	}
}
