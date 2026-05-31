using DolocTown.Config;
using DolocTown.Config.Mission;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardFavorability : Reward
{
	[JsonProperty]
	public readonly string npcName;

	[JsonProperty]
	public readonly int value;

	public override Sprite RewardIcon => null;

	public override int Count => value;

	public override string RewardBriefInfo
	{
		get
		{
			string npcTitle = DolocAPI.GetNpcTitle(npcName, ignoreUnknown: true);
			if (value < 35)
			{
				return DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoFavorabilityLv1, npcTitle);
			}
			if (value < 60)
			{
				return DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoFavorabilityLv2, npcTitle);
			}
			return DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoFavorabilityLv3, npcTitle);
		}
	}

	public override string RewardInfo => RewardBriefInfo;

	[JsonConstructor]
	public RewardFavorability(string npcName, int value)
		: base(RewardType.FAVORABILITY)
	{
		this.npcName = npcName;
		this.value = value;
	}

	public override bool CheckValid()
	{
		Npc npc;
		if (!string.IsNullOrEmpty(npcName))
		{
			return DolocAPI.QueryNpc(npcName, out npc);
		}
		return false;
	}

	public override bool CashReward()
	{
		DolocAPI.AddTargetNpcLikingValue(npcName, value);
		return true;
	}

	public override bool TryCombine(Reward other, out Reward combinedReward)
	{
		if (other is RewardFavorability rewardFavorability && rewardFavorability.npcName == npcName)
		{
			combinedReward = new RewardFavorability(npcName, rewardFavorability.value + value);
			return true;
		}
		combinedReward = null;
		return false;
	}
}
