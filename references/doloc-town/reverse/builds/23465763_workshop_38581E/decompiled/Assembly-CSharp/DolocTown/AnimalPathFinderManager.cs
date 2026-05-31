using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class AnimalPathFinderManager
{
	private readonly Dictionary<Room, AnimalPathFinderForRoom> pathFinderCache = new Dictionary<Room, AnimalPathFinderForRoom>();

	public void RemovePathFinder(Room room)
	{
		if (room != null && pathFinderCache.ContainsKey(room))
		{
			pathFinderCache.Remove(room);
		}
	}

	public AnimalPathFinderForRoom GetPathFinderForRoom(Room room)
	{
		if (room == null)
		{
			return null;
		}
		if (pathFinderCache.TryGetValue(room, out var value))
		{
			return value;
		}
		value = new AnimalPathFinderForRoom(room);
		pathFinderCache[room] = value;
		return value;
	}

	public void OnEnvChanged(Room room)
	{
		if (pathFinderCache.TryGetValue(room, out var value))
		{
			value.OnEnvChanged();
		}
	}

	public Vector2Int[] FindPath(Room room, Vector2Int from, Vector2Int to, int width)
	{
		AnimalPathFinderForRoom pathFinderForRoom = GetPathFinderForRoom(room);
		if (pathFinderForRoom == null)
		{
			Debug.LogWarning("AnimalPathFinderManager: No path finder for room: " + room?.Title);
			return null;
		}
		return pathFinderForRoom.FindPath(from, to, width);
	}
}
