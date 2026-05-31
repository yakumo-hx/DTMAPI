using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StoreItemSpawnData : BeanBase
{
	public const int __ID__ = 1943001854;

	public RangeInt CountRange { get; private set; }

	public float SpawnWeight { get; private set; }

	public StoreItemSpawnData(JSONNode _json)
	{
		if (!_json["count_range"].IsObject)
		{
			throw new SerializationException();
		}
		CountRange = RangeInt.DeserializeRangeInt(_json["count_range"]);
		if (!_json["spawn_weight"].IsNumber)
		{
			throw new SerializationException();
		}
		SpawnWeight = _json["spawn_weight"];
	}

	public StoreItemSpawnData(RangeInt count_range, float spawn_weight)
	{
		CountRange = count_range;
		SpawnWeight = spawn_weight;
	}

	public static StoreItemSpawnData DeserializeStoreItemSpawnData(JSONNode _json)
	{
		return new StoreItemSpawnData(_json);
	}

	public override int GetTypeId()
	{
		return 1943001854;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		CountRange?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		CountRange?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ CountRange:" + CountRange?.ToString() + ",SpawnWeight:" + SpawnWeight + ",}";
	}
}
