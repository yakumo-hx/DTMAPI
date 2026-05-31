using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.Config.Equipment;
using DolocTown.Config.Recipe;
using UnityEngine;

namespace DolocTown.UI;

public struct TechNodeInfoData : IUIData
{
	public Sprite Icon { get; }

	public string Title { get; }

	public string Description { get; }

	public string Comment { get; }

	public string CostInfo { get; }

	public bool notEmpty { get; }

	public TechNodeInfoData(EquipmentInfo proto)
	{
		this = default(TechNodeInfoData);
		notEmpty = true;
		CostInfo = (Comment = (Description = (Title = "<color=red>设备信息丢失</color>")));
		if (proto != null)
		{
			Icon = proto.UiSprite;
			Title = proto.Title;
			Description = proto.Description;
			Comment = (proto.IsAppliance ? DolocConfig.StaticTexts.TechtreeNodeEquipmentElectronic : string.Empty);
			string[] titles = proto.CostList.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
			int[] counts = proto.CostList.Select((CountItem x) => x.itemCount).ToArray();
			CostInfo = DolocUtils.GenerateCountItemInfo(titles, counts, ", ", useWrapSpace: true);
		}
	}

	public TechNodeInfoData(BuildingInfo proto)
	{
		this = default(TechNodeInfoData);
		notEmpty = true;
		if (proto == null)
		{
			CostInfo = (Comment = (Description = (Title = "<color=red>建筑信息丢失</color>")));
			return;
		}
		Icon = proto.UiSpriteAsset.Asset;
		Title = proto.Title;
		Description = proto.Description;
		Comment = string.Format(DolocConfig.StaticTexts.TechtreeNodeBuildingHealth, proto.Health);
		string[] titles = proto.ItemCost.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
		int[] counts = proto.ItemCost.Select((CountItem x) => x.itemCount).ToArray();
		CostInfo = DolocUtils.GenerateCountItemInfo(titles, counts, ", ", useWrapSpace: true);
	}

	public TechNodeInfoData(string recipe)
	{
		this = default(TechNodeInfoData);
		notEmpty = true;
		CostInfo = (Comment = (Description = (Title = "<color=red>配方信息丢失</color>")));
		RecipeInfo orDefault = DolocConfig.Tables.TbRecipe.GetOrDefault(recipe);
		if (orDefault == null)
		{
			return;
		}
		Title = orDefault.RecipeTitle;
		Item item = DolocAPI.GenerateItem(orDefault.OutputItem.itemName);
		if (item != null)
		{
			Icon = item.uiSprite;
			Description = item.description;
			Comment = string.Empty;
			string[] titles = orDefault.InputItems.Select((CountItem x) => DolocAPI.GetItemTitle(x.itemName)).ToArray();
			int[] counts = orDefault.InputItems.Select((CountItem x) => x.itemCount).ToArray();
			CostInfo = DolocUtils.GenerateCountItemInfo(titles, counts, ", ", useWrapSpace: true);
		}
	}
}
