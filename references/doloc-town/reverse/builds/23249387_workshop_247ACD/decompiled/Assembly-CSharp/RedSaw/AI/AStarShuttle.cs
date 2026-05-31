using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class AStarShuttle : AStarBase, IPathFinder
{
	private IGameMap _map;

	private readonly Vector2Int[] _neighbours;

	public AStarShuttle(int range = 10)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = -range; i <= range; i++)
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
		return FindPath(f, to);
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] neighbours = _neighbours;
		foreach (Vector2Int vector2Int in neighbours)
		{
			Vector2Int vector2Int2 = pos + vector2Int;
			if (!_map.IsObstacle(vector2Int2))
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
