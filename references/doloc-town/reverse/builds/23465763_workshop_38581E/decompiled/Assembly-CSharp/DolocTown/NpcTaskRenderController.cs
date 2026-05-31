using DolocTown.GameData;

namespace DolocTown;

public class NpcTaskRenderController
{
	private bool isWorking;

	private NpcScheduleWorkType currentWorkType;

	protected Npc npc { get; private set; }

	public void BindNpc(Npc npc)
	{
		this.npc = npc;
	}

	public virtual void OnRenderNpc()
	{
	}

	public virtual void OnUnRenderNpc()
	{
	}

	public virtual void OnTaskBegin(NpcScheduleWorkType workType)
	{
		isWorking = true;
		currentWorkType = workType;
		npc.Renderer.PlayAnimation("relax", force: false);
	}

	public virtual void OnTaskBreak(NpcScheduleWorkType workType)
	{
		isWorking = false;
	}

	public virtual void OnTaskSuccess(NpcScheduleWorkType workType)
	{
		isWorking = false;
	}

	public virtual void OnTaskFail(NpcScheduleWorkType workType)
	{
		isWorking = false;
	}

	public virtual void OnTaskExecute(NpcScheduleWorkType workType)
	{
	}
}
