using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public struct ItemData : IUIData
{
	public string title;

	public string type;

	public string description;

	public string info1;

	public string info2;

	public string price;

	public EffectGroupData effectGroup;

	public bool notEmpty { get; }

	public static ItemData ShowAsGood(string itemName)
	{
		Item item = DolocAPI.GenerateItem(itemName);
		if (item == null)
		{
			return default(ItemData);
		}
		ItemData itemData = new ItemData(item);
		itemData.description = "";
		ItemData result = itemData;
		if (!result.info1.IsNullOrEmpty() || !result.info2.IsNullOrEmpty() || result.effectGroup.notEmpty)
		{
			return result;
		}
		return default(ItemData);
	}

	public static ItemData ShowWithPrice(Item item, int unitPrice, float priceScale)
	{
		if (item == null)
		{
			return default(ItemData);
		}
		string text;
		if (!item.salable)
		{
			text = DolocConfig.StaticTexts.StoreItemNotSaleableComment.Colored(DolocUiColor.SLIENTCOLOR_RED);
		}
		else
		{
			text = DolocConfig.StaticTexts.ItemTipPrice.Colored(DolocUiColor.TEXTCOLOR_STD) + unitPrice.ToString().Colored(DolocUiColor.EYECATCHCOLOR_CYAN) + DolocConfig.StaticTexts.ItemTipMoneyUnit.Colored(DolocUiColor.TEXTCOLOR_STD);
			if (!Mathf.Approximately(priceScale, 1f) && priceScale > 0f)
			{
				text = ((priceScale < 0.4f) ? (text + "↓↓↓".Colored(DolocUiColor.SLIENTCOLOR_RED)) : ((priceScale < 0.6f) ? (text + "↓↓".Colored(DolocUiColor.SLIENTCOLOR_RED)) : ((priceScale < 1f) ? (text + "↓".Colored(DolocUiColor.SLIENTCOLOR_RED)) : ((priceScale < 1.3f) ? (text + "↑".Colored(DolocUiColor.SLIENTCOLOR_GREEN)) : ((!(priceScale < 1.6f)) ? (text + "↑↑↑".Colored(DolocUiColor.SLIENTCOLOR_GREEN)) : (text + "↑↑".Colored(DolocUiColor.SLIENTCOLOR_GREEN)))))));
			}
		}
		ItemData result = new ItemData(item);
		result.price = text;
		return result;
	}

	public static ItemData ShowMoneyItemData()
	{
		ItemData result = new ItemData(notEmpty: true);
		result.title = DolocConfig.StaticTexts.ItemMoneyTitle;
		result.description = DolocConfig.StaticTexts.ItemMoneyDesc;
		result.type = DolocConfig.StaticTexts.ItemMoneyType;
		return result;
	}

	public ItemData(Item item)
	{
		this = default(ItemData);
		if (item != null)
		{
			notEmpty = true;
			title = item.title;
			description = item.description;
			type = item.subType.Title;
			info1 = item.GetExtraInfo1();
			info2 = item.GetExtraInfo2();
			effectGroup = new EffectGroupData(item);
		}
	}

	private ItemData(bool notEmpty)
	{
		this = default(ItemData);
		this.notEmpty = notEmpty;
	}
}
