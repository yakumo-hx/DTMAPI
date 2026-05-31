using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Building;
using DolocTown.Config.Platform;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public class BuildingSupport
{
	private struct TileCache
	{
		public readonly Vector2Int Position;

		public readonly int Depth;

		public readonly int TileZ;

		public readonly Tile Tile;

		public readonly bool Connect;

		public readonly SupportLayerType LayerType;

		public int LayerIndex;

		public TileCache(Vector2Int position, Tile tile, SupportLayerType layerType, Vector2Int fulcrumPosition, int depth, bool connect)
		{
			Position = position;
			Tile = tile;
			LayerType = layerType;
			Depth = depth;
			TileZ = ((layerType != SupportLayerType.PlatformFront && layerType != SupportLayerType.PlatformSide) ? Mathf.RoundToInt(1000000 * depth + 1000 * fulcrumPosition.y + fulcrumPosition.x + 1) : 0);
			Connect = connect;
			LayerIndex = 0;
		}
	}

	private enum PlatformTileType
	{
		None,
		Left,
		Middle,
		Right
	}

	private enum SupportLayerType
	{
		Column0,
		Column1,
		Column2,
		Column3,
		PlatformFront,
		PlatformSide
	}

	public readonly BuildingInfo buildingProto;

	public readonly Vector2Int anchor;

	private readonly List<TileCache> tileCaches = new List<TileCache>();

	private Dictionary<int, List<TileCache>> tileCachesByLayer = new Dictionary<int, List<TileCache>>();

	private int[] fulcrumHeights;

	private readonly Vector2Int[] contactPositions;

	private readonly int[] columnHeights = new int[4];

	private bool showLeftFulcrum;

	private bool showRightFulcrum;

	public BuildingSupportInfo supportProto => buildingProto?.SupportId_Ref;

	public int[] ColumnHeights => columnHeights;

	public Vector2Int[] ContactPositions => contactPositions;

	public BuildingSupport(BuildingInfo buildingProto, Vector2Int anchor, int[] fulcrumHeights, Vector2Int[] contactPositions, Func<Vector2Int, int> depthGetter)
	{
		this.anchor = anchor;
		this.buildingProto = buildingProto;
		this.fulcrumHeights = fulcrumHeights;
		this.contactPositions = contactPositions;
		if (fulcrumHeights.Max() != 0)
		{
			showLeftFulcrum = true;
			showRightFulcrum = true;
			for (int i = 0; i < 4; i++)
			{
				tileCachesByLayer.Add(i, new List<TileCache>());
			}
			DolocAssert.IsTrue(fulcrumHeights.Length == buildingProto.FulcrumDatas.Sum((FulcrumData x) => x.Width));
			InitFulcrums();
			InitPlatform();
			InitTileLayer(depthGetter);
		}
	}

	private void InitFulcrums()
	{
		int num = 0;
		for (int i = 0; i < buildingProto.FulcrumDatas.Length; i++)
		{
			FulcrumData fulcrumData = buildingProto.FulcrumDatas[i];
			int num2 = 0;
			for (int j = 0; j < fulcrumData.Width; j++)
			{
				Vector2Int value = anchor + fulcrumData.Offsets[j] + Vector2Int.down;
				if (i == 0 && j == 0)
				{
					showLeftFulcrum &= fulcrumHeights[num] > 0 && !contactPositions.Contains(value);
				}
				else if (i == 2 && j == 0)
				{
					showRightFulcrum &= fulcrumHeights[num] > 0 && !contactPositions.Contains(value);
				}
				num2 = Mathf.Max(num2, fulcrumHeights[num++]);
			}
			columnHeights[i] = num2;
		}
		if (!showLeftFulcrum)
		{
			columnHeights[0] = (columnHeights[1] = 0);
		}
		if (!showRightFulcrum)
		{
			columnHeights[2] = (columnHeights[3] = 0);
		}
		if (!showLeftFulcrum && !showRightFulcrum)
		{
			return;
		}
		for (int k = 0; k < buildingProto.FulcrumDatas.Length; k++)
		{
			FulcrumData fulcrumData2 = buildingProto.FulcrumDatas[k];
			int num3 = columnHeights[k];
			if (num3 == 0)
			{
				continue;
			}
			for (int l = 0; l < fulcrumData2.Width; l++)
			{
				Vector2Int vector2Int = anchor + fulcrumData2.Offsets[l] + Vector2Int.down;
				SupportLayerType layerType = (SupportLayerType)k;
				Tile tile = buildingProto.SupportId_Ref.Fulcrums[k].assets[l];
				tileCaches.Add(new TileCache(vector2Int, tile, layerType, vector2Int, fulcrumData2.Depth, contactPositions.Contains(vector2Int)));
				for (int m = 0; m < num3 - 1; m++)
				{
					TileAsset[] array = ((m != num3 - 2) ? ((k % 2 == 0) ? supportProto.ColumnForegroundMiddle : supportProto.ColumnBackgroundMiddle) : ((k % 2 == 0) ? supportProto.ColumnForegroundBottom : supportProto.ColumnBackgroundBottom));
					tileCaches.Add(new TileCache(new Vector2Int(vector2Int.x, vector2Int.y - 1 - m), array[l].Asset, layerType, vector2Int, fulcrumData2.Depth, contactPositions.Contains(vector2Int)));
				}
			}
		}
	}

	private void InitPlatform()
	{
		Vector2Int[] positionsOfType = buildingProto.GetPositionsOfType(anchor, BuildingTileType.Floor, BuildingTileType.Side, Vector2Int.down);
		Vector2Int[] array = positionsOfType.Except(contactPositions).ToArray();
		if (array.Length != 0)
		{
			tileCaches.Add(new TileCache(array[0], supportProto.PlatformFront[0].Asset, SupportLayerType.PlatformFront, Vector2Int.zero, 0, connect: false));
			if (array.Length >= 2)
			{
				tileCaches.Add(new TileCache(array[^1], supportProto.PlatformFront[2].Asset, SupportLayerType.PlatformFront, Vector2Int.zero, 0, connect: false));
				for (int i = 1; i < array.Length - 1; i++)
				{
					tileCaches.Add(new TileCache(array[i], supportProto.PlatformFront[1].Asset, SupportLayerType.PlatformFront, Vector2Int.zero, 0, connect: false));
				}
			}
		}
		if (!showLeftFulcrum && !showRightFulcrum)
		{
			return;
		}
		Vector2Int[] array2 = CalContinuesPositions(positionsOfType.Except(contactPositions).ToArray(), positionsOfType[0].x, positionsOfType[^1].x);
		foreach (KeyValuePair<Vector2Int, PlatformTileType> item in CalPlatformTileType(array2))
		{
			Tile tile = item.Value switch
			{
				PlatformTileType.Left => supportProto.PlatformFront[0].Asset, 
				PlatformTileType.Middle => supportProto.PlatformFront[1].Asset, 
				PlatformTileType.Right => supportProto.PlatformFront[2].Asset, 
				_ => null, 
			};
			tileCaches.Add(new TileCache(item.Key, tile, SupportLayerType.PlatformFront, Vector2Int.zero, 0, connect: false));
		}
		Vector2Int[] array3 = (showRightFulcrum ? buildingProto.GetPositionsOfType(anchor, BuildingTileType.Side | BuildingTileType.Floor, Vector2Int.down) : Array.Empty<Vector2Int>());
		foreach (KeyValuePair<Vector2Int, PlatformTileType> item2 in CalPlatformTileType(array3))
		{
			Tile tile2 = item2.Value switch
			{
				PlatformTileType.Left => supportProto.PlatformSide[0].Asset, 
				PlatformTileType.Middle => supportProto.PlatformSide[1].Asset, 
				PlatformTileType.Right => supportProto.PlatformSide[2].Asset, 
				_ => null, 
			};
			tileCaches.Add(new TileCache(item2.Key, tile2, SupportLayerType.PlatformSide, Vector2Int.zero, 0, connect: false));
		}
		if (!showLeftFulcrum)
		{
			return;
		}
		Vector2Int vector2Int = Vector2Int.zero;
		for (int j = 1; j < array2.Length; j++)
		{
			vector2Int = array2[j];
			if (array2[j].x != array2[j - 1].x + 1)
			{
				break;
			}
		}
		Vector2Int vector2Int2 = vector2Int + Vector2Int.right;
		if (vector2Int.magnitude > 0f && !array3.Contains(vector2Int2))
		{
			Tile asset = supportProto.PlatformSide[0].Asset;
			tileCaches.Add(new TileCache(vector2Int2, asset, SupportLayerType.PlatformSide, Vector2Int.zero, 0, connect: false));
		}
	}

	private Vector2Int[] CalContinuesPositions(Vector2Int[] positions, int leftX, int rightX)
	{
		if (positions.IsNullOrEmpty())
		{
			return Array.Empty<Vector2Int>();
		}
		if (positions.Length == positions[^1].x - positions[0].x + 1)
		{
			return positions;
		}
		List<Vector2Int> list = new List<Vector2Int>();
		int num = positions.Length;
		for (int i = 0; i < num; i++)
		{
			Vector2Int item = positions[i];
			if (item.x == leftX + i)
			{
				list.Add(item);
			}
			if (item.x == i + rightX - num + 1)
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	private Dictionary<Vector2Int, PlatformTileType> CalPlatformTileType(Vector2Int[] points)
	{
		int num = points.Length;
		Dictionary<Vector2Int, PlatformTileType> dictionary = new Dictionary<Vector2Int, PlatformTileType>();
		for (int i = 0; i < num; i++)
		{
			Vector2Int key = points[i];
			bool flag = i != 0 && points[i].x - 1 == points[i - 1].x;
			bool flag2 = i != num - 1 && points[i].x + 1 == points[i + 1].x;
			if (flag && flag2)
			{
				dictionary[key] = PlatformTileType.Middle;
			}
			else if (flag)
			{
				dictionary[key] = PlatformTileType.Right;
			}
			else if (flag2)
			{
				dictionary[key] = PlatformTileType.Left;
			}
			else
			{
				dictionary[key] = PlatformTileType.None;
			}
		}
		return dictionary;
	}

	private void InitTileLayer(Func<Vector2Int, int> depthGetter)
	{
		if (depthGetter != null)
		{
			for (int i = 0; i < tileCaches.Count; i++)
			{
				TileCache item = tileCaches[i];
				int num = depthGetter(item.Position);
				item.LayerIndex = item.LayerType switch
				{
					SupportLayerType.PlatformFront => (num <= 0) ? 2 : 0, 
					SupportLayerType.PlatformSide => (num > 0) ? 1 : 3, 
					SupportLayerType.Column0 => item.Connect ? 2 : 0, 
					SupportLayerType.Column1 => 3, 
					SupportLayerType.Column2 => 0, 
					SupportLayerType.Column3 => (item.Depth <= num) ? 1 : 3, 
					_ => throw new ArgumentOutOfRangeException(), 
				};
				tileCachesByLayer[item.LayerIndex].Add(item);
			}
		}
	}

	public void Render(TilemapGroupManager renderer)
	{
		foreach (int key in tileCachesByLayer.Keys)
		{
			List<TileCache> source = tileCachesByLayer[key];
			Vector3Int[] positions = source.Select((TileCache cache) => new Vector3Int(cache.Position.x, cache.Position.y, cache.TileZ)).ToArray();
			Tile[] tiles = source.Select((TileCache cache) => cache.Tile).ToArray();
			renderer.SetTiles(key, positions, tiles);
		}
	}
}
