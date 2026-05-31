using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class ItemCatalogPanel : DolocUIPanel, IScrollContentRect
{
	[SerializeField]
	public CategoryOption optionItem;

	[SerializeField]
	public ItemListViewer itemListViewer;

	[SerializeField]
	public ItemViewer itemViewer;

	public ScrollRect scrollRect => itemViewer.itemRecipeListViewer._scrollRect;

	public float moveDelta => itemViewer.itemRecipeListViewer.SlotHeight / scrollRect.content.rect.height;

	protected override void __Init()
	{
		base.__Init();
		optionItem.Init();
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		optionItem.Title = base.staticTexts.CollectionPanelItemLabel;
	}

	public void OnMove()
	{
		DolocAPI.HideHoverBox();
	}
}
