using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgSwitchScheduleAsset : CfgAssetBase
{
	public const int __ID__ = 1067012543;

	public CfgSwitchScheduleAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgSwitchScheduleAsset(string url)
		: base(url)
	{
	}

	public static CfgSwitchScheduleAsset DeserializeCfgSwitchScheduleAsset(JSONNode _json)
	{
		return new CfgSwitchScheduleAsset(_json);
	}

	public override int GetTypeId()
	{
		return 1067012543;
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
