using System;
using System.Collections.Generic;
using DolocTown.Config;
using DolocTown.Config.Weather;
using UnityEngine;

namespace DolocTown;

public class WeatherMapGenerator
{
	private readonly System.Random random;

	private readonly List<int> weatherSeeds = new List<int>();

	private readonly Dictionary<int, Dictionary<Vector2Int, WeatherType>> weatherMapCache = new Dictionary<int, Dictionary<Vector2Int, WeatherType>>();

	private readonly ReferenceRecorder referenceRecorder = new ReferenceRecorder();

	private readonly int[] months;

	private int removeCounter;

	public WeatherMapGenerator(int randomSeed, int[] months = null)
	{
		random = new System.Random(randomSeed);
		this.months = months ?? new int[4] { 1, 2, 3, 4 };
		if (this.months.Length != 4)
		{
			Debug.LogWarning("WeatherMapGenerator 构造时传入的 months 长度不为 4，已重置为默认值");
			this.months = new int[4] { 1, 2, 3, 4 };
		}
	}

	private void _EnsureWeatherSeeds(int totalMonth)
	{
		if (weatherSeeds.Count < totalMonth)
		{
			for (int i = weatherSeeds.Count; i < totalMonth; i++)
			{
				weatherSeeds.Add(random.Next());
			}
		}
	}

	public int GetMonth(int month)
	{
		return months[month - 1];
	}

	private void _TryClearUselessCache(int capacity = 3, int interval = 10)
	{
		if (weatherMapCache.Count >= capacity && removeCounter++ >= interval)
		{
			removeCounter = 0;
			int num = referenceRecorder.SelectMinimumReference();
			if (num != -1)
			{
				weatherMapCache.Remove(num);
			}
		}
	}

	public void ClearCache()
	{
		weatherMapCache.Clear();
	}

	public Dictionary<Vector2Int, WeatherType> GetWeatherMapOfMonth(int totalMonth, int month)
	{
		_EnsureWeatherSeeds(totalMonth);
		referenceRecorder.AddReference(totalMonth);
		if (weatherMapCache.TryGetValue(totalMonth, out var value))
		{
			return value;
		}
		int randomSeed = weatherSeeds[totalMonth - 1];
		int month2 = GetMonth(month);
		Dictionary<Vector2Int, WeatherType> dictionary = DolocConfig.Tables.TbSeason.GetByMonth(month2).GenWeatherMap(month2, randomSeed);
		weatherMapCache.Add(totalMonth, dictionary);
		_TryClearUselessCache();
		return dictionary;
	}
}
