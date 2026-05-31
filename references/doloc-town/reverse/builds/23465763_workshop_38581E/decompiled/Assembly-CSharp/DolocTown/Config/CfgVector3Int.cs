using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class CfgVector3Int : BeanBase
{
	public const int __ID__ = -588106269;

	public int X { get; private set; }

	public int Y { get; private set; }

	public int Z { get; private set; }

	public CfgVector3Int(JSONNode _json)
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
		if (!_json["z"].IsNumber)
		{
			throw new SerializationException();
		}
		Z = _json["z"];
	}

	public CfgVector3Int(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public static CfgVector3Int DeserializeCfgVector3Int(JSONNode _json)
	{
		return new CfgVector3Int(_json);
	}

	public override int GetTypeId()
	{
		return -588106269;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ X:" + X + ",Y:" + Y + ",Z:" + Z + ",}";
	}
}
