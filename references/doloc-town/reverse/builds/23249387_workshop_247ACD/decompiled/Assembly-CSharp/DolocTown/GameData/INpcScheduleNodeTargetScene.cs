namespace DolocTown.GameData;

public interface INpcScheduleNodeTargetScene : INpcScheduleNode
{
	string MarkPointName { get; }

	NpcScheduleWork Work { get; }
}
