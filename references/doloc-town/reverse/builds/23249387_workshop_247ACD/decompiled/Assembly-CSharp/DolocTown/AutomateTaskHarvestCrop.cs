using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskHarvestCrop : AutomateTask
{
	private readonly PlantBasin basin;

	public AutomateTaskHarvestCrop(PlantBasin basin)
	{
		this.basin = basin;
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.Bot.locker.UnlockEquipment(basin);
		if (basin.IsRemoved || !basin.HasCrop || !basin.Crop.isMature)
		{
			return TaskStatus.Failure;
		}
		basin.Harvest();
		return TaskStatus.Success;
	}
}
