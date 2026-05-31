using DolocTown.GameData;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class NpcWorkTaskStreet : NpcWorkTask
{
	public override NpcScheduleWorkType WorkType => NpcScheduleWorkType.Street;

	public override void OnBegin()
	{
		if (!(base._npc?.Renderer == null))
		{
			base._npc.StopMove();
			base._npc.ClearSeenNpcs();
		}
	}

	public override TaskStatus OnExecute(float dt)
	{
		return TaskStatus.Executing;
	}
}
