using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.General;

public sealed class TextLabel : BeanBase
{
	public const int __ID__ = 625817601;

	public bool UseBounceEffect { get; private set; }

	public string OpenLabel { get; private set; }

	public string CloseLabel { get; private set; }

	public TextLabel(JSONNode _json)
	{
		if (!_json["use_bounce_effect"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseBounceEffect = _json["use_bounce_effect"];
		if (!_json["open_label"].IsString)
		{
			throw new SerializationException();
		}
		OpenLabel = _json["open_label"];
		if (!_json["close_label"].IsString)
		{
			throw new SerializationException();
		}
		CloseLabel = _json["close_label"];
	}

	public TextLabel(bool use_bounce_effect, string open_label, string close_label)
	{
		UseBounceEffect = use_bounce_effect;
		OpenLabel = open_label;
		CloseLabel = close_label;
	}

	public static TextLabel DeserializeTextLabel(JSONNode _json)
	{
		return new TextLabel(_json);
	}

	public override int GetTypeId()
	{
		return 625817601;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ UseBounceEffect:" + UseBounceEffect + ",OpenLabel:" + OpenLabel + ",CloseLabel:" + CloseLabel + ",}";
	}
}
