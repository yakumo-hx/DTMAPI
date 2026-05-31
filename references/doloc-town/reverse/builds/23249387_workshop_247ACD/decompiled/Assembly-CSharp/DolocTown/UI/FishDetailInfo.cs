using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class FishDetailInfo : DolocUiObject
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text count;

	[SerializeField]
	private Text desc;

	[SerializeField]
	private Text baitText;

	[SerializeField]
	private CostItemViewer baitItemViewer;

	protected override void __Init()
	{
		base.__Init();
		baitItemViewer.Init();
	}

	public void Render(FishDetailData data)
	{
		SetSprite(icon, data.icon, autoSize: true);
		title.text = data.title;
		count.text = data.countText;
		desc.text = data.descText;
		baitText.text = base.staticTexts.CollectionPanelFishBaitText;
		baitItemViewer.Render(data.itemNames, data.itemIcons);
	}
}
