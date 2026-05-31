using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace DolocTown.UI;

public class QuickInventoryPanel : InventoryPanel
{
	[SerializeField]
	private ItemNavSlot droneItem;

	[SerializeField]
	private ItemNavSlot positiveItem;

	private const int DroneSlotIndex = -2;

	private const int PositiveSlotIndex = -1;

	private List<int> navCache = new List<int>();

	private int offset;

	private Action<int> onLeftClicked;

	private Action<int> onRightClicked;

	private Action<int> onSelected;

	private Action<int> onDeselect;

	private int latestSelect;

	private int currentSelect;

	public override bool redoDisplayAnimation => false;

	private int inventoryLineCapacity => DolocAPI.GlobalParameter.InventoryLineCapacity;

	private AgentEquipmentManager agentEquipment => DolocAPI.archiveHandle.farmData.agentData.agentEquipment;

	protected override void __Init()
	{
		base.__Init();
		offset = 0;
		SetCapacity(inventoryLineCapacity, inventoryLineCapacity);
		for (int i = 0; i < base.slots.Count; i++)
		{
			base.slots[i].index = i;
		}
		droneItem.Init();
		droneItem.index = -2;
		positiveItem.Init();
		positiveItem.index = -1;
	}

	public void BindQuickInventory(LinearInventory inventory)
	{
		onLeftClicked = delegate
		{
			DolocAPI.gameStateManager.agentController.UseTool(force: true);
		};
		onRightClicked = delegate
		{
			DolocAPI.gameStateManager.agentController.UseItem(force: true);
		};
		onSelected = delegate(int index)
		{
			GetItem(index)?.QuickSelect();
		};
		onDeselect = delegate(int index)
		{
			GetItem(index)?.QuickDeselect();
		};
		onSlotRender = delegate(int i, Item _)
		{
			GetSlot(i).grayed = false;
		};
		droneItem.SetVisible(value: true);
		positiveItem.SetVisible(value: true);
		BindInventory(inventory, onSlotRender);
	}

	public void BindBuilderInventory(LinearInventory inventory, Func<Item, bool> onItemFilter, Action<int> onSelected)
	{
		onLeftClicked = (this.onSelected = onSelected);
		onRightClicked = null;
		onDeselect = null;
		onSlotRender = delegate(int i, Item item)
		{
			GetSlot(i).grayed = !onItemFilter(item);
		};
		droneItem.SetVisible(value: false);
		positiveItem.SetVisible(value: false);
		BindInventory(inventory, onSlotRender);
		this.onSelected(base.selectedIndex);
	}

	protected override void OnBindInventory()
	{
		SetClickCallbacks(delegate(int idx)
		{
			if (DolocButtonComponent.latestClickType == ClickType.Mouse)
			{
				if (idx != latestSelect)
				{
					latestSelect = idx;
					onSelected?.Invoke(idx);
				}
				else
				{
					onLeftClicked?.Invoke(idx);
				}
			}
		}, delegate(int idx)
		{
			if (idx != latestSelect)
			{
				latestSelect = idx;
				onSelected?.Invoke(idx);
			}
			else
			{
				onRightClicked?.Invoke(idx);
			}
		});
		OnOffsetChange(offset);
		RefreshDroneItem();
		RefreshPositiveItem();
	}

	public void ResetSelection()
	{
		SetQuickInventoryOffset(0);
		Select(0);
		base.selectedIndex = 0;
	}

	public void PrevLine()
	{
		SetQuickInventoryOffset((navCache.Last() + base.totalCapacity - lineCapacity) % base.totalCapacity);
		if (base.selectedIndex >= 0)
		{
			Select((base.selectedIndex - lineCapacity) % base.totalCapacity);
		}
		DolocAPI.Broadcast(OperationEventType.SCROLL_BACKPACK_LINE);
	}

	public void NextLine()
	{
		SetQuickInventoryOffset((navCache.Last() + 1) % base.totalCapacity);
		if (base.selectedIndex >= 0)
		{
			Select((base.selectedIndex + lineCapacity) % base.totalCapacity);
		}
		DolocAPI.Broadcast(OperationEventType.SCROLL_BACKPACK_LINE);
	}

	public void MovePrev()
	{
		int num = navCache.IndexOf(base.selectedIndex);
		num = (num - 1 + navCache.Count) % navCache.Count;
		while (!GetSlot(navCache[num]).interactable)
		{
			num = (num - 1 + navCache.Count) % navCache.Count;
		}
		Select(navCache[num]);
		DolocAPI.Broadcast(OperationEventType.SCROLL_SELECTED_ITEM);
	}

	public void MoveNext()
	{
		int num = navCache.IndexOf(base.selectedIndex);
		num = (num + 1) % navCache.Count;
		while (!GetSlot(navCache[num]).interactable)
		{
			num = (num + 1) % navCache.Count;
		}
		Select(navCache[num]);
		DolocAPI.Broadcast(OperationEventType.SCROLL_SELECTED_ITEM);
	}

	public Item GetSelectedItem()
	{
		return GetItem(base.selectedIndex);
	}

	public void SelectDrone()
	{
		if (agentEquipment.droneItem != null)
		{
			Select(-2);
		}
	}

