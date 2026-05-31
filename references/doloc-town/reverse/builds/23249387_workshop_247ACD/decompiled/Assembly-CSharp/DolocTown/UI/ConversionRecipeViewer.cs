using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ConversionRecipeViewer : DolocUiObject
{
	[SerializeField]
	protected Text title;

	[SerializeField]
	public ItemNavSlot targetItem;

	[SerializeField]
	private Text timeInfo;

	[SerializeField]
	private Text existCount;

	private ConversionRecipeData recipeData;

	protected override void __Init()
	{
		base.__Init();
		targetItem.Init();
		targetItem.RenderItem();
		targetItem.onPointerEnter.AddListener(delegate
		{
			targetItem.HoverItemViewer(recipeData.outputItemData);
		});
		targetItem.onPointerExit.AddListener(delegate
		{
			targetItem.HideHoverBox();
		});
		targetItem.onSelect.AddListener(delegate
		{
			targetItem.HoverItemViewer(recipeData.outputItemData);
			targetItem.GetItemBorder();
		});
		targetItem.onDeselect.AddListener(delegate
		{
			targetItem.HideHoverBox();
		});
	}

	public void Render(ConversionRecipeData data)
	{
		recipeData = data;
		if (data.notEmpty && !(targetItem == null))
		{
			title.text = data.outputItemTitle;
			targetItem.RenderItem(data.outputItemSprite, data.outputCount);
			timeInfo.text = data.costTimeInfo;
			existCount.text = data.existItemInfo;
		}
	}
}
