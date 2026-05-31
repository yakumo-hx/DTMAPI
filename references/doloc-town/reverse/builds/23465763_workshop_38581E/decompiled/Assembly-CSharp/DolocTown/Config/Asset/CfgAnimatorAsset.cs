using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgAnimatorAsset : CfgAssetBase
{
	public const int __ID__ = 852828611;

	public CfgAnimatorAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgAnimatorAsset(string url)
		: base(url)
	{
	}

	public static CfgAnimatorAsset DeserializeCfgAnimatorAsset(JSONNode _json)
	{
		return new CfgAnimatorAsset(_json);
	}

	public override int GetTypeId()
	{
		return 852828611;
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