	public void SelectActiveItem()
	{
		if (agentEquipment.activeItem != null)
		{
			Select(-1);
		}
	}

	public void SelectInCurrentLine(int index)
	{
		Select(index + offset);
	}

	public void RefreshPanelFromSettings()
	{
		RefreshView();
		ResetQuickInventoryLabel();
	}

	public override void SetCapacity(int totalCapacity, int lineCapacity)
	{
		base.SetCapacity(lineCapacity, lineCapacity);
		base.totalCapacity = totalCapacity;
		OnOffsetChange(offset);
	}

	protected override int GetLineCapacity(int total)
	{
		return inventoryLineCapacity;
	}

	protected override void OnStartShow()
	{
		base.OnStartShow();
		GetSlot(base.selectedIndex).highLighted = true;
		RefreshPanelFromSettings();
		if (DolocAPI.IsDataLoaded)
		{
			onSelected?.Invoke(base.selectedIndex);
		}
	}

	protected override void OnStartHide()
	{
		DolocAPI.QuickDeselectCurrentItem();
		base.OnStartHide();
	}

	protected override void Render(int index, Item item, bool isSlotLocked)
	{
		if (offset <= index && index < offset + base.slotCount)
		{
			GetSlot(index).Render(item, isSlotLocked && DolocAPI.userSettings.showSortLockIcons);
			onSlotRender?.Invoke(index, item);
			if (item != null && base.isRender && index == base.selectedIndex)
			{
				GetSlot(index).HoverTextSmall(item.title.Colored(DolocUiColor.SLIENTCOLOR_BLUE), UIAlignmentType.TopMiddle, UIAlignmentType.BottomMiddle, autoFade: true);
			}
		}
	}

	protected override void OnSelectedIndexChange(int oldIndex, int newIndex)
	{
		if (DolocAPI.IsDataLoaded)
		{
			onDeselect?.Invoke(oldIndex);
			onSelected?.Invoke(newIndex);
		}
	}

	protected override void OnSlotSelect(ItemNavSlot slot)
	{
		latestSelect = currentSelect;
		GetSlot(latestSelect).highLighted = false;
		currentSelect = slot.index;
		slot.highLighted = true;
		Item item = GetItem(slot.index);
		if (item == null)
		{
			return;
		}
		slot.HoverTextSmall(item.title.Colored(DolocUiColor.SLIENTCOLOR_BLUE), UIAlignmentType.TopMiddle, UIAlignmentType.BottomMiddle, autoFade: true);
		DolocAPI.UIRaiseRoll();
		DolocAPI.DelayFrame(delegate
		{
			if (EventSystem.current?.currentSelectedGameObject == base.gameObject)
			{
				EventSystem.current?.SetSelectedGameObject(null);
			}
		});
	}

	private Item GetItem(int index)
	{
		if (!DolocAPI.IsGameInitialized || !DolocAPI.IsDataLoaded)
		{
			return null;
		}
		return index switch
		{
			-2 => agentEquipment.droneItem, 
			-1 => agentEquipment.activeItem, 
			_ => DolocAPI.archiveHandle.InventorySystem.inventory.Read(index), 
		};
	}

	public override ItemNavSlot GetSlot(int index)
	{
		return index switch
		{
			-2 => droneItem, 
			-1 => positiveItem, 
			_ => base.GetSlot(index % lineCapacity), 
		};
	}

	protected override ItemData ConvertItemData(int index)
	{
		return index switch
		{
			-2 => new ItemData(agentEquipment.droneItem), 
			-1 => new ItemData(agentEquipment.activeItem), 
			_ => base.ConvertItemData(index), 
		};
	}

	public override void BuildNavigation()
	{
	}

	public override void Clear()
	{
		base.Clear();
		droneItem.highLighted = false;
		positiveItem.highLighted = false;
		foreach (ItemNavSlot slot in base.slots)
		{
			slot.highLighted = false;
		}
		latestSelect = 0;
		currentSelect = 0;
		SetQuickInventoryOffset(0);
		base.selectedIndex = offset;
	}

	public override void RaiseSpriteFadeUp(int index)
	{
		if (index >= offset && index < offset + lineCapacity)
		{
			base.RaiseSpriteFadeUp(index);
		}
	}

	public override void RaiseSpriteFadeUp(int index, Sprite sprite)
	{
		if (index >= offset && index < offset + lineCapacity)
		{
			base.RaiseSpriteFadeUp(index, sprite);
		}
	}

	public override void RaiseSpriteFadeDown(int index)
	{
		if (index >= offset && index < offset + lineCapacity)
		{
			base.RaiseSpriteFadeDown(index);
		}
	}

	public override void RaiseSpriteFadeDown(int index, Sprite sprite)
	{
		if (index >= offset && index < offset + lineCapacity)
		{
			base.RaiseSpriteFadeDown(index, sprite);
		}
	}

