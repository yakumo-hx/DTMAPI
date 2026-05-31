using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class RoomEffectInfo : BeanBase
{
	public const int __ID__ = -1571315059;

	public string Id { get; private set; }

	public float GrowthAddition { get; private set; }

	public float GrowthAdditionFungus { get; private set; }

	public float PowerGenerationAddition { get; private set; }

	public bool IgnoreSeason { get; private set; }

	public bool IgnoreSeasonFungus { get; private set; }

	public Dictionary<string, float> SynthesizerTimeAdditions { get; private set; }

	public RoomEffectInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["growth_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthAddition = _json["growth_addition"];
		if (!_json["growth_addition_fungus"].IsNumber)
		{
			throw new SerializationException();
		}
		GrowthAdditionFungus = _json["growth_addition_fungus"];
		if (!_json["power_generation_addition"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerGenerationAddition = _json["power_generation_addition"];
		if (!_json["ignore_season"].IsBoolean)
		{
			throw new SerializationException();
		}
		IgnoreSeason = _json["ignore_season"];
		if (!_json["ignore_season_fungus"].IsBoolean)
		{
			throw new SerializationException();
		}
		IgnoreSeasonFungus = _json["ignore_season_fungus"];
		JSONNode jSONNode = _json["synthesizer_time_additions"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		SynthesizerTimeAdditions = new Dictionary<string, float>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child[0].IsString)
			{
				throw new SerializationException();
			}
			string key = child[0];
			if (!child[1].IsNumber)
			{
				throw new SerializationException();
			}
			float value = child[1];
			SynthesizerTimeAdditions.Add(key, value);
		}
	}

	public RoomEffectInfo(string id, float growth_addition, float growth_addition_fungus, float power_generation_addition, bool ignore_season, bool ignore_season_fungus, Dictionary<string, float> synthesizer_time_additions)
	{
		Id = id;
		GrowthAddition = growth_addition;
		GrowthAdditionFungus = growth_addition_fungus;
		PowerGenerationAddition = power_generation_addition;
		IgnoreSeason = ignore_season;
		IgnoreSeasonFungus = ignore_season_fungus;
		SynthesizerTimeAdditions = synthesizer_time_additions;
	}

	public static RoomEffectInfo DeserializeRoomEffectInfo(JSONNode _json)
	{
		return new RoomEffectInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1571315059;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",GrowthAddition:" + GrowthAddition + ",GrowthAdditionFungus:" + GrowthAdditionFungus + ",PowerGenerationAddition:" + PowerGenerationAddition + ",IgnoreSeason:" + IgnoreSeason + ",IgnoreSeasonFungus:" + IgnoreSeasonFungus + ",SynthesizerTimeAdditions:" + StringUtil.CollectionToString(SynthesizerTimeAdditions) + ",}";
	}
}
