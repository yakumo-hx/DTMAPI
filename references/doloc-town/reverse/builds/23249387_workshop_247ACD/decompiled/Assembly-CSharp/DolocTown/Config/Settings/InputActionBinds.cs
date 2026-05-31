using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class InputActionBinds : BeanBase
{
	public const int __ID__ = 397806465;

	public string ActionMapName { get; private set; }

	public string ActionName { get; private set; }

	public string ComposePartName { get; private set; }

	public bool IsEmpty => string.IsNullOrEmpty(ActionMapName);

	public string Path => ActionMapName + "/" + ActionName;

	public InputActionBinds(JSONNode _json)
	{
		if (!_json["action_map_name"].IsString)
		{
			throw new SerializationException();
		}
		ActionMapName = _json["action_map_name"];
		if (!_json["action_name"].IsString)
		{
			throw new SerializationException();
		}
		ActionName = _json["action_name"];
		if (!_json["compose_part_name"].IsString)
		{
			throw new SerializationException();
		}
		ComposePartName = _json["compose_part_name"];
	}

	public InputActionBinds(string action_map_name, string action_name, string compose_part_name)
	{
		ActionMapName = action_map_name;
		ActionName = action_name;
		ComposePartName = compose_part_name;
	}

	public static InputActionBinds DeserializeInputActionBinds(JSONNode _json)
	{
		return new InputActionBinds(_json);
	}

	public override int GetTypeId()
	{
		return 397806465;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ ActionMapName:" + ActionMapName + ",ActionName:" + ActionName + ",ComposePartName:" + ComposePartName + ",}";
	}
}
