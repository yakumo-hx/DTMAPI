using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class WDP_AcidRain_Worker : WeatherDecoratorProto
{
	public const int __ID__ = -523376656;

	public int DurationTu { get; private set; }

	public WDP_AcidRain_Worker(JSONNode _json)
		: base(_json)
	{
		if (!_json["durationTu"].IsNumber)
		{
			throw new SerializationException();
		}
		DurationTu = _json["durationTu"];
	}

	public WDP_AcidRain_Worker(int durationTu)
	{
		DurationTu = durationTu;
	}

	public static WDP_AcidRain_Worker DeserializeWDP_AcidRain_Worker(JSONNode _json)
	{
		return new WDP_AcidRain_Worker(_json);
	}

	public override int GetTypeId()
	{
		return -523376656;
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
		return "{ DurationTu:" + DurationTu + ",}";
	}
}
