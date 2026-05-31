using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemSpawnEntry : BeanBase
{
	public const int __ID__ = 1780366693;

	public string SpawnLut { get; private set; }

	public ItemSpawnInfo SpawnLut_Ref { get; private set; }

	public RangeInt CountRange { get; private set; }

	public bool IsEmpty
	{
		get
		{
			if (!SpawnLut.IsNullOrEmpty())
			{
				return SpawnLut_Ref == null;
			}
			return true;
		}
	}

	public ItemSpawnEntry(JSONNode _json)
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
	}

	public ItemSpawnEntry(string spawn_lut, RangeInt count_range)
	{
		SpawnLut = spawn_lut;
		CountRange = count_range;
	}

	public static ItemSpawnEntry DeserializeItemSpawnEntry(JSONNode _json)
	{
		return new ItemSpawnEntry(_json);
	}

	public override int GetTypeId()
	{
		return 1780366693;
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
		return "{ SpawnLut:" + SpawnLut + ",CountRange:" + CountRange?.ToString() + ",}";
	}
}
