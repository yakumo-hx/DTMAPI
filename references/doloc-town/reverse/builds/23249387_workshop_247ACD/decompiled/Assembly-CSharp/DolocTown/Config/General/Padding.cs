using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.General;

public sealed class Padding : BeanBase
{
	public const int __ID__ = -706265909;

	public int L { get; private set; }

	public int R { get; private set; }

	public int T { get; private set; }

	public int B { get; private set; }

	public Padding(JSONNode _json)
	{
		if (!_json["l"].IsNumber)
		{
			throw new SerializationException();
		}
		L = _json["l"];
		if (!_json["r"].IsNumber)
		{
			throw new SerializationException();
		}
		R = _json["r"];
		if (!_json["t"].IsNumber)
		{
			throw new SerializationException();
		}
		T = _json["t"];
		if (!_json["b"].IsNumber)
		{
			throw new SerializationException();
		}
		B = _json["b"];
	}

	public Padding(int l, int r, int t, int b)
	{
		L = l;
		R = r;
		T = t;
		B = b;
	}

	public static Padding DeserializePadding(JSONNode _json)
	{
		return new Padding(_json);
	}

	public override int GetTypeId()
	{
		return -706265909;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ L:" + L + ",R:" + R + ",T:" + T + ",B:" + B + ",}";
	}
}
