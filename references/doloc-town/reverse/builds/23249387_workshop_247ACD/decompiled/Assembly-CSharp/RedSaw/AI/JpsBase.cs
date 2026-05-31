using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public abstract class JpsBase
{
	protected static Dictionary<Vector2Int, Vector2Int[]> verticalLut;

	protected Dictionary<Vector2Int, JpsNode> lut;

	protected IPriorityQueue<JpsDirection> nodes;

	protected Vector2Int from;

	protected Vector2Int to;

	protected IGameMap env;

	public Dictionary<Vector2Int, JpsNode> Lut => lut;

	protected static void init()
	{
		if (verticalLut == null)
		{
			verticalLut = new Dictionary<Vector2Int, Vector2Int[]>();
			Vector2Int[] value = new Vector2Int[2]
			{
				JpsUtils.left,
				JpsUtils.right
			};
			Vector2Int[] value2 = new Vector2Int[2]
			{
				JpsUtils.up,
				JpsUtils.down
			};
			verticalLut.Add(JpsUtils.up, value);
			verticalLut.Add(JpsUtils.down, value);
			verticalLut.Add(JpsUtils.left, value2);
			verticalLut.Add(JpsUtils.right, value2);
		}
	}

	public JpsBase()
	{
		init();
		lut = new Dictionary<Vector2Int, JpsNode>();
		nodes = new RsPriorityQueue<JpsDirection>();
	}

	public abstract bool FindPath(IGameMap env, Vector2Int f, Vector2Int E, out Vector2Int[] path);

	protected bool isLineDireaction(Vector2Int d)
	{
		return d.x * d.y == 0;
	}

	protected Vector2Int[] completePath()
	{
		Dictionary<Vector2Int, Vector2Int> dictionary = new Dictionary<Vector2Int, Vector2Int>();
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		IPriorityQueue<JpsNode> priorityQueue = new RsPriorityQueue<JpsNode>();
		priorityQueue.set(lut[to], lut[to].cost);
		while (priorityQueue.notEmpty)
		{
			JpsNode jpsNode = priorityQueue.get();
			hashSet.Add(jpsNode.pos);
			foreach (Vector2Int parent in jpsNode.parents)
			{
				if (!hashSet.Contains(parent) && !dictionary.ContainsKey(jpsNode.pos))
				{
					dictionary.Add(jpsNode.pos, parent);
					if (parent == from)
					{
						return _trace(dictionary);
					}
					priorityQueue.set(lut[parent], lut[parent].cost);
				}
			}
		}
		return null;
	}

	protected Vector2Int[] _trace(Dictionary<Vector2Int, Vector2Int> cameFrom)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int vector2Int = to;
		while (vector2Int != from)
		{
			list.Add(vector2Int);
			vector2Int = cameFrom[vector2Int];
		}
		list.Add(from);
		list.Reverse();
		return list.ToArray();
	}

	protected void addLut(Vector2Int parent, Vector2Int p, int fcost)
	{
		if (lut.ContainsKey(p))
		{
			lut[p].parents.Add(parent);
		}
		else
		{
			lut.Add(p, new JpsNode(parent, p, fcost));
		}
	}

	protected void addDirections(Vector2Int p, Vector2Int[] dirs, int fcost)
	{
		if (!lut.ContainsKey(p))
		{
			foreach (Vector2Int vector2Int in dirs)
			{
				Vector2Int p2 = p + vector2Int;
				nodes.set(new JpsDirection(p, vector2Int, fcost), fcost + JpsUtils.Manhattan(p2, to));
			}
		}
	}

	protected bool testEnd(Vector2Int parent, Vector2Int p, int fcost)
	{
		if (p == to)
		{
			addLut(parent, p, fcost);
			nodes.set(new JpsDirection(p, Vector2Int.zero, fcost), 0);
			return true;
		}
		return false;
	}
}
