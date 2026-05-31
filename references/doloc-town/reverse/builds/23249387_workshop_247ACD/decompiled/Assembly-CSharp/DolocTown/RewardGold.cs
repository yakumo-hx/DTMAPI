using DolocTown.Config;
using DolocTown.Config.Mission;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardGold : Reward
{
	[JsonProperty]
	public readonly ushort goldCount;

	public override Sprite RewardIcon => LocSprites.UI_ICON_GOLD28X;

	public override int Count => goldCount;

	public override string RewardBriefInfo => DolocConfig.StaticTexts.UiMoneyTip.Format(goldCount);

	public override string RewardInfo => DolocConfig.StaticTexts.RewardInfoGold.Format(goldCount);

	[JsonConstructor]
	public RewardGold(ushort goldCount)
		: base(RewardType.GOLD)
	{
		this.goldCount = goldCount;
	}

	public override bool CheckValid()
	{
		return goldCount > 0;
	}

	public override bool CashReward()
	{
		if (goldCount <= 0)
		{
			return false;
		}
		DolocAPI.archiveHandle.CurrentMoney += goldCount;
		DolocAPI.RaiseItemObtainTip("money", LocSprites.UI_ICON_GOLD28X, DolocConfig.StaticTexts.ItemMoneyTitle, goldCount);
		return true;
	}

	public override bool TryCombine(Reward other, out Reward combined)
	{
		combined = null;
		if (!(other is RewardGold rewardGold))
		{
			return false;
		}
		combined = new RewardGold((ushort)(goldCount + rewardGold.goldCount));
		return true;
	}
}
