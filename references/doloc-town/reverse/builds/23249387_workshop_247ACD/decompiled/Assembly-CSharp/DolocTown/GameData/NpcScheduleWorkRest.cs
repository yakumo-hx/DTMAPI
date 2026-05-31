using RedSaw.AI.LinearTask;

namespace DolocTown.GameData;

public class NpcScheduleWorkRest : NpcScheduleWork
{
	public override NpcScheduleWorkType Type => NpcScheduleWorkType.Rest;

	public override LinearTask CreateTask(Npc npc)
	{
		return new NpcWorkTaskStreet();
	}
}
