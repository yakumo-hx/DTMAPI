using DolocTown.GameData;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class NpcWorkTaskRelax : NpcWorkTask
{
	public override NpcScheduleWorkType WorkType => NpcScheduleWorkType.Relax;

	public override void OnBegin()
	{
		if (!(base._npc?.Renderer == null))
		{
			base._npc.StopMove();
			base.TaskController.OnTaskBegin(NpcScheduleWorkType.Relax);
		}
	}

	public override void OnBreak()
	{
		base.OnBreak();
		base.TaskController.OnTaskBreak(NpcScheduleWorkType.Relax);
	}

	public override void OnFailure()
	{
		base.OnFailure();
		base.TaskController.OnTaskFail(NpcScheduleWorkType.Relax);
	}

	public override void OnSuccess()
	{
		base.OnSuccess();
		base.TaskController.OnTaskSuccess(NpcScheduleWorkType.Relax);
	}

	public override TaskStatus OnExecute(float dt)
	{
		base.TaskController.OnTaskExecute(NpcScheduleWorkType.Relax);
		return TaskStatus.Executing;
	}
}