	private void ResetQuickInventoryLabel()
	{
		GetSlot(0).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect1ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(1).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect2ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(2).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect3ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(3).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect4ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(4).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect5ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(5).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect6ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(6).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect7ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(7).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect8ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(8).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect9ActionName, DolocInputDeviceType.KeyboardMouse);
		GetSlot(9).labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalQuickSelect0ActionName, DolocInputDeviceType.KeyboardMouse);
		droneItem.labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalSelectedDroneActionName, DolocInputDeviceType.KeyboardMouse);
		positiveItem.labelText.text = DolocAPI.UserInput.GetActionBindingKeyName(DolocAPI.UserInput.NormalSelectedActiveActionName, DolocInputDeviceType.KeyboardMouse);
	}

	private void SetQuickInventoryOffset(int index)
	{
		offset = Mathf.FloorToInt((float)index / (float)lineCapacity) * lineCapacity;
		OnOffsetChange(offset);
	}

	private void OnOffsetChange(int value)
	{
		for (int i = 0; i < base.slotCount; i++)
		{
			base.slots[i].index = i + value;
		}
		RefreshView();
		navCache.Clear();
		if (droneItem.gameObject.activeSelf)
		{
			navCache.Add(droneItem.index);
		}
		if (positiveItem.gameObject.activeSelf)
		{
			navCache.Add(positiveItem.index);
		}
		foreach (ItemNavSlot slot in base.slots)
		{
			navCache.Add(slot.index);
		}
	}

	public override void SetClickCallbacks(UnityAction<int> onLeftClick, UnityAction<int> onRightClick = null, UnityAction<int> onAssistLeftClick = null, UnityAction<int> onAssistRightClick = null, UnityAction<int> onLeftLongClick = null, UnityAction<int> onRightLongClick = null, Func<int, bool> onLeftContinuesClick = null, Func<int, bool> onRightContinuesClick = null)
	{
		droneItem.SetClickCallbacks(onLeftClick, onRightClick, onAssistLeftClick, onAssistRightClick, onLeftLongClick, onRightLongClick, onLeftContinuesClick, onRightContinuesClick);
		positiveItem.SetClickCallbacks(onLeftClick, onRightClick, onAssistLeftClick, onAssistRightClick, onLeftLongClick, onRightLongClick, onLeftContinuesClick, onRightContinuesClick);
		base.SetClickCallbacks(onLeftClick, onRightClick, onAssistLeftClick, onAssistRightClick, onLeftLongClick, onRightLongClick, onLeftContinuesClick, onRightContinuesClick);
	}

	public override void RemoveCallbacks()
	{
		droneItem.ClearAllClickCallbacks();
		positiveItem.ClearAllClickCallbacks();
		base.RemoveCallbacks();
	}

	public override void SetSelectCallbacks(UnityAction<int> callback)
	{
		base.SetSelectCallbacks(callback);
		ResetSlotSelectCallback(droneItem);
		ResetSlotSelectCallback(positiveItem);
		if (callback != null)
		{
			droneItem.onSelect.AddListener(callback);
			positiveItem.onSelect.AddListener(callback);
		}
	}

	public override void SetDeselectCallbacks(UnityAction<int> callback)
	{
		base.SetDeselectCallbacks(callback);
		ResetSlotDeselectCallback(droneItem);
		ResetSlotDeselectCallback(positiveItem);
		if (callback != null)
		{
			droneItem.onDeselect.AddListener(callback);
			positiveItem.onDeselect.AddListener(callback);
		}
	}

	public override void SetPointerEnterCallbacks(UnityAction<int> callback)
	{
		base.SetPointerEnterCallbacks(callback);
		ResetSlotPointerEnterCallback(droneItem);
		ResetSlotPointerEnterCallback(positiveItem);
		if (callback != null)
		{
			droneItem.onPointerEnter.AddListener(callback);
			positiveItem.onPointerEnter.AddListener(callback);
		}
	}

	public override void SetPointerExitCallbacks(UnityAction<int> callback)
	{
		base.SetPointerExitCallbacks(callback);
		ResetSlotPointerExitCallback(droneItem);
		ResetSlotPointerExitCallback(positiveItem);
		if (callback != null)
		{
			droneItem.onPointerExit.AddListener(callback);
			positiveItem.onPointerExit.AddListener(callback);
		}
	}

	public override void GetFocus()
	{
		base.isFocused = true;
		GetSlot(base.selectedIndex).highLighted = true;
	}

	public override void LoseFocus()
	{
		base.isFocused = false;
		GetSlot(base.selectedIndex).highLighted = false;
	}

	public void RefreshDroneItem()
	{
		droneItem.Clear();
		Item item = agentEquipment.droneItem;
		bool flag = item != null;
		droneItem.RenderItem(flag ? item.uiSprite : LocSprites.UI_INFOICON_DRONE_28PX);
		droneItem.interactable = flag;
		droneItem.labelText.gameObject.SetActive(flag);
	}

	public void RefreshPositiveItem()
	{
		positiveItem.Clear();
		Item activeItem = agentEquipment.activeItem;
		bool flag = activeItem != null;
		positiveItem.RenderItem(flag ? activeItem.uiSprite : LocSprites.UI_INFOICON_ACTIVE_28PX);
		positiveItem.interactable = flag;
		positiveItem.labelText.gameObject.SetActive(flag);
	}
}
