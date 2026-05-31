using UnityEngine;

namespace DolocTown;

public class PathActionMove : PathAction
{
	public readonly Vector2 Dest;

	public override PathActionType ActionType => PathActionType.Move;

	public PathActionMove(Vector2 dest)
	{
		Dest = dest;
		if (Dest.x < -3f)
		{
			Debug.LogError("目标位置超出左边界");
		}
	}
}
