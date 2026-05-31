using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class ImpactGround : IPathFinder
{
	private readonly int _impactHeight;

	private IGameMap _map;

	private Vector2Int _to;

	public ImpactGround(int impactHeight = 5)
	{
		_impactHeight = impactHeight;
	}

	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		_map = map;
		_to = to;
		Vector2Int vector2Int = ((to.x >= f.x) ? Vector2Int.right : Vector2Int.left);
		Vector2Int[] array = new Vector2Int[2]
		{
			vector2Int,
			-vector2Int
		};
		foreach (Vector2Int moveDir in array)
		{
			Vector2Int[] array2 = Impact(f, moveDir);
			if (array2 != null)
			{
				return array2;
			}
		}
		return null;
	}

	private Vector2Int[] Impact(Vector2Int start, Vector2Int moveDir)
	{
		List<Vector2Int> list = new List<Vector2Int> { start };
		Vector2Int vector2Int = start;
		Vector2Int vector2Int2 = vector2Int;
		while (vector2Int.x >= 0 && vector2Int.x < _map.Width)
		{
			if (vector2Int == _to)
			{
				if (vector2Int != start)
				{
					list.Add(vector2Int);
				}
				return list.ToArray();
			}
			if (_map.IsGround(vector2Int))
			{
				vector2Int2 = vector2Int;
				vector2Int += moveDir;
				continue;
			}
			if (!SearchGround(vector2Int, _impactHeight, out var ground, out var isGully))
			{
				if (!isGully)
				{
					Debug.Log("不是沟壑，无法继续前进");
					return null;
				}
				if (!SearchGroundJump(vector2Int, _impactHeight, 5, moveDir.x, out ground))
				{
					return null;
				}
			}
			if (vector2Int2 != start)
			{
				list.Add(vector2Int2);
			}
			list.Add(ground);
			vector2Int = ground;
		}
		return null;
	}

	private bool SearchGround(Vector2Int pos, int vRange, out Vector2Int ground, out bool isGully)
	{
		isGully = true;
		int num = Mathf.Max(0, -vRange + pos.y);
		int num2 = Mathf.Min(_map.Height - 1, vRange + pos.y);
		for (int i = num; i <= num2; i++)
		{
			if (i != pos.y)
			{
				ground = new Vector2Int(pos.x, i);
				if (_map.IsGround(ground))
				{
					return true;
				}
				if (isGully && ground.y > pos.y && _map.IsObstacle(ground))
				{
					Debug.Log($"沟壑检查失败:{ground}");
					isGully = false;
				}
			}
		}
		ground = default(Vector2Int);
		return false;
	}

	private bool SearchGroundJump(Vector2Int pos, int vRange, int hRange, int dir, out Vector2Int ground)
	{
		Debug.Log($"查询{pos}附近的地面");
		ground = default(Vector2Int);
		bool flag = true;
		int num = Mathf.Max(0, -vRange + pos.y);
		int num2 = Mathf.Min(_map.Height - 1, vRange + pos.y);
		for (int i = 1; i <= hRange; i++)
		{
			if (!flag)
			{
				return false;
			}
			for (int j = num; j <= num2; j++)
			{
				ground = new Vector2Int(pos.x + i * dir, j);
				if (_map.IsGround(ground))
				{
					return true;
				}
				if (ground.y > pos.y && _map.IsObstacle(ground))
				{
					flag = false;
				}
			}
		}
		return false;
	}
}
