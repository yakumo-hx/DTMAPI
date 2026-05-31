using DolocTown.GameData;

namespace DolocTown.NodeCanvas;

public abstract class NpcScheduleNodeResult : NpcScheduleNode, INpcScheduleNodeTargetScene, INpcScheduleNode
{
	public abstract string MarkPointName { get; }

	public abstract NpcScheduleWork Work { get; }
}
