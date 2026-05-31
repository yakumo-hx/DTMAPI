using RedSaw.AI.LinearTask;

namespace DolocTown.GameData;

public class NpcScheduleWorkStreet : NpcScheduleWork
{
	public override NpcScheduleWorkType Type => NpcScheduleWorkType.Street;

	public override LinearTask CreateTask(Npc npc)
	{
		return new NpcWorkTaskStreet();
	}
}
