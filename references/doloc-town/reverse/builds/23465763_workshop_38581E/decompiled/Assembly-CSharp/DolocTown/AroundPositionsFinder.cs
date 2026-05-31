using RedSaw;
using UnityEngine;

namespace DolocTown;

public class AroundPositionsFinder : MonoBehaviour
{
	[SerializeField]
	private bool drawBox;

	[SerializeField]
	private int maxDistance = 3;

	private void OnDrawGizmos()
	{
		if (!drawBox)
		{
			return;
		}
		foreach (Vector2Int item in Vector2Int.zero.AroundPositionsEuclidean(maxDistance))
		{
			DrawBox(item);
		}
	}

	private void DrawBox(Vector2Int position)
	{
		GizmosHelper.DrawBoxMM((Vector3)(position * DolocTransform.TILE_WORLD_SIZE) + base.transform.position, DolocTransform.TILE_WORLD_SIZE, Color.yellow);
	}
}
