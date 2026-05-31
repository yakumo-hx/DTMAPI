using Newtonsoft.Json;
using RedSaw.CommandLineInterface;

namespace DolocTown;

[DebugObject]
[JsonObject(MemberSerialization.OptIn)]
public abstract class AutomateParam
{
	protected AutomateBot Bot { get; private set; }

	protected AutomateParam(AutomateBot bot)
	{
		Bot = bot;
	}

	[JsonConstructor]
	protected AutomateParam()
	{
	}

	public virtual void AfterLoadAutomateBot(AutomateBot bot)
	{
		Bot = bot;
	}

	public abstract void LoadDefault();
}
