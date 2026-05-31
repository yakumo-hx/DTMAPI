using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Global;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class EnvObjectSpawnData : SpawnData
{
	public const int __ID__ = 1679717516;

	public string ObjectId { get; private set; }

	public EnvObjectInfo ObjectId_Ref { get; private set; }

	public override string SpawnId => ObjectId;

	public EnvObjectSpawnData(JSONNode _json)
		: base(_json)
	{
		if (!_json["object_id"].IsString)
		{
			throw new SerializationException();
		}
		ObjectId = _json["object_id"];
	}

	public EnvObjectSpawnData(float spawn_weight, int min_count, int max_count, string object_id)
		: base(spawn_weight, min_count, max_count)
	{
		ObjectId = object_id;
	}

	public static EnvObjectSpawnData DeserializeEnvObjectSpawnData(JSONNode _json)
	{
		return new EnvObjectSpawnData(_json);
	}

	public override int GetTypeId()
	{
		return 1679717516;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		ObjectId_Ref = (_tables["Resource.TbEnvObject"] as TbEnvObject).GetOrDefault(ObjectId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpawnWeight:" + base.SpawnWeight + ",MinCount:" + base.MinCount + ",MaxCount:" + base.MaxCount + ",ObjectId:" + ObjectId + ",}";
	}
}
