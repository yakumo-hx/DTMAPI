using RedSaw.AI.LinearTask;

namespace DolocTown;

public abstract class NpcTask : LinearTask
{
	public Npc _npc { get; private set; }

	public string NpcName => _npc.NpcName;

	public void SetNpc(Npc npc)
	{
		_npc = npc;
	}
}
