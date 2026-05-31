using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgTmpFontAsset : CfgAssetBase
{
	public const int __ID__ = -1507770416;

	public CfgTmpFontAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgTmpFontAsset(string url)
		: base(url)
	{
	}

	public static CfgTmpFontAsset DeserializeCfgTmpFontAsset(JSONNode _json)
	{
		return new CfgTmpFontAsset(_json);
	}

	public override int GetTypeId()
	{
		return -1507770416;
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
