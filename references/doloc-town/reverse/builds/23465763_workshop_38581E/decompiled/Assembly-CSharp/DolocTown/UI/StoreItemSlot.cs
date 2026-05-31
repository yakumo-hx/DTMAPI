using DolocTown.Config;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class StoreItemSlot : DolocNavigationButton
{
	[SerializeField]
	private Text textTitle;

	[SerializeField]
	private Text textDescription;

	[SerializeField]
	private Text textPrice;

	[SerializeField]
	private Text textCount;

	[SerializeField]
	private Text textTip;

	[SerializeField]
	private Image buyback;

	[SerializeField]
	private Image subscript;

	public void Render(StoreItemData data)
	{
		base.iconSprite = data.icon;
		textTitle.text = data.title;
		textDescription.text = data.description;
		textPrice.text = data.price;
		textCount.text = (data.saleOut ? DolocConfig.StaticTexts.StoreSoldOutIcon : data.count);
		buyback.gameObject.SetActive(data.isBuyback);
		SetSprite(subscript, data.subscript);
		if (!data.tip.IsNullOrEmpty())
		{
			textTip.gameObject.SetActive(value: true);
			textTip.text = data.tip;
		}
		else
		{
			textTip.gameObject.SetActive(value: false);
		}
		base.grayed = data.saleOut;
	}
}
