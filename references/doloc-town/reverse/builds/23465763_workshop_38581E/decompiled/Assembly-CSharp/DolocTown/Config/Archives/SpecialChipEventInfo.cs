using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Archives;

public sealed class SpecialChipEventInfo : BeanBase
{
	public const int __ID__ = -1574613728;

	public int Id { get; private set; }

	public List<ChipEvent> ChipEvents { get; private set; }

	public SpecialChipEventInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["chip_events"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		ChipEvents = new List<ChipEvent>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ChipEvent item = ChipEvent.DeserializeChipEvent(child);
			ChipEvents.Add(item);
		}
	}

	public SpecialChipEventInfo(int id, List<ChipEvent> chip_events)
	{
		Id = id;
		ChipEvents = chip_events;
	}

	public static SpecialChipEventInfo DeserializeSpecialChipEventInfo(JSONNode _json)
	{
		return new SpecialChipEventInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1574613728;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ChipEvent chipEvent in ChipEvents)
		{
			chipEvent?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ChipEvent chipEvent in ChipEvents)
		{
			chipEvent?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ChipEvents:" + StringUtil.CollectionToString(ChipEvents) + ",}";
	}
}
