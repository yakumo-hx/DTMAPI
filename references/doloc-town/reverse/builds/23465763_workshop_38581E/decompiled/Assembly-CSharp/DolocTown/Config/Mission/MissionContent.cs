using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class MissionContent : BeanBase
{
	public const int __ID__ = -1824261585;

	public string EventType { get; private set; }

	public string Args { get; private set; }

	public int Count { get; private set; }

	public MissionContent(JSONNode _json)
	{
		if (!_json["event_type"].IsString)
		{
			throw new SerializationException();
		}
		EventType = _json["event_type"];
		if (!_json["args"].IsString)
		{
			throw new SerializationException();
		}
		Args = _json["args"];
		if (!_json["count"].IsNumber)
		{
			throw new SerializationException();
		}
		Count = _json["count"];
	}

	public MissionContent(string event_type, string args, int count)
	{
		EventType = event_type;
		Args = args;
		Count = count;
	}

	public static MissionContent DeserializeMissionContent(JSONNode _json)
	{
		return new MissionContent(_json);
	}

	public override int GetTypeId()
	{
		return -1824261585;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ EventType:" + EventType + ",Args:" + Args + ",Count:" + Count + ",}";
	}
}
