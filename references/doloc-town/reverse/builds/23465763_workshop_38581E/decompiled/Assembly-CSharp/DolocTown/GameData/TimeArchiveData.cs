using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using Newtonsoft.Json;
using RedSaw;
using RedSaw.CommandLineInterface;
using UnityEngine;

namespace DolocTown.GameData;

[JsonObject(MemberSerialization.OptIn)]
public class TimeArchiveData : IDataPersistence
{
	[DebugInfo("总秒数")]
	[JsonProperty]
	public int totalSeconds;

	[DebugInfo("日期信息")]
	[JsonProperty]
	public DateInfo dateNow;

	[DebugInfo("天气系统")]
	[JsonProperty]
	public readonly WeatherSystem weather;

	[JsonProperty]
	public readonly Counter TUCounter;

	[JsonProperty]
	private bool isFirstTimeWakeUpOnDayPassed;

	[JsonProperty]
	private int randomSeed;

	[JsonProperty]
	public readonly WeatherMapPatch weatherPatch;

	[JsonProperty]
	private int weatherRegulatorCd;

	private WeatherMapGenerator _weatherMapGenerator;

	public readonly DateConfig dateConfig;

	private int WeatherRegulatorTotalCdLength = DolocAPI.GlobalParameter.WeatherRegulatorCdDurationTu;

	private Dictionary<Vector2Int, WeatherType> LastWeatherMap => _weatherMapGenerator.GetWeatherMapOfMonth(dateNow.TotalMonth - 1, dateNow.LastMonth);

	public Dictionary<Vector2Int, WeatherType> WeatherMap => _weatherMapGenerator.GetWeatherMapOfMonth(dateNow.TotalMonth, dateNow.Month);

	private Dictionary<Vector2Int, WeatherType> NextWeatherMap => _weatherMapGenerator.GetWeatherMapOfMonth(dateNow.TotalMonth + 1, dateNow.NextMonth);

	public SeasonInfo SeasonProto { get; private set; }

	public int SeasonIndex => SeasonProto.Index;

	public float DayProcess => (float)dateNow.Hour / (float)dateConfig.Day2Hour;

	public int TotalDays => dateNow.TotalDays;

	public bool IsWeatherRegulatorCooling => weatherRegulatorCd > 0;

	public int WeatherRegulatorCd => weatherRegulatorCd;

	public float WeatherRegulatorCdProgress => 1f - (float)weatherRegulatorCd / (float)WeatherRegulatorTotalCdLength;

	public TimeArchiveData()
	{
		dateConfig = DolocAPI.GlobalParameter.DateConfig;
		totalSeconds = 0;
		TUCounter = new Counter(dateConfig.TULength);
		dateNow = new DateInfo(0, 1, 0, 0, 1, 1, 1, WeekDay.MONDAY);
		weather = new WeatherSystem();
		randomSeed = UnityEngine.Random.Range(0, int.MaxValue);
		_weatherMapGenerator = new WeatherMapGenerator(randomSeed);
		weatherPatch = new WeatherMapPatch();
		weatherRegulatorCd = 0;
		RetrieveSeasonProto(dateNow.Month);
	}

	[JsonConstructor]
	public TimeArchiveData(int totalSeconds, DateInfo dateNow, WeatherSystem weather, Counter TUCounter, bool isFirstTimeWakeUpOnDayPassed, int randomSeed = 0, WeatherMapPatch weatherPatch = null, int weatherRegulatorCd = 0)
	{
		dateConfig = DolocAPI.GlobalParameter.DateConfig;
		this.totalSeconds = totalSeconds;
		this.dateNow = dateNow;
		this.weather = weather;
		this.TUCounter = TUCounter;
		this.isFirstTimeWakeUpOnDayPassed = isFirstTimeWakeUpOnDayPassed;
		this.randomSeed = ValidateRandomSeed(randomSeed);
		_weatherMapGenerator = new WeatherMapGenerator(randomSeed);
		this.weatherPatch = weatherPatch ?? new WeatherMapPatch();
		this.weatherRegulatorCd = weatherRegulatorCd;
		this.weatherRegulatorCd = Mathf.Clamp(weatherRegulatorCd, 0, DolocAPI.GlobalParameter.WeatherRegulatorCdDurationTu);
		RetrieveSeasonProto(dateNow.Month);
	}

