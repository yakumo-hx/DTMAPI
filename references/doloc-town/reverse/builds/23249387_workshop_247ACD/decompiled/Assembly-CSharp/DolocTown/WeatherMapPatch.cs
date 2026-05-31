using System.Collections.Generic;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
public class WeatherMapPatch
{
	public struct Record
	{
		public int totalMonth;

		public int day;

		public int hour;

		public WeatherType type;
	}

	private readonly Dictionary<int, Dictionary<Vector2Int, WeatherType>> weatherMapPatch = new Dictionary<int, Dictionary<Vector2Int, WeatherType>>();

	[JsonProperty]
	private Record[] recordList
	{
		get
		{
			List<Record> list = new List<Record>();
			foreach (var (totalMonth, dictionary2) in weatherMapPatch)
			{
				foreach (var (vector2Int2, type) in dictionary2)
				{
					list.Add(new Record
					{
						totalMonth = totalMonth,
						day = vector2Int2.x,
						hour = vector2Int2.y,
						type = type
					});
				}
			}
			return list.ToArray();
		}
	}

	[JsonConstructor]
	public WeatherMapPatch(Record[] recordList = null)
	{
		if (!recordList.IsNullOrEmpty())
		{
			for (int i = 0; i < recordList.Length; i++)
			{
				Record record = recordList[i];
				AddPatch(record.totalMonth, record.day, record.hour, record.type);
			}
		}
	}

	public void AddPatch(int totalMonth, int day, int hour, WeatherType weatherType)
	{
		if (!weatherMapPatch.ContainsKey(totalMonth))
		{
			weatherMapPatch.Add(totalMonth, new Dictionary<Vector2Int, WeatherType>());
		}
		Vector2Int key = new Vector2Int(day, hour);
		weatherMapPatch[totalMonth][key] = weatherType;
	}

	public bool QueryPatch(int totalMonth, int day, int hour, out WeatherType weatherType)
	{
		weatherType = WeatherType.NONE;
		if (!weatherMapPatch.TryGetValue(totalMonth, out var value))
		{
			return false;
		}
		Vector2Int key = new Vector2Int(day, hour);
		return value.TryGetValue(key, out weatherType);
	}

	public void ApplyPatch(int totalMonth, Dictionary<Vector2Int, WeatherType> originMap)
	{
		if (!weatherMapPatch.TryGetValue(totalMonth, out var value))
		{
			return;
		}
		foreach (KeyValuePair<Vector2Int, WeatherType> item in value)
		{
			item.Deconstruct(out var key, out var value2);
			Vector2Int key2 = key;
			WeatherType value3 = value2;
			originMap[key2] = value3;
		}
	}
}
