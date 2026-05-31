using DolocTown.Config;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemFertilizer : Item
{
	public ItemFunctionFertilizer func => base.proto.Function as ItemFunctionFertilizer;

	public ItemFertilizer(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemFertilizer(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Fertilizer();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Fertilizer();
	}

	private void Fertilizer()
	{
		if (!DolocAPI.agent.IsCurrentStateSupportInteract || func == null || !TryGetSelectedEquipment(out var equipment))
		{
			return;
		}
		if (func.IsTree)
		{
			PlantBasinTree tree = equipment as PlantBasinTree;
			if (tree == null)
			{
				return;
			}
			DolocAPI.agent._Interact(delegate
			{
				if (tree.Fertilizer(base.proto, func.Duration, func.Addition, shouldRender: true, shouldSendMessage: true))
				{
					CostSelf();
				}
				else
				{
					DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotFertilizer);
				}
			});
			return;
		}
		PlantBasin basin = equipment as PlantBasin;
		if (basin == null)
		{
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			if (basin.Fertilizer(func.Duration, func.Addition, base.proto, shouldRender: true, sendMessage: true))
			{
				CostSelf();
			}
			else
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrCannotFertilizer);
			}
		});
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		if (base.allowBuildEquipment)
		{
			ShowCellTip(new Vector2Int(0, 0), new Vector2Int(1, 1), flipWhenFaceLeft: true);
		}
	}

	protected override void OnQuickDeselect()
	{
		base.OnQuickDeselect();
		HideCellTip();
	}

	protected override void RefreshCellTip()
	{
		base.cellTip.CellTipValid = TryGetSelectedEquipment(out PlantBasin equipment) && equipment.IsPlanted;
	}
}
