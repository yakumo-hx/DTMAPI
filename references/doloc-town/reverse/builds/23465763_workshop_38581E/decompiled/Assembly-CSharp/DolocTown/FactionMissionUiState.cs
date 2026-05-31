using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.UI;
using UnityEngine.Events;

namespace DolocTown;

public class FactionMissionUiState : DolocUiState<FactionMissionPanel>
{
	private FactionMissionType[] allSubSeries;

	private bool isEmpty;

	private FactionType mainSeriesType;

	private FactionMission[] currentMissions;

	private int currentMissionIndex;

	private FactionMission currentMission;

	private int currentBackpackIndex;

	private BackpackSideBarWidget backpackPanel => base.panel.backpackPanel;

	private FactionMissionWidget MissionWidget => base.panel.missionWidget;

	private FactionMissionViewer missionViewer => MissionWidget.viewer;

	private FactionMissionManager missionMgr => DolocAPI.archiveHandle.cityData.factionMissionManager;

	private int currentSubSeriesIndex => MissionWidget.currentStickIndex;

	private FactionMissionType currentSubSeries => allSubSeries[currentSubSeriesIndex];

	private InventorySystem inventorySystem => DolocAPI.archiveHandle.InventorySystem;

	private LinearInventory backpack => inventorySystem.inventory;

	protected override UnityEvent OnCloseButtonClick => backpackPanel.OnCloseButtonClick;

	private IScrollContentRect _contentRect => base.panel.missionWidget.contentRect;

	public bool HandleStartUpArgs(FactionType type)
	{
		mainSeriesType = type;
		allSubSeries = missionMgr.GetFactionMissionSubTypes(type);
		isEmpty = allSubSeries.IsNullOrEmpty();
		return !isEmpty;
	}

	private void OnSubSeriesClick(int index)
	{
		MissionWidget.SelectStick(index);
		currentMissionIndex = 0;
		currentMissions = missionMgr.GetFactionMissions(mainSeriesType, currentSubSeries).ToArray();
		missionViewer.currentIndex = 0;
		missionViewer.SetTotalCapacity(currentMissions.Length);
		missionViewer.SelectPage(0);
		backpackPanel.Select(currentBackpackIndex);
		missionViewer.RefreshView();
	}

	private void OnBackpackItemSelect(int index)
	{
		currentBackpackIndex = index;
	}

