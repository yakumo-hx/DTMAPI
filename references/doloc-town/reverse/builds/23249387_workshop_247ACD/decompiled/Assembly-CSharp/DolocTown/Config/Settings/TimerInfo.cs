using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class TimerInfo : BeanBase
{
	public const int __ID__ = -149155448;

	public string Id { get; private set; }

	public TimerTickInfo[] TickInfo { get; private set; }

	public TimerInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["tick_info"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		TickInfo = new TimerTickInfo[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TimerTickInfo timerTickInfo = TimerTickInfo.DeserializeTimerTickInfo(child);
			TickInfo[num++] = timerTickInfo;
		}
	}

	public TimerInfo(string id, TimerTickInfo[] tick_info)
	{
		Id = id;
		TickInfo = tick_info;
	}

	public static TimerInfo DeserializeTimerInfo(JSONNode _json)
	{
		return new TimerInfo(_json);
	}

	public override int GetTypeId()
	{
		return -149155448;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TimerTickInfo[] tickInfo = TickInfo;
		for (int i = 0; i < tickInfo.Length; i++)
		{
			tickInfo[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		TimerTickInfo[] tickInfo = TickInfo;
		for (int i = 0; i < tickInfo.Length; i++)
		{
			tickInfo[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",TickInfo:" + StringUtil.CollectionToString(TickInfo) + ",}";
	}
}
