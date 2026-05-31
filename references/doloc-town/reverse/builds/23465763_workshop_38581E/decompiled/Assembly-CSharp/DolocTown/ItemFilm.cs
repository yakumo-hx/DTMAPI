using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.TechTree;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemFilm : Item
{
	public int HealingAmount => ((ItemFunctionFilm)base.proto.Function).HealingAmount;

	public ItemFilm(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemFilm(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		base.OnUseAsTool();
		Protect();
	}

	protected override void OnUseAsItem()
	{
		base.OnUseAsItem();
		Protect();
	}

	private void Protect()
	{
		if (!DolocAPI.agent.IsCurrentStateSupportInteract || !TryGetSelectedEquipment(out PlantBasin basin))
		{
			return;
		}
		DolocAPI.agent._Interact(delegate
		{
			if (!basin.Protect(this, isRender: true))
			{
				DolocAPI.ShowMessageBoxSmallErr(DolocConfig.StaticTexts.UiOperationErrFullPlasticFilm);
			}
			else
			{
				CostSelf();
				DolocAPI.AddTechExp(TechPointType.NATURE, 1);
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
		base.cellTip.CellTipValid = TryGetSelectedEquipment(out PlantBasin _);
	}

	public override string GetExtraInfo1()
	{
		return DolocConfig.StaticTexts.ItemFilmValueFormat.Format(HealingAmount);
	}

	public override string GetDetailInfo()
	{
		return string.Empty;
	}
}