	private void OnBackpackItemClick(int index)
	{
		if ((!currentMission.Proto.UnlockBeforeSettled && !DolocAPI.QueryFactionJoinState(currentMission.MainSeries)) || isEmpty)
		{
			return;
		}
		ItemNavSlot itemNavSlot = backpackPanel.slots[index];
		Item item = backpack.Read(index);
		if (item == null)
		{
			return;
		}
		int submitCount = currentMission.MaxSubmitFactionMissionItemCount(item.name);
		if (currentMission.IsComplete)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.FactionMissionFinish);
			return;
		}
		if (itemNavSlot.grayed)
		{
			DolocAPI.ShowMessageBoxSmallErr((submitCount == 0) ? base.staticTexts.FactionMissionCannotSubmit : base.staticTexts.FactionMissionFinishSubmit);
			return;
		}
		bool isGeneItem = item is IHasGeneGroup;
		if (DolocAPI.CountItem(item, checkBox: false, !isGeneItem) < submitCount)
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.FactionMissionErrSubmit);
			return;
		}
		DolocAPI.ShowQuestionBox((isGeneItem ? base.staticTexts.UiSubmitConfirmGene : base.staticTexts.UiSubmitConfirm).Format(submitCount, item.title), delegate
		{
			HashSet<int> hashSet = new HashSet<int>();
			backpack.TryCostAtIndex(index, submitCount, !isGeneItem, hashSet, out var _);
			foreach (int item2 in hashSet)
			{
				backpackPanel.RaiseSpriteFadeUp(item2);
			}
			missionMgr.SubmitFactionMissionItem(currentMission.Id, item.name);
			RefreshView();
		});
	}

	private void OnMissionSelect(int index)
	{
		currentMissionIndex = index;
		currentMission = currentMissions[index];
		DolocAPI.HideItemBorder();
		SetItemSlotGrayed();
		base.panel.RebuildNavigation();
		if (!currentMission.MoneyFinishState)
		{
			missionViewer.moneyConfirmButton.Select();
		}
		else
		{
			backpackPanel.GetFocus();
		}
		DolocAPI.DelayFrame(base.panel.RebuildNavigation);
		DolocAPI.DelayFrame(base.panel.RebuildNavigation);
	}

	private void SetItemSlotGrayed()
	{
		foreach (ItemNavSlot slot in backpackPanel.slots)
		{
			Item item = backpackPanel.itemGetter?.Invoke(slot.index);
			slot.grayed = !ItemFilter(item);
		}
	}

	private void OnMoneyConfirmButtonSelect(int _)
	{
		DolocAPI.HideItemBorder();
		missionViewer.moneyConfirmButton.GetItemBorder(BorderType.Arrow);
	}

	private void OnMoneyConfirmButtonDeSelect(int _)
	{
		DolocAPI.HideItemBorder();
	}

	private void OnMoneyConfirmButtonClick(int _)
	{
		int requiredMoney = currentMission.Proto.RequiredMoney;
		if (requiredMoney == 0)
		{
			return;
		}
		if (!DolocAPI.CanAffordMoney(currentMission.Proto.RequiredMoney))
		{
			DolocAPI.ShowMessageBoxSmallErr(base.staticTexts.StorePlayerMoneyNotEnough);
			return;
		}
		DolocAPI.ShowQuestionBox(DolocUtils.Format(base.staticTexts.UiSubmitConfirmMoney, requiredMoney.ToString()), delegate
		{
			DolocAPI.archiveHandle.CurrentMoney -= requiredMoney;
			backpackPanel.RefreshMoney();
			missionMgr.SubmitMoney(currentMission.Id);
			RefreshView();
		});
	}

	private bool ItemFilter(Item item)
	{
		if (!currentMission.Proto.UnlockBeforeSettled && !DolocAPI.QueryFactionJoinState(currentMission.MainSeries))
		{
			return false;
		}
		if (item == null)
		{
			return false;
		}
		int count = backpack.Count(item.name);
		if (isEmpty)
		{
			return false;
		}
		return currentMission.CanSubmitFactionMissionItem(item.name, count);
	}

	protected override void BeforeRegister()
	{
		base.BeforeRegister();
		if (!isEmpty)
		{
			string[] titles = allSubSeries.Select((FactionMissionType x) => DolocConfig.Tables.TbFactionMissionType.Get(x).Title).ToArray();
			currentMissions = missionMgr.GetFactionMissions(mainSeriesType, currentSubSeries).ToArray();
			MissionWidget.RenderSticks(titles);
		}
	}

	protected override void Register()
	{
		backpackPanel.BindInventory(inventorySystem.inventory);
		backpackPanel.SetClickCallbacks(OnBackpackItemClick);
		backpackPanel.SetSelectCallbacks(OnBackpackItemSelect);
		if (!isEmpty)
		{
			missionViewer.DataGetter = DataGetter;
			missionViewer.onSelect.AddListener(OnMissionSelect);
			FactionSubSeriesSlot[] stickSlots = MissionWidget.stickSlots;
			for (int i = 0; i < stickSlots.Length; i++)
			{
				stickSlots[i].onClick.AddListener(OnSubSeriesClick);
			}
			missionViewer.moneyConfirmButton.onClick.AddListener(OnMoneyConfirmButtonClick);
			missionViewer.moneyConfirmButton.onSelect.AddListener(OnMoneyConfirmButtonSelect);
			missionViewer.moneyConfirmButton.onDeselect.AddListener(OnMoneyConfirmButtonDeSelect);
		}
	}

	private FactionMissionData DataGetter(int index)
	{
		if (index < 0 || index >= currentMissions.Length)
		{
			return default(FactionMissionData);
		}
		return new FactionMissionData(currentMissions[index]);
	}

	protected override void Unregister()
	{
		backpackPanel.Clear();
		if (!isEmpty)
		{
			missionViewer.DataGetter = null;
			missionViewer.onSelect.RemoveListener(OnMissionSelect);
			FactionSubSeriesSlot[] stickSlots = MissionWidget.stickSlots;
			for (int i = 0; i < stickSlots.Length; i++)
			{
				stickSlots[i].onClick.RemoveListener(OnSubSeriesClick);
			}
			missionViewer.moneyConfirmButton.onClick.RemoveListener(OnMoneyConfirmButtonClick);
			missionViewer.moneyConfirmButton.onSelect.RemoveListener(OnMoneyConfirmButtonSelect);
			missionViewer.moneyConfirmButton.onDeselect.RemoveListener(OnMoneyConfirmButtonDeSelect);
		}
	}

	private void RefreshView()
	{
		if (!isEmpty)
		{
			currentMissions = missionMgr.GetFactionMissions(mainSeriesType, currentSubSeries).ToArray();
			missionViewer.SetTotalCapacity(currentMissions.Length);
			missionViewer.RefreshView();
			missionViewer.SelectPage(currentMissionIndex);
			backpackPanel.Select(currentBackpackIndex);
			MissionWidget.RebuildLayout();
		}
	}

	protected override void Show()
	{
		base.Show();
		if (isEmpty)
		{
			MissionWidget.SetEmpty(value: true);
			SetItemSlotGrayed();
		}
		else
		{
			RefreshView();
			MissionWidget.SelectStick(0);
			OnSubSeriesClick(0);
			backpackPanel.Select(0);
			backpackPanel.GetSlot(0).GetItemBorder();
		}
		base.panel.Show();
		base.panel.operationTip.SetTextKey(base.staticTexts.UiTipSwitchClassifying);
	}

	protected override void Hide()
	{
		base.panel.Hide();
		base.Hide();
	}

	protected override void OnUiUpdate(float deltaTime)
	{
		if (userInput.BaseScrollDir.magnitude > 0f)
		{
			_contentRect?.SetScrollMoveCallback(userInput.BaseScrollDir.y);
		}
		if (userInput.BaseIsLastPressed && !isEmpty)
		{
			missionViewer.PrevPage();
		}
		else if (userInput.BaseIsNextPressed && !isEmpty)
		{
			missionViewer.NextPage();
		}
		else if (userInput.BasePageUpPressed && !isEmpty)
		{
			MissionWidget.LastSticks();
		}
		else if (userInput.BasePageDownPressed && !isEmpty)
		{
			MissionWidget.NextSticks();
		}
		else if (userInput.BaseIsCancelPressed)
		{
			DolocAPI.HideItemBorder();
			gameController.PopState();
		}
	}
}
