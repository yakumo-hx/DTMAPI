using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncMalignantWeatherSuppressor : EquipmentFuncEquipment
{
	public const int __ID__ = 2015132783;

	public int Duration { get; private set; }

	public EquipmentFuncMalignantWeatherSuppressor(JSONNode _json)
		: base(_json)
	{
		if (!_json["duration"].IsNumber)
		{
			throw new SerializationException();
		}
		Duration = _json["duration"];
	}

	public EquipmentFuncMalignantWeatherSuppressor(int duration)
	{
		Duration = duration;
	}

	public static EquipmentFuncMalignantWeatherSuppressor DeserializeEquipmentFuncMalignantWeatherSuppressor(JSONNode _json)
	{
		return new EquipmentFuncMalignantWeatherSuppressor(_json);
	}

	public override int GetTypeId()
	{
		return 2015132783;
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
		return "{ Duration:" + Duration + ",}";
	}
}
