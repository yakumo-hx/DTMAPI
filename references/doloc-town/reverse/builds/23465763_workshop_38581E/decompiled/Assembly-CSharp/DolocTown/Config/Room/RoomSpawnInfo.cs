using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Monster;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class RoomSpawnInfo : BeanBase
{
	public const int __ID__ = 1183221851;

	public ResourceSpawnEntry ResourceSpawnEntry { get; private set; }

	public VegetationSpawnEntry VegetationSpawnEntry { get; private set; }

	public MonsterSpawnEntry MonsterSpawnEntry { get; private set; }

	public EnvObjectSpawnEntry EnvObjectSpawnEntry { get; private set; }

	public RoomSpawnInfo(JSONNode _json)
	{
		if (!_json["resource_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		ResourceSpawnEntry = ResourceSpawnEntry.DeserializeResourceSpawnEntry(_json["resource_spawn_entry"]);
		if (!_json["vegetation_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		VegetationSpawnEntry = VegetationSpawnEntry.DeserializeVegetationSpawnEntry(_json["vegetation_spawn_entry"]);
		if (!_json["monster_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		MonsterSpawnEntry = MonsterSpawnEntry.DeserializeMonsterSpawnEntry(_json["monster_spawn_entry"]);
		if (!_json["env_object_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		EnvObjectSpawnEntry = EnvObjectSpawnEntry.DeserializeEnvObjectSpawnEntry(_json["env_object_spawn_entry"]);
	}

	public RoomSpawnInfo(ResourceSpawnEntry resource_spawn_entry, VegetationSpawnEntry vegetation_spawn_entry, MonsterSpawnEntry monster_spawn_entry, EnvObjectSpawnEntry env_object_spawn_entry)
	{
		ResourceSpawnEntry = resource_spawn_entry;
		VegetationSpawnEntry = vegetation_spawn_entry;
		MonsterSpawnEntry = monster_spawn_entry;
		EnvObjectSpawnEntry = env_object_spawn_entry;
	}

	public static RoomSpawnInfo DeserializeRoomSpawnInfo(JSONNode _json)
	{
		return new RoomSpawnInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1183221851;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ResourceSpawnEntry?.Resolve(_tables);
		VegetationSpawnEntry?.Resolve(_tables);
		MonsterSpawnEntry?.Resolve(_tables);
		EnvObjectSpawnEntry?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ResourceSpawnEntry?.TranslateText(translator);
		VegetationSpawnEntry?.TranslateText(translator);
		MonsterSpawnEntry?.TranslateText(translator);
		EnvObjectSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ ResourceSpawnEntry:" + ResourceSpawnEntry?.ToString() + ",VegetationSpawnEntry:" + VegetationSpawnEntry?.ToString() + ",MonsterSpawnEntry:" + MonsterSpawnEntry?.ToString() + ",EnvObjectSpawnEntry:" + EnvObjectSpawnEntry?.ToString() + ",}";
	}
}
