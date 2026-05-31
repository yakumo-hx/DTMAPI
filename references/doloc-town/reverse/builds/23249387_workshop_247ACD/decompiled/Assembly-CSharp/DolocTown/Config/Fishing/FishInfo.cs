using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using DolocTown.Config.Item;
using DolocTown.Config.Time;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using SimpleJSON;

namespace DolocTown.Config.Fishing;

public sealed class FishInfo : BeanBase
{
	public const int __ID__ = -657336758;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public bool IsFish { get; private set; }

	public bool IsGarbage { get; private set; }

	public int Rarity { get; private set; }

	public int Size { get; private set; }

	public int ExpFishing { get; private set; }

	public int[] Month { get; private set; }

	public WeatherType[] WeatherTypes { get; private set; }

	public TimeRange TimeRange { get; private set; }

	public string[] FishBait { get; private set; }

	public ItemInfo[] FishBait_Ref { get; private set; }

	public int FishingRodLv { get; private set; }

	public int FishingLv { get; private set; }

	public RangeInt MaxStamina { get; private set; }

	public RangeInt StableDuration { get; private set; }

	public RangeInt StruggleDuration { get; private set; }

	public float NoteSpeedMultiplier { get; private set; }

	public float InitialStableProbability { get; private set; }

	public float CatchMultiplier { get; private set; }

	public int EscapeSpeed { get; private set; }

	public float StruggleMultiplier { get; private set; }

	public float BonusProbability { get; private set; }

	public RangeInt BonusDuration { get; private set; }

	public int BonusBaseScore { get; private set; }

	public int BonusExtraScore { get; private set; }

	public float BonusMultiplier { get; private set; }

