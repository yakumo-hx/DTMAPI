using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class RoomTimeRange : BeanBase
{
	public const int __ID__ = 1580638210;

	public int StartTime { get; private set; }

	public int EndTime { get; private set; }

	public string OutRangeTip { get; private set; }

	public string OutRangeTip_l10n_key { get; }

	public RoomTimeRange(JSONNode _json)
	{
		if (!_json["start_time"].IsNumber)
		{
			throw new SerializationException();
		}
		StartTime = _json["start_time"];
		if (!_json["end_time"].IsNumber)
		{
			throw new SerializationException();
		}
		EndTime = _json["end_time"];
		if (!_json["out_range_tip"]["key"].IsString)
		{
			throw new SerializationException();
		}
		OutRangeTip_l10n_key = _json["out_range_tip"]["key"];
		if (!_json["out_range_tip"]["text"].IsString)
		{
			throw new SerializationException();
		}
		OutRangeTip = _json["out_range_tip"]["text"];
	}

	public RoomTimeRange(int start_time, int end_time, string out_range_tip)
	{
		StartTime = start_time;
		EndTime = end_time;
		OutRangeTip = out_range_tip;
	}

	public static RoomTimeRange DeserializeRoomTimeRange(JSONNode _json)
	{
		return new RoomTimeRange(_json);
	}

	public override int GetTypeId()
	{
		return 1580638210;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		OutRangeTip = translator(OutRangeTip_l10n_key, OutRangeTip);
	}

	public override string ToString()
	{
		return "{ StartTime:" + StartTime + ",EndTime:" + EndTime + ",OutRangeTip:" + OutRangeTip + ",}";
	}

	public bool InRange(int hour)
	{
		if (StartTime < EndTime)
		{
			if (StartTime <= hour)
			{
				return hour <= EndTime;
			}
			return false;
		}
		if (EndTime > hour)
		{
			return hour <= StartTime;
		}
		return true;
	}
}
