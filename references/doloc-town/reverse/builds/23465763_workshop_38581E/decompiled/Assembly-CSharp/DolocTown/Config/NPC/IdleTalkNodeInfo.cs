using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Room;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using SimpleJSON;

namespace DolocTown.Config.NPC;

public sealed class IdleTalkNodeInfo : BeanBase
{
	public const int __ID__ = -668955587;

	public string Id { get; private set; }

	public string NpcName { get; private set; }

	public int Order { get; private set; }

	public List<WeatherType> Weather { get; private set; }

	public List<int> Month { get; private set; }

	public List<TimeRange> HourRange { get; private set; }

	public List<TimeRange> LikabilityRange { get; private set; }

	public List<string> EventDecorator { get; private set; }

	public List<string> MarkPoint { get; private set; }

	public List<MarkPointInfo> MarkPoint_Ref { get; private set; }

	public List<string> RoomId { get; private set; }

	public IdleTalkNodeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["npc_name"].IsString)
		{
			throw new SerializationException();
		}
		NpcName = _json["npc_name"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
		JSONNode jSONNode = _json["weather"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		Weather = new List<WeatherType>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			WeatherType asInt = (WeatherType)child.AsInt;
			Weather.Add(asInt);
		}
		JSONNode jSONNode2 = _json["month"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		Month = new List<int>(jSONNode2.Count);
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsNumber)
			{
				throw new SerializationException();
			}
			int item = child2;
			Month.Add(item);
		}
		JSONNode jSONNode3 = _json["hour_range"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		HourRange = new List<TimeRange>(jSONNode3.Count);
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			TimeRange item2 = TimeRange.DeserializeTimeRange(child3);
			HourRange.Add(item2);
		}
		JSONNode jSONNode4 = _json["likability_range"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		LikabilityRange = new List<TimeRange>(jSONNode4.Count);
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			TimeRange item3 = TimeRange.DeserializeTimeRange(child4);
			LikabilityRange.Add(item3);
		}
		JSONNode jSONNode5 = _json["event_decorator"];
		if (!jSONNode5.IsArray)
		{
			throw new SerializationException();
		}
		EventDecorator = new List<string>(jSONNode5.Count);
		foreach (JSONNode child5 in jSONNode5.Children)
		{
			if (!child5.IsString)
			{
				throw new SerializationException();
			}
			string item4 = child5;
			EventDecorator.Add(item4);
		}
		JSONNode jSONNode6 = _json["mark_point"];
		if (!jSONNode6.IsArray)
		{
			throw new SerializationException();
		}
		MarkPoint = new List<string>(jSONNode6.Count);
		foreach (JSONNode child6 in jSONNode6.Children)
		{
			if (!child6.IsString)
			{
				throw new SerializationException();
			}
			string item5 = child6;
			MarkPoint.Add(item5);
		}
		JSONNode jSONNode7 = _json["room_id"];
		if (!jSONNode7.IsArray)
		{
			throw new SerializationException();
		}
		RoomId = new List<string>(jSONNode7.Count);
		foreach (JSONNode child7 in jSONNode7.Children)
		{
			if (!child7.IsString)
			{
				throw new SerializationException();
			}
			string item6 = child7;
			RoomId.Add(item6);
		}
	}

	public IdleTalkNodeInfo(string id, string npc_name, int order, List<WeatherType> weather, List<int> month, List<TimeRange> hour_range, List<TimeRange> likability_range, List<string> event_decorator, List<string> mark_point, List<string> room_id)
	{
		Id = id;
		NpcName = npc_name;
		Order = order;
		Weather = weather;
		Month = month;
		HourRange = hour_range;
		LikabilityRange = likability_range;
		EventDecorator = event_decorator;
		MarkPoint = mark_point;
		RoomId = room_id;
	}

	public static IdleTalkNodeInfo DeserializeIdleTalkNodeInfo(JSONNode _json)
	{
		return new IdleTalkNodeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -668955587;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TimeRange item in HourRange)
		{
			item?.Resolve(_tables);
		}
		foreach (TimeRange item2 in LikabilityRange)
		{
			item2?.Resolve(_tables);
		}
		TbMarkPoint tbMarkPoint = (TbMarkPoint)_tables["Room.TbMarkPoint"];
		MarkPoint_Ref = new List<MarkPointInfo>();
		foreach (string item3 in MarkPoint)
		{
			MarkPoint_Ref.Add(tbMarkPoint.GetOrDefault(item3));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TimeRange item in HourRange)
		{
			item?.TranslateText(translator);
		}
		foreach (TimeRange item2 in LikabilityRange)
		{
			item2?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",NpcName:" + NpcName + ",Order:" + Order + ",Weather:" + StringUtil.CollectionToString(Weather) + ",Month:" + StringUtil.CollectionToString(Month) + ",HourRange:" + StringUtil.CollectionToString(HourRange) + ",LikabilityRange:" + StringUtil.CollectionToString(LikabilityRange) + ",EventDecorator:" + StringUtil.CollectionToString(EventDecorator) + ",MarkPoint:" + StringUtil.CollectionToString(MarkPoint) + ",RoomId:" + StringUtil.CollectionToString(RoomId) + ",}";
	}
}
