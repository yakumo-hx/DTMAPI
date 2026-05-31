using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Global;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class VegetationSpawnData : SpawnData
{
	public const int __ID__ = 1086022263;

	public string VegetationId { get; private set; }

	public VegetationInfo VegetationId_Ref { get; private set; }

	public override string SpawnId => VegetationId;

	public VegetationSpawnData(JSONNode _json)
		: base(_json)
	{
		if (!_json["vegetation_id"].IsString)
		{
			throw new SerializationException();
		}
		VegetationId = _json["vegetation_id"];
	}

	public VegetationSpawnData(float spawn_weight, int min_count, int max_count, string vegetation_id)
		: base(spawn_weight, min_count, max_count)
	{
		VegetationId = vegetation_id;
	}

	public static VegetationSpawnData DeserializeVegetationSpawnData(JSONNode _json)
	{
		return new VegetationSpawnData(_json);
	}

	public override int GetTypeId()
	{
		return 1086022263;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		VegetationId_Ref = (_tables["Resource.TbVegetation"] as TbVegetation).GetOrDefault(VegetationId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpawnWeight:" + base.SpawnWeight + ",MinCount:" + base.MinCount + ",MaxCount:" + base.MaxCount + ",VegetationId:" + VegetationId + ",}";
	}
}