	private int ValidateRandomSeed(int randomSeed)
	{
		if (randomSeed != 0)
		{
			return randomSeed;
		}
		return UnityEngine.Random.Range(0, int.MaxValue);
	}

	public void CoolingWeatherRegulator()
	{
		weatherRegulatorCd = WeatherRegulatorTotalCdLength;
	}

	public void ClearWeatherGeneratorMapCache()
	{
		_weatherMapGenerator.ClearCache();
	}

	public WeatherInfo[] GetWeatherInfoOfDay(int dayOffset)
	{
		int totalMonth = dateNow.TotalMonth;
		int num = dayOffset + dateNow.Day;
		if (num > dateConfig.Month2Day)
		{
			num -= dateConfig.Month2Day;
			return _ToWeatherInfos(ReadWeatherOfDay(totalMonth, WeatherMap, NextWeatherMap, num));
		}
		if (num == 1)
		{
			return _ToWeatherInfos(ReadWeatherOfDay(totalMonth - 1, LastWeatherMap, WeatherMap, num));
		}
		return _ToWeatherInfos(ReadWeatherOfDay(totalMonth, WeatherMap, WeatherMap, num));
	}

	private WeatherType[] ReadWeatherOfDay(int lastMonth, Dictionary<Vector2Int, WeatherType> lastMonthMap, Dictionary<Vector2Int, WeatherType> nextMonthMap, int today)
	{
		weatherPatch.ApplyPatch(lastMonth, lastMonthMap);
		weatherPatch.ApplyPatch(lastMonth + 1, lastMonthMap);
		int num = today - 1;
		if (num == 0)
		{
			List<WeatherType> obj = new List<WeatherType> { lastMonthMap[new Vector2Int(dateConfig.Month2Day, 18)] };
			WeatherType item = nextMonthMap[new Vector2Int(today, 6)];
			WeatherType item2 = nextMonthMap[new Vector2Int(today, 18)];
			obj.Add(item2);
			obj.Add(item);
			return obj.ToArray();
		}
		List<WeatherType> obj2 = new List<WeatherType> { nextMonthMap[new Vector2Int(num, 18)] };
		WeatherType item3 = nextMonthMap[new Vector2Int(today, 6)];
		WeatherType item4 = nextMonthMap[new Vector2Int(today, 18)];
		obj2.Add(item3);
		obj2.Add(item4);
		return obj2.ToArray();
	}

	private static WeatherInfo[] _ToWeatherInfos(WeatherType[] types)
	{
		WeatherInfo[] array = new WeatherInfo[types.Length];
		for (int i = 0; i < types.Length; i++)
		{
			array[i] = DolocConfig.Tables.TbWeather.GetWeatherInfo(types[i]);
		}
		return array;
	}

	public WeatherHistoryRecord[] QueryWeatherHistory(Room room, int maxSeconds = -1)
	{
		int num = room.StopTime;
		if (maxSeconds > 0 && totalSeconds - num > maxSeconds)
		{
			num = totalSeconds - maxSeconds;
		}
		if (num < totalSeconds)
		{
			return weather.QueryHistory(num, totalSeconds);
		}
		return Array.Empty<WeatherHistoryRecord>();
	}

	private void BeforeNewGame()
	{
	}

	public void AfterNewGame(ref ArchiveDataHandle data)
	{
	}

	public void BeforeLoadData(ref ArchiveDataHandle data)
	{
	}

	public void AfterLoadData(ref ArchiveDataHandle data)
	{
		DolocAPI.archiveHandle.RenderWeatherAndDayNight();
	}

	public void BeforeSaveData(ref ArchiveDataHandle data)
	{
	}

	public void AfterSaveData(ref ArchiveDataHandle data)
	{
	}

