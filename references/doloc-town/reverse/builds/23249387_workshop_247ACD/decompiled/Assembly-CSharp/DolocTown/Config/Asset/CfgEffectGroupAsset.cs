using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgEffectGroupAsset : CfgAssetBase
{
	public const int __ID__ = -13497240;

	public CfgEffectGroupAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgEffectGroupAsset(string url)
		: base(url)
	{
	}

	public static CfgEffectGroupAsset DeserializeCfgEffectGroupAsset(JSONNode _json)
	{
		return new CfgEffectGroupAsset(_json);
	}

	public override int GetTypeId()
	{
		return -13497240;
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
