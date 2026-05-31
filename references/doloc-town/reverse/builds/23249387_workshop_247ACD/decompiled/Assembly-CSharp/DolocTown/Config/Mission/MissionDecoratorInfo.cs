using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class MissionDecoratorInfo : BeanBase
{
	public const int __ID__ = -598564673;

	public string Id { get; private set; }

	public MissionDecoratorInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
	}

	public MissionDecoratorInfo(string id)
	{
		Id = id;
	}

	public static MissionDecoratorInfo DeserializeMissionDecoratorInfo(JSONNode _json)
	{
		return new MissionDecoratorInfo(_json);
	}

	public override int GetTypeId()
	{
		return -598564673;
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
