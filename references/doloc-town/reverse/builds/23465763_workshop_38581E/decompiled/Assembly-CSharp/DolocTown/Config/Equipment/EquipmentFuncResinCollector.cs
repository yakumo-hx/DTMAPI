using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncResinCollector : EquipmentFuncEquipment
{
	public const int __ID__ = -589967942;

	public string DefaultOutput { get; private set; }

	public ResinCollectorOutputInfo DefaultOutput_Ref { get; private set; }

	public int Interval { get; private set; }

	public int Capacity { get; private set; }

	public EquipmentFuncResinCollector(JSONNode _json)
		: base(_json)
	{
		if (!_json["default_output"].IsString)
		{
			throw new SerializationException();
		}
		DefaultOutput = _json["default_output"];
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
	}

	public EquipmentFuncResinCollector(string default_output, int interval, int capacity)
	{
		DefaultOutput = default_output;
		Interval = interval;
		Capacity = capacity;
	}

	public static EquipmentFuncResinCollector DeserializeEquipmentFuncResinCollector(JSONNode _json)
	{
		return new EquipmentFuncResinCollector(_json);
	}

	public override int GetTypeId()
	{
		return -589967942;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		DefaultOutput_Ref = (_tables["Resource.TbResinCollectorOutput"] as TbResinCollectorOutput).GetOrDefault(DefaultOutput);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ DefaultOutput:" + DefaultOutput + ",Interval:" + Interval + ",Capacity:" + Capacity + ",}";
	}
}
