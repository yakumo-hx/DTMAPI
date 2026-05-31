using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class AnimalDetailInfo : DolocUiObject
{
	[SerializeField]
	private Image icon;

	[SerializeField]
	private Text title;

	[SerializeField]
	private Text count;

	[SerializeField]
	private Text outputText;

	[SerializeField]
	private CostItemViewer outputItemViewer;

	protected override void __Init()
	{
		base.__Init();
		outputItemViewer.Init();
	}

	public void Render(AnimalDetailData data)
	{
		SetSprite(icon, data.icon, autoSize: true);
		title.text = data.title;
		count.text = data.countText;
		outputText.text = base.staticTexts.CollectionPanelAnimalProductText;
		outputItemViewer.Render(data.itemNames, data.itemIcons, data.obtainedProduct);
	}
}
