using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class CfgVector2Int : BeanBase
{
	public const int __ID__ = -588136060;

	public int X { get; private set; }

	public int Y { get; private set; }

	public CfgVector2Int(JSONNode _json)
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

	public CfgVector2Int(int x, int y)
	{
		X = x;
		Y = y;
	}

	public static CfgVector2Int DeserializeCfgVector2Int(JSONNode _json)
	{
		return new CfgVector2Int(_json);
	}

	public override int GetTypeId()
	{
		return -588136060;
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
