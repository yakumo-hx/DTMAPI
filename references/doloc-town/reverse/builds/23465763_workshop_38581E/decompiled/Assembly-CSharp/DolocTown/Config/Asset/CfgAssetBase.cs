using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public abstract class CfgAssetBase : BeanBase
{
	public string Url { get; private set; }

	public CfgAssetBase(JSONNode _json)
	{
		if (!_json["url"].IsString)
		{
			throw new SerializationException();
		}
		Url = _json["url"];
	}

	public CfgAssetBase(string url)
	{
		Url = url;
	}

	public static CfgAssetBase DeserializeCfgAssetBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"CfgSpriteAsset" => new CfgSpriteAsset(_json), 
			"CfgTexture2DAsset" => new CfgTexture2DAsset(_json), 
			"CfgTileAsset" => new CfgTileAsset(_json), 
			"CfgAnimatorAsset" => new CfgAnimatorAsset(_json), 
			"CfgNpcScheduleAsset" => new CfgNpcScheduleAsset(_json), 
			"CfgPrefabAsset" => new CfgPrefabAsset(_json), 
			"CfgSwitchScheduleAsset" => new CfgSwitchScheduleAsset(_json), 
			"CfgEffectGroupAsset" => new CfgEffectGroupAsset(_json), 
			"CfgMaterialAsset" => new CfgMaterialAsset(_json), 
			"CfgShaderAsset" => new CfgShaderAsset(_json), 
			"CfgTmpFontAsset" => new CfgTmpFontAsset(_json), 
			"CfgFontAsset" => new CfgFontAsset(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Url:" + Url + ",}";
	}
}
