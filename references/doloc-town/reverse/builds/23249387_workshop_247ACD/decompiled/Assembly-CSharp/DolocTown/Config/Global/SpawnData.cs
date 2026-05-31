using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.Config.Monster;
using DolocTown.Config.Resource;
using DolocTown.Config.Room;
using SimpleJSON;

namespace DolocTown.Config.Global;

public abstract class SpawnData : BeanBase
{
	public float SpawnWeight { get; private set; }

	public int MinCount { get; private set; }

	public int MaxCount { get; private set; }

	public abstract string SpawnId { get; }

	public bool Unlimited
	{
		get
		{
			if (MaxCount > 0)
			{
				return MaxCount < MinCount;
			}
			return true;
		}
	}

	public SpawnData(JSONNode _json)
	{
		if (!_json["spawn_weight"].IsNumber)
		{
			throw new SerializationException();
		}
		SpawnWeight = _json["spawn_weight"];
		if (!_json["min_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MinCount = _json["min_count"];
		if (!_json["max_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxCount = _json["max_count"];
	}

	public SpawnData(float spawn_weight, int min_count, int max_count)
	{
		SpawnWeight = spawn_weight;
		MinCount = min_count;
		MaxCount = max_count;
	}

	public static SpawnData DeserializeSpawnData(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"Monster.MonsterSpawnData" => new MonsterSpawnData(_json), 
			"Item.ItemSpawnData" => new ItemSpawnData(_json), 
			"Resource.ResourceSpawnData" => new ResourceSpawnData(_json), 
			"Resource.VegetationSpawnData" => new VegetationSpawnData(_json), 
			"Room.EnvObjectSpawnData" => new EnvObjectSpawnData(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SpawnWeight:" + SpawnWeight + ",MinCount:" + MinCount + ",MaxCount:" + MaxCount + ",}";
	}
}
