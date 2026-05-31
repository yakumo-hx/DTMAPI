using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class VegetationSpawnEntry : BeanBase
{
	public const int __ID__ = -691736763;

	public string SpawnLut { get; private set; }

	public VegetationSpawnInfo SpawnLut_Ref { get; private set; }

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

	public VegetationSpawnEntry(JSONNode _json)
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

	public VegetationSpawnEntry(string spawn_lut, RangeInt count_range)
	{
		SpawnLut = spawn_lut;
		CountRange = count_range;
	}

	public static VegetationSpawnEntry DeserializeVegetationSpawnEntry(JSONNode _json)
	{
		return new VegetationSpawnEntry(_json);
	}

	public override int GetTypeId()
	{
		return -691736763;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpawnLut_Ref = (_tables["Resource.TbVegetationSpawn"] as TbVegetationSpawn).GetOrDefault(SpawnLut);
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
