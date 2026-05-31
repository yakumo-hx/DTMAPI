using RedSaw.AI.LinearTask;

namespace DolocTown.GameData;

public class NpcScheduleWorkRelax : NpcScheduleWork
{
	public override NpcScheduleWorkType Type => NpcScheduleWorkType.Relax;

	public override LinearTask CreateTask(Npc npc)
	{
		return new NpcWorkTaskRelax();
	}
}
