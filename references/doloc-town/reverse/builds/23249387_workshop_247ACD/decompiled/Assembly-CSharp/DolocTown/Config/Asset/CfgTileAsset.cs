using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgTileAsset : CfgAssetBase
{
	public const int __ID__ = 79268700;

	public CfgTileAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgTileAsset(string url)
		: base(url)
	{
	}

	public static CfgTileAsset DeserializeCfgTileAsset(JSONNode _json)
	{
		return new CfgTileAsset(_json);
	}

	public override int GetTypeId()
	{
		return 79268700;
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
