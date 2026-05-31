using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using RedSaw;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemSpawnRandom : BeanBase
{
	public const int __ID__ = -283226576;

	public string SpawnLut { get; private set; }

	public ItemSpawnInfo SpawnLut_Ref { get; private set; }

	public RangeInt CountRange { get; private set; }

	public float Probability { get; private set; }

	public ItemSpawnRandom(JSONNode _json)
	{
		if (!_json["spawn_lut"].IsString)
		{
			throw new SerializationException();
		}
		SpawnLut = _json["spawn_lut"];
		if (!_json["count_range"].IsObject)
		{
			throw new SerializationException();
		}
		CountRange = RangeInt.DeserializeRangeInt(_json["count_range"]);
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
	}

	public ItemSpawnRandom(string spawn_lut, RangeInt count_range, float probability)
	{
		SpawnLut = spawn_lut;
		CountRange = count_range;
		Probability = probability;
	}

	public static ItemSpawnRandom DeserializeItemSpawnRandom(JSONNode _json)
	{
		return new ItemSpawnRandom(_json);
	}

	public override int GetTypeId()
	{
		return -283226576;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpawnLut_Ref = (_tables["Item.TbItemSpawn"] as TbItemSpawn).GetOrDefault(SpawnLut);
		CountRange?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		CountRange?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpawnLut:" + SpawnLut + ",CountRange:" + CountRange?.ToString() + ",Probability:" + Probability + ",}";
	}

	public CountItem[] SpawnItems(Func<ItemSpawnData, bool> spawnFilter = null)
	{
		if (!RandomUtils.Dice(Probability))
		{
			return Array.Empty<CountItem>();
		}
		return SpawnLut_Ref.SpawnItems(CountRange.MinCount, CountRange.MaxCount, spawnFilter);
	}
}
