using System.Collections.Generic;
using RedSaw.GameMap;
using UnityEngine;

namespace RedSaw.AI;

public class AStarRaw : IPathFinder
{
	public struct Node
	{
		public Vector2Int pos;

		public int dstFromStart;

		public int dstTotal;

		public Node(Vector2Int pos, int dstFromStart, int dstToEnd)
		{
			this.pos = pos;
			this.dstFromStart = dstFromStart;
			dstTotal = dstToEnd + dstFromStart;
		}
	}

	private class PriorityQueue
	{
		private readonly List<Node> values = new List<Node>();

		public int Count => values.Count;

		public void Clear()
		{
			values.Clear();
		}

		public void Put(Node value)
		{
			values.Add(value);
			int num = Count - 1;
			int num2 = num / 2;
			while (num2 > 0 && values[num].dstTotal < values[num2].dstTotal)
			{
				Swap(num, num2);
				num = num2;
				num2 = num / 2;
			}
		}

		public Node Next()
		{
			Node result = values[0];
			values[0] = values[Count - 1];
			values.RemoveAt(Count - 1);
			Heapify(Count - 1, 0);
			return result;
		}

		public bool TryGet(out Node node)
		{
			if (Count == 0)
			{
				node = default(Node);
				return false;
			}
			node = values[0];
			values[0] = values[Count - 1];
			values.RemoveAt(Count - 1);
			Heapify(Count - 1, 0);
			return true;
		}

		private void Heapify(int count, int i)
		{
			while (true)
			{
				int num = i;
				int num2 = i * 2;
				int num3 = num2 + 1;
				if (num2 <= count && values[i].dstTotal > values[num2].dstTotal)
				{
					num = num2;
				}
				if (num3 <= count && values[num].dstTotal > values[num3].dstTotal)
				{
					num = num3;
				}
				if (num != i)
				{
					Swap(i, num);
					i = num;
					continue;
				}
				break;
			}
		}

		private void Swap(int a, int b)
		{
			List<Node> list = values;
			List<Node> list2 = values;
			Node node = values[a];
			Node node2 = values[b];
			Node node4 = (list[b] = node);
			node4 = (list2[a] = node2);
		}
	}

	private static readonly int[] dx = new int[8] { -1, 0, 1, -1, 1, -1, 0, 1 };

	private static readonly int[] dy = new int[8] { -1, -1, -1, 0, 0, 1, 1, 1 };

	private readonly PriorityQueue unexploredNodes = new PriorityQueue();

	private readonly Dictionary<Vector2Int, int> costsFromStart = new Dictionary<Vector2Int, int>();

	private readonly Dictionary<Vector2Int, Node> parentNodeMap = new Dictionary<Vector2Int, Node>();

	private IGameMap map;

	private Vector2Int from;

	private Vector2Int to;

	private static Vector2Int[] GetAroundPositions(Vector2Int pos)
	{
		Vector2Int[] array = new Vector2Int[8];
		for (int i = 0; i < 8; i++)
		{
			array[i] = pos + new Vector2Int(dx[i], dy[i]);
		}
		return array;
	}

	private static int Manhattan(Vector2Int from, Vector2Int to)
	{
		return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
	}

	public Vector2Int[] FindPath(IGameMap map, Vector2Int f, Vector2Int to)
	{
		this.map = map;
		from = f;
		this.to = to;
		if (map.IsObstacle(f) || map.IsObstacle(to))
		{
			return null;
		}
		if (f == to)
		{
			return new Vector2Int[1] { f };
		}
		unexploredNodes.Clear();
		unexploredNodes.Put(new Node(f, 0, Manhattan(f, to)));
		parentNodeMap.Clear();
		costsFromStart.Clear();
		costsFromStart.Add(f, 0);
		return FindPath();
	}

	private Vector2Int[] FindPath()
	{
		while (unexploredNodes.Count > 0)
		{
			Node value = unexploredNodes.Next();
			if (value.pos == to)
			{
				return GeneratePath();
			}
			int num = value.dstFromStart + 1;
			Vector2Int[] aroundPositions = GetAroundPositions(value.pos);
			foreach (Vector2Int vector2Int in aroundPositions)
			{
				if (!map.IsObstacle(vector2Int) && (!costsFromStart.TryGetValue(vector2Int, out var value2) || num < value2))
				{
					costsFromStart[vector2Int] = num;
					Node value3 = new Node(vector2Int, num, Manhattan(vector2Int, to));
					parentNodeMap[value3.pos] = value;
					unexploredNodes.Put(value3);
				}
			}
		}
		return null;
	}

	private Vector2Int[] GeneratePath()
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int pos = to;
		while (pos != from)
		{
			list.Add(pos);
			pos = parentNodeMap[pos].pos;
		}
		list.Add(from);
		list.Reverse();
		return list.ToArray();
	}
}
