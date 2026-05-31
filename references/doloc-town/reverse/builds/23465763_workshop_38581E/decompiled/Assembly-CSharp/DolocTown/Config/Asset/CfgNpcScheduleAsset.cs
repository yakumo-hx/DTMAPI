using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgNpcScheduleAsset : CfgAssetBase
{
	public const int __ID__ = 262382974;

	public CfgNpcScheduleAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgNpcScheduleAsset(string url)
		: base(url)
	{
	}

	public static CfgNpcScheduleAsset DeserializeCfgNpcScheduleAsset(JSONNode _json)
	{
		return new CfgNpcScheduleAsset(_json);
	}

	public override int GetTypeId()
	{
		return 262382974;
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
