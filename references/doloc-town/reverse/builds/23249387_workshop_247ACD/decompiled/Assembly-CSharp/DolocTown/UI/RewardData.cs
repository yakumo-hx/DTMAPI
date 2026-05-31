using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Mission;
using UnityEngine;

namespace DolocTown.UI;

public struct RewardData : IUIData
{
	public Sprite[] rewardIcons;

	public int rewardsCount;

	public int[] rewardCounts;

	public string rewardsInfo;

	public bool notEmpty { get; }

	public RewardData(Reward[] rewards)
	{
		this = default(RewardData);
		if (rewards.IsNullOrEmpty())
		{
			rewardsInfo = DolocConfig.StaticTexts.UiTipNone;
			return;
		}
		notEmpty = true;
		rewardsInfo = string.Join(", ", rewards.Select((Reward x) => x.RewardInfo));
		Reward[] array = rewards.Where((Reward x) => x.type != RewardType.FAVORABILITY).ToArray();
		rewardsCount = array.Length;
		rewardIcons = array.Select((Reward x) => x.RewardIcon).ToArray();
		rewardCounts = array.Select((Reward x) => x.Count).ToArray();
	}
}
