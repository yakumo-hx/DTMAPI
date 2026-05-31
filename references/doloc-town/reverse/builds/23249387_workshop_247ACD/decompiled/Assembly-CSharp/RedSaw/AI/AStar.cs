using System;
using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class AStar : AStarBase, IPathFinder
{
	private IGameMap _gameMap;

	private readonly Vector2Int[] _neighbours;

	public AStar(bool constraintDiagonal = false)
	{
		Vector2Int[] array = (constraintDiagonal ? PathFinderHelper.Neighbours4 : PathFinderHelper.Neighbours8);
		_neighbours = new Vector2Int[array.Length];
		Array.Copy(array, _neighbours, array.Length);
	}

	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		_gameMap = map;
		return PathFinderUtils.SimplifyPath(FindPath(f, to));
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] neighbours = _neighbours;
		foreach (Vector2Int vector2Int in neighbours)
		{
			Vector2Int vector2Int2 = pos + vector2Int;
			if (!_gameMap.IsObstacle(vector2Int2))
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
		return _gameMap.IsObstacle(pos);
	}
}
