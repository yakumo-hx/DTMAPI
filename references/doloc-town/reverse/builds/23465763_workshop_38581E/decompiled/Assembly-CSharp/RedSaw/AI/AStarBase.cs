using System;
using System.Collections.Generic;
using UnityEngine;

namespace RedSaw.AI;

public abstract class AStarBase
{
	protected struct Node
	{
		public readonly Vector2Int pos;

		public readonly int dstFromStart;

		public readonly int dstTotal;

		public Node(Vector2Int pos, int dstFromStart, int dstToEnd)
		{
			this.pos = pos;
			this.dstFromStart = dstFromStart;
			dstTotal = dstToEnd + dstFromStart;
		}
	}

	protected class PriorityQueue
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

	private readonly Dictionary<Vector2Int, Node> parentMap = new Dictionary<Vector2Int, Node>();

	protected readonly PriorityQueue unexploredSet = new PriorityQueue();

	protected readonly Dictionary<Vector2Int, int> costsFromStart = new Dictionary<Vector2Int, int>();

	protected Vector2Int from;

	protected Vector2Int to;

	private Vector2Int[] Path
	{
		get
		{
			List<Vector2Int> list = new List<Vector2Int>();
			while (to != from)
			{
				list.Add(to);
				to = parentMap[to].pos;
			}
			list.Add(from);
			list.Reverse();
			return list.ToArray();
		}
	}

	protected Vector2Int[] FindPath(Vector2Int from, Vector2Int to, bool fromDir = true, int limit = 1000)
	{
		if (IsObstacle(from) || IsObstacle(to))
		{
			return null;
		}
		if (from == to)
		{
			return new Vector2Int[1] { from };
		}
		unexploredSet.Clear();
		parentMap.Clear();
		costsFromStart.Clear();
		this.to = to;
		this.from = from;
		unexploredSet.Put(new Node(from, 0, Distance(from, to)));
		costsFromStart.Add(from, 0);
		if (!fromDir)
		{
			return FindPath(limit);
		}
		return FindPathFromDirs(limit);
	}

	private Vector2Int[] FindPath(int limit = 1000)
	{
		while (unexploredSet.Count > 0)
		{
			if (limit-- <= 0)
			{
				return null;
			}
			Node value = unexploredSet.Next();
			if (value.pos == to)
			{
				return Path;
			}
			int num = value.dstFromStart + 1;
			Vector2Int[] neighbours = GetNeighbours(value.pos);
			foreach (Vector2Int vector2Int in neighbours)
			{
				if (!costsFromStart.TryGetValue(vector2Int, out var value2) || num < value2)
				{
					costsFromStart[vector2Int] = num;
					Node value3 = new Node(vector2Int, num, Distance(vector2Int, to));
					parentMap[value3.pos] = value;
					unexploredSet.Put(value3);
				}
			}
		}
		return null;
	}

	private Vector2Int[] FindPathFromDirs(int limit = 1000)
	{
		while (unexploredSet.Count > 0)
		{
			if (limit-- <= 0)
			{
				return null;
			}
			Node value = unexploredSet.Next();
			if (value.pos == to)
			{
				return Path;
			}
			int num = value.dstFromStart + 1;
			Vector2Int[] neighbourDirs = GetNeighbourDirs();
			foreach (Vector2Int vector2Int in neighbourDirs)
			{
				Vector2Int vector2Int2 = value.pos + vector2Int;
				if (!IsObstacle(vector2Int2) && (!costsFromStart.TryGetValue(vector2Int2, out var value2) || num < value2))
				{
					costsFromStart[vector2Int2] = num;
					Node value3 = new Node(vector2Int2, num, Distance(vector2Int2, to));
					parentMap[value3.pos] = value;
					unexploredSet.Put(value3);
				}
			}
		}
		return null;
	}

	protected virtual bool IsObstacle(Vector2Int pos)
	{
		return false;
	}

	protected virtual Vector2Int[] GetNeighbours(Vector2Int pos)
	{
		return Array.Empty<Vector2Int>();
	}

	protected virtual Vector2Int[] GetNeighbourDirs()
	{
		return Array.Empty<Vector2Int>();
	}

	protected virtual int Distance(Vector2Int from, Vector2Int to)
	{
		return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
	}
}
