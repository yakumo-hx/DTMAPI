using DolocTown.Config.Mission;
using DolocTown.GameData;
using NodeCanvas.Framework;
using ParadoxNotion.Design;

namespace DolocTown.NodeCanvas;

[Category("多洛可小镇/成就")]
[Name("生成任务奖励", 0)]
[Description("生成一个任务奖励")]
public class DialogueTask_CashMissionReward : DialogueTask
{
	public string missionRewardTypeString = RewardType.NONE.ToString();

	public string targetId;

	public int targetCount;

	public override string taskTitle => rewardType switch
	{
		RewardType.NONE => "无", 
		RewardType.GOLD => $"金币<{targetCount}>", 
		RewardType.ITEM => $"道具<{targetId}>×{targetCount}", 
		RewardType.RECIPE_UNLOCK => "解锁配方<" + targetId + ">", 
		RewardType.PLATFORM_UNLOCK => "解锁平台<" + targetId + ">", 
		RewardType.BUILDING_UNLOCK => "解锁建筑<" + targetId + ">", 
		RewardType.INTERACTABLE_UNLOCK => "解锁站台<" + targetId + ">", 
		_ => "任务奖励", 
	};

	public RewardType rewardType => missionRewardTypeString.ConvertToEnumOrDefault<RewardType>();

	public override void DoAction(Graph graph)
	{
		DolocAPI.CashReward(new RewardProto(rewardType, targetId, targetCount));
	}
}
