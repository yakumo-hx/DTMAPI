using RedSaw.AI.LinearTask;

namespace DolocTown;

public abstract class AutomateTask : LinearTask
{
	protected AutomateBot Bot { get; private set; }

	protected AutomateBotStation Station => Bot.Station;

	protected TemplateRoom StationRoom => (TemplateRoom)Station.CurrentRoom;

	protected AutomateTask(LinearTaskBreaker breaker = null)
		: base(breaker)
	{
	}

	public void SetBot(AutomateBot bot)
	{
		Bot = bot;
	}
}
