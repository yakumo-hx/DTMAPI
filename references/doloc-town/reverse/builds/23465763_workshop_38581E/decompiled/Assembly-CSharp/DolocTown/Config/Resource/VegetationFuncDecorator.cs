using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class VegetationFuncDecorator : VegetationFuncBase
{
	public const int __ID__ = -844715639;

	public SpriteAsset[] Skins { get; private set; }

	public VegetationFuncDecorator(JSONNode _json)
		: base(_json)
	{
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

	public VegetationFuncDecorator(SpriteAsset[] skins)
	{
		Skins = skins;
	}

	public static VegetationFuncDecorator DeserializeVegetationFuncDecorator(JSONNode _json)
	{
		return new VegetationFuncDecorator(_json);
	}

	public override int GetTypeId()
	{
		return -844715639;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Skins:" + StringUtil.CollectionToString(Skins) + ",}";
	}
}
