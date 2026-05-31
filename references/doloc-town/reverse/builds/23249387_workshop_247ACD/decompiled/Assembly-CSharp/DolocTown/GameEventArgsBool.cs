namespace DolocTown;

public class GameEventArgsBool : GameEventArgs
{
	public readonly bool value;

	public GameEventArgsBool(bool value)
	{
		this.value = value;
	}

	public bool Check(string args)
	{
		if (!args.IsNullOrEmpty())
		{
			return value.ToString() == args;
		}
		return false;
	}

	public bool Check(bool args)
	{
		return value == args;
	}

	public override string ToString()
	{
		return $"<bool> {value}";
	}
}
