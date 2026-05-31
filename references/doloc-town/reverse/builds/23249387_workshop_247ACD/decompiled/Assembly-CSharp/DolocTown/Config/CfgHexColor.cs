using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class CfgHexColor : BeanBase
{
	public const int __ID__ = -612741780;

	public string Hex { get; private set; }

	public CfgHexColor(JSONNode _json)
	{
		if (!_json["hex"].IsString)
		{
			throw new SerializationException();
		}
		Hex = _json["hex"];
	}

	public CfgHexColor(string hex)
	{
		Hex = hex;
	}

	public static CfgHexColor DeserializeCfgHexColor(JSONNode _json)
	{
		return new CfgHexColor(_json);
	}

	public override int GetTypeId()
	{
		return -612741780;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Hex:" + Hex + ",}";
	}
}
