using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Global;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class VegetationSpawnInfo : BeanBase, ISpawnLut
{
	public readonly Dictionary<string, VegetationSpawnData> SpawnDatas_Index = new Dictionary<string, VegetationSpawnData>();

	public const int __ID__ = 1086183291;

	public string Id { get; private set; }

	public List<VegetationSpawnData> SpawnDatas { get; private set; }

	public int Ceiling { get; private set; }

	public List<SpawnData> SpawnDataList { get; private set; }

	public VegetationSpawnInfo(JSONNode _json)
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
		SpawnDatas = new List<VegetationSpawnData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			VegetationSpawnData item = VegetationSpawnData.DeserializeVegetationSpawnData(child);
			SpawnDatas.Add(item);
		}
		foreach (VegetationSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.VegetationId, spawnData);
		}
	}

	public VegetationSpawnInfo(string id, List<VegetationSpawnData> spawn_datas)
	{
		Id = id;
		SpawnDatas = spawn_datas;
		foreach (VegetationSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.VegetationId, spawnData);
		}
	}

	public static VegetationSpawnInfo DeserializeVegetationSpawnInfo(JSONNode _json)
	{
		return new VegetationSpawnInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1086183291;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (VegetationSpawnData spawnData in SpawnDatas)
		{
			spawnData?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (VegetationSpawnData spawnData in SpawnDatas)
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
		SpawnDataList = ((IEnumerable<VegetationSpawnData>)SpawnDatas).Select((Func<VegetationSpawnData, SpawnData>)((VegetationSpawnData x) => x)).ToList();
		Ceiling = ((ISpawnLut)this).GetCeiling();
	}

	private bool SpawnFilter(VegetationSpawnData data)
	{
		if ((data?.VegetationId_Ref?.CheckMatchMonth(DolocAPI.archiveHandle.DateNow.Month)).GetValueOrDefault())
		{
			return DolocAPI.CheckVegetationUnlocked(data.SpawnId);
		}
		return false;
	}

	public bool TrySpawnSingle(Dictionary<string, int> currentCount, out VegetationSpawnData spawnData)
	{
		return ((ISpawnLut)this).SpawnSingleInternal(currentCount, out spawnData, (Func<VegetationSpawnData, bool>)SpawnFilter);
	}

	public List<(VegetationSpawnData, int)> SpawnByFloor()
	{
		return ((ISpawnLut)this).SpawnByFloorOrderedInternal((Func<VegetationSpawnData, bool>)SpawnFilter);
	}
}
