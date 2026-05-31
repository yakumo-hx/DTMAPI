namespace DolocTown;

public class GameEventArgsString : GameEventArgs
{
	public readonly string value;

	public GameEventArgsString(string value)
	{
		this.value = value;
	}

	public string _ToString()
	{
		return "<string> \"" + value + "\"";
	}

	public override string ToString()
	{
		return value;
	}
}
