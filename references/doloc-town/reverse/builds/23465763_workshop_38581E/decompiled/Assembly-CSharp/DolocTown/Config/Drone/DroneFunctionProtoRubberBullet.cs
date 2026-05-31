using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoRubberBullet : DroneFunctionProto
{
	public const int __ID__ = -682079872;

	public int MaxReboundTimes { get; private set; }

	public DroneFunctionProtoRubberBullet(JSONNode _json)
		: base(_json)
	{
		if (!_json["max_rebound_times"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxReboundTimes = _json["max_rebound_times"];
	}

	public DroneFunctionProtoRubberBullet(int max_rebound_times)
	{
		MaxReboundTimes = max_rebound_times;
	}

	public static DroneFunctionProtoRubberBullet DeserializeDroneFunctionProtoRubberBullet(JSONNode _json)
	{
		return new DroneFunctionProtoRubberBullet(_json);
	}

	public override int GetTypeId()
	{
		return -682079872;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ MaxReboundTimes:" + MaxReboundTimes + ",}";
	}
}
