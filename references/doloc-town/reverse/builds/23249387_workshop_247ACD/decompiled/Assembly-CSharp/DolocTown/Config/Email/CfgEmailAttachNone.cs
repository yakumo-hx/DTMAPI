using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Email;

public sealed class CfgEmailAttachNone : CfgEmailAttachBase
{
	public const int __ID__ = 1756268199;

	public CfgEmailAttachNone(JSONNode _json)
		: base(_json)
	{
	}

	public CfgEmailAttachNone()
	{
	}

	public static CfgEmailAttachNone DeserializeCfgEmailAttachNone(JSONNode _json)
	{
		return new CfgEmailAttachNone(_json);
	}

	public override int GetTypeId()
	{
		return 1756268199;
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
		return "{ }";
	}
}
