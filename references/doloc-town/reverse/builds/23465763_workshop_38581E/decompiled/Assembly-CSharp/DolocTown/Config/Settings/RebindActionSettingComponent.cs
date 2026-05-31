using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class RebindActionSettingComponent : SettingComponentBase
{
	public const int __ID__ = -1933965858;

	public string InputAction { get; private set; }

	public RebindActionInfo InputAction_Ref { get; private set; }

	public RebindActionSettingComponent(JSONNode _json)
		: base(_json)
	{
		if (!_json["input_action"].IsString)
		{
			throw new SerializationException();
		}
		InputAction = _json["input_action"];
	}

	public RebindActionSettingComponent(string input_action)
	{
		InputAction = input_action;
	}

	public static RebindActionSettingComponent DeserializeRebindActionSettingComponent(JSONNode _json)
	{
		return new RebindActionSettingComponent(_json);
	}

	public override int GetTypeId()
	{
		return -1933965858;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		InputAction_Ref = (_tables["Settings.TbRebindAction"] as TbRebindAction).GetOrDefault(InputAction);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ InputAction:" + InputAction + ",}";
	}
}
