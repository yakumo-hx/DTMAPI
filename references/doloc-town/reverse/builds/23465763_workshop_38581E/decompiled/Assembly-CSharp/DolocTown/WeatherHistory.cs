using System;
using System.Collections.Generic;
using DolocTown.Config.Weather;
using Newtonsoft.Json;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class WeatherHistory
{
	[JsonProperty]
	private List<WeatherHistoryRecord> records = new List<WeatherHistoryRecord>();

	[JsonProperty]
	public int lastTime { get; private set; }

	public WeatherHistory()
	{
	}

	[JsonConstructor]
	private WeatherHistory(int lastTime, List<WeatherHistoryRecord> records)
	{
		this.lastTime = lastTime;
		this.records = records;
	}

	public void Record(int currentTime, WeatherType type)
	{
		DolocAPI.output($"正在记录天气:({currentTime},{type})");
		if (lastTime == currentTime)
		{
			DolocAPI.outputError("天气记录:当前时间和最后一次记录的时间相同,不记录");
		}
		else if (records.Count > 0 && records[^1].weatherType == (byte)type)
		{
			WeatherHistoryRecord weatherHistoryRecord = records[^1];
			int duration = currentTime - weatherHistoryRecord.startTime;
			records[^1] = new WeatherHistoryRecord((byte)type, weatherHistoryRecord.startTime, duration);
			lastTime = currentTime;
		}
		else
		{
			records.Add(new WeatherHistoryRecord((byte)type, lastTime, currentTime - lastTime));
			lastTime = currentTime;
		}
	}

	public List<WeatherHistoryRecord> _QueryHistory(int stopTime)
	{
		List<WeatherHistoryRecord> list = new List<WeatherHistoryRecord>();
		if (records.Count == 0)
		{
			DolocAPI.outputWarning("没有历史天气");
			return list;
		}
		if (stopTime >= lastTime)
		{
			DolocAPI.outputWarning("查询时间超过最后记录的天气变化时间");
			return list;
		}
		if (records.Count == 1)
		{
			WeatherHistoryRecord weatherHistoryRecord = records[0];
			int duration = lastTime - stopTime;
			list.Add(new WeatherHistoryRecord(weatherHistoryRecord.weatherType, stopTime, duration));
			return list;
		}
		for (int i = 0; i < records.Count; i++)
		{
			WeatherHistoryRecord weatherHistoryRecord2 = records[i];
			if (weatherHistoryRecord2.startTime == stopTime)
			{
				return records.GetRange(i, records.Count - i);
			}
			int num = weatherHistoryRecord2.startTime + weatherHistoryRecord2.duration;
			if (num > stopTime)
			{
				List<WeatherHistoryRecord> range = records.GetRange(i + 1, records.Count - i - 1);
				range.Insert(0, new WeatherHistoryRecord(weatherHistoryRecord2.weatherType, stopTime, num - stopTime));
				return range;
			}
		}
		return records.GetRange(0, records.Count);
	}

	public void ShowHistory(Action<string> output)
	{
		foreach (WeatherHistoryRecord record in records)
		{
			output(record.ToString());
		}
	}

	public void ClipHistory(int historyTiming)
	{
		if (records.Count == 0 || historyTiming >= lastTime)
		{
			return;
		}
		if (historyTiming <= records[0].startTime)
		{
			records.Clear();
			lastTime = historyTiming;
			return;
		}
		for (int num = records.Count - 1; num >= 0; num--)
		{
			WeatherHistoryRecord weatherHistoryRecord = records[num];
			if (weatherHistoryRecord.startTime < historyTiming)
			{
				records.RemoveRange(num, records.Count - num);
				records.Add(new WeatherHistoryRecord(weatherHistoryRecord.weatherType, weatherHistoryRecord.startTime, historyTiming - weatherHistoryRecord.startTime));
				lastTime = historyTiming;
				break;
			}
		}
	}
}
