using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class EmptySettingComponent : SettingComponentBase
{
	public const int __ID__ = -750588817;

	public EmptySettingComponent(JSONNode _json)
		: base(_json)
	{
	}

	public EmptySettingComponent()
	{
	}

	public static EmptySettingComponent DeserializeEmptySettingComponent(JSONNode _json)
	{
		return new EmptySettingComponent(_json);
	}

	public override int GetTypeId()
	{
		return -750588817;
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
		return "{ }";
	}
}
