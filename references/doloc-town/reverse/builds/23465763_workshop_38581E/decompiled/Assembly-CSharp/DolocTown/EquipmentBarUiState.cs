using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Player;
using DolocTown.GameData;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DolocTown;

public class EquipmentBarUiState : DolocUiState<EquipmentBarPanel>
{
	private bool _inBackpack;

	private readonly Timer _timer = new Timer(DolocAPI.GlobalParameter.QuantitySelectTimer_Ref);

	private bool canQuitBySelectPrev;

	private bool canQuitBySelectNext;

	public override bool PermanentState => true;

	private InventorySystem invSys => DolocAPI.archiveHandle.InventorySystem;

	private LinearInventory inventory => invSys.inventory;

	private BackpackBottomPanel backpackPanel => base.panel.backpackPanel;

	private AccessoriesBar accessoriesBar => base.panel.equipmentBar.accessoriesBar;

	private DroneBar droneBar => base.panel.equipmentBar.droneEquipmentBar.droneBar;

	private AgentEquipmentManager agentEquipment => DolocAPI.archiveHandle.farmData.agentData.agentEquipment;

	private ItemDroneStructure droneItem => agentEquipment.droneItem as ItemDroneStructure;

	private int currentIndex => backpackPanel.selectedIndex;

	private Item selectedItem => inventory.Read(currentIndex);

	private SingleInventory buffer => DolocAPI.archiveHandle.InventorySystem.buffer;

	private bool noneItemSelected => (buffer.CurrentItem ?? selectedItem) == null;

	protected override UnityEvent OnCloseButtonClick => backpackPanel.OnCloseButtonClick;

	protected override void Register()
	{
		droneBar.structItemSlot.onSelect.AddListener(OnDroneSlotSelect);
		droneBar.structItemSlot.SetClickCallbacks(OnDroneSlotClick, null, null, null, OnDroneSlotClick);
		droneBar.structItemSlot.onPointerEnter.AddListener(delegate(int index)
		{
			ShowEquipmentItemViewer(droneBar.GetSlot(index), agentEquipment.droneItem, base.staticTexts.EquipmentBarDroneTip);
		});
		accessoriesBar.SetHatItemCallBack(OnHatSlotSelect, OnHatSlotClick);
		accessoriesBar.SetPositiveItemCallBack(OnPositiveSlotSelect, OnPositiveSlotClick);
		accessoriesBar.SetPassiveItem1CallBack(OnPassiveSlot1Select, OnPassiveSlot1Click);
		accessoriesBar.SetPassiveItem2CallBack(OnPassiveSlot2Select, OnPassiveSlot2Click);
		backpackPanel.BindInventory(inventory);
		backpackPanel.SetClickCallbacks(OnBackpackItemClick, DolocAPI.SwapOneItemFromInventory, OnBackpackItemClick, DolocAPI.SwapHalfItemFromInventory, TryQuickPlaceItem, null, null, SwapOneItemFromInventoryInterrupted);
		backpackPanel.SetSelectCallbacks(OnItemSelect);
		backpackPanel.SetDeleteCallback(DestroyItem);
		buffer.onValueChanged.AddListener(OnBufferChanged);
		DolocAPI.Broadcast(OperationEventType.OPEN_BACKPACK_PANEL);
	}

	protected override void Unregister()
	{
		droneBar.structItemSlot.onSelect.RemoveListener(OnDroneSlotSelect);
		droneBar.structItemSlot.ClearAllClickCallbacks();
		droneBar.structItemSlot.onPointerEnter.RemoveAllListeners();
		droneBar.RemoveCallbacks();
		accessoriesBar.ClearCallBack();
		backpackPanel.Clear();
		buffer.onValueChanged.RemoveListener(OnBufferChanged);
	}

