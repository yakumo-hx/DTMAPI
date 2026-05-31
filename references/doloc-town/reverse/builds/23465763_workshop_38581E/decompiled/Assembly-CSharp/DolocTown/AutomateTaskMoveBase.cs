using RedSaw.AI.LinearTask;

namespace DolocTown;

public abstract class AutomateTaskMoveBase : AutomateTask
{
	protected AutomateTaskMoveBase(LinearTaskBreaker breaker = null)
		: base(breaker)
	{
	}

	public override void OnSuccess()
	{
		base.Bot.StopMove();
	}

	public override void OnFailure()
	{
		base.Bot.StopMove();
	}

	public override void OnBreak()
	{
		base.Bot.StopMove();
	}
}
