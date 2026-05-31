using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DolocTown.UI;

public class BackpackBottomPanel : InventoryPanel
{
	[SerializeField]
	private DolocNavigationButton destroyBtn;

	private Sprite normalDestroySprite;

	private Sprite highlightedDestroySprite;

	private Sprite selectedDestroySprite;

	private Sprite pressedDestroySprite;

	private Sprite disabledDestroySprite;

	private Action onDestroyItem;

	protected override void __Init()
	{
		base.__Init();
		destroyBtn.Init();
		normalDestroySprite = destroyBtn.iconSprite;
		highlightedDestroySprite = destroyBtn.spriteState.highlightedSprite;
		selectedDestroySprite = destroyBtn.spriteState.selectedSprite;
		pressedDestroySprite = destroyBtn.spriteState.pressedSprite;
		disabledDestroySprite = destroyBtn.spriteState.disabledSprite;
		destroyBtn.gameObject.SetActive(value: false);
		destroyBtn.onPointerEnter.AddListener(delegate
		{
			destroyBtn.HoverTextSmall(base.staticTexts.UiTipDestroyItem);
		});
		destroyBtn.onPointerExit.AddListener(delegate
		{
			destroyBtn.HideHoverBox();
		});
		destroyBtn.onClick.AddListener(delegate
		{
			onDestroyItem?.Invoke();
		});
		destroyBtn.onSelect.AddListener(delegate
		{
			DolocAPI.HideItemBorder();
			destroyBtn.button.SetSpriteState(selectedDestroySprite);
			DolocAPI.uiSystem.inventoryMouse.HoverTo(destroyBtn.rectTransform);
		});
		SetCloseButtonVisible(value: true);
	}

	protected override int GetLineCapacity(int total)
	{
		return DolocAPI.GlobalParameter.InventoryLineCapacity;
	}

	public void SetDeleteCallback(Action onDestroyItem)
	{
		this.onDestroyItem = onDestroyItem;
		destroyBtn.gameObject.SetActive(onDestroyItem != null);
		if (base.slots.Count != 0)
		{
			ItemNavSlot itemNavSlot = base.slots.Last();
			ItemNavSlot itemNavSlot2 = base.slots.First();
			itemNavSlot.button.SetNavigationOnRight(itemNavSlot2.button);
			if (destroyBtn.gameObject.activeSelf)
			{
				itemNavSlot.button.SetNavigationOnRight(destroyBtn.button);
				destroyBtn.button.SetNavigation(null, null, itemNavSlot.button, itemNavSlot2.button);
			}
		}
	}

	public void DisableDestroyButton(bool value, bool disableSelect)
	{
		if (value)
		{
			destroyBtn.iconSprite = disabledDestroySprite;
			destroyBtn.button.SetSpriteState(disabledDestroySprite);
			return;
		}
		destroyBtn.iconSprite = normalDestroySprite;
		if (disableSelect)
		{
			destroyBtn.button.SetSpriteState(normalDestroySprite);
		}
		else
		{
			destroyBtn.button.SetSpriteState(highlightedDestroySprite, selectedDestroySprite, pressedDestroySprite, disabledDestroySprite);
		}
		destroyBtn.transition = Selectable.Transition.SpriteSwap;
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		SetTitle(base.staticTexts.InventoryPanelBackpackTitle);
	}

	protected override void OnStartHide()
	{
		base.OnStartHide();
		SetDeleteCallback(null);
		SetCloseButtonVisible(value: true);
		DisableDestroyButton(value: false, disableSelect: false);
	}
}
