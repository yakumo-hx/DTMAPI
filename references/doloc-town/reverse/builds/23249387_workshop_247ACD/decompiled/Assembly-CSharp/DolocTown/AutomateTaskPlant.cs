using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskPlant : AutomateTask
{
	private readonly AutomatePlantMissionInfo _missionInfo;

	public AutomateTaskPlant(AutomatePlantMissionInfo missionInfo)
	{
		_missionInfo = missionInfo;
	}

	public override TaskStatus OnExecute(float dt)
	{
		PlantBasin plantBasin = _missionInfo.plantBasin;
		base.Bot.locker.UnlockEquipment(plantBasin);
		ItemSeed itemSeed = base.Bot.inventory.AutomatePatchTakeSeedFromContainer(_missionInfo.condition);
		if (plantBasin.Plant(itemSeed, base.Bot.IsRenderNow))
		{
			base.Bot.RaiseSpriteFadeUp(itemSeed.uiSprite, fadeUp: false);
			return TaskStatus.Success;
		}
		base.Bot.inventory.PlaceItem(itemSeed);
		return TaskStatus.Failure;
	}
}
