using DolocTown.Config;
using DolocTown.Config.Mission;
using DolocTown.Config.Platform;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardPlatformUnlock : Reward
{
	[JsonProperty]
	public readonly string platformName;

	public override int Count => 1;

	public override Sprite RewardIcon
	{
		get
		{
			if (!DolocAPI.QueryPlatformProto(platformName, out var proto))
			{
				return LocSprites.UI_MENUICON_PLATFORM;
			}
			return DolocAPI.GetItemSprite(proto.Id);
		}
	}

	public override string RewardBriefInfo => DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoPlatformUnlock, DolocAPI.GetItemTitle(platformName));

	public override string RewardInfo => RewardBriefInfo;

	[JsonConstructor]
	public RewardPlatformUnlock(string platformName)
		: base(RewardType.PLATFORM_UNLOCK)
	{
		this.platformName = platformName;
	}

	public override bool CheckValid()
	{
		PlatformInfo proto;
		return DolocAPI.QueryPlatformProto(platformName, out proto);
	}

	public override bool CashReward()
	{
		if (!DolocAPI.archiveHandle.UnlockPlatform(platformName))
		{
			return false;
		}
		DolocAPI.ShowMessageBoxNodeComplete(RewardInfo);
		return true;
	}

	public override bool TryCombine(Reward other, out Reward combinedReward)
	{
		if (other is RewardPlatformUnlock rewardPlatformUnlock && rewardPlatformUnlock.platformName == platformName)
		{
			combinedReward = new RewardPlatformUnlock(platformName);
			return true;
		}
		combinedReward = null;
		return false;
	}
}
