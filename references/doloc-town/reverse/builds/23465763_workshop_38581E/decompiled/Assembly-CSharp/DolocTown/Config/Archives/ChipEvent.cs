using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class ChipEvent : BeanBase
{
	public const int __ID__ = 1838197745;

	public ChipEventType EventType { get; private set; }

	public string TargetId { get; private set; }

	public ChipEvent(JSONNode _json)
	{
		if (!_json["event_type"].IsNumber)
		{
			throw new SerializationException();
		}
		EventType = (ChipEventType)_json["event_type"].AsInt;
		if (!_json["target_id"].IsString)
		{
			throw new SerializationException();
		}
		TargetId = _json["target_id"];
	}

	public ChipEvent(ChipEventType event_type, string target_id)
	{
		EventType = event_type;
		TargetId = target_id;
	}

	public static ChipEvent DeserializeChipEvent(JSONNode _json)
	{
		return new ChipEvent(_json);
	}

	public override int GetTypeId()
	{
		return 1838197745;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ EventType:" + EventType.ToString() + ",TargetId:" + TargetId + ",}";
	}
}
