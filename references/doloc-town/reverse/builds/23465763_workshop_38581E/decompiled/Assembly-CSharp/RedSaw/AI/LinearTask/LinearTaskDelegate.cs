using System;

namespace RedSaw.AI.LinearTask;

public class LinearTaskDelegate : LinearTask
{
	private readonly Action action;

	public LinearTaskDelegate(Action action, LinearTaskBreaker breaker = null)
		: base(breaker)
	{
		this.action = action;
	}

	public override TaskStatus OnExecute(float dt)
	{
		action?.Invoke();
		return TaskStatus.Success;
	}
}
