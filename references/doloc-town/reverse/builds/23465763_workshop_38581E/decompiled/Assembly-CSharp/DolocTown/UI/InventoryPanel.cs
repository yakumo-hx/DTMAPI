using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DolocTown.UI;

public abstract class InventoryPanel : DolocGridUI<ItemNavSlot>, INavPanel
{
	[SerializeField]
	private AutoSizeText title;

	protected Action<int, Item> onSlotRender;

	private LinearInventory inventory;

	protected override GameObject slotPrefab => LocPfbs.UI_ELEMENT_NAV_SLOT_ITEMICON;

	public Func<int, Item> itemGetter { get; private set; }

	public Func<Item, ItemData> itemDataConverter { get; private set; }

	public Selectable[] allSelectablesArray => GetAllSelectables();

	public int allSelectableCount => allSelectablesArray.Length;

	public override void Select(int index)
	{
		EventSystem.current?.SetSelectedGameObject(null);
		ItemNavSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.Select();
		}
	}

	protected override void OnSlotSelect(ItemNavSlot slot)
	{
		base.OnSlotSelect(slot);
		if (base.isRender)
		{
			slot.GetItemBorder();
			HoverItemViewerAt(slot);
			DolocAPI.UIRaiseRoll();
		}
	}

	protected override void OnSlotDeselect(ItemNavSlot slot)
	{
		base.OnSlotDeselect(slot);
		DolocAPI.HideHoverBox();
	}

	protected override void OnSlotPointerEnter(ItemNavSlot slot)
	{
		base.OnSlotPointerEnter(slot);
		HoverItemViewerAt(slot);
	}

	protected override void OnSlotPointerExit(ItemNavSlot slot)
	{
		base.OnSlotPointerExit(slot);
		DolocAPI.HideHoverBox();
	}

	protected abstract int GetLineCapacity(int total);

	protected void SetCapacity(int total)
	{
		int num = GetLineCapacity(total);
		SetCapacity(total, num);
	}

	protected ItemData ConvertItemData(Item item)
	{
		return itemDataConverter(item);
	}

	protected virtual ItemData ConvertItemData(int index)
	{
		Item arg = itemGetter?.Invoke(index);
		return itemDataConverter(arg);
	}

	public void BindInventory(LinearInventory inventory, Action<int, Item> onSlotRender = null)
	{
		BindInventory(inventory, (Item x) => new ItemData(x), onSlotRender);
	}

	public void BindInventory(LinearInventory inventory, Func<Item, ItemData> itemDataConverter, Action<int, Item> onSlotRender = null)
	{
		this.inventory = inventory;
		if (this.inventory == null)
		{
			itemGetter = (int _) => (Item)null;
			return;
		}
		itemGetter = inventory.Read;
		this.itemDataConverter = itemDataConverter ?? ((Func<Item, ItemData>)((Item x) => new ItemData(x)));
		SetCapacity(inventory.capacity);
		this.onSlotRender = onSlotRender;
		inventory.AddReceiver(Render);
		OnBindInventory();
	}

	protected virtual void OnBindInventory()
	{
	}

	public void RefreshView()
	{
		inventory?.InvokeAll(Render);
	}

	public void UnBindInventory()
	{
		itemGetter = null;
		onSlotRender = null;
		inventory?.RemoveReceiver(Render);
		inventory = null;
	}

	public virtual void Clear()
	{
		RemoveCallbacks();
		UnBindInventory();
		BuildNavigation();
		foreach (ItemNavSlot slot in base.slots)
		{
			slot.grayed = false;
			slot.highLighted = false;
		}
	}

	public void SetTitle(string text)
	{
		if (title != null)
		{
			title.text = text;
		}
	}

	protected virtual void Render(int index, Item item, bool isSlotLocked)
	{
		if (index < slotPool.ActiveCount)
		{
			onSlotRender?.Invoke(index, item);
			GetSlot(index).Render(item, isSlotLocked);
		}
	}

	public virtual void RaiseSpriteFadeUp(int index)
	{
		ItemNavSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.RaiseUiSpriteFadeUp();
		}
	}

	public virtual void RaiseSpriteFadeUp(int index, Sprite sprite)
	{
		ItemNavSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.RaiseUiSpriteFadeUp(sprite);
		}
	}

	public virtual void RaiseSpriteFadeDown(int index)
	{
		ItemNavSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.RaiseUiSpriteFadeDown();
		}
	}

	public virtual void RaiseSpriteFadeDown(int index, Sprite sprite)
	{
		ItemNavSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.RaiseUiSpriteFadeDown(sprite);
		}
	}

	protected override void OnFinishShow()
	{
		base.OnFinishShow();
		RebuildLayout();
	}

	protected override void OnStartHide()
	{
		DolocAPI.HideHoverBox();
		DolocAPI.HideItemBorder();
		base.OnStartHide();
	}

	public void HoverItemViewerAt(int index)
	{
		ItemNavSlot slot = GetSlot(index);
		if (slot != null)
		{
			slot.HoverItemViewer(ConvertItemData(index));
		}
	}

	private void HoverItemViewerAt(ItemNavSlot slot)
	{
		UIAlignmentType targetAnchor = UIAlignmentType.LeftTop;
		UIAlignmentType hoverPivot = UIAlignmentType.LeftBottom;
		if (DolocAPI.screenManager.screenSize.y - slot.positionY < 2f * slot.size.y)
		{
			targetAnchor = UIAlignmentType.LeftBottom;
			hoverPivot = UIAlignmentType.LeftTop;
		}
		slot.HoverItemViewer(ConvertItemData(slot.index), targetAnchor, hoverPivot);
	}

	protected virtual Selectable[] GetAllSelectables()
	{
		Selectable[] array = new Selectable[base.slotCount];
		int num = 0;
		foreach (ItemNavSlot slot in base.slots)
		{
			array[num++] = slot.button;
		}
		return array;
	}

	public override void LoseFocus()
	{
		base.LoseFocus();
		this.HideItemBorder();
	}
}
