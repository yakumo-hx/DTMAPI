using Newtonsoft.Json;

namespace DolocTown;

public class GameEventRecorderBool : GameEventRecorder
{
	[JsonProperty]
	private readonly int[] boolCounts = new int[2];

	public GameEventRecorderBool()
	{
	}

	[JsonConstructor]
	public GameEventRecorderBool(int totalCount, int[] boolCounts)
		: base(totalCount)
	{
		this.boolCounts = boolCounts;
	}

	public override int Record(GameEventArgs args)
	{
		if (!(args is GameEventArgsBool gameEventArgsBool))
		{
			return 0;
		}
		base.Record(args);
		int num = (gameEventArgsBool.value ? 1 : 0);
		return ++boolCounts[num];
	}

	public int GetCount(bool value)
	{
		return boolCounts[value ? 1 : 0];
	}

	public override string ToString()
	{
		return $"True: {boolCounts[1]}\nFalse: {boolCounts[0]}";
	}
}
