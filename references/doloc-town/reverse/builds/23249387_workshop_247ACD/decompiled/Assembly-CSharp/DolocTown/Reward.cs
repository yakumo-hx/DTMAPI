using System.Collections.Generic;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public abstract class Reward
{
	public readonly RewardType type;

	public abstract Sprite RewardIcon { get; }

	public abstract int Count { get; }

	public abstract string RewardInfo { get; }

	public abstract string RewardBriefInfo { get; }

	public static void CombineRewards(List<Reward> rewards, Reward R)
	{
		if (rewards.Count == 0)
		{
			rewards.Add(R);
			return;
		}
		int num = 0;
		foreach (Reward reward in rewards)
		{
			if (reward.TryCombine(R, out var combinedReward))
			{
				rewards[num] = combinedReward;
				return;
			}
			num++;
		}
		rewards.Add(R);
	}

	public Reward(RewardType type)
	{
		this.type = type;
	}

	public abstract bool CheckValid();

	public abstract bool CashReward();

	public virtual bool TryCombine(Reward other, out Reward combinedReward)
	{
		combinedReward = null;
		return false;
	}

	public virtual bool CashRewardOverflowAsEmail(string emailTemplate = "send_item_template")
	{
		return CashReward();
	}

	public static Reward CreateReward(RewardProto proto)
	{
		Reward reward = proto.RewardType switch
		{
			RewardType.GOLD => new RewardGold((ushort)proto.TargetCount), 
			RewardType.ITEM => new RewardItem(proto.TargetId, proto.TargetCount), 
			RewardType.RECIPE_UNLOCK => new RewardRecipe(proto.TargetId), 
			RewardType.PLATFORM_UNLOCK => new RewardPlatformUnlock(proto.TargetId), 
			RewardType.BUILDING_UNLOCK => new RewardBuildingUnlock(proto.TargetId), 
			RewardType.INTERACTABLE_UNLOCK => new RewardInteractableObjectUnlock(proto.TargetId), 
			RewardType.FAVORABILITY => new RewardFavorability(proto.TargetId, proto.TargetCount), 
			_ => new RewardNone(), 
		};
		if (!reward.CheckValid())
		{
			Debug.LogError($"奖励生成失败！{proto.RewardType}: {proto.TargetId} {proto.TargetCount}");
			return new RewardNone();
		}
		return reward;
	}
}
