using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AnimalWink : AnimalTask
{
	private int duration;

	public AnimalWink(int duration)
	{
		this.duration = duration;
	}

	public override void OnBegin()
	{
		base.OnBegin();
		animal.Wink();
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (duration-- > 0)
		{
			return TaskStatus.Executing;
		}
		return TaskStatus.Success;
	}
}
