using DolocTown.Config.Fishing;
using DolocTown.Config.Player;
using DolocTown.GameData;
using RedSaw;

namespace DolocTown;

public class AgentEquipmentFunctionFishTank : AgentEquipmentFunction
{
	private string poolName;

	private bool shouldGenFish;

	public AgentEquipmentFuncProtoFishTank func => skill.Function as AgentEquipmentFuncProtoFishTank;

	public AgentEquipmentFunctionFishTank(Item item, AgentEquipmentManager manager, AgentEquipmentSkillInfo skill)
		: base(item, manager, skill)
	{
	}

	public override void SetFishingPoolName(string poolName)
	{
		this.poolName = poolName;
	}

	public void TryRollFish()
	{
		if (!RandomUtils.Dice(func.Probability))
		{
			return;
		}
		FishInfo fishInfo = DolocAPI.RollFishByRarity(poolName, func.Rarity);
		if (fishInfo != null)
		{
			DolocAPI.SendCatchFishEvent(fishInfo);
			DolocAPI.BroadcastString(GameEventType.CATCH_FISH_BY_HAT, fishInfo.Id);
			Item item = DolocAPI.GenerateItem(fishInfo.Id);
			if (item != null)
			{
				DolocAPI.GenerateDropItem(DolocAPI.CurrentRoom, item, DolocAPI.AgentPosition);
				DolocAPI.RaiseEmotion(DolocAPI.agent.transform, EmotionName.AMAZING);
			}
		}
	}
}
