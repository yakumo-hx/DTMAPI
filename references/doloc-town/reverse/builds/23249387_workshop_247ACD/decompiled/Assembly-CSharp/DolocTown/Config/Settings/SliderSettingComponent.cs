using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class SliderSettingComponent : SettingComponentBase
{
	public const int __ID__ = 966761913;

	public int DefaultValue { get; private set; }

	public int MinValue { get; private set; }

	public int MaxValue { get; private set; }

	public float Scale { get; private set; }

	public SliderSettingComponent(JSONNode _json)
		: base(_json)
	{
		if (!_json["default_value"].IsNumber)
		{
			throw new SerializationException();
		}
		DefaultValue = _json["default_value"];
		if (!_json["min_value"].IsNumber)
		{
			throw new SerializationException();
		}
		MinValue = _json["min_value"];
		if (!_json["max_value"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxValue = _json["max_value"];
		if (!_json["scale"].IsNumber)
		{
			throw new SerializationException();
		}
		Scale = _json["scale"];
	}

	public SliderSettingComponent(int default_value, int min_value, int max_value, float scale)
	{
		DefaultValue = default_value;
		MinValue = min_value;
		MaxValue = max_value;
		Scale = scale;
	}

	public static SliderSettingComponent DeserializeSliderSettingComponent(JSONNode _json)
	{
		return new SliderSettingComponent(_json);
	}

	public override int GetTypeId()
	{
		return 966761913;
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
		return "{ DefaultValue:" + DefaultValue + ",MinValue:" + MinValue + ",MaxValue:" + MaxValue + ",Scale:" + Scale + ",}";
	}
}
