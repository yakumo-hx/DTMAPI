using System;

namespace RedSaw.AI.LinearTask;

public class LinearTaskBreakerDelegate : LinearTaskBreaker
{
	private readonly Func<bool> conditionFunc;

	protected override bool ShouldBreak => conditionFunc();

	public LinearTaskBreakerDelegate(Func<bool> conditionFunc)
	{
		this.conditionFunc = conditionFunc;
	}
}
