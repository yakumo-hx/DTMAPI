namespace DolocTown;

public class GameEventArgsFloat : GameEventArgs
{
	public float value;

	public GameEventArgsFloat(float value)
	{
		this.value = value;
	}

	public override string ToString()
	{
		return $"<float> {value}";
	}
}
