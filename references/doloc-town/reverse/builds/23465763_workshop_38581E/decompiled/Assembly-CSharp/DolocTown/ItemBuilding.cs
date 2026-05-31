using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.Config.Item;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class ItemBuilding : Item
{
	private BuildingInfo _buildingProto;

	private TbBuilding configs => DolocConfig.Tables.TbBuilding;

	private BuildingItemBuilderTip BuilderTip => DolocAPI.dolocBuilder.GetBuilderTip<BuildingItemBuilderTip>();

	public BuildingInfo BuildingProto => _buildingProto ?? (_buildingProto = configs.GetOrDefault(base.proto?.Id ?? ""));

	public Building buildingEntity { get; set; }

	public override bool noOverlay
	{
		get
		{
			if (!base.noOverlay)
			{
				return buildingEntity != null;
			}
			return true;
		}
	}

	public override string title
	{
		get
		{
			if (buildingEntity != null)
			{
				return buildingEntity.Title;
			}
			return base.title;
		}
	}

	public override Sprite uiSprite
	{
		get
		{
			if (buildingEntity != null)
			{
				return buildingEntity.UiSprite;
			}
			return base.uiSprite;
		}
	}

	public override string description => BuildingProto?.Description ?? base.description;

	public ItemBuilding(ItemInfo proto, int count)
		: base(proto, count)
	{
	}

	[JsonConstructor]
	protected ItemBuilding(string itemName, int itemCount)
		: base(itemName, itemCount)
	{
	}

	protected override void OnUseAsTool()
	{
		if (BuilderTip.ConfirmBuild())
		{
			DolocAPI.ReQuickSelectCurrentItem();
			DolocAPI.RefreshScanner();
		}
	}

	protected override void OnUseAsItem()
	{
		BuilderTip.TurnIndicator();
	}

	protected override void OnQuickSelect()
	{
		base.OnQuickSelect();
		BuilderTip.RunBuilder(this);
	}

	protected override void OnQuickDeselect()
	{
		BuilderTip.ExitBuilder();
		base.OnQuickDeselect();
	}

	protected override int GetSellingPrice()
	{
		if (base.proto.SellingPrice >= 0 || BuildingProto == null)
		{
			return Mathf.Max(0, base.proto.SellingPrice);
		}
		int num = 0;
		CountItem[] itemCost = BuildingProto.ItemCost;
		for (int i = 0; i < itemCost.Length; i++)
		{
			CountItem countItem = itemCost[i];
			DolocAPI.QueryItemProto(countItem.itemName, out var itemInfo);
			num += itemInfo.SellingPrice * countItem.itemCount;
		}
		return Mathf.Max(0, num);
	}

	public override string GetExtraInfo1()
	{
		return base.GetExtraInfo1();
	}

	public override string GetExtraInfo2()
	{
		return base.description;
	}
}
