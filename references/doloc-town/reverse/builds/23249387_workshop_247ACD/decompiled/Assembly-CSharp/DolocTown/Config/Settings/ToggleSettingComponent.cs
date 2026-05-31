using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class ToggleSettingComponent : SettingComponentBase
{
	public const int __ID__ = -955737556;

	public bool DefaultValue { get; private set; }

	public bool ShowSplitLine { get; private set; }

	public ToggleSettingComponent(JSONNode _json)
		: base(_json)
	{
		if (!_json["default_value"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultValue = _json["default_value"];
		if (!_json["show_split_line"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShowSplitLine = _json["show_split_line"];
	}

	public ToggleSettingComponent(bool default_value, bool show_split_line)
	{
		DefaultValue = default_value;
		ShowSplitLine = show_split_line;
	}

	public static ToggleSettingComponent DeserializeToggleSettingComponent(JSONNode _json)
	{
		return new ToggleSettingComponent(_json);
	}

	public override int GetTypeId()
	{
		return -955737556;
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
		return "{ DefaultValue:" + DefaultValue + ",ShowSplitLine:" + ShowSplitLine + ",}";
	}
}
