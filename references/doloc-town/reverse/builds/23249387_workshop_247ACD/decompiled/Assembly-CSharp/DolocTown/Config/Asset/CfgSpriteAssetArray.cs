using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgSpriteAssetArray : BeanBase
{
	public const int __ID__ = 605361716;

	public SpriteAsset[] Array { get; private set; }

	public CfgSpriteAssetArray(JSONNode _json)
	{
		JSONNode jSONNode = _json["array"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Array = new SpriteAsset[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child));
			Array[num++] = spriteAsset;
		}
	}

	public CfgSpriteAssetArray(SpriteAsset[] array)
	{
		Array = array;
	}

	public static CfgSpriteAssetArray DeserializeCfgSpriteAssetArray(JSONNode _json)
	{
		return new CfgSpriteAssetArray(_json);
	}

	public override int GetTypeId()
	{
		return 605361716;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Array:" + StringUtil.CollectionToString(Array) + ",}";
	}
}
