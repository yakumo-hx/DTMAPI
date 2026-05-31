namespace DolocTown;

public class GameMessage
{
	public readonly GameEventType Type;

	public readonly GameEventArgs Args;

	public bool IsUsed { get; private set; }

	public GameMessage(GameEventType type, GameEventArgs args = null)
	{
		Type = type;
		Args = args;
	}

	public GameMessage(GameEventType type, string args)
	{
		Type = type;
		Args = new GameEventArgsString(args);
	}

	public GameMessage(GameEventType type, int args)
	{
		Type = type;
		Args = new GameEventArgsInt(args);
	}

	public void Use()
	{
		IsUsed = true;
	}

	public GameMessage Clone()
	{
		return new GameMessage(Type, Args);
	}
}