	public void InitTotalSeconds(int totalSeconds)
	{
		totalSeconds = Mathf.Max(0, totalSeconds);
		this.totalSeconds = totalSeconds;
		int num = totalSeconds / dateConfig.TULength;
		dateNow = new DateInfo(0, 1, 0, 0, 1, 1, 1, WeekDay.MONDAY);
		for (int i = 0; i < num; i++)
		{
			UpdateDate(out var _, out var _, out var _, out var _);
		}
	}

	public void CoolingDownWeatherRegulatorPerTU()
	{
		if (weatherRegulatorCd > 0)
		{
			weatherRegulatorCd--;
		}
	}

	public void UpdateDate(out bool hasHourChanged, out bool hasDayChanged, out bool hasMonthChanged, out bool hasYearChanged)
	{
		hasHourChanged = false;
		hasDayChanged = false;
		hasMonthChanged = false;
		hasYearChanged = false;
		dateNow.TotalTUs++;
		dateNow.Minute += dateConfig.TU2Min;
		if (dateNow.Minute < dateConfig.Hour2Min)
		{
			return;
		}
		dateNow.Minute %= dateConfig.Hour2Min;
		dateNow.Hour++;
		hasHourChanged = true;
		if (dateNow.Hour < dateConfig.Day2Hour)
		{
			return;
		}
		dateNow.Hour = 0;
		dateNow.TotalDays++;
		dateNow.Day++;
		dateNow.WeekDay = DateInfo.NextWeekDay(dateNow.WeekDay);
		hasDayChanged = true;
		if (dateNow.Day > dateConfig.Month2Day)
		{
			dateNow.Day = 1;
			dateNow.Month++;
			hasMonthChanged = true;
			if (dateNow.Month <= dateConfig.Year2Month)
			{
				RetrieveSeasonProto(dateNow.Month);
				return;
			}
			dateNow.Month = 1;
			RetrieveSeasonProto(dateNow.Month);
			dateNow.Year++;
			hasYearChanged = true;
		}
	}

	public void RetrieveSeasonProto(int month)
	{
		SeasonInfo byMonth = DolocConfig.Tables.TbSeason.GetByMonth(month);
		if (byMonth == null)
		{
			Debug.LogError($"{month}月的季节原型未定义");
		}
		else
		{
			SeasonProto = byMonth;
		}
	}

	public void TraceBackTime(out bool hasHourChanged, out bool hasDayChanged, out bool hasMonthChanged, out bool hasYearChanged)
	{
		hasHourChanged = false;
		hasDayChanged = false;
		hasMonthChanged = false;
		hasYearChanged = false;
		if (dateNow.TotalTUs == 0)
		{
			return;
		}
		dateNow.TotalTUs--;
		dateNow.Minute -= dateConfig.TU2Min;
		if (dateNow.Minute >= 0)
		{
			return;
		}
		dateNow.Minute += dateConfig.Hour2Min;
		dateNow.Hour--;
		hasHourChanged = true;
		if (dateNow.Hour >= 0)
		{
			return;
		}
		dateNow.Hour = dateConfig.Day2Hour - 1;
		dateNow.TotalDays--;
		dateNow.Day--;
		dateNow.WeekDay = TimeUtils.TraceBackWeekday(dateNow.WeekDay, 1);
		hasDayChanged = true;
		if (dateNow.Day <= 0)
		{
			dateNow.Day = dateConfig.Month2Day;
			dateNow.Month--;
			hasMonthChanged = true;
			if (dateNow.Month > 0)
			{
				RetrieveSeasonProto(dateNow.Month);
				return;
			}
			dateNow.Month = dateConfig.Year2Month;
			RetrieveSeasonProto(dateNow.Month);
			dateNow.Year--;
			hasYearChanged = true;
		}
	}

	public void SetFirstTimeWakeUpOnDayPassed()
	{
		isFirstTimeWakeUpOnDayPassed = true;
	}

	public bool IsFirstTimeWakeUpOnDayPassed()
	{
		if (!isFirstTimeWakeUpOnDayPassed)
		{
			return false;
		}
		isFirstTimeWakeUpOnDayPassed = false;
		return true;
	}
}
