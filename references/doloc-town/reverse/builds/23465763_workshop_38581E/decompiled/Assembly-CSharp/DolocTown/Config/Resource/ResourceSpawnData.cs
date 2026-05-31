using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Global;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class ResourceSpawnData : SpawnData
{
	public const int __ID__ = 661178231;

	public string ResourceId { get; private set; }

	public ResourceInfo ResourceId_Ref { get; private set; }

	public override string SpawnId => ResourceId;

	public ResourceSpawnData(JSONNode _json)
		: base(_json)
	{
		if (!_json["resource_id"].IsString)
		{
			throw new SerializationException();
		}
		ResourceId = _json["resource_id"];
	}

	public ResourceSpawnData(float spawn_weight, int min_count, int max_count, string resource_id)
		: base(spawn_weight, min_count, max_count)
	{
		ResourceId = resource_id;
	}

	public static ResourceSpawnData DeserializeResourceSpawnData(JSONNode _json)
	{
		return new ResourceSpawnData(_json);
	}

	public override int GetTypeId()
	{
		return 661178231;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		ResourceId_Ref = (_tables["Resource.TbResource"] as TbResource).GetOrDefault(ResourceId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpawnWeight:" + base.SpawnWeight + ",MinCount:" + base.MinCount + ",MaxCount:" + base.MaxCount + ",ResourceId:" + ResourceId + ",}";
	}
}
