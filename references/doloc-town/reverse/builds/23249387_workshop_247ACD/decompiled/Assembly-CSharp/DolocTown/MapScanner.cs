using UnityEngine;

namespace DolocTown;

public class MapScanner : IScanner
{
	private Room currentRoom;

	private MapManager mapManager => DolocAPI.archiveHandle.farmData.mapManager;

	public void OnRoomChanged(Room room)
	{
		if (currentRoom != room && room != null)
		{
			mapManager.SetCurrentRoom(room);
			currentRoom = room;
		}
	}

	public void OnWorldPosChanged(Vector2 positionWS)
	{
	}

	public void OnPosChanged(Vector2Int pos)
	{
		mapManager.ClearFog(DolocAPI.cameraController.position2d, circle: false);
	}
}
