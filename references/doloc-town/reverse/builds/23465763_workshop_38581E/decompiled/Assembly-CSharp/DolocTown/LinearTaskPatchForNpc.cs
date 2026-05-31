using System;
using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public static class LinearTaskPatchForNpc
{
	public static LinearTask NpcEnterScene(this LinearTask mission, string sceneName, Vector2 remote)
	{
		NpcTaskAction task = new NpcTaskAction(delegate(Npc npc)
		{
			npc.__EnterScene(sceneName, remote, shouldSnapToGround: false);
		});
		return mission.Then(task);
	}

	public static LinearTask NpcMove(this LinearTask mission, float dest)
	{
		NpcTaskMove task = new NpcTaskMove(dest);
		return mission.Then(task);
	}

	public static LinearTask NpcDo(this LinearTask mission, Action<Npc> action)
	{
		NpcTaskAction task = new NpcTaskAction(action);
		return mission.Then(task);
	}
}
