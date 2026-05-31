using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgFontAsset : CfgAssetBase
{
	public const int __ID__ = -609767845;

	public CfgFontAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgFontAsset(string url)
		: base(url)
	{
	}

	public static CfgFontAsset DeserializeCfgFontAsset(JSONNode _json)
	{
		return new CfgFontAsset(_json);
	}

	public override int GetTypeId()
	{
		return -609767845;
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
