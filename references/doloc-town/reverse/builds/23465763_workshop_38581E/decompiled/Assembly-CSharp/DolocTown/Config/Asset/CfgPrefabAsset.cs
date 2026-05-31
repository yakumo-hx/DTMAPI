using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgPrefabAsset : CfgAssetBase
{
	public const int __ID__ = -694989882;

	public CfgPrefabAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgPrefabAsset(string url)
		: base(url)
	{
	}

	public static CfgPrefabAsset DeserializeCfgPrefabAsset(JSONNode _json)
	{
		return new CfgPrefabAsset(_json);
	}

	public override int GetTypeId()
	{
		return -694989882;
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
