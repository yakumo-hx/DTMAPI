using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class Jps : JpsBase
{
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
		lut.Add(f, new JpsNode(f, f, 0));
		while (nodes.notEmpty)
		{
			JpsDirection jpsDirection = nodes.get();
			if (jpsDirection.pos == to)
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
				testDiagonal(jpsDirection.pos, jpsDirection.pos, jpsDirection.direction, jpsDirection.cost);
			}
		}
		path = null;
		return false;
	}

	public void test(IGameMap env, Vector2Int s, Vector2Int e)
	{
		if (s == e)
		{
			return;
		}
		lut.Clear();
		nodes.clear();
		base.env = env;
		from = s;
		to = e;
		addDirections(s, JpsUtils.allDirections, 0);
		lut.Add(s, new JpsNode(s, s, 0));
		while (nodes.notEmpty)
		{
			JpsDirection jpsDirection = nodes.get();
			if (!(jpsDirection.pos == to))
			{
				if (isLineDireaction(jpsDirection.direction))
				{
					testLine(jpsDirection.pos, jpsDirection.direction, jpsDirection.cost);
				}
				else
				{
					testDiagonal(jpsDirection.pos, jpsDirection.pos, jpsDirection.direction, jpsDirection.cost);
				}
				continue;
			}
			break;
		}
	}

	private void testDiagonal(Vector2Int parent, Vector2Int p, Vector2Int d, int fcost)
	{
		Vector2Int vector2Int = new Vector2Int(p.x + d.x, p.y);
		if (env.IsEmpty(vector2Int))
		{
			Vector2Int vector2Int2 = new Vector2Int(p.x, p.y + d.y);
			if (env.IsEmpty(vector2Int2))
			{
				p += d;
				if (!env.IsEmpty(p))
				{
					return;
				}
				fcost += 2;
				if (!testEnd(parent, p, fcost))
				{
					if (diagonalExplore(p, d, fcost))
					{
						addLut(parent, p, fcost);
					}
					testDiagonal(parent, p, d, fcost);
				}
			}
			else
			{
				testDiagonalSide(parent, p, fcost, d, vector2Int2, JpsUtils.up);
			}
		}
		else
		{
			Vector2Int pos = new Vector2Int(p.x, p.y + d.y);
			if (env.IsEmpty(pos))
			{
				testDiagonalSide(parent, p, fcost, d, vector2Int, JpsUtils.right);
			}
		}
	}

	private void testDiagonalSide(Vector2Int parent, Vector2Int p, int fcost, Vector2Int d, Vector2Int b, Vector2Int mask)
	{
		p += d;
		if (!env.IsEmpty(p))
		{
			return;
		}
		fcost += 2;
		if (!testEnd(parent, p, fcost))
		{
			List<Vector2Int> list = testForceNeighborsInDiagonal(p, b, d, mask);
			if (diagonalExplore(p, d, fcost) || list.Count > 0)
			{
				list.Add(d);
				addDirections(p, list.ToArray(), fcost);
				addLut(parent, p, fcost);
			}
			else
			{
				testDiagonal(parent, p, d, fcost);
			}
		}
	}

	private List<Vector2Int> testForceNeighborsInDiagonal(Vector2Int X, Vector2Int B, Vector2Int D, Vector2Int mask)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		B += D * mask;
		if (env.IsEmpty(B))
		{
			list.Add(B - X);
		}
		return list;
	}

	private bool diagonalExplore(Vector2Int p, Vector2Int d, int cost)
	{
		bool num = testLine(p, new Vector2Int(d.x, 0), cost);
		bool flag = testLine(p, new Vector2Int(0, d.y), cost);
		return num || flag;
	}

	private bool testLine(Vector2Int parent, Vector2Int d, int fcost)
	{
		Vector2Int[] array = JpsBase.verticalLut[d];
		Vector2Int vector2Int = parent + d;
		Vector2Int vector2Int2 = array[0] + vector2Int;
		Vector2Int vector2Int3 = array[1] + vector2Int;
		for (; env.IsEmpty(vector2Int); vector2Int += d)
		{
			fcost++;
			if (testEnd(parent, vector2Int, fcost))
			{
				return true;
			}
			List<Vector2Int> list = new List<Vector2Int>();
			Vector2Int vector2Int4 = vector2Int2 + d;
			if (env.IsObstacle(vector2Int2) && env.IsEmpty(vector2Int4))
			{
				list.Add(vector2Int4 - vector2Int);
			}
			Vector2Int vector2Int5 = vector2Int3 + d;
			if (env.IsObstacle(vector2Int3) && env.IsEmpty(vector2Int5))
			{
				list.Add(vector2Int5 - vector2Int);
			}
			if (list.Count > 0)
			{
				list.Add(d);
				addDirections(vector2Int, list.ToArray(), fcost);
				addLut(parent, vector2Int, fcost);
				return true;
			}
			vector2Int2 = vector2Int4;
			vector2Int3 = vector2Int5;
		}
		return false;
	}

	private List<Vector2Int> testForceNeighborsInLine(Vector2Int p, Vector2Int d)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int[] array = JpsBase.verticalLut[d];
		foreach (Vector2Int vector2Int in array)
		{
			Vector2Int vector2Int2 = vector2Int + p;
			if (env.IsObstacle(vector2Int2) && env.IsEmpty(vector2Int2 + d))
			{
				list.Add(vector2Int + d);
			}
		}
		return list;
	}
}
