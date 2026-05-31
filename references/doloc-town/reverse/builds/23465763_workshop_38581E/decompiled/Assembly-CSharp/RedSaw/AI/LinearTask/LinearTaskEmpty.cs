namespace RedSaw.AI.LinearTask;

public class LinearTaskEmpty : LinearTask
{
	public override bool SkipAfterSuccess => false;

	public override TaskStatus OnExecute(float dt)
	{
		return TaskStatus.Success;
	}
}
