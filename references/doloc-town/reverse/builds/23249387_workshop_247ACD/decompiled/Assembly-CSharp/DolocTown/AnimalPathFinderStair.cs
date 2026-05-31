using System;
using System.Linq;
using RedSaw;
using RedSaw.AI;
using UnityEngine;

namespace DolocTown;

public class AnimalPathFinderStair : AStarBase
{
	private readonly AnimalMap animalMap;

	public readonly int touchThreshold;

	public readonly int animalWidth;

	public AnimalPathFinderStair(AnimalMap animalMap, int width = 1)
	{
		this.animalMap = animalMap;
		animalWidth = width;
		touchThreshold = ((width % 2 == 0) ? (width / 2) : ((width - 1) / 2));
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		if (!animalMap.TryFindStair(pos, out var stair))
		{
			return Array.Empty<Vector2Int>();
		}
		return (from x in animalMap.TryGetNearStairs(stair, touchThreshold)
			where x.length >= animalWidth
			select x.start).ToArray();
	}

	public Vector2Int[] FindPath(Vector2Int from, Vector2Int to)
	{
		if (from == to)
		{
			return new Vector2Int[1] { from };
		}
		if (!animalMap.TryFindStair(from, out var stair) || !animalMap.TryFindStair(to, out var stair2))
		{
			return null;
		}
		if (stair == stair2)
		{
			return new Vector2Int[2] { from, to };
		}
		return null;
	}

	public Vector2Int[] FindPathStair(Vector2Int stFrom, Vector2Int stTo)
	{
		return FindPath(stFrom, stTo, fromDir: false);
	}

	protected override int Distance(Vector2Int from, Vector2Int to)
	{
		AnimalStair animalStair = animalMap.FindStair(from);
		AnimalStair animalStair2 = animalMap.FindStair(to);
		return animalStair.center.ManhattenDistance(animalStair2.center);
	}

	protected override Vector2Int[] GetNeighbourDirs()
	{
		return Array.Empty<Vector2Int>();
	}

	protected override bool IsObstacle(Vector2Int pos)
	{
		return false;
	}
}
