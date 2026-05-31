using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class GoodsDurationLevel : BeanBase
{
	public const int __ID__ = -1759530182;

	public int MoneyThreshold { get; private set; }

	public int Duration { get; private set; }

	public GoodsDurationLevel(JSONNode _json)
	{
		if (!_json["money_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		MoneyThreshold = _json["money_threshold"];
		if (!_json["duration"].IsNumber)
		{
			throw new SerializationException();
		}
		Duration = _json["duration"];
	}

	public GoodsDurationLevel(int money_threshold, int duration)
	{
		MoneyThreshold = money_threshold;
		Duration = duration;
	}

	public static GoodsDurationLevel DeserializeGoodsDurationLevel(JSONNode _json)
	{
		return new GoodsDurationLevel(_json);
	}

	public override int GetTypeId()
	{
		return -1759530182;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ MoneyThreshold:" + MoneyThreshold + ",Duration:" + Duration + ",}";
	}
}
