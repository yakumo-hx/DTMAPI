using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Global;
using SimpleJSON;

namespace DolocTown.Config.Monster;

public sealed class MonsterSpawnData : SpawnData
{
	public const int __ID__ = -185031969;

	public string MonsterId { get; private set; }

	public MonsterInfo MonsterId_Ref { get; private set; }

	public override string SpawnId => MonsterId;

	public MonsterSpawnData(JSONNode _json)
		: base(_json)
	{
		if (!_json["monster_id"].IsString)
		{
			throw new SerializationException();
		}
		MonsterId = _json["monster_id"];
	}

	public MonsterSpawnData(float spawn_weight, int min_count, int max_count, string monster_id)
		: base(spawn_weight, min_count, max_count)
	{
		MonsterId = monster_id;
	}

	public static MonsterSpawnData DeserializeMonsterSpawnData(JSONNode _json)
	{
		return new MonsterSpawnData(_json);
	}

	public override int GetTypeId()
	{
		return -185031969;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		MonsterId_Ref = (_tables["Monster.TbMonster"] as TbMonster).GetOrDefault(MonsterId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpawnWeight:" + base.SpawnWeight + ",MinCount:" + base.MinCount + ",MaxCount:" + base.MaxCount + ",MonsterId:" + MonsterId + ",}";
	}
}
