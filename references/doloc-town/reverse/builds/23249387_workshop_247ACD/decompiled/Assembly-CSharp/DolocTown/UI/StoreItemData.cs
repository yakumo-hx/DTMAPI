using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Equipment;
using DolocTown.Config.Item;
using DolocTown.Config.Plant;
using UnityEngine;

namespace DolocTown.UI;

public class StoreItemData : IUIData
{
	public bool isBuyback;

	public Sprite icon;

	public Sprite subscript;

	public string title;

	public string description;

	public string price;

	public string count;

	public string tip;

	public bool saleOut;

	public bool notEmpty { get; }

	public StoreItemData(StoreItemRef storeItemRef, Store store)
	{
		Item item = DolocAPI.GenerateItem(storeItemRef.itemName);
		if (item != null)
		{
			notEmpty = true;
			isBuyback = storeItemRef.isBuyback;
			icon = item.uiSprite;
			subscript = ((item is IHasSubscript hasSubscript) ? hasSubscript.SubscriptSprite : null);
			title = item.title;
			description = item.description;
			price = DolocConfig.StaticTexts.UiMoneyTip.Format(store.GetItemUnitBuyingPrice(storeItemRef.itemName, isBuyback));
			int currentCount = store.GetCurrentCount(storeItemRef);
			count = currentCount.ToString();
			saleOut = currentCount == 0;
			tip = GetTip(item.proto);
		}
	}

	private static string GetTip(ItemInfo itemProto)
	{
		if (itemProto.Function is ItemFunctionSeed)
		{
			SeedInfo orDefault = DolocConfig.Tables.TbSeed.GetOrDefault(itemProto.Id);
			if (orDefault != null)
			{
				foreach (string item in DolocAPI.archiveHandle.farmData.recipeManager.UnlockedRecipes.ToList())
				{
					DolocAPI.QueryEquipment(item, out var proto);
					if (proto?.Function is EquipmentFuncPlantBasinBase equipmentFuncPlantBasinBase && equipmentFuncPlantBasinBase.SeedType == orDefault.SeedType)
					{
						return "";
					}
				}
				return DolocConfig.StaticTexts.StoreItemTipPlantbasinLocked;
			}
		}
		return "";
	}
}
