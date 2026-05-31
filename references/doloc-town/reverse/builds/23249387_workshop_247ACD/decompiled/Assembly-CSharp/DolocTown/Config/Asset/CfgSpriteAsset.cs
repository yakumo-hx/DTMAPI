using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgSpriteAsset : CfgAssetBase
{
	public const int __ID__ = 1577098021;

	public CfgSpriteAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgSpriteAsset(string url)
		: base(url)
	{
	}

	public static CfgSpriteAsset DeserializeCfgSpriteAsset(JSONNode _json)
	{
		return new CfgSpriteAsset(_json);
	}

	public override int GetTypeId()
	{
		return 1577098021;
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
		return "{ Url:" + base.Url + ",}";
	}
}
