namespace RedSaw.AI.LinearTask;

public class LinearTaskWaitInt : LinearTask
{
	private readonly int duration;

	private int current;

	public LinearTaskWaitInt(int duration, LinearTaskBreaker breaker = null)
		: base(breaker)
	{
		this.duration = duration;
	}

	public override TaskStatus OnExecute(float dt)
	{
		return EndExecute(++current >= duration);
	}
}
