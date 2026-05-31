using DolocTown.Config;
using DolocTown.Config.Building;
using DolocTown.Config.Mission;
using DolocTown.GameData;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

public class RewardBuildingUnlock : Reward
{
	[JsonProperty]
	public readonly string buildingName;

	public override int Count => 1;

	public override Sprite RewardIcon
	{
		get
		{
			if (!DolocAPI.QueryBuilding(buildingName, out var proto))
			{
				return LocSprites.UI_MENUICON_BUILDINGS;
			}
			return proto.UiSpriteAsset.Asset;
		}
	}

	public override string RewardBriefInfo => DolocUtils.Format(DolocConfig.StaticTexts.RewardInfoBuildingUnlock, DolocAPI.GetBuildingTitle(buildingName));

	public override string RewardInfo => RewardBriefInfo;

	[JsonConstructor]
	public RewardBuildingUnlock(string buildingName)
		: base(RewardType.BUILDING_UNLOCK)
	{
		this.buildingName = buildingName;
	}

	public override bool CheckValid()
	{
		BuildingInfo proto;
		if (!string.IsNullOrEmpty(buildingName))
		{
			return DolocAPI.QueryBuilding(buildingName, out proto);
		}
		return false;
	}

	public override bool CashReward()
	{
		if (!DolocAPI.archiveHandle.UnlockBuilding(buildingName))
		{
			return false;
		}
		DolocAPI.ShowMessageBoxNodeComplete(RewardInfo);
		return true;
	}

	public override bool TryCombine(Reward other, out Reward combined)
	{
		if (other is RewardBuildingUnlock rewardBuildingUnlock && rewardBuildingUnlock.buildingName == buildingName)
		{
			combined = new RewardBuildingUnlock(buildingName);
			return true;
		}
		combined = null;
		return false;
	}
}
