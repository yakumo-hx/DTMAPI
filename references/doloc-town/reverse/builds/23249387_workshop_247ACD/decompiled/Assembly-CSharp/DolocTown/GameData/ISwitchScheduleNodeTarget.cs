namespace DolocTown.GameData;

public interface ISwitchScheduleNodeTarget : ISwitchScheduleNode
{
	bool ShouldLight { get; }
}
