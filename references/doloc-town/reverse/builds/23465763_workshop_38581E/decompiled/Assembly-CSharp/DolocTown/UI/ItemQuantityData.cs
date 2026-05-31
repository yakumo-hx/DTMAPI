using DolocTown.Config;
using UnityEngine;

namespace DolocTown.UI;

public struct ItemQuantityData : IUIData
{
	public Sprite icon;

	public int currentCount;

	public string info;

	public bool notEmpty { get; }

	public ItemQuantityData(Sprite icon, string info, int currentCount = 1)
	{
		this = default(ItemQuantityData);
		if (!(icon == null))
		{
			notEmpty = true;
			this.icon = icon;
			this.currentCount = currentCount;
			this.info = info;
		}
	}

	public ItemQuantityData(Item item, int itemCount)
	{
		this = default(ItemQuantityData);
		icon = item.uiSprite;
		info = DolocConfig.StaticTexts.StoreQuantitySubmitCountInBack.Format(itemCount);
	}

	public ItemQuantityData Multiple(int n)
	{
		return new ItemQuantityData(icon, info, currentCount * n);
	}
}
