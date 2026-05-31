using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.General;

public sealed class RangeInt : BeanBase
{
	public const int __ID__ = -939845640;

	public int MinCount { get; private set; }

	public int MaxCount { get; private set; }

	public int RandomCount
	{
		get
		{
			if (MaxCount > MinCount)
			{
				return UnityEngine.Random.Range(MinCount, MaxCount + 1);
			}
			return MinCount;
		}
	}

	public RangeInt(JSONNode _json)
	{
		if (!_json["min_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MinCount = _json["min_count"];
		if (!_json["max_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxCount = _json["max_count"];
	}

	public RangeInt(int min_count, int max_count)
	{
		MinCount = min_count;
		MaxCount = max_count;
	}

	public static RangeInt DeserializeRangeInt(JSONNode _json)
	{
		return new RangeInt(_json);
	}

	public override int GetTypeId()
	{
		return -939845640;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ MinCount:" + MinCount + ",MaxCount:" + MaxCount + ",}";
	}
}
