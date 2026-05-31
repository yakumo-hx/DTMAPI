using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;

namespace DolocTown.UI;

public struct FactionMissionData : IUIData
{
	public string title;

	public string description;

	public string sender;

	public bool isCompleted;

	public List<FactionItemData> items;

	public FactionItemData moneyCost;

	public RewardData rewardData;

	public string costInfo;

	public string buttonText;

	public bool isMoneyCostEnough;

	public bool unlock;

	public string lockHint;

	public bool notEmpty { get; }

	public FactionMissionData(FactionMission mission)
	{
		notEmpty = true;
		FactionMissionInfo proto = mission.Proto;
		title = proto.Title;
		description = proto.Description;
		sender = proto.Sender;
		isCompleted = mission.IsComplete;
		unlock = true;
		lockHint = string.Empty;
		if (!proto.UnlockBeforeSettled && !DolocAPI.QueryFactionJoinState(mission.MainSeries))
		{
			unlock = false;
			lockHint = DolocConfig.StaticTexts.FactionMissionLock;
		}
		FactionItem[] factionItems = mission.FactionItems;
		items = factionItems.Select((FactionItem x) => new FactionItemData(x)).ToList();
		int[] counts = factionItems.Select((FactionItem x) => x.Count).ToArray();
		string[] titles = factionItems.Select((FactionItem x) => DolocAPI.GetItemTitle(x.ItemName)).ToArray();
		costInfo = DolocUtils.GenerateCountItemInfo(titles, counts);
		rewardData = new RewardData(mission.Rewards);
		int requiredMoney = proto.RequiredMoney;
		moneyCost = new FactionItemData(LocSprites.UI_ICON_GOLD28X, requiredMoney, mission.MoneyFinishState);
		if (requiredMoney > 0)
		{
			items.Add(moneyCost);
			if (items.Count > 1)
			{
				costInfo += ", ";
			}
			costInfo += DolocUtils.Format(DolocConfig.StaticTexts.UiMoneyTip, requiredMoney.ToString().Colored(DolocUiColor.EYECATCHCOLOR_CYAN));
		}
		isMoneyCostEnough = DolocAPI.archiveHandle.CurrentMoney >= requiredMoney;
		buttonText = (mission.MoneyFinishState ? string.Empty : ((!isMoneyCostEnough) ? DolocConfig.StaticTexts.BuildingPanelMoneyNotEnough : DolocConfig.StaticTexts.FactionMissionMoneySubmit.Format(requiredMoney)));
	}
}
