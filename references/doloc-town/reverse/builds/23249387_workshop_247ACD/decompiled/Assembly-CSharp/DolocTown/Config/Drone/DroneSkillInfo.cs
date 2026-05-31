using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneSkillInfo : BeanBase
{
	public const int __ID__ = -105360323;

	public string Id { get; private set; }

	public DroneFunctionProto Function { get; private set; }

	public DroneSkillInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = DroneFunctionProto.DeserializeDroneFunctionProto(_json["function"]);
	}

	public DroneSkillInfo(string id, DroneFunctionProto function)
	{
		Id = id;
		Function = function;
	}

	public static DroneSkillInfo DeserializeDroneSkillInfo(JSONNode _json)
	{
		return new DroneSkillInfo(_json);
	}

	public override int GetTypeId()
	{
		return -105360323;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Function:" + Function?.ToString() + ",}";
	}
}
