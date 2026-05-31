using RedSaw.AI.LinearTask;

namespace DolocTown;

public class AutomateBotDecisionMakerEmpty : AutomateBotDecisionMaker
{
	public AutomateBotDecisionMakerEmpty(AutomateBot bot, AutomateSystemLocker locker)
		: base(bot, locker)
	{
	}

	protected override LinearTask AutomateBotMakeDecision()
	{
		return base.FixedTaskWanderAroundStation;
	}
}
