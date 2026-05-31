using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgMaterialAsset : CfgAssetBase
{
	public const int __ID__ = 864440195;

	public CfgMaterialAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgMaterialAsset(string url)
		: base(url)
	{
	}

	public static CfgMaterialAsset DeserializeCfgMaterialAsset(JSONNode _json)
	{
		return new CfgMaterialAsset(_json);
	}

	public override int GetTypeId()
	{
		return 864440195;
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
