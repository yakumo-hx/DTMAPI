using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class JpsL : JpsBase, IPathFinder
{
	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		FindPath(map, f, to, out var path);
		return path;
	}

	public override bool FindPath(IGameMap env, Vector2Int f, Vector2Int to, out Vector2Int[] path)
	{
		if (f == to)
		{
			path = new Vector2Int[1] { f };
			return true;
		}
		lut.Clear();
		nodes.clear();
		base.env = env;
		from = f;
		base.to = to;
		addDirections(f, JpsUtils.allDirections, 0);
		addLut(f, f, 0);
		while (nodes.notEmpty)
		{
			JpsDirection jpsDirection = nodes.get();
			if (jpsDirection.pos == base.to)
			{
				path = completePath();
				return true;
			}
			if (isLineDireaction(jpsDirection.direction))
			{
				testLine(jpsDirection.pos, jpsDirection.direction, jpsDirection.cost);
			}
			else
			{
				testDiagonal(jpsDirection.pos, jpsDirection.direction, jpsDirection.cost);
			}
		}
		path = null;
		return false;
	}

	protected void testDiagonal(Vector2Int parent, Vector2Int d, int fcost)
	{
		Vector2Int vector2Int = parent;
		Vector2Int pos = new Vector2Int(vector2Int.x + d.x, vector2Int.y);
		while (env.IsEmpty(pos))
		{
			Vector2Int pos2 = new Vector2Int(vector2Int.x, vector2Int.y + d.y);
			if (env.IsEmpty(pos2))
			{
				vector2Int += d;
				if (env.IsEmpty(vector2Int))
				{
					fcost += 2;
					if (testEnd(parent, vector2Int, fcost))
					{
						break;
					}
					if (diagonalExplore(vector2Int, d, fcost))
					{
						addLut(parent, vector2Int, fcost);
					}
					pos = new Vector2Int(vector2Int.x + d.x, vector2Int.y);
					continue;
				}
				break;
			}
			break;
		}
	}

	protected bool diagonalExplore(Vector2Int p, Vector2Int d, int cost)
	{
		bool num = testLine(p, new Vector2Int(d.x, 0), cost);
		bool flag = testLine(p, new Vector2Int(0, d.y), cost);
		return num || flag;
	}

	protected bool testLine(Vector2Int parent, Vector2Int d, int fcost)
	{
		Vector2Int[] array = JpsBase.verticalLut[d];
		Vector2Int vector2Int = array[0] + parent;
		Vector2Int vector2Int2 = array[1] + parent;
		for (Vector2Int vector2Int3 = parent + d; env.IsEmpty(vector2Int3); vector2Int3 += d)
		{
			fcost++;
			if (testEnd(parent, vector2Int3, fcost))
			{
				return true;
			}
			List<Vector2Int> list = new List<Vector2Int>();
			Vector2Int vector2Int4 = vector2Int + d;
			if (env.IsObstacle(vector2Int) && env.IsEmpty(vector2Int4))
			{
				list.Add(array[0] + d);
				list.Add(array[0]);
			}
			Vector2Int vector2Int5 = vector2Int2 + d;
			if (env.IsObstacle(vector2Int2) && env.IsEmpty(vector2Int5))
			{
				list.Add(array[1] + d);
				list.Add(array[1]);
			}
			if (list.Count > 0)
			{
				list.Add(d);
				addDirections(vector2Int3, list.ToArray(), fcost);
				addLut(parent, vector2Int3, fcost);
				return true;
			}
			vector2Int = vector2Int4;
			vector2Int2 = vector2Int5;
		}
		return false;
	}

	protected List<Vector2Int> testForceNeighborsInLine(Vector2Int p, Vector2Int d)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] array = JpsBase.verticalLut[d];
		foreach (Vector2Int vector2Int in array)
		{
			Vector2Int vector2Int2 = vector2Int + p;
			if (env.IsObstacle(vector2Int2) && env.IsEmpty(vector2Int2 + d))
			{
				list.Add(vector2Int + d);
				list.Add(vector2Int);
			}
		}
		return list;
	}
}
