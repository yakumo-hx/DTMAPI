namespace DolocTown;

public class AnimalEvent
{
	public AnimalEventType Type { get; private set; }

	public GameEventArgs Args { get; private set; }

	public bool Used { get; private set; }

	public AnimalEvent(AnimalEventType type, GameEventArgs args = null)
	{
		Type = type;
		Args = args;
		Used = false;
	}

	public void Use()
	{
		if (!Used)
		{
			Used = true;
		}
	}
}
