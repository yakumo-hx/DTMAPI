using Newtonsoft.Json;

namespace DolocTown;

public class GameEventRecorder
{
	[JsonProperty]
	public int totalCount { get; private set; }

	public GameEventRecorder()
	{
		totalCount = 0;
	}

	[JsonConstructor]
	protected GameEventRecorder(int totalCount)
	{
		this.totalCount = totalCount;
	}

	public virtual int Record(GameEventArgs args)
	{
		return ++totalCount;
	}
}
