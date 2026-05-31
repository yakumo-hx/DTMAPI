using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class WDP_AcidRain_PowerGenerator : WeatherDecoratorProto
{
	public const int __ID__ = -369587776;

	public int DurationTu { get; private set; }

	public bool CouldInteract { get; private set; }

	public WDP_AcidRain_PowerGenerator(JSONNode _json)
		: base(_json)
	{
		if (!_json["durationTu"].IsNumber)
		{
			throw new SerializationException();
		}
		DurationTu = _json["durationTu"];
		if (!_json["couldInteract"].IsBoolean)
		{
			throw new SerializationException();
		}
		CouldInteract = _json["couldInteract"];
	}

	public WDP_AcidRain_PowerGenerator(int durationTu, bool couldInteract)
	{
		DurationTu = durationTu;
		CouldInteract = couldInteract;
	}

	public static WDP_AcidRain_PowerGenerator DeserializeWDP_AcidRain_PowerGenerator(JSONNode _json)
	{
		return new WDP_AcidRain_PowerGenerator(_json);
	}

	public override int GetTypeId()
	{
		return -369587776;
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
		return "{ DurationTu:" + DurationTu + ",CouldInteract:" + CouldInteract + ",}";
	}
}
