using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class CustomEventInfo : BeanBase
{
	public const int __ID__ = 1734650197;

	public string Id { get; private set; }

	public CustomEventInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
	}

	public CustomEventInfo(string id)
	{
		Id = id;
	}

	public static CustomEventInfo DeserializeCustomEventInfo(JSONNode _json)
	{
		return new CustomEventInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1734650197;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",}";
	}
}
