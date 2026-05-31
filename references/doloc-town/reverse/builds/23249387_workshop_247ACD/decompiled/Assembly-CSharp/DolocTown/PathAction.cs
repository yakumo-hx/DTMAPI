using RedSaw.AI.LinearTask;
using UnityEngine;

namespace DolocTown;

public abstract class PathAction
{
	public abstract PathActionType ActionType { get; }

	public static void OutputPathInfos(PathAction[] path)
	{
		if (path.IsNullOrEmpty())
		{
			Debug.LogWarning("空路径");
			return;
		}
		foreach (PathAction pathAction in path)
		{
			if (pathAction is PathActionMove pathActionMove)
			{
				Debug.Log($"移动至:{pathActionMove.Dest}");
			}
			else if (pathAction is PathActionTeleport pathActionTeleport)
			{
				if (DolocAPI.assets.cityRooms.QueryData(pathActionTeleport.TargetSceneName, out var proto))
				{
					Debug.Log($"传送至场景\"{proto.name}\"的目标位置{pathActionTeleport.TargetScenePosition}");
				}
				else
				{
					Debug.Log($"传送至场景\"{pathActionTeleport.TargetSceneName}\"的目标位置{pathActionTeleport.TargetScenePosition}");
				}
			}
		}
	}

	public static LinearTask BuildTask(PathAction[] actions)
	{
		return null;
	}
}
