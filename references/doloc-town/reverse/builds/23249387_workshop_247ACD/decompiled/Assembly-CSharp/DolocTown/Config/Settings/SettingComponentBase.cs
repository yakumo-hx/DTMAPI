using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public abstract class SettingComponentBase : BeanBase
{
	public SettingComponentBase(JSONNode _json)
	{
	}

	public SettingComponentBase()
	{
	}

	public static SettingComponentBase DeserializeSettingComponentBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EmptySettingComponent" => new EmptySettingComponent(_json), 
			"SliderSettingComponent" => new SliderSettingComponent(_json), 
			"ToggleSettingComponent" => new ToggleSettingComponent(_json), 
			"OptionSettingComponent" => new OptionSettingComponent(_json), 
			"RebindActionSettingComponent" => new RebindActionSettingComponent(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
