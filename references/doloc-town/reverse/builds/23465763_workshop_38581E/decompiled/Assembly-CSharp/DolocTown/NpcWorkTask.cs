using DolocTown.GameData;

namespace DolocTown;

public abstract class NpcWorkTask : NpcTask
{
	protected NpcTaskRenderController TaskController => base._npc.TaskRenderController;

	public abstract NpcScheduleWorkType WorkType { get; }
}
