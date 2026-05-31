using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class CfgVector2 : BeanBase
{
	public const int __ID__ = 1165017707;

	public float X { get; private set; }

	public float Y { get; private set; }

	public CfgVector2(JSONNode _json)
	{
		if (!_json["x"].IsNumber)
		{
			throw new SerializationException();
		}
		X = _json["x"];
		if (!_json["y"].IsNumber)
		{
			throw new SerializationException();
		}
		Y = _json["y"];
	}

	public CfgVector2(float x, float y)
	{
		X = x;
		Y = y;
	}

	public static CfgVector2 DeserializeCfgVector2(JSONNode _json)
	{
		return new CfgVector2(_json);
	}

	public override int GetTypeId()
	{
		return 1165017707;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ X:" + X + ",Y:" + Y + ",}";
	}
}
