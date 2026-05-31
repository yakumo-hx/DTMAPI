using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class AnimalInfo : BeanBase
{
	public const int __ID__ = 1458987004;

	private Vector2 emotionOffsetChild;

	private Vector2 emotionOffsetAdult;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string DefaulInputName { get; private set; }

	public string DefaulInputName_l10n_key { get; }

	public string ScheduleId { get; private set; }

	public AnimalLevelData[] Levels { get; private set; }

	public int Size { get; private set; }

	public int Space { get; private set; }

	public float MoveSpeed { get; private set; }

	public float RunSpeed { get; private set; }

	public int RunThreshold { get; private set; }

	public int DefaultEnergy { get; private set; }

	public int EatInterval { get; private set; }

	public int EatCount { get; private set; }

	public int GrowInterval { get; private set; }

	public float GrowCost { get; private set; }

	public float GrowIncrease { get; private set; }

	public int FertilityInterval { get; private set; }

	public float FertilityCost { get; private set; }

	public float FertilityIncrease { get; private set; }

	public int BreedDuration { get; private set; }

	public float BreedEnergyCost { get; private set; }

	public int MetabolismInterval { get; private set; }

	public float MetabolismCost { get; private set; }

	public float MetabolismIncrease { get; private set; }

	public ItemSpawnEntry ProduceSpawnEntry { get; private set; }

	public int ProduceRequireMood { get; private set; }

	public int ExcreteInterval { get; private set; }

	public float ExcreteCost { get; private set; }

	public int HungryThreshold { get; private set; }

	public int MoodDecreaseThreshold { get; private set; }

	public int MaxNatureMood { get; private set; }

	public int MoodIncreaseFondle { get; private set; }

	public int MoodDecreaseThunder { get; private set; }

	public float EscapeProbabilityBase { get; private set; }

	public float EscapeProbabilityIncrease { get; private set; }

	public int MoodAffectedDistance { get; private set; }

	public int MoodRangeHorizontal { get; private set; }

	public int MoodRangeVerticalBottom { get; private set; }

	public int MoodRangeVerticalTop { get; private set; }

	public int ProduceTechPoint { get; private set; }

	public int BreedTechPoint { get; private set; }

	public bool ManualMetabolism { get; private set; }

	public float JumpHeight { get; private set; }

	public Vector2Int[] MoodAffectPositions { get; private set; }

	public Vector2Int[] OccupiedPositions { get; private set; }

	public Vector2 ColliderSizeAdult { get; private set; }

	public Vector2 ColliderSizeChild { get; private set; }

	private AnimalLevelData ChildData => Levels[0];

	private AnimalLevelData AdultData => Levels[Mathf.Min(Levels.Length - 1, 1)];

	public AnimatorAsset ChildAnimator => ChildData.Animator;

	public AnimatorAsset Animator => AdultData.Animator;

	public int AdultPrice => AdultData.Price;

	public int ChildPrice => ChildData.Price;

	public string ChildDescriptionInSack_l10n_key => ChildData.DescriptionInSack_l10n_key;

	public string ChildDescriptionInSack => ChildData.DescriptionInSack;

	public string AdultDescriptionInSack_l10n_key => AdultData.DescriptionInSack_l10n_key;

	public string AdultDescriptionInSack => AdultData.DescriptionInSack;

	public string SoundEvent => AdultData.SoundEvent;

	public string SoundEventChild => ChildData.SoundEvent;

	public SpriteAsset UiChildSprite => ChildData.Icon;

	public SpriteAsset UiSprite => AdultData.Icon;

	public AnimalInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
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
		if (!_json["defaul_input_name"]["key"].IsString)
		{
			throw new SerializationException();
		}
		DefaulInputName_l10n_key = _json["defaul_input_name"]["key"];
		if (!_json["defaul_input_name"]["text"].IsString)
		{
			throw new SerializationException();
		}
		DefaulInputName = _json["defaul_input_name"]["text"];
		if (!_json["schedule_id"].IsString)
		{
			throw new SerializationException();
		}
		ScheduleId = _json["schedule_id"];
		JSONNode jSONNode = _json["levels"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Levels = new AnimalLevelData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			AnimalLevelData animalLevelData = AnimalLevelData.DeserializeAnimalLevelData(child);
			Levels[num++] = animalLevelData;
		}
		if (!_json["size"].IsNumber)
		{
			throw new SerializationException();
		}
		Size = _json["size"];
		if (!_json["space"].IsNumber)
		{
			throw new SerializationException();
		}
		Space = _json["space"];
		if (!_json["move_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		MoveSpeed = _json["move_speed"];
		if (!_json["run_speed"].IsNumber)
		{
			throw new SerializationException();
		}
		RunSpeed = _json["run_speed"];
		if (!_json["run_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		RunThreshold = _json["run_threshold"];
		if (!_json["default_energy"].IsNumber)
		{
			throw new SerializationException();
		}
		DefaultEnergy = _json["default_energy"];
		if (!_json["eat_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		EatInterval = _json["eat_interval"];
		if (!_json["eat_count"].IsNumber)
		{
			throw new SerializationException();
		}
		EatCount = _json["eat_count"];
		if (!_json["grow_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowInterval = _json["grow_interval"];
		if (!_json["grow_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowCost = _json["grow_cost"];
		if (!_json["grow_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowIncrease = _json["grow_increase"];
		if (!_json["fertility_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		FertilityInterval = _json["fertility_interval"];
		if (!_json["fertility_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		FertilityCost = _json["fertility_cost"];
		if (!_json["fertility_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		FertilityIncrease = _json["fertility_increase"];
		if (!_json["breed_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		BreedDuration = _json["breed_duration"];
		if (!_json["breed_energy_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		BreedEnergyCost = _json["breed_energy_cost"];
		if (!_json["metabolism_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		MetabolismInterval = _json["metabolism_interval"];
		if (!_json["metabolism_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		MetabolismCost = _json["metabolism_cost"];
		if (!_json["metabolism_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		MetabolismIncrease = _json["metabolism_increase"];
		if (!_json["produce_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		ProduceSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["produce_spawn_entry"]);
		if (!_json["produce_require_mood"].IsNumber)
		{
			throw new SerializationException();
		}
		ProduceRequireMood = _json["produce_require_mood"];
		if (!_json["excrete_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		ExcreteInterval = _json["excrete_interval"];
		if (!_json["excrete_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		ExcreteCost = _json["excrete_cost"];
		if (!_json["hungry_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		HungryThreshold = _json["hungry_threshold"];
		if (!_json["mood_decrease_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodDecreaseThreshold = _json["mood_decrease_threshold"];
		if (!_json["max_nature_mood"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxNatureMood = _json["max_nature_mood"];
		if (!_json["mood_increase_fondle"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodIncreaseFondle = _json["mood_increase_fondle"];
		if (!_json["mood_decrease_thunder"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodDecreaseThunder = _json["mood_decrease_thunder"];
		if (!_json["escape_probability_base"].IsNumber)
		{
			throw new SerializationException();
		}
		EscapeProbabilityBase = _json["escape_probability_base"];
		if (!_json["escape_probability_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		EscapeProbabilityIncrease = _json["escape_probability_increase"];
		if (!_json["mood_affected_distance"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodAffectedDistance = _json["mood_affected_distance"];
		if (!_json["mood_range_horizontal"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodRangeHorizontal = _json["mood_range_horizontal"];
		if (!_json["mood_range_vertical_bottom"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodRangeVerticalBottom = _json["mood_range_vertical_bottom"];
		if (!_json["mood_range_vertical_top"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodRangeVerticalTop = _json["mood_range_vertical_top"];
		if (!_json["produce_tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		ProduceTechPoint = _json["produce_tech_point"];
		if (!_json["breed_tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		BreedTechPoint = _json["breed_tech_point"];
		if (!_json["manual_metabolism"].IsBoolean)
		{
			throw new SerializationException();
		}
		ManualMetabolism = _json["manual_metabolism"];
		if (!_json["jump_height"].IsNumber)
		{
			throw new SerializationException();
		}
		JumpHeight = _json["jump_height"];
	}

	public AnimalInfo(string id, string title, string defaul_input_name, string schedule_id, AnimalLevelData[] levels, int size, int space, float move_speed, float run_speed, int run_threshold, int default_energy, int eat_interval, int eat_count, int grow_interval, float grow_cost, float grow_increase, int fertility_interval, float fertility_cost, float fertility_increase, int breed_duration, float breed_energy_cost, int metabolism_interval, float metabolism_cost, float metabolism_increase, ItemSpawnEntry produce_spawn_entry, int produce_require_mood, int excrete_interval, float excrete_cost, int hungry_threshold, int mood_decrease_threshold, int max_nature_mood, int mood_increase_fondle, int mood_decrease_thunder, float escape_probability_base, float escape_probability_increase, int mood_affected_distance, int mood_range_horizontal, int mood_range_vertical_bottom, int mood_range_vertical_top, int produce_tech_point, int breed_tech_point, bool manual_metabolism, float jump_height)
	{
		Id = id;
		Title = title;
		DefaulInputName = defaul_input_name;
		ScheduleId = schedule_id;
		Levels = levels;
		Size = size;
		Space = space;
		MoveSpeed = move_speed;
		RunSpeed = run_speed;
		RunThreshold = run_threshold;
		DefaultEnergy = default_energy;
		EatInterval = eat_interval;
		EatCount = eat_count;
		GrowInterval = grow_interval;
		GrowCost = grow_cost;
		GrowIncrease = grow_increase;
		FertilityInterval = fertility_interval;
		FertilityCost = fertility_cost;
		FertilityIncrease = fertility_increase;
		BreedDuration = breed_duration;
		BreedEnergyCost = breed_energy_cost;
		MetabolismInterval = metabolism_interval;
		MetabolismCost = metabolism_cost;
		MetabolismIncrease = metabolism_increase;
		ProduceSpawnEntry = produce_spawn_entry;
		ProduceRequireMood = produce_require_mood;
		ExcreteInterval = excrete_interval;
		ExcreteCost = excrete_cost;
		HungryThreshold = hungry_threshold;
		MoodDecreaseThreshold = mood_decrease_threshold;
		MaxNatureMood = max_nature_mood;
		MoodIncreaseFondle = mood_increase_fondle;
		MoodDecreaseThunder = mood_decrease_thunder;
		EscapeProbabilityBase = escape_probability_base;
		EscapeProbabilityIncrease = escape_probability_increase;
		MoodAffectedDistance = mood_affected_distance;
		MoodRangeHorizontal = mood_range_horizontal;
		MoodRangeVerticalBottom = mood_range_vertical_bottom;
		MoodRangeVerticalTop = mood_range_vertical_top;
		ProduceTechPoint = produce_tech_point;
		BreedTechPoint = breed_tech_point;
		ManualMetabolism = manual_metabolism;
		JumpHeight = jump_height;
	}

	public static AnimalInfo DeserializeAnimalInfo(JSONNode _json)
	{
		return new AnimalInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1458987004;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		AnimalLevelData[] levels = Levels;
		for (int i = 0; i < levels.Length; i++)
		{
			levels[i]?.Resolve(_tables);
		}
		ProduceSpawnEntry?.Resolve(_tables);
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		DefaulInputName = translator(DefaulInputName_l10n_key, DefaulInputName);
		AnimalLevelData[] levels = Levels;
		for (int i = 0; i < levels.Length; i++)
		{
			levels[i]?.TranslateText(translator);
		}
		ProduceSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",DefaulInputName:" + DefaulInputName + ",ScheduleId:" + ScheduleId + ",Levels:" + StringUtil.CollectionToString(Levels) + ",Size:" + Size + ",Space:" + Space + ",MoveSpeed:" + MoveSpeed + ",RunSpeed:" + RunSpeed + ",RunThreshold:" + RunThreshold + ",DefaultEnergy:" + DefaultEnergy + ",EatInterval:" + EatInterval + ",EatCount:" + EatCount + ",GrowInterval:" + GrowInterval + ",GrowCost:" + GrowCost + ",GrowIncrease:" + GrowIncrease + ",FertilityInterval:" + FertilityInterval + ",FertilityCost:" + FertilityCost + ",FertilityIncrease:" + FertilityIncrease + ",BreedDuration:" + BreedDuration + ",BreedEnergyCost:" + BreedEnergyCost + ",MetabolismInterval:" + MetabolismInterval + ",MetabolismCost:" + MetabolismCost + ",MetabolismIncrease:" + MetabolismIncrease + ",ProduceSpawnEntry:" + ProduceSpawnEntry?.ToString() + ",ProduceRequireMood:" + ProduceRequireMood + ",ExcreteInterval:" + ExcreteInterval + ",ExcreteCost:" + ExcreteCost + ",HungryThreshold:" + HungryThreshold + ",MoodDecreaseThreshold:" + MoodDecreaseThreshold + ",MaxNatureMood:" + MaxNatureMood + ",MoodIncreaseFondle:" + MoodIncreaseFondle + ",MoodDecreaseThunder:" + MoodDecreaseThunder + ",EscapeProbabilityBase:" + EscapeProbabilityBase + ",EscapeProbabilityIncrease:" + EscapeProbabilityIncrease + ",MoodAffectedDistance:" + MoodAffectedDistance + ",MoodRangeHorizontal:" + MoodRangeHorizontal + ",MoodRangeVerticalBottom:" + MoodRangeVerticalBottom + ",MoodRangeVerticalTop:" + MoodRangeVerticalTop + ",ProduceTechPoint:" + ProduceTechPoint + ",BreedTechPoint:" + BreedTechPoint + ",ManualMetabolism:" + ManualMetabolism + ",JumpHeight:" + JumpHeight + ",}";
	}

	private void PostResolve()
	{
		MoodAffectPositions = GeometryUtils.GetAffectorAroundPositions(size: new Vector2Int(Size, 1), horizontalRange: MoodRangeHorizontal, verticalRangeTop: MoodRangeVerticalTop, verticalRangeBottom: MoodRangeVerticalBottom);
		OccupiedPositions = new Vector2Int[Size];
		for (int i = 0; i < Size; i++)
		{
			OccupiedPositions[i] = new Vector2Int(i, 0);
		}
		int a = Levels.Length - 1;
		Vector2 vector = Levels[Mathf.Min(a, 1)].SpriteSize;
		Vector2 vector2 = Levels[0].SpriteSize;
		Vector2 vector3 = new Vector2(0.5f, 0.9f) * 0.125f;
		emotionOffsetAdult = new Vector2(vector.x, vector.y) * vector3;
		emotionOffsetChild = new Vector2(vector2.x, vector2.y) * vector3;
		ColliderSizeAdult = new Vector2(vector.x, vector.y) * 0.125f;
		ColliderSizeChild = new Vector2(vector2.x, vector2.y) * 0.125f;
	}

	public Vector2 GetEmotionOffset(bool isAdult)
	{
		if (!isAdult)
		{
			return emotionOffsetChild;
		}
		return emotionOffsetAdult;
	}
}
