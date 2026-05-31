namespace RedSaw.AI.LinearTask;

public class LinearTaskWait : LinearTask
{
	private readonly float duration;

	private readonly float durationReciprocal;

	private float current;

	public float Process => current * durationReciprocal;

	public LinearTaskWait(float duration, LinearTaskBreaker breaker = null)
		: base(breaker)
	{
		this.duration = duration;
		durationReciprocal = ((this.duration != 0f) ? (1f / duration) : 0f);
	}

	public override TaskStatus OnExecute(float dt)
	{
		current += dt;
		return EndExecute(current >= duration);
	}
}
