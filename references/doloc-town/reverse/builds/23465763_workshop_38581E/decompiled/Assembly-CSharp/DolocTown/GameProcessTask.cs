using RedSaw.AI.LinearTask;

namespace DolocTown;

public class GameProcessTask : LinearTask
{
	public override TaskStatus OnExecute(float dt)
	{
		return TaskStatus.Executing;
	}
}
