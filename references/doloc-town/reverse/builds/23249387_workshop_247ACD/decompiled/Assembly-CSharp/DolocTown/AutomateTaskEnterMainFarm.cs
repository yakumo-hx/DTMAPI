using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskEnterMainFarm : AutomateTask
{
	private readonly Building fromBuilding;

	public override bool SkipAfterSuccess => false;

	public AutomateTaskEnterMainFarm(Building fromBuilding = null)
	{
		this.fromBuilding = fromBuilding;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (base.Bot.CurrentRoom == DolocAPI.archiveHandle.MainFarm)
		{
			return TaskStatus.Success;
		}
		base.Bot.EnterMainFarm(fromBuilding);
		return TaskStatus.Success;
	}
}
