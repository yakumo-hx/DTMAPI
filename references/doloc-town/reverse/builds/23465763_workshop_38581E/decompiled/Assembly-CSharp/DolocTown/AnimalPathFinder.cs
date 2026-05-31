using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw.AI;
using UnityEngine;

namespace DolocTown;

public class AnimalPathFinder : AStarBase, IAnimalPathFinder
{
	public readonly Room room;

	public AnimalPathFinder(Room room)
	{
		this.room = room ?? throw new ArgumentException("Room cannot be null", "room");
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		if (room.Geometry.IsOnGround(pos))
		{
			Vector2Int vector2Int = pos + new Vector2Int(-1, 0);
			Vector2Int vector2Int2 = pos + new Vector2Int(1, 0);
			if (room.Geometry.IsOnGround(vector2Int))
			{
				list.Add(vector2Int);
			}
			if (room.Geometry.IsOnGround(vector2Int2))
			{
				list.Add(vector2Int2);
			}
			foreach (Vector2Int item in AnimalUtils._GetAroundPositions(pos, 1))
			{
				if (AnimalUtils.IsOnPlatformOrBuilding(room, item))
				{
					list.Add(item);
				}
			}
			return list.Where(room.Geometry.Contains).ToArray();
		}
		if (AnimalUtils.IsOnPlatformOrBuilding(room, pos))
		{
			foreach (Vector2Int item2 in AnimalUtils._GetAroundPositions(pos, 1))
			{
				if (room.Geometry.IsOnGround(item2) || AnimalUtils.IsOnPlatformOrBuilding(room, item2))
				{
					list.Add(item2);
				}
			}
			return list.Where(room.Geometry.Contains).ToArray();
		}
		return Array.Empty<Vector2Int>();
	}

	public Vector2Int[] FindPath(Vector2Int from, Vector2Int to)
	{
		if (IsObstacle(from) || IsObstacle(to))
		{
			return null;
		}
		if (!(from == to))
		{
			return FindPath(from, to, fromDir: false);
		}
		return new Vector2Int[1] { from };
	}

	public void OnEnvChanged()
	{
	}
}
