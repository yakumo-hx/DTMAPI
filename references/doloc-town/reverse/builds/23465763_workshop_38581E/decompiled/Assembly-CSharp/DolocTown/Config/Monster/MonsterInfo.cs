using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Monster;

public sealed class MonsterInfo : BeanBase
{
	public const int __ID__ = 1081875284;

	public string Id { get; private set; }

	public bool IsAir { get; private set; }

	public int Size { get; private set; }

	public PrefabAsset Prefab { get; private set; }

	public int Health { get; private set; }

	public int Defense { get; private set; }

	public float HurtDuration { get; private set; }

	public ItemSpawnEntry DropSpawnEntry { get; private set; }

	public int ExpValue { get; private set; }

	public string HummingSound { get; private set; }

	public string HurtSound { get; private set; }

	public string DeadSound { get; private set; }

	public string DeadEffects { get; private set; }

	public MonsterInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["is_air"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsAir = _json["is_air"];
		if (!_json["size"].IsNumber)
		{
			throw new SerializationException();
		}
		Size = _json["size"];
		if (!_json["prefab"].IsObject)
		{
			throw new SerializationException();
		}
		Prefab = ExternalTypeUtil.PrefabAssetConverter(CfgPrefabAsset.DeserializeCfgPrefabAsset(_json["prefab"]));
		if (!_json["health"].IsNumber)
		{
			throw new SerializationException();
		}
		Health = _json["health"];
		if (!_json["defense"].IsNumber)
		{
			throw new SerializationException();
		}
		Defense = _json["defense"];
		if (!_json["hurtDuration"].IsNumber)
		{
			throw new SerializationException();
		}
		HurtDuration = _json["hurtDuration"];
		if (!_json["drop_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		DropSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["drop_spawn_entry"]);
		if (!_json["expValue"].IsNumber)
		{
			throw new SerializationException();
		}
		ExpValue = _json["expValue"];
		if (!_json["hummingSound"].IsString)
		{
			throw new SerializationException();
		}
		HummingSound = _json["hummingSound"];
		if (!_json["hurtSound"].IsString)
		{
			throw new SerializationException();
		}
		HurtSound = _json["hurtSound"];
		if (!_json["deadSound"].IsString)
		{
			throw new SerializationException();
		}
		DeadSound = _json["deadSound"];
		if (!_json["deadEffects"].IsString)
		{
			throw new SerializationException();
		}
		DeadEffects = _json["deadEffects"];
	}

	public MonsterInfo(string id, bool is_air, int size, PrefabAsset prefab, int health, int defense, float hurtDuration, ItemSpawnEntry drop_spawn_entry, int expValue, string hummingSound, string hurtSound, string deadSound, string deadEffects)
	{
		Id = id;
		IsAir = is_air;
		Size = size;
		Prefab = prefab;
		Health = health;
		Defense = defense;
		HurtDuration = hurtDuration;
		DropSpawnEntry = drop_spawn_entry;
		ExpValue = expValue;
		HummingSound = hummingSound;
		HurtSound = hurtSound;
		DeadSound = deadSound;
		DeadEffects = deadEffects;
	}

	public static MonsterInfo DeserializeMonsterInfo(JSONNode _json)
	{
		return new MonsterInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1081875284;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		DropSpawnEntry?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		DropSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",IsAir:" + IsAir + ",Size:" + Size + ",Prefab:" + Prefab?.ToString() + ",Health:" + Health + ",Defense:" + Defense + ",HurtDuration:" + HurtDuration + ",DropSpawnEntry:" + DropSpawnEntry?.ToString() + ",ExpValue:" + ExpValue + ",HummingSound:" + HummingSound + ",HurtSound:" + HurtSound + ",DeadSound:" + DeadSound + ",DeadEffects:" + DeadEffects + ",}";
	}
}
