using RedSaw;
using UnityEngine;

namespace DolocTown.Editor;

public class PathDrawer : MonoBehaviour
{
	[SerializeField]
	private Color startColor = Color.red;

	[SerializeField]
	private Color endColor = Color.blue;

	[SerializeField]
	private Color pathColor = Color.green;

	[SerializeField]
	private Color lineColor = Color.yellow;

	[SerializeField]
	private Vector2 padding = new Vector2(0.1f, 0.1f);

	public Vector2Int[] Path { get; set; }

	public Vector2 Offset { get; set; }

	private void OnDrawGizmos()
	{
		if (Path.IsNullOrEmpty())
		{
			return;
		}
		Vector2 size = DolocTransform.TILE_WORLD_SIZE - padding * 2f;
		Vector2 vector = DolocTransform.TILE_WORLD_SIZE * 0.5f + Offset;
		Vector2 vector2 = padding + Offset;
		if (Path.Length == 1)
		{
			Gizmos.color = startColor;
			GizmosHelper.DrawBoxLB((Vector2)Path[0] * 1.5f + vector2, size);
			return;
		}
		Gizmos.color = startColor;
		GizmosHelper.DrawBoxLB((Vector2)Path[0] * 1.5f + vector2, size);
		Vector2Int vector2Int = Path[0];
		for (int i = 1; i < Path.Length - 1; i++)
		{
			Gizmos.color = lineColor;
			Gizmos.DrawLine((Vector2)vector2Int * 1.5f + vector, (Vector2)Path[i] * 1.5f + vector);
			Gizmos.color = pathColor;
			GizmosHelper.DrawBoxLB((Vector2)Path[i] * 1.5f + vector2, size);
			vector2Int = Path[i];
		}
		Gizmos.color = endColor;
		GizmosHelper.DrawBoxLB((Vector2)Path[^1] * 1.5f + vector2, size);
		Gizmos.color = lineColor;
		Gizmos.DrawLine((Vector2)vector2Int * 1.5f + vector, (Vector2)Path[^1] * 1.5f + vector);
	}
}
