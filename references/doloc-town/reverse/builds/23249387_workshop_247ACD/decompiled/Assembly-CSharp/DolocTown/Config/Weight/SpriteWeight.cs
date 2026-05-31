using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Weight;

public sealed class SpriteWeight : BeanBase
{
	public const int __ID__ = -981462701;

	public SpriteAsset SpriteAsset { get; private set; }

	public int Weight { get; private set; }

	public SpriteWeight(JSONNode _json)
	{
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
		if (!_json["weight"].IsNumber)
		{
			throw new SerializationException();
		}
		Weight = _json["weight"];
	}

	public SpriteWeight(SpriteAsset sprite_asset, int weight)
	{
		SpriteAsset = sprite_asset;
		Weight = weight;
	}

	public static SpriteWeight DeserializeSpriteWeight(JSONNode _json)
	{
		return new SpriteWeight(_json);
	}

	public override int GetTypeId()
	{
		return -981462701;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SpriteAsset:" + SpriteAsset?.ToString() + ",Weight:" + Weight + ",}";
	}
}
