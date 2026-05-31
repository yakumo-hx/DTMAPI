using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class CfgColor : BeanBase
{
	public const int __ID__ = -145636993;

	public int R { get; private set; }

	public int G { get; private set; }

	public int B { get; private set; }

	public int A { get; private set; }

	public CfgColor(JSONNode _json)
	{
		if (!_json["r"].IsNumber)
		{
			throw new SerializationException();
		}
		R = _json["r"];
		if (!_json["g"].IsNumber)
		{
			throw new SerializationException();
		}
		G = _json["g"];
		if (!_json["b"].IsNumber)
		{
			throw new SerializationException();
		}
		B = _json["b"];
		if (!_json["a"].IsNumber)
		{
			throw new SerializationException();
		}
		A = _json["a"];
	}

	public CfgColor(int r, int g, int b, int a)
	{
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public static CfgColor DeserializeCfgColor(JSONNode _json)
	{
		return new CfgColor(_json);
	}

	public override int GetTypeId()
	{
		return -145636993;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ R:" + R + ",G:" + G + ",B:" + B + ",A:" + A + ",}";
	}
}
