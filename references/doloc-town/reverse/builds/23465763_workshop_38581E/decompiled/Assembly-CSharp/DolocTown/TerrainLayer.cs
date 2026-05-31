using System.Collections.Generic;
using System.Linq;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class TerrainLayer
{
	public readonly TerrainLayerName layerName;

	public readonly Vector2Int size;

	private readonly Dictionary<Vector2Int, TerrainSlot> slots = new Dictionary<Vector2Int, TerrainSlot>();

	private readonly HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

	public HashSet<Vector2Int> OccupiedPositionsSet => new HashSet<Vector2Int>(usedPositions);

	public IEnumerable<Vector2Int> OccupiedPositions => usedPositions;

	public IEnumerable<TerrainContent> Contents
	{
		get
		{
			HashSet<TerrainContent> hashSet = new HashSet<TerrainContent>();
			foreach (Vector2Int usedPosition in usedPositions)
			{
				TerrainContent content = slots[usedPosition].Content;
				if (content != null)
				{
					hashSet.Add(content);
				}
			}
			return hashSet;
		}
	}

	public TerrainLayer(TerrainLayerName layerName, Vector2Int size)
	{
		this.layerName = layerName;
		this.size = size;
		foreach (Vector2Int item in size.IterateGrid())
		{
			slots.Add(item, new TerrainSlot(item));
		}
	}

	public bool Raycast(Vector2Int pos, int distance, Vector2Int dir, out Vector2Int hitpos)
	{
		hitpos = pos;
		if (IsFilled(pos))
		{
			return false;
		}
		for (int i = 0; i < distance; i++)
		{
			hitpos += dir;
			if (IsFilled(hitpos))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerable<T> GetContentsFromPositions<T>(Vector2Int[] positions) where T : TerrainContent
	{
		return GetContentsOfSet<T>(new HashSet<Vector2Int>(positions));
	}

	public IEnumerable<T> GetContentsFromArea<T>(Vector2Int anchor, Vector2Int size) where T : TerrainContent
	{
		if (size.x <= 0 || size.y <= 0)
		{
			yield break;
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		int num = Mathf.Min(size.x, this.size.x - anchor.x) + anchor.x;
		int num2 = Mathf.Min(size.y, this.size.y - anchor.y) + anchor.y;
		for (int i = anchor.x; i < num; i++)
		{
			for (int j = anchor.y; j < num2; j++)
			{
				hashSet.Add(new Vector2Int(i, j));
			}
		}
		foreach (T item in GetContentsOfSet<T>(hashSet))
		{
			yield return item;
		}
	}

	private IEnumerable<T> GetContentsOfSet<T>(HashSet<Vector2Int> set) where T : TerrainContent
	{
		set.IntersectWith(usedPositions);
		while (set.Count > 0)
		{
			Vector2Int key = set.First();
			TerrainContent content = slots[key].Content;
			if (content.LayerPositions.TryGetValue(layerName, out var value))
			{
				Vector2Int[] array = value;
				foreach (Vector2Int item in array)
				{
					set.Remove(item);
				}
				if (content is T val)
				{
					yield return val;
				}
			}
		}
	}

	public bool Match(TerrainLayerName mask)
	{
		return (layerName & mask) != 0;
	}

	public IEnumerable<Vector2Int> FilterPositions(IEnumerable<Vector2Int> positions)
	{
		return positions.Where(slots.ContainsKey);
	}

	public IEnumerable<Vector2Int> FilterOccupiedPositions(IEnumerable<Vector2Int> positions)
	{
		return positions.Where(usedPositions.Contains);
	}

	public IEnumerable<Vector2Int> FilterFreePositions(IEnumerable<Vector2Int> positions)
	{
		return positions.Where((Vector2Int pos) => !usedPositions.Contains(pos));
	}

	public bool QueryContent<T>(Vector2Int pos, out T content) where T : TerrainContent
	{
		if (usedPositions.Contains(pos) && slots[pos].Content is T val)
		{
			content = val;
			return true;
		}
		content = null;
		return false;
	}

	public T GetContent<T>(Vector2Int pos) where T : TerrainContent
	{
		if (usedPositions.Contains(pos))
		{
			return slots[pos].Content as T;
		}
		return null;
	}

	public void FillContent(TerrainContent content)
	{
		if (!content.LayerPositions.TryGetValue(layerName, out var value))
		{
			return;
		}
		Vector2Int[] array = value;
		foreach (Vector2Int vector2Int in array)
		{
			if (!slots.ContainsKey(vector2Int))
			{
				Debug.LogError($"填充的位置超出了地形层级{layerName}的范围:{vector2Int},现有层级范围:{size}");
				continue;
			}
			usedPositions.Add(vector2Int);
			slots[vector2Int].Fill(content);
		}
	}

	public void RemoveContent(TerrainContent content)
	{
		if (!content.LayerPositions.TryGetValue(layerName, out var value))
		{
			return;
		}
		Vector2Int[] array = value;
		foreach (Vector2Int vector2Int in array)
		{
			if (slots.ContainsKey(vector2Int))
			{
				usedPositions.Remove(vector2Int);
				slots[vector2Int].Clear();
			}
		}
	}

	public bool IsFilled(Vector2Int pos)
	{
		if (slots.ContainsKey(pos))
		{
			return slots[pos].IsFilled;
		}
		return true;
	}

	public bool IsEmpty(Vector2Int pos)
	{
		if (slots.ContainsKey(pos))
		{
			return !usedPositions.Contains(pos);
		}
		return false;
	}

	public bool AllFilled(IEnumerable<Vector2Int> positions)
	{
		return positions.All(IsFilled);
	}

	public bool AnyFilled(IEnumerable<Vector2Int> positions)
	{
		return positions.Any(IsFilled);
	}

	public bool AllEmpty(IEnumerable<Vector2Int> positions)
	{
		return positions.All(IsEmpty);
	}

	public void Clear()
	{
		usedPositions.Clear();
		foreach (TerrainSlot value in slots.Values)
		{
			value.Clear();
		}
	}

	public bool CheckFilled(Vector2Int pos)
	{
		if (!slots.ContainsKey(pos))
		{
			return false;
		}
		return slots[pos].IsFilled;
	}
}
