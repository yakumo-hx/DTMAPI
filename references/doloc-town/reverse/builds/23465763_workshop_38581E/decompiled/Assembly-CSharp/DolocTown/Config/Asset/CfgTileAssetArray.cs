using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgTileAssetArray : BeanBase
{
	public const int __ID__ = -649285539;

	public TileAsset[] Array { get; private set; }

	public CfgTileAssetArray(JSONNode _json)
	{
		JSONNode jSONNode = _json["array"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Array = new TileAsset[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TileAsset tileAsset = ExternalTypeUtil.TileAssetConverter(CfgTileAsset.DeserializeCfgTileAsset(child));
			Array[num++] = tileAsset;
		}
	}

	public CfgTileAssetArray(TileAsset[] array)
	{
		Array = array;
	}

	public static CfgTileAssetArray DeserializeCfgTileAssetArray(JSONNode _json)
	{
		return new CfgTileAssetArray(_json);
	}

	public override int GetTypeId()
	{
		return -649285539;
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
