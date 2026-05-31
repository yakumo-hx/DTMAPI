using System;
using System.Collections.Generic;
using System.Reflection;
using DolocTown.Config;
using DolocTown.Config.Weather;
using DolocTown.Weathers;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown;

[JsonObject(MemberSerialization.OptIn)]
[DebugObject]
public class WeatherSystem
{
	[DebugInfo]
	[JsonProperty]
	private readonly WeatherHistory history;

	private Dictionary<WeatherType, WeatherController> weatherControllers = new Dictionary<WeatherType, WeatherController>();

	private WeatherController currentWeatherController;

	[DebugInfo]
	[JsonProperty]
	public WeatherType WeatherType => WeatherInfo.Id;

	public WeatherInfo WeatherInfo { get; private set; }

	public bool IsInMalignantWeather => WeatherInfo.IsMalignantWeather;

	public WeatherSystem(WeatherType WeatherType = WeatherType.SUNNY)
	{
		history = new WeatherHistory();
		WeatherInfo = DolocConfig.Tables.TbWeather.GetWeatherInfo(WeatherType);
		InitWeatherControllers();
	}

	[JsonConstructor]
	public WeatherSystem(WeatherType WeatherType, WeatherHistory history, int weatherRegulatorCd)
	{
		this.history = history;
		WeatherInfo = DolocConfig.Tables.TbWeather.GetWeatherInfo(WeatherType);
		InitWeatherControllers();
	}

	private void InitWeatherControllers()
	{
		Type[] subTypes = typeof(WeatherController).GetSubTypes();
		foreach (Type type in subTypes)
		{
			WeatherControllerAttribute customAttribute = type.GetCustomAttribute<WeatherControllerAttribute>();
			if (customAttribute != null)
			{
				if (weatherControllers.ContainsKey(customAttribute.type))
				{
					Debug.LogWarning($"天气控制效果\"{customAttribute.type}\"重定义");
					continue;
				}
				WeatherController value = (WeatherController)Activator.CreateInstance(type);
				weatherControllers.Add(customAttribute.type, value);
			}
		}
		if (weatherControllers.TryGetValue(WeatherType, out currentWeatherController))
		{
			currentWeatherController.OnStart();
		}
	}

	public bool SetCurrentWeather(WeatherType type, int totalSecs, out bool hasWeatherPropertyChanged)
	{
		hasWeatherPropertyChanged = false;
		if (WeatherType == type)
		{
			return false;
		}
		currentWeatherController?.OnStop();
		if (weatherControllers.TryGetValue(type, out currentWeatherController))
		{
			currentWeatherController.OnStart();
		}
		history.Record(totalSecs, WeatherType);
		WeatherInfo weatherInfo = DolocConfig.Tables.TbWeather.GetWeatherInfo(type);
		hasWeatherPropertyChanged = WeatherInfo.IsMalignantWeather != weatherInfo.IsMalignantWeather;
		WeatherInfo = weatherInfo;
		return true;
	}

	public void UpdatePerTU(bool shouldRender = false)
	{
		currentWeatherController?.UpdatePerTU(shouldRender);
	}

	public WeatherHistoryRecord[] QueryHistory(int startTime, int now)
	{
		DolocAPI.output($"查询从{startTime}到{now}的历史天气");
		if (startTime >= now)
		{
			return Array.Empty<WeatherHistoryRecord>();
		}
		List<WeatherHistoryRecord> list = history._QueryHistory(startTime);
		if (list.Count == 0)
		{
			WeatherHistoryRecord weatherHistoryRecord = new WeatherHistoryRecord((byte)WeatherType, startTime, now - startTime);
			return new WeatherHistoryRecord[1] { weatherHistoryRecord };
		}
		WeatherHistoryRecord item = new WeatherHistoryRecord((byte)WeatherType, history.lastTime, now - history.lastTime);
		list.Add(item);
		return list.ToArray();
	}

	public void ClipHistory(int historyTiming)
	{
		history.ClipHistory(historyTiming);
	}

	public void ShowHistory(Action<string> output)
	{
		history.ShowHistory(output);
	}
}
