using System;
using System.Collections.Generic;
using System.Linq;
using RedSaw.AI;
using UnityEngine;

namespace DolocTown;

public class AnimalPathFinderWidth : AStarBase, IAnimalPathFinder
{
	public readonly Room room;

	public readonly int width;

	public AnimalPathFinderWidth(Room room, int width = 2)
	{
		if (width <= 1)
		{
			throw new ArgumentException("with must be greater than 1", "width");
		}
		this.room = room;
		this.width = width;
	}

	private Vector2Int[] _GetNeighbours(Vector2Int pos)
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
			foreach (Vector2Int item in AnimalUtils._GetAroundPositions(pos, width))
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
			foreach (Vector2Int item2 in AnimalUtils._GetAroundPositions(pos, width))
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

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		return (from n in _GetNeighbours(pos)
			where !IsObstacle(n)
			select n).ToArray();
	}

	protected override bool IsObstacle(Vector2Int pos)
	{
		return !AnimalUtils.IsPositionWalkable(room, pos, width);
	}

	protected override int Distance(Vector2Int from, Vector2Int to)
	{
		return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) * width;
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
