using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.AI;

public class JpsLConstraint : JpsL
{
	private HashSet<Vector2Int> accessed;

	public JpsLConstraint()
	{
		accessed = new HashSet<Vector2Int>();
	}

	protected new void testDiagonal(Vector2Int parent, Vector2Int d, int fcost)
	{
		Vector2Int vector2Int = parent;
		Vector2Int pos = new Vector2Int(vector2Int.x + d.x, vector2Int.y);
		while (env.IsEmpty(pos))
		{
			Vector2Int pos2 = new Vector2Int(vector2Int.x, vector2Int.y + d.y);
			if (env.IsEmpty(pos2))
			{
				vector2Int += d;
				if (accessed.Contains(vector2Int))
				{
					break;
				}
				accessed.Add(vector2Int);
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

	private new bool testLine(Vector2Int parent, Vector2Int d, int fcost)
	{
		Vector2Int[] array = JpsBase.verticalLut[d];
		Vector2Int vector2Int = array[0] + parent;
		Vector2Int vector2Int2 = array[1] + parent;
		for (Vector2Int vector2Int3 = parent + d; env.IsEmpty(vector2Int3); vector2Int3 += d)
		{
			if (accessed.Contains(vector2Int3))
			{
				return false;
			}
			accessed.Add(vector2Int3);
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
}
