using System;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Room;
using DolocTown.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace DolocTown;

public class TeleportUiState : DolocUiState<TeleportPanel>
{
	private StationInfo[] unlockedStationProtos;

	private ItemInfo ticketItemProto;

	private Action<string> transportCallback;

	private int currentIndex;

	private bool costMoney;

	private TicketTip ticketTip => base.panel.ticketTip;

	private int ticketPrice => ticketItemProto.BuyingPrice + DolocAPI.GlobalParameter.StationTicketMarkup;

	protected override UnityEvent OnCloseButtonClick => base.panel.OnCloseButtonClick;

	private StationInfo stationProto => unlockedStationProtos[currentIndex];

	private int ticketCount => DolocAPI.CountItem(ticketItemProto.Id, checkBox: true);

	public bool HandleStartUpArgs(Action<string> transportCallback)
	{
		if (ticketItemProto == null)
		{
			Debug.LogError("未找车票到对应道具配置");
			return false;
		}
		this.transportCallback = transportCallback;
		return true;
	}

	protected override void OnInit()
	{
		base.OnInit();
		DolocAPI.QueryItemProto(DolocAPI.GlobalParameter.TicketItemName, out ticketItemProto);
	}

	private void OnSlotSelect(int index)
	{
		currentIndex = index;
		base.panel.GetSlot(index).HoverTextSmall(stationProto.Title);
	}

	private void OnSlotClick(int index)
	{
		if (stationProto.MarkPointId_Ref.RoomId == DolocAPI.CurrentRoom.RoomId)
		{
			DolocAPI.ShowMessageBoxSmall(string.Format(base.staticTexts.TeleportFail, stationProto.Title));
		}
		else if (ticketCount > 0)
		{
			DolocAPI.ShowQuestionBox(string.Format(base.staticTexts.TeleportConfirm, stationProto.Title), delegate
			{
				base.panel.HideHoverBox();
				DolocAPI.CostItem(ticketItemProto.Id, 1, checkBox: true);
				DolocAPI.RaiseUiSpriteFadeUp(ticketTip.position, DolocAPI.GetItemSprite(DolocAPI.GlobalParameter.ItemRefTicket));
				Teleport();
			});
		}
		else if (DolocAPI.archiveHandle.CurrentMoney >= ticketPrice)
		{
			DolocAPI.uiSystem.MoneyTip.ForceShow();
			DolocAPI.ShowQuestionBox(string.Format(base.staticTexts.TeleportBuyTicket, ticketPrice, stationProto.Title), delegate
			{
				base.panel.HideHoverBox();
				costMoney = true;
				DolocAPI.archiveHandle.CurrentMoney -= ticketPrice;
				DolocAPI.uiSystem.MoneyTip.SetMoney(DolocAPI.archiveHandle.CurrentMoney - ticketPrice);
				Teleport();
			});
		}
		else
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.TeleportErrMaterialNotEnough);
		}
	}

	private void Teleport()
	{
		gameController.PopState();
		transportCallback?.Invoke(stationProto.MarkPointId);
	}

	private bool GetTeleportPointIndex(out int index)
	{
		index = 0;
		if (unlockedStationProtos.IsNullOrEmpty())
		{
			return false;
		}
		for (int i = 0; i < unlockedStationProtos.Length; i++)
		{
			if (!(unlockedStationProtos[i].MarkPointId_Ref.RoomId != DolocAPI.CurrentRoom.RoomId))
			{
				index = i;
				return true;
			}
		}
		return true;
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		costMoney = false;
		unlockedStationProtos = DolocAPI.archiveHandle.cityData.globalInteractableObjectManager.GetUnlockedStationsInMap();
		StationInfo[] allStations = DolocConfig.Tables.TbStation.DataList.Where((StationInfo x) => x.FreeTeleport).ToArray().ToArray();
		base.panel.InitTeleportPoints(new TeleportPointGroupData(allStations, unlockedStationProtos));
	}

	protected override void Register()
	{
		foreach (TeleportPoint slot in base.panel.slots)
		{
			slot.onSelect.AddListener(OnSlotSelect);
			slot.onClick.AddListener(OnSlotClick);
		}
	}

	protected override void Unregister()
	{
		foreach (TeleportPoint slot in base.panel.slots)
		{
			slot.onSelect.RemoveListener(OnSlotSelect);
			slot.onClick.RemoveListener(OnSlotClick);
		}
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
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_MAP);
		base.panel.SetTicketCount(ticketCount);
		base.panel.Show(useTween: true, base.panel.RebuildNavigation);
		EventSystem.current?.SetSelectedGameObject(null);
		if (GetTeleportPointIndex(out currentIndex))
		{
			base.panel.Select(currentIndex);
		}
		else if (!unlockedStationProtos.IsNullOrEmpty())
		{
			base.panel.Select(0);
		}
		if (ticketCount == 0)
		{
			DolocAPI.uiSystem.MoneyTip.ForceShow();
		}
	}

	protected override void Hide()
	{
		DolocAPI.Sound.PostSoundEvent(SoundEvents.PLAY_UI_MAP_CLOSE);
		base.panel.Hide();
	}
}
