using System;
using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class ImpactGroundPlus : AStarBase, IPathFinder
{
	private readonly int vRange;

	private readonly int hRange;

	private readonly int gullyTolerance;

	private HashSet<Vector2Int> _jumpPoints = new HashSet<Vector2Int>();

	private IGameMap _map;

	public ImpactGroundPlus(int vRange = 5, int hRange = 4, int gullyTolerance = 3)
	{
		this.vRange = vRange;
		this.hRange = hRange;
		this.gullyTolerance = gullyTolerance;
	}

	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		_map = map;
		_jumpPoints.Clear();
		return SimplifyGroundPath(FindPath(f, to, fromDir: false));
	}

	private Vector2Int[] SimplifyGroundPath(Vector2Int[] path)
	{
		if (path == null || path.Length <= 2)
		{
			return path;
		}
		Vector2Int vector2Int = path[0];
		Vector2Int vector2Int2 = vector2Int;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 1; i < path.Length; i++)
		{
			Vector2Int vector2Int3 = path[i];
			if (vector2Int3.y != vector2Int.y || _jumpPoints.Contains(vector2Int3))
			{
				list.Add(vector2Int);
				if (vector2Int != vector2Int2)
				{
					list.Add(vector2Int2);
				}
				vector2Int = vector2Int3;
			}
			vector2Int2 = vector2Int3;
		}
		list.Add(vector2Int);
		if (vector2Int != vector2Int2)
		{
			list.Add(vector2Int2);
		}
		return list.ToArray();
	}

	private Vector2Int[] FindGroundPointsOfSide(Vector2Int side, int dir, int vRange, int hRange)
	{
		if (_map.IsGround(side))
		{
			return new Vector2Int[1] { side };
		}
		List<Vector2Int> list = new List<Vector2Int>();
		bool isGully = true;
		for (int i = 0; i <= hRange; i++)
		{
			if (!isGully)
			{
				break;
			}
			Vector2Int pos = new Vector2Int(i * dir + side.x, side.y);
			if (!FindGround(pos, vRange, out var points, out isGully))
			{
				continue;
			}
			list.AddRange(points);
			if (i != 0)
			{
				Vector2Int[] array = points;
				foreach (Vector2Int item in array)
				{
					_jumpPoints.Add(item);
				}
			}
		}
		return list.ToArray();
	}

	private bool FindGround(Vector2Int pos, int vRange, out Vector2Int[] points, out bool isGully)
	{
		isGully = true;
		int num = Mathf.Max(0, -vRange + pos.y);
		int num2 = Mathf.Min(_map.Height - 1, vRange + pos.y);
		List<Vector2Int> list = new List<Vector2Int>();
		int num3 = -1;
		for (int i = num; i <= num2; i++)
		{
			Vector2Int vector2Int = new Vector2Int(pos.x, i);
			if (_map.IsGround(vector2Int))
			{
				list.Add(vector2Int);
			}
			if (isGully && vector2Int.y >= pos.y && vector2Int.y - pos.y < gullyTolerance && _map.IsObstacle(vector2Int))
			{
				num3 = vector2Int.y;
				isGully = false;
			}
		}
		List<Vector2Int> list2 = new List<Vector2Int>();
		foreach (Vector2Int item in list)
		{
			if (item.y > num3)
			{
				list2.Add(item);
			}
		}
		points = list2.ToArray();
		return list2.Count > 0;
	}

	protected override Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		Vector2Int side = pos + Vector2Int.left;
		Vector2Int side2 = pos + Vector2Int.right;
		List<Vector2Int> list = new List<Vector2Int>();
		list.AddRange(FindGroundPointsOfSide(side, -1, vRange, hRange));
		list.AddRange(FindGroundPointsOfSide(side2, 1, vRange, hRange));
		return list.ToArray();
	}

	protected override Vector2Int[] GetNeighbourDirs()
	{
		return Array.Empty<Vector2Int>();
	}

	protected override bool IsObstacle(Vector2Int pos)
	{
		return !_map.IsGround(pos);
	}
}
