using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskEnterBuilding : AutomateTask
{
	private readonly Building building;

	public AutomateTaskEnterBuilding(Building building)
	{
		this.building = building;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (base.Bot.CurrentRoom == building.room)
		{
			return TaskStatus.Success;
		}
		if (building.IsRemoved)
		{
			return TaskStatus.Failure;
		}
		base.Bot.EnterBuilding(building);
		return TaskStatus.Success;
	}
}
