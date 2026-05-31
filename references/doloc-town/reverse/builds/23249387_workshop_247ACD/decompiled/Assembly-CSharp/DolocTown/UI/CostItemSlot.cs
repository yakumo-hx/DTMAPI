using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public class CostItemSlot : DolocIconWithText, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private Text textHoldPlace;

	private string itemName;

	private bool _visible = true;

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			if (_visible != value)
			{
				canvasGroup.alpha = (value ? 1 : 0);
				canvasGroup.interactable = value;
				canvasGroup.blocksRaycasts = value;
				_visible = value;
			}
			itemName = string.Empty;
		}
	}

	public bool UseItemHoverBox { get; set; }

	public override void OnRecycle()
	{
		base.OnRecycle();
		itemName = string.Empty;
	}

	public void Render(string itemName, Sprite sprite, string countInfo)
	{
		this.itemName = itemName;
		base.iconSprite = sprite;
		base.iconColor = ((sprite == null) ? DolocColor.empty : Color.white);
		base.description = countInfo;
		textHoldPlace.text = countInfo;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (UseItemHoverBox)
		{
			if (itemName == "money")
			{
				this.HoverItemViewer(ItemData.ShowMoneyItemData());
				return;
			}
			Item item = DolocAPI.GenerateItem(itemName);
			this.HoverItemViewer(new ItemData(item));
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		DolocAPI.HideHoverBox();
	}
}
