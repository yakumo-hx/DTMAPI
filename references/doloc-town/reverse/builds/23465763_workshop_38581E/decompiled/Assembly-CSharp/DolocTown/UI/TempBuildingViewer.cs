using System;
using System.Linq;

namespace DolocTown.UI;

public class TempBuildingViewer : InventoryPanel
{
	private Action<int> onClicked;

	private Action<int> onSelected;

	public override bool redoDisplayAnimation => false;

	private ClickableOperationTipInUI clickableOperationTip => (ClickableOperationTipInUI)operationTip;

	protected override int GetLineCapacity(int total)
	{
		return 2;
	}

	public void BindBuilderInventory(LinearInventory inventory, Action<int> onSelected)
	{
		this.onSelected = onSelected;
		SetSelectCallbacks(this.onSelected.Invoke);
		BindInventory(inventory, null, null);
	}

	public void SetThumbnailClickCallback(Action<int> onClick)
	{
		onClicked = onClick;
	}

	public void SetOperationTipClickCallback(Action onClick)
	{
		clickableOperationTip.SetClickCallback(onClick);
	}

	public void ExpandBuildingList()
	{
		SetClickCallbacks(delegate(int index)
		{
			if (DolocButtonComponent.latestClickType == ClickType.Mouse)
			{
				onSelected?.Invoke(index);
			}
		});
		SetCapacity(DolocAPI.GlobalParameter.TemporaryBuildingCount);
		onSlotRender = delegate(int i, Item item)
		{
			GetSlot(i).grayed = false;
		};
		RefreshView();
		Select(0);
		clickableOperationTip.SetTextKey(base.staticTexts.BuilderPanellFoldBuildingList, LocSprites.UI_POINTER_DOWN);
	}

	public void FoldBuildingList()
	{
		LoseFocus();
		SetClickCallbacks(delegate(int index)
		{
			if (DolocButtonComponent.latestClickType == ClickType.Mouse)
			{
				onClicked?.Invoke(index);
			}
		});
		SetCapacity(lineCapacity);
		onSlotRender = delegate(int i, Item item)
		{
			GetSlot(i).grayed = true;
		};
		RefreshView();
		base.selectedIndex = -1;
		clickableOperationTip.SetTextKey(base.staticTexts.BuilderPanelExpandBuildingList, LocSprites.UI_POINTER_UP);
	}

	public void PrevLine()
	{
		Select((base.selectedIndex - lineCapacity + base.totalCapacity) % base.totalCapacity);
	}

	public void NextLine()
	{
		Select((base.selectedIndex + lineCapacity) % base.totalCapacity);
	}

	public void MovePrev()
	{
		Select((base.selectedIndex - 1 + base.totalCapacity) % base.totalCapacity);
	}

	public void MoveNext()
	{
		Select((base.selectedIndex + 1) % base.totalCapacity);
	}

	protected override void OnSlotSelect(ItemNavSlot slot)
	{
		slot.GetItemBorder();
		DolocAPI.UIRaiseRoll();
		Item item = base.itemGetter?.Invoke(slot.index);
		if (item != null)
		{
			slot.HoverTextSmall(item.title.Colored(DolocUiColor.SLIENTCOLOR_BLUE), UIAlignmentType.TopMiddle, UIAlignmentType.BottomMiddle, autoFade: true);
		}
	}

	public override void BuildNavigation()
	{
	}

	public override ItemNavSlot GetSlot(int index)
	{
		if (index < 0 || index >= base.totalCapacity)
		{
			return base.slots.Last();
		}
		return base.GetSlot(index);
	}

	public override void RemoveCallbacks()
	{
		onClicked = null;
		onSelected = null;
		base.RemoveCallbacks();
		clickableOperationTip.SetClickCallback(null);
	}
}
