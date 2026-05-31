using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class OptionSettingComponent : SettingComponentBase
{
	public const int __ID__ = -1254095539;

	public string DefaultValue { get; private set; }

	public List<string> OptionValues { get; private set; }

	public bool UseL10nKey { get; private set; }

	public List<string> OptionLables { get; private set; }

	public OptionSettingComponent(JSONNode _json)
		: base(_json)
	{
		if (!_json["default_value"].IsString)
		{
			throw new SerializationException();
		}
		DefaultValue = _json["default_value"];
		JSONNode jSONNode = _json["option_values"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		OptionValues = new List<string>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string item = child;
			OptionValues.Add(item);
		}
		if (!_json["use_l10n_key"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseL10nKey = _json["use_l10n_key"];
		JSONNode jSONNode2 = _json["option_lables"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		OptionLables = new List<string>(jSONNode2.Count);
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string item2 = child2;
			OptionLables.Add(item2);
		}
	}

	public OptionSettingComponent(string default_value, List<string> option_values, bool use_l10n_key, List<string> option_lables)
	{
		DefaultValue = default_value;
		OptionValues = option_values;
		UseL10nKey = use_l10n_key;
		OptionLables = option_lables;
	}

	public static OptionSettingComponent DeserializeOptionSettingComponent(JSONNode _json)
	{
		return new OptionSettingComponent(_json);
	}

	public override int GetTypeId()
	{
		return -1254095539;
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
		return "{ DefaultValue:" + DefaultValue + ",OptionValues:" + StringUtil.CollectionToString(OptionValues) + ",UseL10nKey:" + UseL10nKey + ",OptionLables:" + StringUtil.CollectionToString(OptionLables) + ",}";
	}
}
