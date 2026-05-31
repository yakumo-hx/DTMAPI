using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Global;
using DolocTown.Config.Room;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class EnvObjectSpawnInfo : BeanBase, ISpawnLut
{
	public readonly Dictionary<string, EnvObjectSpawnData> SpawnDatas_Index = new Dictionary<string, EnvObjectSpawnData>();

	public const int __ID__ = 89426813;

	public string Id { get; private set; }

	public List<EnvObjectSpawnData> SpawnDatas { get; private set; }

	public int Ceiling { get; private set; }

	public List<SpawnData> SpawnDataList { get; private set; }

	public EnvObjectSpawnInfo(JSONNode _json)
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
		SpawnDatas = new List<EnvObjectSpawnData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			EnvObjectSpawnData item = EnvObjectSpawnData.DeserializeEnvObjectSpawnData(child);
			SpawnDatas.Add(item);
		}
		foreach (EnvObjectSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.ObjectId, spawnData);
		}
	}

	public EnvObjectSpawnInfo(string id, List<EnvObjectSpawnData> spawn_datas)
	{
		Id = id;
		SpawnDatas = spawn_datas;
		foreach (EnvObjectSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.ObjectId, spawnData);
		}
	}

	public static EnvObjectSpawnInfo DeserializeEnvObjectSpawnInfo(JSONNode _json)
	{
		return new EnvObjectSpawnInfo(_json);
	}

	public override int GetTypeId()
	{
		return 89426813;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EnvObjectSpawnData spawnData in SpawnDatas)
		{
			spawnData?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EnvObjectSpawnData spawnData in SpawnDatas)
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
		SpawnDataList = ((IEnumerable<EnvObjectSpawnData>)SpawnDatas).Select((Func<EnvObjectSpawnData, SpawnData>)((EnvObjectSpawnData x) => x)).ToList();
		Ceiling = ((ISpawnLut)this).GetCeiling();
	}
}
