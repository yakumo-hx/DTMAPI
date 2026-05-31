using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config;

public sealed class CfgHDRColor : BeanBase
{
	public const int __ID__ = -923499919;

	public int R { get; private set; }

	public int G { get; private set; }

	public int B { get; private set; }

	public float Intensity { get; private set; }

	public CfgHDRColor(JSONNode _json)
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
		if (!_json["intensity"].IsNumber)
		{
			throw new SerializationException();
		}
		Intensity = _json["intensity"];
	}

	public CfgHDRColor(int r, int g, int b, float intensity)
	{
		R = r;
		G = g;
		B = b;
		Intensity = intensity;
	}

	public static CfgHDRColor DeserializeCfgHDRColor(JSONNode _json)
	{
		return new CfgHDRColor(_json);
	}

	public override int GetTypeId()
	{
		return -923499919;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ R:" + R + ",G:" + G + ",B:" + B + ",Intensity:" + Intensity + ",}";
	}
}
