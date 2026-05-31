using System;

namespace DolocTown;

public abstract class GameStatePlugin
{
	public Type Id => GetType();

	public virtual void BeforeSwitch(IDolocGameState lst, IDolocGameState cur)
	{
	}

	public virtual void AfterSwitch(IDolocGameState lst, IDolocGameState cur)
	{
	}
}
