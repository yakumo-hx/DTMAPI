using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgTexture2DAsset : CfgAssetBase
{
	public const int __ID__ = -1249184759;

	public CfgTexture2DAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgTexture2DAsset(string url)
		: base(url)
	{
	}

	public static CfgTexture2DAsset DeserializeCfgTexture2DAsset(JSONNode _json)
	{
		return new CfgTexture2DAsset(_json);
	}

	public override int GetTypeId()
	{
		return -1249184759;
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
