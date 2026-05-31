using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Platform;
using DolocTown.Config.Weight;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DolocTown;

public static class PlatformTileGeneratorUtils
{
	private static readonly Dictionary<string, Tile> TileCache = new Dictionary<string, Tile>();

	public static Tile[] GenerateSurfaceTiles(Platform platform)
	{
		int width = platform.geometry.Width;
		if (width < 2)
		{
			return Array.Empty<Tile>();
		}
		Tile[] array = new Tile[width];
		System.Random rng = new System.Random(DeriveSeed(platform.randomSeed, 0));
		PlatformSurfaceData platformSuface = platform.proto.PlatformSuface;
		array[0] = GetTileBySpriteAsset(platformSuface.LeftSock);
		array[width - 1] = GetTileBySpriteAsset(platformSuface.RightSock);
		if (width == 2)
		{
			return array;
		}
		int num = 1;
		int num2 = width - 2;
		for (int i = 0; i < platformSuface.LeftSequence.Length; i++)
		{
			if (num > num2)
			{
				break;
			}
			array[num++] = GetTileBySpriteAsset(platformSuface.LeftSequence[i]);
		}
		for (int j = 0; j < platformSuface.RightSequence.Length; j++)
		{
			if (num > num2)
			{
				break;
			}
			array[num2--] = GetTileBySpriteAsset(platformSuface.RightSequence[j]);
		}
		for (int k = num; k <= num2; k++)
		{
			array[k] = PickWeightedTile(platformSuface.FillTiles, rng);
		}
		return array;
	}

	public static Tile[] GenerateColumnTiles(Platform platform, bool isLeftColumn)
	{
		int num = (isLeftColumn ? platform.geometry.columnHeight.x : platform.geometry.columnHeight.y);
		if (num <= 0)
		{
			return Array.Empty<Tile>();
		}
		System.Random rng = new System.Random(DeriveSeed(platform.randomSeed, isLeftColumn ? 1 : 2));
		PlatformColumnData platformColumnData = (isLeftColumn ? platform.proto.PlatformLeftColumn : platform.proto.PlatformRightColumn);
		if (num == 1)
		{
			return new Tile[1] { GetTileBySpriteAsset(platformColumnData.ColumnBottomTile) };
		}
		int num2 = Math.Min(num - 1, platformColumnData.FixedSequence.Length);
		List<Tile> list = new List<Tile>(num);
		for (int i = 0; i < num2; i++)
		{
			list.Add(GetTileBySpriteAsset(platformColumnData.FixedSequence[i]));
		}
		if (num > platformColumnData.FixedSequence.Length + 1 && platformColumnData.FillTiles.Length != 0)
		{
			int num3 = num - platformColumnData.FixedSequence.Length - 1;
			for (int j = 0; j < num3; j++)
			{
				list.Add(PickWeightedTile(platformColumnData.FillTiles, rng));
			}
		}
		int num4 = num - list.Count;
		for (int k = 0; k < num4; k++)
		{
			list.Add(GetTileBySpriteAsset(platformColumnData.ColumnBottomTile));
		}
		return list.ToArray();
	}

	public static void SetPlatformDraft(this Tilemap tilemap, PlatformGeometry geometry, PlatformInfo proto)
	{
		tilemap.ClearAllTiles();
		tilemap.SetTile((Vector3Int)geometry.LeftTop, GetTileBySpriteAsset(proto.PlatformSuface.LeftSock));
		tilemap.SetTile((Vector3Int)geometry.RightTop, GetTileBySpriteAsset(proto.PlatformSuface.RightSock));
		SpriteWeight spriteWeight = proto.PlatformSuface.FillTiles.OrderByDescending((SpriteWeight i) => i.Weight).FirstOrDefault();
		foreach (Vector2Int platformPosition in geometry.PlatformPositions)
		{
			tilemap.SetTile((Vector3Int)platformPosition, GetTileBySpriteAsset(spriteWeight?.SpriteAsset));
		}
		foreach (Vector2Int columnPosition in geometry.ColumnPositions)
		{
			tilemap.SetTile((Vector3Int)columnPosition, GetTileBySpriteAsset(proto.PlatformLeftColumn.ColumnBottomTile));
		}
	}

	public static Tile GetTileBySpriteAsset(SpriteAsset asset)
	{
		if (asset == null || asset.Asset == null)
		{
			return null;
		}
		if (TileCache.TryGetValue(asset.AssetUrl, out var value))
		{
			return value;
		}
		Tile tile = ScriptableObject.CreateInstance<Tile>();
		tile.sprite = asset.Asset;
		TileCache[asset.AssetUrl] = tile;
		return tile;
	}

	private static Tile PickWeightedTile(SpriteWeight[] weights, System.Random rng)
	{
		int maxValue = weights.Sum((SpriteWeight w) => w.Weight);
		int num = rng.Next(0, maxValue);
		int num2 = 0;
		foreach (SpriteWeight spriteWeight in weights)
		{
			num2 += spriteWeight.Weight;
			if (num < num2)
			{
				return GetTileBySpriteAsset(spriteWeight.SpriteAsset);
			}
		}
		return GetTileBySpriteAsset(weights[^1].SpriteAsset);
	}

	private static int DeriveSeed(int baseSeed, int index)
	{
		return (baseSeed * 397) ^ index;
	}

	public static void ClearCache()
	{
		foreach (Tile value in TileCache.Values)
		{
			if (value != null)
			{
				UnityEngine.Object.Destroy(value);
			}
		}
		TileCache.Clear();
	}
}
