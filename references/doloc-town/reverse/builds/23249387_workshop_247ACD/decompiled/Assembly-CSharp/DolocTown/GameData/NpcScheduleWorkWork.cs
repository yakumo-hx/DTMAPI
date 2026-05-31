using RedSaw.AI.LinearTask;

namespace DolocTown.GameData;

public class NpcScheduleWorkWork : NpcScheduleWork
{
	public override NpcScheduleWorkType Type => NpcScheduleWorkType.Work;

	public override LinearTask CreateTask(Npc npc)
	{
		return new NpcWorkTaskStreet();
	}
}
