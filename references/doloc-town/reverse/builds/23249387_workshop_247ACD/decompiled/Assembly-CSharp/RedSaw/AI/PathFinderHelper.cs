using UnityEngine;

namespace RedSaw.AI;

public static class PathFinderHelper
{
	public static readonly Vector2Int[] Neighbours4 = new Vector2Int[4]
	{
		Vector2Int.left,
		Vector2Int.right,
		Vector2Int.up,
		Vector2Int.down
	};

	public static readonly Vector2Int[] Neighbours8 = new Vector2Int[8]
	{
		Vector2Int.left,
		Vector2Int.right,
		Vector2Int.up,
		Vector2Int.down,
		Vector2Int.one,
		Vector2Int.one * -1,
		new Vector2Int(1, -1),
		new Vector2Int(-1, 1)
	};
}
