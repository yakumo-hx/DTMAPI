using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropLevelData : BeanBase
{
	public const int __ID__ = 1518262811;

	public int GrowthValue { get; private set; }

	public SpriteAsset[] Skins { get; private set; }

	public CropLevelData(JSONNode _json)
	{
		if (!_json["growth_value"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthValue = _json["growth_value"];
		JSONNode jSONNode = _json["skins"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Skins = new SpriteAsset[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child));
			Skins[num++] = spriteAsset;
		}
	}

	public CropLevelData(int growth_value, SpriteAsset[] skins)
	{
		GrowthValue = growth_value;
		Skins = skins;
	}

	public static CropLevelData DeserializeCropLevelData(JSONNode _json)
	{
		return new CropLevelData(_json);
	}

	public override int GetTypeId()
	{
		return 1518262811;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ GrowthValue:" + GrowthValue + ",Skins:" + StringUtil.CollectionToString(Skins) + ",}";
	}
}
