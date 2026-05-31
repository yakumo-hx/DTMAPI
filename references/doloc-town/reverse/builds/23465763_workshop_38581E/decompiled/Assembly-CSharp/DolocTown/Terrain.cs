using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class Terrain
{
	private readonly Dictionary<TerrainLayerName, TerrainLayer> layerMap = new Dictionary<TerrainLayerName, TerrainLayer>();

	private readonly Dictionary<DecalSlotType, List<IDecalHost>> decalHostList = new Dictionary<DecalSlotType, List<IDecalHost>>();

	public Vector2Int Size { get; private set; }

	public IEnumerable<TerrainContent> AllContents
	{
		get
		{
			HashSet<TerrainContent> hashSet = new HashSet<TerrainContent>();
			foreach (TerrainLayer value in layerMap.Values)
			{
				hashSet.UnionWith(value.Contents);
			}
			return hashSet;
		}
	}

	public static Terrain Create(Vector2Int size, Vector2Int[] groundPositions, Vector2Int[] extraObstaclePositions)
	{
		Terrain terrain = new Terrain(size);
		terrain.FillGroundTerrain(groundPositions, extraObstaclePositions);
		return terrain;
	}

	public Terrain(Vector2Int size)
	{
		Size = size;
		layerMap = CreateLayers();
	}

	private Dictionary<TerrainLayerName, TerrainLayer> CreateLayers()
	{
		Dictionary<TerrainLayerName, TerrainLayer> dictionary = new Dictionary<TerrainLayerName, TerrainLayer>();
		foreach (TerrainLayerName value in Enum.GetValues(typeof(TerrainLayerName)))
		{
			if (value.IsSingle())
			{
				dictionary.Add(value, new TerrainLayer(value, Size));
			}
		}
		return dictionary;
	}

	public void Clear(TerrainLayerName layer)
	{
		HashSet<TerrainContent> hashSet = new HashSet<TerrainContent>();
		if (layer.IsSingle())
		{
			hashSet.UnionWith(layerMap[layer].Contents);
			layerMap[layer].Clear();
		}
		else
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layer))
				{
					hashSet.UnionWith(value.Contents);
					value.Clear();
				}
			}
		}
		foreach (TerrainContent item in hashSet)
		{
			foreach (TerrainLayerName item2 in item.LayerPositions.Keys.Where((TerrainLayerName l) => !layer.HasFlag(l)))
			{
				layerMap[item2].RemoveContent(item);
			}
		}
	}

	public void FillContent(TerrainContent cnt)
	{
		TerrainLayerName layerMask = cnt.LayerMask;
		if (layerMask.IsSingle())
		{
			layerMap[layerMask].FillContent(cnt);
		}
		else
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask))
				{
					value.FillContent(cnt);
				}
			}
		}
		OnFillContent(cnt);
	}

	public IEnumerable<T> GetContentsFromPositions<T>(Vector2Int[] positions) where T : TerrainContent
	{
		HashSet<T> hashSet = new HashSet<T>();
		foreach (TerrainLayer value in layerMap.Values)
		{
			hashSet.UnionWith(value.GetContentsFromPositions<T>(positions));
		}
		return hashSet;
	}

	public IEnumerable<T> GetContentsFromArea<T>(Vector2Int anchor, Vector2Int size) where T : TerrainContent
	{
		HashSet<T> hashSet = new HashSet<T>();
		foreach (TerrainLayer value in layerMap.Values)
		{
			hashSet.UnionWith(value.GetContentsFromArea<T>(anchor, size));
		}
		return hashSet;
	}

	public void RemoveContent(TerrainContent cnt)
	{
		TerrainLayerName layerMask = cnt.LayerMask;
		if (layerMask.IsSingle())
		{
			layerMap[layerMask].RemoveContent(cnt);
		}
		else
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask))
				{
					value.RemoveContent(cnt);
				}
			}
		}
		OnRemoveContent(cnt);
	}

	public bool QueryContent<T>(Vector2Int pos, out T cnt) where T : TerrainContent
	{
		cnt = GetContent<T>(pos);
		return cnt != null;
	}

	public IEnumerable<T> GetContentsOfType<T>() where T : TerrainContent
	{
		HashSet<T> hashSet = new HashSet<T>();
		foreach (TerrainLayer value in layerMap.Values)
		{
			foreach (TerrainContent content in value.Contents)
			{
				if (content is T item)
				{
					hashSet.Add(item);
				}
			}
		}
		return hashSet;
	}

	public T GetContent<T>(Vector2Int pos) where T : TerrainContent
	{
		HashSet<T> hashSet = new HashSet<T>();
		foreach (TerrainLayer value in layerMap.Values)
		{
			if (value.QueryContent<T>(pos, out var content))
			{
				hashSet.Add(content);
			}
		}
		if (hashSet.Count != 0)
		{
			return hashSet.First();
		}
		return null;
	}

	public bool QueryContent<T>(Vector2Int pos, TerrainLayerName layerMask, out T cnt) where T : TerrainContent
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].QueryContent<T>(pos, out cnt);
		}
		foreach (TerrainLayer value in layerMap.Values)
		{
			if (value.Match(layerMask) && value.QueryContent<T>(pos, out cnt))
			{
				return true;
			}
		}
		cnt = null;
		return false;
	}

	public T GetContent<T>(Vector2Int pos, TerrainLayerName layerMask) where T : TerrainContent
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].GetContent<T>(pos);
		}
		foreach (TerrainLayer value in layerMap.Values)
		{
			if (value.Match(layerMask))
			{
				T content = value.GetContent<T>(pos);
				if (content != null)
				{
					return content;
				}
			}
		}
		return null;
	}

	public bool Raycast(Vector2Int pos, int distance, Vector2Int dir, TerrainLayerName layerMask, out Vector2Int hitpos)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].Raycast(pos, distance, dir, out hitpos);
		}
		hitpos = pos;
		if (IsFilledMultiLayer(hitpos, layerMask))
		{
			return false;
		}
		for (int i = 0; i < distance; i++)
		{
			hitpos += dir;
			if (IsFilledMultiLayer(hitpos, layerMask))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsFilledMultiLayer(Vector2Int pos, TerrainLayerName layerMask, bool requireAllLayer = false)
	{
		if (requireAllLayer)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask) && !value.IsFilled(pos))
				{
					return false;
				}
			}
			return true;
		}
		foreach (TerrainLayer value2 in layerMap.Values)
		{
			if (value2.Match(layerMask) && value2.IsFilled(pos))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsEmpty(Vector2Int pos)
	{
		foreach (TerrainLayer value in layerMap.Values)
		{
			if (!value.IsEmpty(pos))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsEmpty(Vector2Int pos, TerrainLayerName layerMask, bool requireAllLayer = true)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].IsEmpty(pos);
		}
		if (requireAllLayer)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask) && !value.IsEmpty(pos))
				{
					return false;
				}
			}
			return true;
		}
		foreach (TerrainLayer value2 in layerMap.Values)
		{
			if (value2.Match(layerMask) && value2.IsEmpty(pos))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsOnGround(IEnumerable<Vector2Int> positions)
	{
		return positions.All((Vector2Int pos) => IsFilled(new Vector2Int(pos.x, pos.y - 1), TerrainLayerName.Ground));
	}

	public bool AllEmpty(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask, bool requireAllLayer = true)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].AllEmpty(positions);
		}
		if (requireAllLayer)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask) && !value.AllEmpty(positions))
				{
					return false;
				}
			}
			return true;
		}
		foreach (TerrainLayer value2 in layerMap.Values)
		{
			if (value2.Match(layerMask) && value2.AllEmpty(positions))
			{
				return true;
			}
		}
		return false;
	}

	public bool AnyEmpty(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask, bool requireAllLayer = true)
	{
		foreach (Vector2Int position in positions)
		{
			if (IsEmpty(position, layerMask, requireAllLayer))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsFilled(Vector2Int pos, TerrainLayerName layerMask, bool requireAllLayer = false)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].IsFilled(pos);
		}
		if (requireAllLayer)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask) && !value.IsFilled(pos))
				{
					return false;
				}
			}
			return true;
		}
		foreach (TerrainLayer value2 in layerMap.Values)
		{
			if (value2.Match(layerMask) && value2.IsFilled(pos))
			{
				return true;
			}
		}
		return false;
	}

	public bool AllFilled(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask, bool requireAllLayer = false)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].AllFilled(positions);
		}
		if (requireAllLayer)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask) && !value.AllFilled(positions))
				{
					return false;
				}
			}
			return true;
		}
		foreach (TerrainLayer value2 in layerMap.Values)
		{
			if (value2.Match(layerMask) && value2.AllFilled(positions))
			{
				return true;
			}
		}
		return false;
	}

	public bool AllPositionsFilledInAnyLayer(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask)
	{
		return positions.All((Vector2Int x) => IsFilled(x, layerMask));
	}

	public bool AllPositionsFilledInAllLayer(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask)
	{
		return positions.All((Vector2Int x) => IsFilled(x, layerMask, requireAllLayer: true));
	}

	public bool AnyPositionFilledInAnyLayer(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask)
	{
		return positions.Any((Vector2Int position) => IsFilled(position, layerMask));
	}

	public bool AnyPositionFilledInAllLayer(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask)
	{
		return positions.Any((Vector2Int position) => IsFilled(position, layerMask, requireAllLayer: true));
	}

	public bool AnyFilledIgnoreTerrain(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask)
	{
		foreach (Vector2Int position in positions)
		{
			bool flag = false;
			if (layerMask.IsSingle())
			{
				flag = layerMap[layerMask].CheckFilled(position);
			}
			else
			{
				foreach (TerrainLayer value in layerMap.Values)
				{
					flag |= value.Match(layerMask) && value.CheckFilled(position);
				}
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerable<Vector2Int> GetOccupiedPositions(TerrainLayerName layerMask, bool isUnion = true)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].OccupiedPositions;
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		if (isUnion)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask))
				{
					hashSet.UnionWith(value.OccupiedPositions);
				}
			}
		}
		else
		{
			foreach (TerrainLayer value2 in layerMap.Values)
			{
				if (value2.Match(layerMask))
				{
					hashSet.IntersectWith(value2.OccupiedPositions);
				}
			}
		}
		return hashSet;
	}

	public IEnumerable<Vector2Int> FilterOccupiedPositions(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask, bool isUnion = true)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].FilterOccupiedPositions(positions);
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>();
		if (isUnion)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask))
				{
					hashSet.UnionWith(value.FilterOccupiedPositions(positions));
				}
			}
		}
		else
		{
			foreach (TerrainLayer value2 in layerMap.Values)
			{
				if (value2.Match(layerMask))
				{
					hashSet.IntersectWith(value2.FilterOccupiedPositions(positions));
				}
			}
		}
		return hashSet;
	}

	public IEnumerable<Vector2Int> FilterFreePositions(IEnumerable<Vector2Int> positions, TerrainLayerName layerMask, bool isUnion = false)
	{
		if (layerMask.IsSingle())
		{
			return layerMap[layerMask].FilterFreePositions(positions);
		}
		HashSet<Vector2Int> hashSet = new HashSet<Vector2Int>(positions);
		if (isUnion)
		{
			foreach (TerrainLayer value in layerMap.Values)
			{
				if (value.Match(layerMask))
				{
					hashSet.UnionWith(value.FilterFreePositions(positions));
				}
			}
		}
		else
		{
			foreach (TerrainLayer value2 in layerMap.Values)
			{
				if (value2.Match(layerMask))
				{
					hashSet.IntersectWith(value2.FilterFreePositions(positions));
				}
			}
		}
		return hashSet;
	}

	public IDecalHost[] GetAllCachedDecalHosts()
	{
		List<IDecalHost> list = new List<IDecalHost>();
		foreach (List<IDecalHost> value in decalHostList.Values)
		{
			list.AddRange(value);
		}
		return list.ToArray();
	}

	public IDecalHost[] GetCachedDecalHosts(DecalSlotType slotType)
	{
		decalHostList.TryAdd(slotType, new List<IDecalHost>());
		return decalHostList[slotType].ToArray();
	}

	private void OnFillContent(TerrainContent content)
	{
		if (content is IDecalHost { Valid: not false, ContainedSlots: var containedSlots } decalHost)
		{
			foreach (DecalSlot decalSlot in containedSlots)
			{
				decalHostList.TryAdd(decalSlot.SlotType, new List<IDecalHost>());
				decalHostList[decalSlot.SlotType].Add(decalHost);
			}
		}
	}

	private void OnRemoveContent(TerrainContent content)
	{
		if (!(content is IDecalHost { Valid: not false, ContainedSlots: var containedSlots } decalHost))
		{
			return;
		}
		foreach (DecalSlot decalSlot in containedSlots)
		{
			if (decalHostList.ContainsKey(decalSlot.SlotType))
			{
				decalHostList[decalSlot.SlotType].Remove(decalHost);
			}
		}
	}
}
