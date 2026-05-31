using RedSaw;
using UnityEngine;

namespace DolocTown.Editor;

public class PositionMarker : MonoBehaviour
{
	[SerializeField]
	private Color color;

	[SerializeField]
	private bool inCurrentRoom;

	public Vector2Int GridPosition
	{
		get
		{
			if (inCurrentRoom && Application.isPlaying && DolocAPI.IsGameInitialized)
			{
				return DolocAPI.CurrentRoom.Geometry.CalcCellPosition(base.transform.position);
			}
			Vector3 position = base.transform.position;
			int x = Mathf.FloorToInt(position.x * 0.66667f);
			int y = Mathf.FloorToInt(position.y * 0.66667f);
			return new Vector2Int(x, y);
		}
	}

	private void OnDrawGizmos()
	{
		if (inCurrentRoom && Application.isPlaying && DolocAPI.IsGameInitialized)
		{
			Room currentRoom = DolocAPI.CurrentRoom;
			if (currentRoom != null)
			{
				Vector2Int cellPosition = currentRoom.Geometry.CalcCellPosition(base.transform.position);
				GizmosHelper.DrawBoxLB(currentRoom.Geometry.CalcWorldPosition(cellPosition), DolocTransform.TILE_WORLD_SIZE, color);
			}
		}
		else
		{
			GizmosHelper.DrawBoxLB(GridPosition * DolocTransform.TILE_WORLD_SIZE, DolocTransform.TILE_WORLD_SIZE, color);
		}
	}

	private Vector2 GridSnap(Vector2 pos, Vector2 size)
	{
		float x = Mathf.Floor(pos.x / size.x) * size.x;
		float y = Mathf.Floor(pos.y / size.y) * size.y;
		return new Vector2(x, y);
	}
}
