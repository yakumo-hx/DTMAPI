using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public abstract class TerrainContent : IHasIndex
{
	[JsonProperty]
	public int id;

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	private Vector2Int anchor;

	[JsonProperty]
	[JsonConverter(typeof(VectorConverter))]
	protected Vector3 position;

	private bool isCacheDirty;

	public bool isDeserializationValid => ValidateDeserialization();

	public int index
	{
		get
		{
			return id;
		}
		set
		{
			id = value;
		}
	}

	public virtual bool IsRemoved => id < 0;

	[DebugInfo("锚点位置", Color = "white")]
	public Vector2Int Anchor => anchor;

	[DebugInfo("世界位置", Color = "white")]
	public Vector3 Position => position;

	public TerrainLayerName LayerMask
	{
		get
		{
			TerrainLayerName terrainLayerName = TerrainLayerName.None;
			foreach (TerrainLayerName key in LayerPositions.Keys)
			{
				terrainLayerName |= key;
			}
			return terrainLayerName;
		}
	}

	protected Dictionary<TerrainLayerName, Vector2Int[]> layerPositionsCache { get; private set; }

	public Dictionary<TerrainLayerName, Vector2Int[]> LayerPositions
	{
		get
		{
			if (isCacheDirty || layerPositionsCache == null)
			{
				UpdateLayerPositions();
			}
			return layerPositionsCache;
		}
	}

	protected Vector2Int[] coveredPositionsCache { get; private set; }

	public Vector2Int[] CoveredPositions
	{
		get
		{
			if (isCacheDirty || coveredPositionsCache == null)
			{
				UpdateLayerPositions();
			}
			return coveredPositionsCache;
		}
	}

	[JsonConstructor]
	public TerrainContent(int id, Vector2Int anchor, Vector3 position)
	{
		this.id = id;
		this.anchor = anchor;
		this.position = position;
		isCacheDirty = true;
	}

	protected abstract bool ValidateDeserialization();

	private void UpdateLayerPositions()
	{
		isCacheDirty = false;
		layerPositionsCache = CalLayerPositions(anchor);
		if (layerPositionsCache.Keys.Count == 1)
		{
			coveredPositionsCache = layerPositionsCache.Values.First();
			return;
		}
		coveredPositionsCache = (from p in layerPositionsCache.Values.SelectMany((Vector2Int[] p) => p).Distinct()
			orderby p.x, p.y
			select p).ToArray();
	}

	protected abstract Dictionary<TerrainLayerName, Vector2Int[]> CalLayerPositions(Vector2Int anchor);

	public virtual void ShiftTerrainContent(Vector2Int offset, Vector3 positionOffset)
	{
		anchor += offset;
		position += positionOffset;
		isCacheDirty = true;
	}

	public virtual void MoveTerrainContent(Vector2Int anchor, Vector3 position)
	{
		this.anchor = anchor;
		this.position = position;
		isCacheDirty = true;
	}

	public bool Match(TerrainLayerName layer)
	{
		return LayerMask.HasFlag(layer);
	}
}
