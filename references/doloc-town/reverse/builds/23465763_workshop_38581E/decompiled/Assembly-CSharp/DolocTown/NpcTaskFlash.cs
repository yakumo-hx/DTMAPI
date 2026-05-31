using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public class NpcTaskFlash : NpcTask
{
	private readonly string sceneName;

	private readonly bool force;

	private readonly bool hasPointPosition;

	private readonly Vector2 targetPosition;

	public NpcTaskFlash(string sceneName, bool force = true)
	{
		this.sceneName = sceneName;
		this.force = force;
		hasPointPosition = false;
		targetPosition = default(Vector2);
	}

	public NpcTaskFlash(string sceneName, Vector2 position, bool force = true)
	{
		this.sceneName = sceneName;
		targetPosition = position;
		hasPointPosition = true;
		this.force = force;
	}

	public override TaskStatus OnExecute(float dt)
	{
		if (!force && (DolocAPI.IsInPlayerScene(base._npc.sceneName) || DolocAPI.IsInPlayerScene(sceneName)))
		{
			return TaskStatus.Executing;
		}
		if (hasPointPosition)
		{
			base._npc.__EnterScene(sceneName, targetPosition, shouldSnapToGround: true);
			return TaskStatus.Success;
		}
		base._npc.__EnterScene(sceneName, Vector2.zero, shouldSnapToGround: true);
		return TaskStatus.Success;
	}
}
