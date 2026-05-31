using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class DishSlot : DolocNavigationButton
{
	[SerializeField]
	protected Text title;

	[SerializeField]
	private Text desc;

	[SerializeField]
	protected CanvasGroup contentCanvas;

	[SerializeField]
	protected CanvasGroup emptyCanvas;

	public bool isEmpty { get; private set; }

	protected override void __Init()
	{
		base.__Init();
		Clear();
	}

	private void SetSlotEmptyState(bool value)
	{
		base.grayed = value;
		contentCanvas.alpha = ((!value) ? 1 : 0);
		emptyCanvas.alpha = (value ? 0.3f : 0f);
		isEmpty = value;
	}

	public void Render(Item item)
	{
		if (item == null)
		{
			SetSlotEmptyState(value: true);
			return;
		}
		SetSlotEmptyState(value: false);
		base.iconSprite = item.uiSprite;
		SetText(title, item.title);
		SetText(desc, item.subType.Title);
	}

	public void Clear()
	{
		SetSlotEmptyState(value: true);
	}
}
