using RedSaw;
using UnityEngine;

namespace DolocTown;

public class StuckHelper : MonoBehaviour
{
	public static Vector2Int CellPosition
	{
		get
		{
			Vector3 agentPosition = DolocAPI.AgentPosition;
			agentPosition.y += 0.5f;
			return DolocAPI.archiveHandle.currentRoom.Geometry.CalcMinCellPosition(agentPosition);
		}
	}

	private void OnDrawGizmos()
	{
		GizmosHelper.DrawBoxLB(CellPosition * DolocTransform.TILE_WORLD_SIZE + DolocAPI.CurrentRoom.RoomPosition, DolocTransform.TILE_WORLD_SIZE, Color.magenta);
	}
}
