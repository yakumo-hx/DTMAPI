using System;
using RedSaw.AI.LinearTask;

namespace DolocTown;

public class NpcTaskAction : NpcTask
{
	private readonly Action<Npc> callback;

	public NpcTaskAction(Action<Npc> callback)
	{
		this.callback = callback;
	}

	public override TaskStatus OnExecute(float dt)
	{
		callback?.Invoke(base._npc);
		return TaskStatus.Success;
	}
}
