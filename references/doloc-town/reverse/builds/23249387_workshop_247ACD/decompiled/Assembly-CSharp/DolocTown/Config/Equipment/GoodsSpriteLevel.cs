using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class GoodsSpriteLevel : BeanBase
{
	public const int __ID__ = -2077571287;

	public int CountThreshold { get; private set; }

	public SpriteAsset SpriteAsset { get; private set; }

	public GoodsSpriteLevel(JSONNode _json)
	{
		if (!_json["count_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		CountThreshold = _json["count_threshold"];
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
	}

	public GoodsSpriteLevel(int count_threshold, SpriteAsset sprite_asset)
	{
		CountThreshold = count_threshold;
		SpriteAsset = sprite_asset;
	}

	public static GoodsSpriteLevel DeserializeGoodsSpriteLevel(JSONNode _json)
	{
		return new GoodsSpriteLevel(_json);
	}

	public override int GetTypeId()
	{
		return -2077571287;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ CountThreshold:" + CountThreshold + ",SpriteAsset:" + SpriteAsset?.ToString() + ",}";
	}
}
