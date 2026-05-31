using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Global;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class ItemSpawnInfo : BeanBase, ISpawnLut
{
	public readonly Dictionary<string, ItemSpawnData> SpawnDatas_Index = new Dictionary<string, ItemSpawnData>();

	public const int __ID__ = 334644571;

	public string Id { get; private set; }

	public List<ItemSpawnData> SpawnDatas { get; private set; }

	public int Ceiling { get; private set; }

	public List<SpawnData> SpawnDataList { get; private set; }

	public ItemSpawnInfo(JSONNode _json)
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
		SpawnDatas = new List<ItemSpawnData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ItemSpawnData item = ItemSpawnData.DeserializeItemSpawnData(child);
			SpawnDatas.Add(item);
		}
		foreach (ItemSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.ItemName, spawnData);
		}
	}

	public ItemSpawnInfo(string id, List<ItemSpawnData> spawn_datas)
	{
		Id = id;
		SpawnDatas = spawn_datas;
		foreach (ItemSpawnData spawnData in SpawnDatas)
		{
			SpawnDatas_Index.Add(spawnData.ItemName, spawnData);
		}
	}

	public static ItemSpawnInfo DeserializeItemSpawnInfo(JSONNode _json)
	{
		return new ItemSpawnInfo(_json);
	}

	public override int GetTypeId()
	{
		return 334644571;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ItemSpawnData spawnData in SpawnDatas)
		{
			spawnData?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ItemSpawnData spawnData in SpawnDatas)
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
		SpawnDataList = ((IEnumerable<ItemSpawnData>)SpawnDatas).Select((Func<ItemSpawnData, SpawnData>)((ItemSpawnData x) => x)).ToList();
		Ceiling = ((ISpawnLut)this).GetCeiling();
	}

	public CountItem[] SpawnItems(int totalCount, Func<ItemSpawnData, bool> spawnFilter = null, string debugInfo = "")
	{
		return (from kv in ((ISpawnLut)this).SpawnInternal(totalCount, spawnFilter, debugInfo)
			select new CountItem(kv.Key.SpawnId, kv.Value)).ToArray();
	}

	public CountItem[] SpawnItems(int minCount, int maxCount, Func<ItemSpawnData, bool> spawnFilter = null, string debugInfo = "")
	{
		int totalCount = new Vector2Int(minCount, maxCount).DiceCount();
		return SpawnItems(totalCount, spawnFilter, debugInfo);
	}
}
