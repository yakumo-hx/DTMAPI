using DolocTown.Config.Drone;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class DronePanelUiState : DolocUiState<DronePanel>
{
	private ItemDroneStructure droneItem;

	private readonly SingleInventory buffer = new SingleInventory();

	private int bufferIndex = -1;

	protected override UnityEvent OnCloseButtonClick => backpackPanel.OnCloseButtonClick;

	private DroneWidget DroneWidget => base.panel.droneWidget;

	private BackpackSideBarWidget backpackPanel => base.panel.backpackPanel;

	private InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	private DroneStruct currentStructure => droneItem.droneStructure;

	private Item selectedItem => inventorySystem.inventory.Read(backpackPanel.selectedIndex);

	public bool HandleStartUpArgs(ItemDroneStructure droneStructureItem)
	{
		if (droneStructureItem == null)
		{
			return false;
		}
		droneItem = droneStructureItem;
		return true;
	}

	protected override void Register()
	{
		DroneWidget.structItemSlot.onSelect.AddListener(HoverStructureDetails);
		DroneWidget.structItemSlot.onDeselect.AddListener(HideHoverBox);
		DroneWidget.structItemSlot.onPointerEnter.AddListener(HoverStructureDetails);
		DroneWidget.structItemSlot.onPointerExit.AddListener(HideHoverBox);
		DroneWidget.ForEachComponent(delegate(DroneItemSlot item)
		{
			item.onClick.AddListener(OnComponentItemClick);
			item.onSelect.AddListener(OnDroneItemSelect);
			item.onDeselect.AddListener(HideHoverBox);
			item.onPointerEnter.AddListener(HoverComponentDetails);
			item.onPointerExit.AddListener(HideHoverBox);
		});
		buffer.onValueChanged.AddListener(OnBufferChanged);
		backpackPanel.BindInventory(inventorySystem.inventory);
		backpackPanel.SetClickCallbacks(OnBackpackItemClick);
	}

	protected override void Unregister()
	{
		DroneWidget.structItemSlot.onSelect.RemoveListener(HoverStructureDetails);
		DroneWidget.structItemSlot.onSelect.RemoveListener(HideHoverBox);
		DroneWidget.structItemSlot.onPointerEnter.RemoveListener(HoverStructureDetails);
		DroneWidget.structItemSlot.onPointerExit.RemoveListener(HideHoverBox);
		DroneWidget.ForEachComponent(delegate(DroneItemSlot item)
		{
			item.onClick.RemoveListener(OnComponentItemClick);
			item.onSelect.RemoveListener(OnDroneItemSelect);
			item.onDeselect.RemoveListener(HideHoverBox);
			item.onPointerEnter.RemoveListener(HoverComponentDetails);
			item.onPointerExit.RemoveListener(HideHoverBox);
		});
		buffer.onValueChanged.RemoveListener(OnBufferChanged);
		backpackPanel.Clear();
	}

	private void OnBackpackItemClick(int itemIndex)
	{
		Item item = inventorySystem.inventory.Read(itemIndex);
		int cnt = 0;
		int componentIndex = -1;
		DroneWidget.ForEachComponent(delegate(int idx, DroneItemSlot ui)
		{
			DroneStruct droneStruct = currentStructure;
			if (droneStruct != null && droneStruct.CanEquip(idx, item))
			{
				cnt++;
				if (componentIndex < 0)
				{
					componentIndex = idx;
				}
			}
		});
		if (cnt == 1 && currentStructure.GetComponent(componentIndex) == null)
		{
			TryEquipComponent(componentIndex, item, itemIndex);
		}
		else
		{
			SwapToBuffer(itemIndex);
		}
	}

	private void SwapToBuffer(int index)
	{
		if (bufferIndex >= 0)
		{
			backpackPanel.GetSlot(bufferIndex).highLighted = false;
		}
		if (!backpackPanel.GetSlot(index).grayed)
		{
			Item item = inventorySystem.inventory.Read(index);
			if (item != null)
			{
				buffer.CurrentItem = item;
				bufferIndex = index;
				backpackPanel.GetSlot(index).highLighted = true;
			}
		}
	}

	private void InitDroneStructure()
	{
		DroneStructureInfo proto = currentStructure.proto;
		Debug.Log("无人机框架：" + proto.Id);
		DroneWidget.structItemSlot.Render(droneItem);
		DroneSlot[] slots = currentStructure.slots;
		DroneWidget.CheckComponentItemCount(slots.Length);
		for (int i = 0; i < slots.Length; i++)
		{
			DroneSlot droneSlot = slots[i];
			DroneComponentItemSlot droneComponentItemSlot = (DroneComponentItemSlot)DroneWidget.GetComponentItemSlot(i);
			if (droneSlot == null)
			{
				droneComponentItemSlot.SetVisible(value: false);
				continue;
			}
			droneComponentItemSlot.button.interactable = true;
			droneComponentItemSlot.SetType(droneSlot.proto.SlotType);
			if (!droneSlot.IsEmpty)
			{
				Item item = droneSlot.item;
				droneComponentItemSlot.Render(item);
			}
		}
		DolocAPI.DelayFrame(base.panel.RebuildNavigation);
	}

	private void OnDroneItemSelect(int index)
	{
		HoverComponentDetails(index);
		backpackPanel.LoseFocus();
		DolocAPI.UIRaiseRoll();
	}

	private void HoverStructureDetails(int index)
	{
		if (!droneItem.GetExtraInfo1().IsNullOrEmpty())
		{
			DroneWidget.structItemSlot.HoverText(new TextGroup("", "", droneItem.GetExtraInfo1().Colored(DolocUiColor.EYECATCHCOLOR_PURPLE)), UIAlignmentType.LeftMiddle, UIAlignmentType.RightMiddle);
		}
	}

	private void HoverComponentDetails(int index)
	{
		DroneItemSlot componentItemSlot = DroneWidget.GetComponentItemSlot(index);
		if (!componentItemSlot.isEmpty)
		{
			Item component = currentStructure.GetComponent(index);
			if (!component.GetExtraInfo1().IsNullOrEmpty())
			{
				componentItemSlot.HoverText(new TextGroup("", "", component.GetExtraInfo1().Colored(DolocUiColor.EYECATCHCOLOR_PURPLE)), UIAlignmentType.LeftMiddle, UIAlignmentType.RightMiddle);
			}
		}
	}

	private void HideHoverBox(int index)
	{
		DolocAPI.HideHoverBox();
	}

	private void OnComponentItemClick(int index)
	{
		if (currentStructure == null)
		{
			return;
		}
		if (buffer.CurrentItem != null && !DroneWidget.GetComponentItemSlot(index).isHighLight)
		{
			if (buffer.CurrentItem is IDroneComponentItem)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.DronePanelErrLoad);
			}
			return;
		}
		Item item = buffer.Take();
		bool isLocked;
		Item component = currentStructure.GetComponent(index, out isLocked);
		if (component != null)
		{
			if (isLocked)
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.DronePanelErrLocked);
				return;
			}
			if (!inventorySystem.CanPlaceItem(component))
			{
				DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrBackpackIsFull);
				return;
			}
			DroneWidget.GetComponentItemSlot(index).Clear();
			currentStructure.TakeOff(index);
		}
		TryEquipComponent(index, item, bufferIndex);
		if (component != null)
		{
			DolocAPI.PlaceItem(component);
		}
		SetItemSlotGrayed();
	}

	private void TryEquipComponent(int componentIndex, Item item, int itemIndex)
	{
		if (item is IDroneComponentItem && currentStructure.Equip(componentIndex, item))
		{
			DroneWidget.GetComponentItemSlot(componentIndex).Render(item);
			if (itemIndex >= 0)
			{
				inventorySystem.CostAt(itemIndex, 1);
				backpackPanel.GetSlot(itemIndex).Select();
			}
		}
	}

	private void OnBufferChanged(Item item)
	{
		if (item == null)
		{
			backpackPanel.GetSlot(bufferIndex).highLighted = false;
		}
		DroneWidget.structItemSlot.HighLight(item is ItemDroneStructure);
		if (DroneWidget.structItemSlot.isHighLight)
		{
			DroneWidget.structItemSlot.button.Select();
		}
		bool first = true;
		DroneWidget.ForEachComponent(delegate(int idx, DroneItemSlot ui)
		{
			bool flag = currentStructure != null && currentStructure.CanEquip(idx, item);
			ui.HighLight(flag);
			if (flag && first)
			{
				ui.highLighted = true;
				first = false;
			}
		});
		SetItemSlotGrayed();
	}

	private void SetItemSlotGrayed()
	{
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			Item item = backpackPanel.itemGetter?.Invoke(slot.index);
			slot.grayed = !ItemFilter(item);
		}
	}

	private bool ItemFilter(Item item)
	{
		return currentStructure.CanEquip(item);
	}

	private void ClearHighlight()
	{
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			slot.highLighted = false;
		}
	}

	private Item GenerateItem(string id)
	{
		ItemFactory.GenerateItem(id, 1, out var item);
		if (item == null)
		{
			Debug.LogError("创建道具: " + id + "失败，请检查是否有相应配置文件");
		}
		return item;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseIsCancelPressed)
		{
			gameController.PopState();
		}
	}

	protected override void Show()
	{
		base.Show();
		ClearHighlight();
		InitDroneStructure();
		SetItemSlotGrayed();
		base.panel.Show();
		backpackPanel.SelectFirst();
	}

	protected override void Hide()
	{
		base.panel.Hide();
		ClearHighlight();
		DroneStruct droneStructure = droneItem.droneStructure;
		if (droneStructure != null && !droneStructure.IsEmpty)
		{
			DolocAPI.Broadcast(OperationEventType.OPEN_DRONE_PANEL);
		}
		base.Hide();
	}
}
