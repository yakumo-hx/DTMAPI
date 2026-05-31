using UnityEngine;

namespace DolocTown.GameData;

public static class RoomGeometryExtension
{
	public static Vector2 ValidateDronePosition(this RoomGeometry geometry, Vector2 originPosition)
	{
		Vector2Int vector2Int = geometry.CalcCellPosition(originPosition);
		if (geometry.IsNotObstacle(vector2Int))
		{
			return originPosition;
		}
		Vector2Int nearestEmptyPosition = geometry.GetNearestEmptyPosition(vector2Int);
		return geometry.CalcWorldPositionCenter(nearestEmptyPosition);
	}
}