	private bool SwapOneItemFromInventoryInterrupted(int index)
	{
		if (selectedItem == null)
		{
			return false;
		}
		DolocAPI.SwapOneItemFromInventory(index);
		return true;
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.GlobalQuickSelectPrev)
		{
			canQuitBySelectPrev = true;
		}
		if (userInput.GlobalQuickSelectNext)
		{
			canQuitBySelectNext = true;
		}
		if (userInput.GlobalQuickSelectPrevInProgress && userInput.GlobalQuickSelectNextInProgress && canQuitBySelectPrev && canQuitBySelectNext && DolocAPI.userSettings.enableBackpackShortcut)
		{
			DolocAPI.gameStateManager.agentController.CanScrollItemInLine = false;
			gameController.PopState();
		}
		else if (userInput.BaseIsCancelPressed || (userInput.BaseNotFixedOperation && userInput.GlobalToggleBackpack))
		{
			gameController.PopState();
		}
		else
		{
			if (!_inBackpack)
			{
				return;
			}
			if (userInput.BaseIsSplitPressed)
			{
				_timer.ReStart();
				DolocAPI.SwapOneItemFromInventory(currentIndex);
			}
			else if (userInput.BaseIsSplitInProgress)
			{
				if (_timer.Update(deltaTime))
				{
					SwapOneItemFromInventoryInterrupted(currentIndex);
				}
			}
			else if (userInput.BaseIsConfirmPressed)
			{
				DolocAPI.SwapItemFromInventory(currentIndex);
			}
			else if (userInput.BaseIsConfirmHold)
			{
				TryQuickPlaceItem(currentIndex);
			}
			else if (userInput.BaseSortItem)
			{
				inventory.Sort();
				DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_BACKPACK_TIDY_UP);
			}
			else if (userInput.BaseLockItem)
			{
				SwitchSlotLockStatus();
			}
			else if (userInput.BaseDisposeItem)
			{
				DolocAPI.DisposeItem(currentIndex);
			}
			else if (userInput.BaseDestroyItem)
			{
				DestroyItem();
			}
		}
	}

	protected override void Show()
	{
		base.Show();
		canQuitBySelectPrev = false;
		canQuitBySelectNext = false;
		base.panel.Show();
		InitEquipmentBar();
		backpackPanel.Select(DolocAPI.SelectedItemIndex);
	}

	protected override void Hide()
	{
		base.Hide();
		base.panel.Hide();
		DolocAPI.PlaceInBackpackOrGenerateDropItem(buffer.Take(), DolocAPI.userSettings.autoUseBox);
		DolocAPI.HideItemBorder();
	}

	public override void OnResume()
	{
		base.OnResume();
		backpackPanel.Select(currentIndex);
	}

	protected override void RefreshTip()
	{
		base.RefreshTip();
		string[] first = new string[5]
		{
			base.staticTexts.UiTipTidy,
			base.staticTexts.UiTipLockSlot,
			base.staticTexts.UiTipDispose,
			base.staticTexts.UiTipDestroyItem,
			base.staticTexts.UiTipQuickEquipment
		};
		first = ((DolocAPI.UserInput.DeviceType != 0) ? first.Concat(new string[1] { base.staticTexts.UiTipTakeOutOneGamepad }).ToArray() : first.Concat(new string[2]
		{
			base.staticTexts.UiTipTakeOutOne,
			base.staticTexts.UiTipTakeOutHalf
		}).ToArray());
		base.panel.operationTip.SetTextKey(first);
	}

	private void InitEquipmentBar()
	{
		AgentArchiveData agentData = DolocAPI.archiveHandle.farmData.agentData;
		DateInfo dateNow = DolocAPI.archiveHandle.timeData.dateNow;
		string time = DolocUtils.Format(base.staticTexts.EquipmentBarTimeFormat, DolocUtils.Format(base.staticTexts.EquipmentBarTimeLabel, dateNow.GetDateInfo()).Colored(DolocUiColor.EYECATCHCOLOR_CYAN), DolocConfig.GetEnumText(dateNow.WeekDay), DolocAPI.archiveHandle.timeData.SeasonProto?.SeasonTitle ?? "");
		Sprite bodySprite = (DolocAPI.modManager.HasPlayerOverride ? DolocAPI.GlobalParameter.UiPlayerDefaultSprite.Asset : DolocAPI.GlobalParameter.UiPlayerBodyDefaultSprite.Asset);
		Sprite hairSprite = (DolocAPI.modManager.HasPlayerOverride ? null : DolocAPI.GlobalParameter.UiPlayerHairDefaultSprite.Asset);
		base.panel.equipmentBar.RenderPlayerInfo(bodySprite, hairSprite, agentData.playerName, agentData.money, time, agentData.isDoubleJumpUnlocked, agentData.isDashUnlocked);
		RefreshDroneViewer(rebuildNavigation: true);
		DolocAPI.QueryRoom(agentData.motorData.roomId, out var room);
		DolocAPI.QuerySceneInfo(room?.SceneRawName, out var proto);
		base.panel.equipmentBar.droneEquipmentBar.RenderMotorBar(agentData.motorData.isUnlocked, proto?.Title);
		RefreshHatViewer();
		RefreshActiveViewer();
		RefreshPassiveViewer();
		accessoriesBar.passiveItem2.SetVisible(DolocAPI.archiveHandle.IsAdditionalPassiveSlotUnlocked());
	}

	private void RefreshHatViewer()
	{
		HatInfo hatInfo = (agentEquipment.hatItem as ItemHat)?.function?.HatId_Ref;
		base.panel.equipmentBar.RenderHat(hatInfo);
		accessoriesBar.hatItem.Render(agentEquipment.hatItem?.uiSprite);
	}

	private void RefreshActiveViewer()
	{
		accessoriesBar.positiveItem.Render(agentEquipment.activeItem?.uiSprite);
	}

	private void RefreshPassiveViewer()
	{
		accessoriesBar.passiveItem1.Render(agentEquipment.passiveItem1?.uiSprite);
		accessoriesBar.passiveItem2.Render(agentEquipment.passiveItem2?.uiSprite);
	}

	private void RefreshDroneViewer(bool rebuildNavigation)
	{
		droneBar.Render(new DroneBarData(droneItem));
		if (rebuildNavigation)
		{
			DolocAPI.DelayFrame(delegate
			{
				base.panel.RebuildNavigation();
			});
		}
		droneBar.SetSelectCallbacks(OnComponentSlotSelect);
		droneBar.SetPointerEnterCallbacks(delegate(int index)
		{
			ShowEquipmentItemViewer(droneBar.GetSlot(index), droneItem.droneStructure.GetComponent(index), droneItem.droneStructure.proto.GetSlotInfo(index)?.Title);
		});
		droneBar.SetClickCallbacks(OnComponentSlotClick, null, null, null, OnComponentSlotClick);
	}

	private void ResetBackpack()
	{
		if (_inBackpack)
		{
			return;
		}
		_inBackpack = true;
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			slot.grayed = false;
		}
	}

	protected virtual void OnBackpackItemClick(int index)
	{
		if (!userInput.BaseIsConfirmPressed && !userInput.BaseIsConfirmInProgress)
		{
			DolocAPI.SwapItemFromInventory(index);
		}
	}

	private void OnItemSelect(int index)
	{
		ResetBackpack();
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
	}

	private void TryQuickPlaceItem(int index)
	{
		Item item = backpackPanel.itemGetter?.Invoke(index);
		if (item == null)
		{
			return;
		}
		if (buffer.IsEmpty)
		{
			Item oldHat;
			if (!(item is ItemHat))
			{
				Item oldItem2;
				if (!(item is IActiveItem))
				{
					Item oldItem;
					if (!(item is ItemPassive))
					{
						Item oldDrone;
						if (!(item is ItemDroneStructure))
						{
							if (item is IDroneComponentItem && droneItem != null)
							{
								int[] equipIndexes = droneItem.droneStructure.GetEquipIndexes(item);
								if (!equipIndexes.IsNullOrEmpty())
								{
									int[] array = equipIndexes;
									foreach (int index2 in array)
									{
										if (droneItem.droneStructure.IsSlotEmpty(index2))
										{
											droneItem.droneStructure.Equip(index2, inventory.Take(index));
											RefreshDroneViewer(rebuildNavigation: false);
											return;
										}
									}
									array = equipIndexes;
									foreach (int index3 in array)
									{
										if (droneItem.droneStructure.TryTakeOff(index3, out var item2))
										{
											droneItem.droneStructure.Equip(index3, inventory.Take(index));
											DolocAPI.PlaceItem(item2);
											RefreshDroneViewer(rebuildNavigation: false);
											return;
										}
									}
								}
							}
						}
						else if (DolocAPI.EquipDrone(item, out oldDrone))
						{
							inventory.Take(index);
							DolocAPI.PlaceItem(oldDrone);
							RefreshDroneViewer(rebuildNavigation: true);
							return;
						}
					}
					else if (DolocAPI.EquipPassiveItem(item, out oldItem))
					{
						inventory.Take(index);
						DolocAPI.PlaceItem(oldItem);
						RefreshPassiveViewer();
						return;
					}
				}
				else if (DolocAPI.EquipActiveItem(item, out oldItem2))
				{
					inventory.Take(index);
					DolocAPI.PlaceItem(oldItem2);
					RefreshActiveViewer();
					return;
				}
			}
			else if (DolocAPI.EquipHat(item.name, out oldHat))
			{
				inventory.Take(index);
				DolocAPI.PlaceItem(oldHat);
				RefreshHatViewer();
				return;
			}
		}
		DolocAPI.SwapItemFromInventory(index);
	}

	private void OnBufferChanged(Item item)
	{
		backpackPanel.DisableDestroyButton(DisableDestroyButton(), noneItemSelected);
		if (!(item is ItemHat))
		{
			if (!(item is IActiveItem))
			{
				if (!(item is ItemPassive))
				{
					if (!(item is ItemDroneStructure))
					{
						if (item is IDroneComponentItem)
						{
							if (droneItem != null)
							{
								int[] equipIndexes = droneItem.droneStructure.GetEquipIndexes(item);
								foreach (int focus in equipIndexes)
								{
									droneBar.SetFocus(focus);
								}
							}
						}
						else
						{
							accessoriesBar.ResetPanel();
							droneBar.ResetPanel();
						}
					}
					else
					{
						droneBar.SetStructureFocus();
					}
				}
				else if (DolocAPI.archiveHandle.IsAdditionalPassiveSlotUnlocked() && agentEquipment.HasPassiveItem1 && !agentEquipment.HasPassiveItem2)
				{
					accessoriesBar.SetFocus(accessoriesBar.passiveItem2);
				}
				else
				{
					accessoriesBar.SetFocus(accessoriesBar.passiveItem1);
				}
			}
			else
			{
				accessoriesBar.SetFocus(accessoriesBar.positiveItem);
			}
		}
		else
		{
			accessoriesBar.SetFocus(accessoriesBar.hatItem);
		}
	}

	private bool DisableDestroyButton()
	{
		if (buffer.CurrentItem == null && selectedItem == null)
		{
			return false;
		}
		return !DolocAPI.IsItemDisposable(buffer.CurrentItem ?? selectedItem);
	}

	private void DestroyItem()
	{
		Item bufferItem = buffer.CurrentItem;
		Item item = bufferItem ?? selectedItem;
		if (item == null)
		{
			return;
		}
		if (!DolocAPI.IsItemDisposable(item))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.UiErrNotDisposeItem);
			return;
		}
		buffer.Take();
		DolocAPI.ShowQuestionBox(string.Format(base.staticTexts.UiQuesDisposeItem, item.count, item.title), delegate
		{
			if (bufferItem == null)
			{
				DolocAPI.DestroyItem(currentIndex);
			}
			DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_ITEM_DELETE);
		}, delegate
		{
			buffer.CurrentItem = bufferItem;
		});
	}

	private void SwitchSlotLockStatus()
	{
		inventory.SwitchSlotLockStatus(currentIndex);
	}

	private void OnHatSlotSelect()
	{
		_inBackpack = false;
		if (buffer.IsEmpty)
		{
			accessoriesBar.ResetPanel();
			droneBar.ResetPanel();
			SetItemSlotGrayed<ItemHat>();
		}
		ShowEquipmentItemViewer(accessoriesBar.hatItem, agentEquipment.hatItem, base.staticTexts.EquipmentBarHatTip);
	}

	private void OnHatSlotClick()
	{
		Item oldHat;
		if (agentEquipment.HasHat && buffer.IsEmpty && !DolocAPI.CanPlaceItem(agentEquipment.hatItem))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.EquipmentBarBackpackFull);
		}
		else if (DolocAPI.EquipHat(buffer.CurrentItem?.name, out oldHat))
		{
			buffer.Take();
			DolocAPI.PlaceItem(oldHat);
			SetItemSlotGrayed<ItemHat>();
			RefreshHatViewer();
		}
	}

	private void OnPositiveSlotSelect()
	{
		_inBackpack = false;
		if (buffer.IsEmpty)
		{
			droneBar.ResetPanel();
			accessoriesBar.ResetPanel();
			SetItemSlotGrayed<IActiveItem>();
		}
		ShowEquipmentItemViewer(accessoriesBar.positiveItem, agentEquipment.activeItem, base.staticTexts.EquipmentBarPositiveTip);
	}

	private void OnPositiveSlotClick()
	{
		Item oldItem;
		if (agentEquipment.HasActiveItem && buffer.IsEmpty && !DolocAPI.CanPlaceItem(agentEquipment.activeItem))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.EquipmentBarBackpackFull);
		}
		else if (DolocAPI.EquipActiveItem(buffer.CurrentItem, out oldItem))
		{
			buffer.Take();
			DolocAPI.PlaceItem(oldItem);
			SetItemSlotGrayed<IActiveItem>();
			RefreshActiveViewer();
		}
	}

	private void OnPassiveSlot1Select()
	{
		_inBackpack = false;
		if (buffer.IsEmpty)
		{
			droneBar.ResetPanel();
			accessoriesBar.ResetPanel();
			SetItemSlotGrayed<ItemPassive>();
		}
		ShowEquipmentItemViewer(accessoriesBar.passiveItem1, agentEquipment.passiveItem1, base.staticTexts.EquipmentBarPassive1Tip);
	}

	private void OnPassiveSlot1Click()
	{
		Item oldItem;
		if (agentEquipment.HasPassiveItem1 && buffer.IsEmpty && !DolocAPI.CanPlaceItem(agentEquipment.passiveItem1))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.EquipmentBarBackpackFull);
		}
		else if (DolocAPI.EquipPassiveItem1(buffer.CurrentItem, out oldItem))
		{
			buffer.Take();
			DolocAPI.PlaceItem(oldItem);
			SetItemSlotGrayed<ItemPassive>();
			RefreshPassiveViewer();
		}
	}

	private void OnPassiveSlot2Select()
	{
		_inBackpack = false;
		if (buffer.IsEmpty)
		{
			droneBar.ResetPanel();
			accessoriesBar.ResetPanel();
			SetItemSlotGrayed<ItemPassive>();
		}
		ShowEquipmentItemViewer(accessoriesBar.passiveItem2, agentEquipment.passiveItem2, base.staticTexts.EquipmentBarPassive2Tip);
	}

	private void OnPassiveSlot2Click()
	{
		Item oldItem;
		if (agentEquipment.HasPassiveItem2 && buffer.IsEmpty && !DolocAPI.CanPlaceItem(agentEquipment.passiveItem2))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.EquipmentBarBackpackFull);
		}
		else if (DolocAPI.EquipPassiveItem2(buffer.CurrentItem, out oldItem))
		{
			buffer.Take();
			DolocAPI.PlaceItem(oldItem);
			SetItemSlotGrayed<ItemPassive>();
			RefreshPassiveViewer();
		}
	}

	private void OnDroneSlotSelect(int index)
	{
		_inBackpack = false;
		if (buffer.IsEmpty)
		{
			droneBar.ResetPanel();
			accessoriesBar.ResetPanel();
			SetItemSlotGrayed<ItemDroneStructure>();
		}
		ShowEquipmentItemViewer(droneBar.GetSlot(index), agentEquipment.droneItem, base.staticTexts.EquipmentBarDroneTip);
	}

	private void OnDroneSlotClick(int index)
	{
		Item oldDrone;
		if (agentEquipment.HasDrone && buffer.IsEmpty && !DolocAPI.CanPlaceItem(agentEquipment.droneItem))
		{
			DolocAPI.ShowMessageBoxSmall(base.staticTexts.EquipmentBarBackpackFull);
		}
		else if (DolocAPI.EquipDrone(buffer.CurrentItem, out oldDrone))
		{
			buffer.Take();
			DolocAPI.PlaceItem(oldDrone);
			SetItemSlotGrayed<ItemDroneStructure>();
			RefreshDroneViewer(rebuildNavigation: true);
		}
	}

	private void OnComponentSlotSelect(int index)
	{
		_inBackpack = false;
		if (buffer.IsEmpty)
		{
			droneBar.ResetPanel();
			accessoriesBar.ResetPanel();
			SetDroneComponentItemSlotGrayed(index);
		}
		ShowEquipmentItemViewer(droneBar.GetSlot(index), droneItem.droneStructure.GetComponent(index), droneItem.droneStructure.proto.GetSlotInfo(index)?.Title);
	}

	private void OnComponentSlotClick(int index)
	{
		if ((!buffer.IsEmpty && !(buffer.CurrentItem is IDroneComponentItem)) || droneItem == null)
		{
			return;
		}
		bool isLocked;
		Item component = droneItem.droneStructure.GetComponent(index, out isLocked);
		if (component != null)
		{
			if (isLocked)
			{
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.DronePanelErrLocked);
				return;
			}
			if (buffer.IsEmpty && !DolocAPI.CanPlaceItem(component))
			{
				DolocAPI.ShowMessageBoxSmall(base.staticTexts.EquipmentBarBackpackFull);
				return;
			}
		}
		if (droneItem.droneStructure.TryTakeOff(index, out var item))
		{
			DolocAPI.PlaceItem(item);
		}
		if (buffer.CurrentItem is IDroneComponentItem)
		{
			if (!droneItem.droneStructure.CanEquip(index, buffer.CurrentItem, out var reason))
			{
				DolocAPI.ShowMessageBoxSmallErr(reason);
			}
			else
			{
				droneItem.droneStructure.Equip(index, buffer.Take());
			}
		}
		SetDroneComponentItemSlotGrayed(index);
		RefreshDroneViewer(rebuildNavigation: false);
	}

	private void SetDroneComponentItemSlotGrayed(int slotIndex)
	{
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			Item item = backpackPanel.itemGetter?.Invoke(slot.index);
			bool flag = droneItem.droneStructure.CanEquip(slotIndex, item);
			slot.grayed = !flag;
		}
	}

	private void SetItemSlotGrayed<T>()
	{
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			bool flag = backpackPanel.itemGetter?.Invoke(slot.index) is T;
			slot.grayed = !flag;
		}
	}

	private void ShowEquipmentItemViewer(DolocUiObject slot, Item item, string defaultText)
	{
		if (item == null)
		{
			slot.HoverTextSmall(defaultText.Colored(DolocUiColor.SLIENTCOLOR_BLUE));
			return;
		}
		UIAlignmentType targetAnchor = UIAlignmentType.LeftTop;
		UIAlignmentType hoverPivot = UIAlignmentType.LeftBottom;
		if (DolocAPI.screenManager.screenSize.y - slot.positionY < 2f * slot.size.y)
		{
			targetAnchor = UIAlignmentType.LeftBottom;
			hoverPivot = UIAlignmentType.LeftTop;
		}
		slot.HoverItemViewer(new ItemData(item), targetAnchor, hoverPivot);
	}
}
