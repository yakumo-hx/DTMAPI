using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Time;

public sealed class SeasonInfo : BeanBase
{
	public const int __ID__ = 236431346;

	private bool _isValidated;

	private bool _isTransitionMatrixValid;

	public int Index { get; private set; }

	public int Month { get; private set; }

	public bool IsRainy { get; private set; }

	public bool IsSoft { get; private set; }

	public int ResourceSpawnInterval { get; private set; }

	public int ResourceGrowInterval { get; private set; }

	public int VegetationSpawnInterval { get; private set; }

	public int VegetationGrowInterval { get; private set; }

	public int[][] TransitionMatrix { get; private set; }

	public string SeasonTitle { get; private set; }

	public string SeasonTitle_l10n_key { get; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string SubTitle { get; private set; }

	public string SubTitle_l10n_key { get; }

	public ItemSpawnRandom BirdDropSpawnEntry { get; private set; }

	public bool IsDry => !IsRainy;

	public bool IsHard => !IsSoft;

	private bool IsCurrentMatrixValid
	{
		get
		{
			if (_isValidated)
			{
				return _isTransitionMatrixValid;
			}
			int len = Enum.GetValues(typeof(WeatherType)).Length;
			_isValidated = true;
			_isTransitionMatrixValid = TransitionMatrix.Length == len && TransitionMatrix.Any((int[] v) => v.Length == len);
			return _isTransitionMatrixValid;
		}
	}

	public SeasonInfo(JSONNode _json)
	{
		if (!_json["index"].IsNumber)
		{
			throw new SerializationException();
		}
		Index = _json["index"];
		if (!_json["month"].IsNumber)
		{
			throw new SerializationException();
		}
		Month = _json["month"];
		if (!_json["is_rainy"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsRainy = _json["is_rainy"];
		if (!_json["is_soft"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsSoft = _json["is_soft"];
		if (!_json["resource_spawn_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		ResourceSpawnInterval = _json["resource_spawn_interval"];
		if (!_json["resource_grow_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		ResourceGrowInterval = _json["resource_grow_interval"];
		if (!_json["vegetation_spawn_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		VegetationSpawnInterval = _json["vegetation_spawn_interval"];
		if (!_json["vegetation_grow_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		VegetationGrowInterval = _json["vegetation_grow_interval"];
		JSONNode jSONNode = _json["transition_matrix"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		TransitionMatrix = new int[count][];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsArray)
			{
				throw new SerializationException();
			}
			int[] array = new int[child.Count];
			int num2 = 0;
			foreach (JSONNode child2 in child.Children)
			{
				if (!child2.IsNumber)
				{
					throw new SerializationException();
				}
				int num3 = child2;
				array[num2++] = num3;
			}
			TransitionMatrix[num++] = array;
		}
		if (!_json["season_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SeasonTitle_l10n_key = _json["season_title"]["key"];
		if (!_json["season_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SeasonTitle = _json["season_title"]["text"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["sub_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SubTitle_l10n_key = _json["sub_title"]["key"];
		if (!_json["sub_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SubTitle = _json["sub_title"]["text"];
		if (!_json["bird_drop_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		BirdDropSpawnEntry = ItemSpawnRandom.DeserializeItemSpawnRandom(_json["bird_drop_spawn_entry"]);
	}

	public SeasonInfo(int index, int month, bool is_rainy, bool is_soft, int resource_spawn_interval, int resource_grow_interval, int vegetation_spawn_interval, int vegetation_grow_interval, int[][] transition_matrix, string season_title, string title, string sub_title, ItemSpawnRandom bird_drop_spawn_entry)
	{
		Index = index;
		Month = month;
		IsRainy = is_rainy;
		IsSoft = is_soft;
		ResourceSpawnInterval = resource_spawn_interval;
		ResourceGrowInterval = resource_grow_interval;
		VegetationSpawnInterval = vegetation_spawn_interval;
		VegetationGrowInterval = vegetation_grow_interval;
		TransitionMatrix = transition_matrix;
		SeasonTitle = season_title;
		Title = title;
		SubTitle = sub_title;
		BirdDropSpawnEntry = bird_drop_spawn_entry;
	}

	public static SeasonInfo DeserializeSeasonInfo(JSONNode _json)
	{
		return new SeasonInfo(_json);
	}

	public override int GetTypeId()
	{
		return 236431346;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		BirdDropSpawnEntry?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SeasonTitle = translator(SeasonTitle_l10n_key, SeasonTitle);
		Title = translator(Title_l10n_key, Title);
		SubTitle = translator(SubTitle_l10n_key, SubTitle);
		BirdDropSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Index:" + Index + ",Month:" + Month + ",IsRainy:" + IsRainy + ",IsSoft:" + IsSoft + ",ResourceSpawnInterval:" + ResourceSpawnInterval + ",ResourceGrowInterval:" + ResourceGrowInterval + ",VegetationSpawnInterval:" + VegetationSpawnInterval + ",VegetationGrowInterval:" + VegetationGrowInterval + ",TransitionMatrix:" + StringUtil.CollectionToString(TransitionMatrix) + ",SeasonTitle:" + SeasonTitle + ",Title:" + Title + ",SubTitle:" + SubTitle + ",BirdDropSpawnEntry:" + BirdDropSpawnEntry?.ToString() + ",}";
	}

	private bool QueryFixedWeather(int month, int day, out WeatherType weatherType)
	{
		int activeSlotCount = DolocAPI.archiveHandle.farmData.envOptimizerSystem.GetActiveSlotCount();
		if (activeSlotCount > 0 && TbSeasonWeather.QueryWeatherOverride(month, day, activeSlotCount, out weatherType))
		{
			return true;
		}
		return TbSeasonWeather.QueryWeather(month, day, out weatherType);
	}

	public Dictionary<Vector2Int, WeatherType> GenWeatherMap(int month, int randomSeed)
	{
		System.Random random = new System.Random(randomSeed);
		Dictionary<Vector2Int, WeatherType> dictionary = new Dictionary<Vector2Int, WeatherType>();
		int num = 1;
		for (int i = 1; i <= 28; i++)
		{
			if (QueryFixedWeather(month, i, out var weatherType))
			{
				dictionary.Add(new Vector2Int(i, 6), weatherType);
				dictionary.Add(new Vector2Int(i, 18), weatherType);
				num = (int)weatherType;
				continue;
			}
			int value = random.Next(0, TransitionMatrix[num].Sum());
			int num2 = RandomUtils._RussianRoulette(TransitionMatrix[num], value);
			dictionary.Add(new Vector2Int(i, 6), (WeatherType)num2);
			num = num2;
			value = random.Next(0, TransitionMatrix[num].Sum());
			num2 = RandomUtils._RussianRoulette(TransitionMatrix[num], value);
			dictionary.Add(new Vector2Int(i, 18), (WeatherType)num2);
			num = num2;
		}
		return dictionary;
	}

	public bool TestWeatherMap(Dictionary<Vector2Int, WeatherType> L, Dictionary<Vector2Int, WeatherType> R)
	{
		if (L.Count == R.Count)
		{
			return L.All((KeyValuePair<Vector2Int, WeatherType> kv) => R.ContainsKey(kv.Key) && R[kv.Key] == kv.Value);
		}
		return false;
	}

	public WeatherType GenerateWeather(DateInfo date, WeatherType lastType)
	{
		if (!IsCurrentMatrixValid)
		{
			return WeatherType.SUNNY;
		}
		return (WeatherType)RandomUtils._RussianRoulette(TransitionMatrix[(int)lastType]);
	}
}
