namespace DolocTown;

public class GameEventArgsInt : GameEventArgs
{
	public readonly int value;

	public GameEventArgsInt(int value)
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

	public override string ToString()
	{
		return $"<int> {value}";
	}
}
