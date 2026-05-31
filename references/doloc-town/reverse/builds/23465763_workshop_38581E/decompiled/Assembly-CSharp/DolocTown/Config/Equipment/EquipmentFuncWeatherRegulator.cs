using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncWeatherRegulator : EquipmentFuncEquipment
{
	public const int __ID__ = -612428449;

	public int CdDuration { get; private set; }

	public EquipmentFuncWeatherRegulator(JSONNode _json)
		: base(_json)
	{
		if (!_json["cdDuration"].IsNumber)
		{
			throw new SerializationException();
		}
		CdDuration = _json["cdDuration"];
	}

	public EquipmentFuncWeatherRegulator(int cdDuration)
	{
		CdDuration = cdDuration;
	}

	public static EquipmentFuncWeatherRegulator DeserializeEquipmentFuncWeatherRegulator(JSONNode _json)
	{
		return new EquipmentFuncWeatherRegulator(_json);
	}

	public override int GetTypeId()
	{
		return -612428449;
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
		return "{ CdDuration:" + CdDuration + ",}";
	}
}
