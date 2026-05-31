using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Global;
using DolocTown.GameData;
using SimpleJSON;

namespace DolocTown.Config.Resource;

public sealed class ResourceSpawnInfo : BeanBase, ISpawnLut
{
	public readonly Dictionary<string, ResourceSpawnData> SpawnDatas_Index = new Dictionary<string, ResourceSpawnData>();

	public const int __ID__ = 661339259;

	public string Id { get; private set; }

	public List<ResourceSpawnData> SpawnDatas { get; private set; }

	public int Ceiling { get; private set; }

	public List<SpawnData> SpawnDataList { get; private set; }

	public ResourceSpawnInfo(JSONNode _json)
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
		SpawnDatas = new List<ResourceSpawnData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ResourceSpawnData item = ResourceSpawnData.DeserializeResourceSpawnData(child);
			SpawnDatas.Add(item);
		}
		foreach (ResourceSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.ResourceId, spawnData);
		}
	}

	public ResourceSpawnInfo(string id, List<ResourceSpawnData> spawn_datas)
	{
		Id = id;
		SpawnDatas = spawn_datas;
		foreach (ResourceSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.ResourceId, spawnData);
		}
	}

	public static ResourceSpawnInfo DeserializeResourceSpawnInfo(JSONNode _json)
	{
		return new ResourceSpawnInfo(_json);
	}

	public override int GetTypeId()
	{
		return 661339259;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ResourceSpawnData spawnData in SpawnDatas)
		{
			spawnData?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ResourceSpawnData spawnData in SpawnDatas)
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
		SpawnDataList = ((IEnumerable<ResourceSpawnData>)SpawnDatas).Select((Func<ResourceSpawnData, SpawnData>)((ResourceSpawnData x) => x)).ToList();
		Ceiling = ((ISpawnLut)this).GetCeiling();
	}

	private bool SpawnFilter(ResourceSpawnData data, DateInfo currentDate)
	{
		if ((data?.ResourceId_Ref?.CheckMatchMonth(currentDate.Month)).GetValueOrDefault())
		{
			return DolocAPI.CheckResourceUnlocked(data.SpawnId);
		}
		return false;
	}

	public bool TrySpawnSingle(Dictionary<string, int> currentCount, out ResourceSpawnData spawnData, DateInfo currentDate)
	{
		return ((ISpawnLut)this).SpawnSingleInternal(currentCount, out spawnData, (Func<ResourceSpawnData, bool>)((ResourceSpawnData data) => SpawnFilter(data, currentDate)));
	}

	public List<(ResourceSpawnData, int)> SpawnByFloor(DateInfo currentDate)
	{
		return ((ISpawnLut)this).SpawnByFloorOrderedInternal((Func<ResourceSpawnData, bool>)((ResourceSpawnData data) => SpawnFilter(data, currentDate)));
	}

	public Dictionary<ResourceSpawnData, int> SpawnResources(int totalCount, DateInfo currentDate, Func<ResourceSpawnData, bool> spawnFilter = null, string debugInfo = "")
	{
		Func<ResourceSpawnData, bool> spawnFilter2 = (ResourceSpawnData x) => SpawnFilter(x, currentDate) && (spawnFilter?.Invoke(x) ?? true);
		return ((ISpawnLut)this).SpawnInternal(totalCount, spawnFilter2, debugInfo);
	}
}
