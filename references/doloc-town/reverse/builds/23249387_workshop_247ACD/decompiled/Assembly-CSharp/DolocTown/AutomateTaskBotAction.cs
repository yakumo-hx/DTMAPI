using System;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateTaskBotAction : AutomateTask
{
	private readonly Action<AutomateBot> callback;

	public AutomateTaskBotAction(Action<AutomateBot> cb, LinearTaskBreaker breaker = null)
		: base(breaker)
	{
		callback = cb;
	}

	public override TaskStatus OnExecute(float dt)
	{
		callback?.Invoke(base.Bot);
		return TaskStatus.Success;
	}
}
