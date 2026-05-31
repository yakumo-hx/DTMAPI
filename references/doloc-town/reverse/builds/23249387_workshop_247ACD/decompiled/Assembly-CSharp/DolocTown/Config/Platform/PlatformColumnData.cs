using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Weight;
using SimpleJSON;

namespace DolocTown.Config.Platform;

public sealed class PlatformColumnData : BeanBase
{
	public const int __ID__ = -431012274;

	public SpriteAsset[] FixedSequence { get; private set; }

	public SpriteWeight[] FillTiles { get; private set; }

	public SpriteAsset ColumnBottomTile { get; private set; }

	public PlatformColumnData(JSONNode _json)
	{
		JSONNode jSONNode = _json["fixed_sequence"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		FixedSequence = new SpriteAsset[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child));
			FixedSequence[num++] = spriteAsset;
		}
		JSONNode jSONNode2 = _json["fill_tiles"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		FillTiles = new SpriteWeight[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			SpriteWeight spriteWeight = SpriteWeight.DeserializeSpriteWeight(child2);
			FillTiles[num2++] = spriteWeight;
		}
		if (!_json["column_bottom_tile"].IsObject)
		{
			throw new SerializationException();
		}
		ColumnBottomTile = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["column_bottom_tile"]));
	}

	public PlatformColumnData(SpriteAsset[] fixed_sequence, SpriteWeight[] fill_tiles, SpriteAsset column_bottom_tile)
	{
		FixedSequence = fixed_sequence;
		FillTiles = fill_tiles;
		ColumnBottomTile = column_bottom_tile;
	}

	public static PlatformColumnData DeserializePlatformColumnData(JSONNode _json)
	{
		return new PlatformColumnData(_json);
	}

	public override int GetTypeId()
	{
		return -431012274;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpriteWeight[] fillTiles = FillTiles;
		for (int i = 0; i < fillTiles.Length; i++)
		{
			fillTiles[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SpriteWeight[] fillTiles = FillTiles;
		for (int i = 0; i < fillTiles.Length; i++)
		{
			fillTiles[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ FixedSequence:" + StringUtil.CollectionToString(FixedSequence) + ",FillTiles:" + StringUtil.CollectionToString(FillTiles) + ",ColumnBottomTile:" + ColumnBottomTile?.ToString() + ",}";
	}
}