	public FishInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["is_fish"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsFish = _json["is_fish"];
		if (!_json["is_garbage"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsGarbage = _json["is_garbage"];
		if (!_json["rarity"].IsNumber)
		{
			throw new SerializationException();
		}
		Rarity = _json["rarity"];
		if (!_json["size"].IsNumber)
		{
			throw new SerializationException();
		}
		Size = _json["size"];
		if (!_json["exp_fishing"].IsNumber)
		{
			throw new SerializationException();
		}
		ExpFishing = _json["exp_fishing"];
		JSONNode jSONNode = _json["month"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Month = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			Month[num++] = num2;
		}
		JSONNode jSONNode2 = _json["weather_types"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		WeatherTypes = new WeatherType[count2];
		int num3 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsNumber)
			{
				throw new SerializationException();
			}
			WeatherType asInt = (WeatherType)child2.AsInt;
			WeatherTypes[num3++] = asInt;
		}
		if (!_json["time_range"].IsObject)
		{
			throw new SerializationException();
		}
		TimeRange = TimeRange.DeserializeTimeRange(_json["time_range"]);
		JSONNode jSONNode3 = _json["fish_bait"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		FishBait = new string[count3];
		int num4 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsString)
			{
				throw new SerializationException();
			}
			string text = child3;
			FishBait[num4++] = text;
		}
		if (!_json["fishing_rod_lv"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingRodLv = _json["fishing_rod_lv"];
		if (!_json["fishing_lv"].IsNumber)
		{
			throw new SerializationException();
		}
		FishingLv = _json["fishing_lv"];
		if (!_json["max_stamina"].IsObject)
		{
			throw new SerializationException();
		}
		MaxStamina = RangeInt.DeserializeRangeInt(_json["max_stamina"]);
		if (!_json["stable_duration"].IsObject)
		{
			throw new SerializationException();
		}
		StableDuration = RangeInt.DeserializeRangeInt(_json["stable_duration"]);
		if (!_json["struggle_duration"].IsObject)
		{
			throw new SerializationException();
		}
		StruggleDuration = RangeInt.DeserializeRangeInt(_json["struggle_duration"]);
		if (!_json["note_speed_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		NoteSpeedMultiplier = _json["note_speed_multiplier"];
		if (!_json["initial_stable_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		InitialStableProbability = _json["initial_stable_probability"];
		if (!_json["catch_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		CatchMultiplier = _json["catch_multiplier"];
		if (!_json["escape_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		EscapeSpeed = _json["escape_speed"];
		if (!_json["struggle_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		StruggleMultiplier = _json["struggle_multiplier"];
		if (!_json["bonus_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		BonusProbability = _json["bonus_probability"];
		if (!_json["bonus_duration"].IsObject)
		{
			throw new SerializationException();
		}
		BonusDuration = RangeInt.DeserializeRangeInt(_json["bonus_duration"]);
		if (!_json["bonus_base_score"].IsNumber)
		{
			throw new SerializationException();
		}
		BonusBaseScore = _json["bonus_base_score"];
		if (!_json["bonus_extra_score"].IsNumber)
		{
			throw new SerializationException();
		}
		BonusExtraScore = _json["bonus_extra_score"];
		if (!_json["bonus_multiplier"].IsNumber)
		{
			throw new SerializationException();
		}
		BonusMultiplier = _json["bonus_multiplier"];
	}

	public FishInfo(string id, bool default_unlock, bool is_fish, bool is_garbage, int rarity, int size, int exp_fishing, int[] month, WeatherType[] weather_types, TimeRange time_range, string[] fish_bait, int fishing_rod_lv, int fishing_lv, RangeInt max_stamina, RangeInt stable_duration, RangeInt struggle_duration, float note_speed_multiplier, float initial_stable_probability, float catch_multiplier, int escape_speed, float struggle_multiplier, float bonus_probability, RangeInt bonus_duration, int bonus_base_score, int bonus_extra_score, float bonus_multiplier)
	{
		Id = id;
		DefaultUnlock = default_unlock;
		IsFish = is_fish;
		IsGarbage = is_garbage;
		Rarity = rarity;
		Size = size;
		ExpFishing = exp_fishing;
		Month = month;
		WeatherTypes = weather_types;
		TimeRange = time_range;
		FishBait = fish_bait;
		FishingRodLv = fishing_rod_lv;
		FishingLv = fishing_lv;
		MaxStamina = max_stamina;
		StableDuration = stable_duration;
		StruggleDuration = struggle_duration;
		NoteSpeedMultiplier = note_speed_multiplier;
		InitialStableProbability = initial_stable_probability;
		CatchMultiplier = catch_multiplier;
		EscapeSpeed = escape_speed;
		StruggleMultiplier = struggle_multiplier;
		BonusProbability = bonus_probability;
		BonusDuration = bonus_duration;
		BonusBaseScore = bonus_base_score;
		BonusExtraScore = bonus_extra_score;
		BonusMultiplier = bonus_multiplier;
	}

	public static FishInfo DeserializeFishInfo(JSONNode _json)
	{
		return new FishInfo(_json);
	}

	public override int GetTypeId()
	{
		return -657336758;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		TimeRange?.Resolve(_tables);
		int num = FishBait.Length;
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		FishBait_Ref = new ItemInfo[num];
		for (int i = 0; i < num; i++)
		{
			FishBait_Ref[i] = tbItem.GetOrDefault(FishBait[i]);
		}
		MaxStamina?.Resolve(_tables);
		StableDuration?.Resolve(_tables);
		StruggleDuration?.Resolve(_tables);
		BonusDuration?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		TimeRange?.TranslateText(translator);
		MaxStamina?.TranslateText(translator);
		StableDuration?.TranslateText(translator);
		StruggleDuration?.TranslateText(translator);
		BonusDuration?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",DefaultUnlock:" + DefaultUnlock + ",IsFish:" + IsFish + ",IsGarbage:" + IsGarbage + ",Rarity:" + Rarity + ",Size:" + Size + ",ExpFishing:" + ExpFishing + ",Month:" + StringUtil.CollectionToString(Month) + ",WeatherTypes:" + StringUtil.CollectionToString(WeatherTypes) + ",TimeRange:" + TimeRange?.ToString() + ",FishBait:" + StringUtil.CollectionToString(FishBait) + ",FishingRodLv:" + FishingRodLv + ",FishingLv:" + FishingLv + ",MaxStamina:" + MaxStamina?.ToString() + ",StableDuration:" + StableDuration?.ToString() + ",StruggleDuration:" + StruggleDuration?.ToString() + ",NoteSpeedMultiplier:" + NoteSpeedMultiplier + ",InitialStableProbability:" + InitialStableProbability + ",CatchMultiplier:" + CatchMultiplier + ",EscapeSpeed:" + EscapeSpeed + ",StruggleMultiplier:" + StruggleMultiplier + ",BonusProbability:" + BonusProbability + ",BonusDuration:" + BonusDuration?.ToString() + ",BonusBaseScore:" + BonusBaseScore + ",BonusExtraScore:" + BonusExtraScore + ",BonusMultiplier:" + BonusMultiplier + ",}";
	}

	public bool IsMatch(bool isGarbage, int lv, int fishingLv, WeatherType weather, DateInfo dateInfo)
	{
		if (_IsGarbage(isGarbage) && _CheckToolLevel(lv) && _CheckFishingLevel(fishingLv) && _CheckWeather(weather))
		{
			return _CheckDate(dateInfo);
		}
		return false;
	}

	public bool IsMatch(bool isGarbage, int rarity, WeatherType weather, DateInfo dateInfo)
	{
		if (_IsGarbage(isGarbage) && _CheckRarity(rarity) && _CheckWeather(weather))
		{
			return _CheckDate(dateInfo);
		}
		return false;
	}

	private bool _IsGarbage(bool isGarbage)
	{
		return isGarbage == IsGarbage;
	}

	private bool _CheckToolLevel(int lv)
	{
		return lv >= FishingRodLv;
	}

	private bool _CheckFishingLevel(int lv)
	{
		return lv >= FishingLv;
	}

	private bool _CheckRarity(int rarity)
	{
		return rarity >= Rarity;
	}

	private bool _CheckWeather(WeatherType weather)
	{
		if (!WeatherTypes.IsNullOrEmpty())
		{
			return WeatherTypes.Any((WeatherType x) => x == weather);
		}
		return true;
	}

	private bool _CheckDate(DateInfo dateInfo)
	{
		if (_CheckMonth(dateInfo.Month))
		{
			return _CheckHour(dateInfo.Hour);
		}
		return false;
	}

	private bool _CheckMonth(int month)
	{
		if (!Month.IsNullOrEmpty())
		{
			return Month.Any((int x) => x == month);
		}
		return true;
	}

	private bool _CheckHour(int hour)
	{
		return TimeRange.InRange(hour);
	}
}
