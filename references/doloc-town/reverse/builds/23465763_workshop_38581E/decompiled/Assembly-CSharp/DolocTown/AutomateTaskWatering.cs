using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class AutomateTaskWatering : AutomateTask
{
	private readonly PlantBasin[] basins;

	private readonly int sprinklerCost;

	public AutomateTaskWatering(PlantBasin[] basins, int sprinklerCost)
	{
		this.basins = basins;
		this.sprinklerCost = sprinklerCost;
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.Bot.Power -= Mathf.Min(sprinklerCost, base.Bot.Power);
		PlantBasin[] array = basins;
		foreach (PlantBasin plantBasin in array)
		{
			base.Bot.locker.UnlockEquipment(plantBasin);
			plantBasin.Water(base.Bot.IsRenderNow, sendMessage: false);
		}
		return TaskStatus.Success;
	}
}
