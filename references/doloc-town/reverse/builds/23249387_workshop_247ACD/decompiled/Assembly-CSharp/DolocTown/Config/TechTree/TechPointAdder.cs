using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.TechTree;

public sealed class TechPointAdder : BeanBase
{
	public const int __ID__ = -847467922;

	public TechPointType Type { get; private set; }

	public int Count { get; private set; }

	public TechPointAdder(JSONNode _json)
	{
		if (!_json["type"].IsNumber)
		{
			throw new SerializationException();
		}
		Type = (TechPointType)_json["type"].AsInt;
		if (!_json["count"].IsNumber)
		{
			throw new SerializationException();
		}
		Count = _json["count"];
	}

	public TechPointAdder(TechPointType type, int count)
	{
		Type = type;
		Count = count;
	}

	public static TechPointAdder DeserializeTechPointAdder(JSONNode _json)
	{
		return new TechPointAdder(_json);
	}

	public override int GetTypeId()
	{
		return -847467922;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Type:" + Type.ToString() + ",Count:" + Count + ",}";
	}
}
