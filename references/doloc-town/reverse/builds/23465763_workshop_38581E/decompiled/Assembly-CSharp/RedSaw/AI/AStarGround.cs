using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class AStarGround : AStarBase, IPathFinder
{
	private IGameMap _map;

	private readonly Vector2Int[] _neighbours;

	public AStarGround(int range = 3)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -range; j <= range; j++)
			{
				if (i != 0 || j != 0)
				{
					list.Add(new Vector2Int(i, j));
				}
			}
		}
		_neighbours = list.ToArray();
	}

	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		_map = map;
		return PathFinderUtils.SimplifyGroundPath(FindPath(f, to));
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] neighbours = _neighbours;
		foreach (Vector2Int vector2Int in neighbours)
		{
			Vector2Int vector2Int2 = pos + vector2Int;
			if (_map.IsGround(vector2Int2))
			{
				list.Add(vector2Int2);
			}
		}
		return list.ToArray();
	}

	protected override Vector2Int[] GetNeighbourDirs()
	{
		return _neighbours;
	}

	protected override bool IsObstacle(Vector2Int pos)
	{
		return !_map.IsGround(pos);
	}
}
