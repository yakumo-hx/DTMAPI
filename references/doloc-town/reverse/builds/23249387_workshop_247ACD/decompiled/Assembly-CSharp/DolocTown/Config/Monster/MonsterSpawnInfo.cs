using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Global;
using SimpleJSON;

namespace DolocTown.Config.Monster;

public sealed class MonsterSpawnInfo : BeanBase, ISpawnLut
{
	public readonly Dictionary<string, MonsterSpawnData> SpawnDatas_Index = new Dictionary<string, MonsterSpawnData>();

	public const int __ID__ = -184870941;

	public string Id { get; private set; }

	public List<MonsterSpawnData> SpawnDatas { get; private set; }

	public int Ceiling { get; private set; }

	public List<SpawnData> SpawnDataList { get; private set; }

	public MonsterSpawnInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["spawn_datas"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		SpawnDatas = new List<MonsterSpawnData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			MonsterSpawnData item = MonsterSpawnData.DeserializeMonsterSpawnData(child);
			SpawnDatas.Add(item);
		}
		foreach (MonsterSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.MonsterId, spawnData);
		}
	}

	public MonsterSpawnInfo(string id, List<MonsterSpawnData> spawn_datas)
	{
		Id = id;
		SpawnDatas = spawn_datas;
		foreach (MonsterSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.MonsterId, spawnData);
		}
	}

	public static MonsterSpawnInfo DeserializeMonsterSpawnInfo(JSONNode _json)
	{
		return new MonsterSpawnInfo(_json);
	}

	public override int GetTypeId()
	{
		return -184870941;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MonsterSpawnData spawnData in SpawnDatas)
		{
			spawnData?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MonsterSpawnData spawnData in SpawnDatas)
		{
			spawnData?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SpawnDatas:" + StringUtil.CollectionToString(SpawnDatas) + ",}";
	}

	private void PostResolve()
	{
		SpawnDataList = ((IEnumerable<MonsterSpawnData>)SpawnDatas).Select((Func<MonsterSpawnData, SpawnData>)((MonsterSpawnData x) => x)).ToList();
		Ceiling = ((ISpawnLut)this).GetCeiling();
	}

	public Dictionary<MonsterSpawnData, int> Spawn(int totalCount, Func<MonsterSpawnData, bool> spawnFilter = null, string debugInfo = "")
	{
		return ((ISpawnLut)this).SpawnInternal(totalCount, spawnFilter, debugInfo);
	}
}
