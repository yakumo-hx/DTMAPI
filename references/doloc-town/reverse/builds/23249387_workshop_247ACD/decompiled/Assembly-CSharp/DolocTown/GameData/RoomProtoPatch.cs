using RedSaw.GameMap;
using UnityEngine;

namespace DolocTown.GameData;

public static class RoomProtoPatch
{
	public static (IGameMap, IGameMap) CreateGameMaps(this RoomProto room, Vector2 cellsize)
	{
		Vector2Int gridSize = room.geometry.gridSize;
		Vector2 roomPosition = room.geometry.gridPos * DolocTransform.TILE_WORLD_SIZE;
		if (room.roomType == RoomType.Dungeon)
		{
			SimpleGameMap item = new SimpleGameMap(gridSize, room.geometry.totalObstacles, roomPosition, cellsize);
			SimpleGameMap item2 = new SimpleGameMap(gridSize, room.geometry.obstacles, roomPosition, cellsize);
			return (item, item2);
		}
		SimpleGameMap simpleGameMap = new SimpleGameMap(gridSize, room.geometry.obstacles, roomPosition, cellsize);
		return (simpleGameMap, simpleGameMap);
	}
}
