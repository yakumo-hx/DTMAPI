using UnityEngine;

namespace DolocTown;

public class PathActionTeleport : PathAction
{
	public readonly string TargetSceneName;

	public readonly Vector2 TargetScenePosition;

	public override PathActionType ActionType => PathActionType.Teleport;

	public PathActionTeleport(string targetSceneName, Vector2 targetScenePosition)
	{
		TargetSceneName = targetSceneName;
		TargetScenePosition = targetScenePosition;
	}
}
