using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Asset;

public sealed class CfgShaderAsset : CfgAssetBase
{
	public const int __ID__ = -663397883;

	public CfgShaderAsset(JSONNode _json)
		: base(_json)
	{
	}

	public CfgShaderAsset(string url)
		: base(url)
	{
	}

	public static CfgShaderAsset DeserializeCfgShaderAsset(JSONNode _json)
	{
		return new CfgShaderAsset(_json);
	}

	public override int GetTypeId()
	{
		return -663397883;
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
