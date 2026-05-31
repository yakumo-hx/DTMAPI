using DolocTown.Config.Mission;

namespace DolocTown.GameData;

public readonly struct RewardProto
{
	public readonly RewardType RewardType;

	public readonly string TargetId;

	public readonly int TargetCount;

	public RewardProto(RewardType rewardType, string targetId = "", int targetCount = 0)
	{
		RewardType = rewardType;
		TargetId = targetId;
		TargetCount = targetCount;
	}
}
