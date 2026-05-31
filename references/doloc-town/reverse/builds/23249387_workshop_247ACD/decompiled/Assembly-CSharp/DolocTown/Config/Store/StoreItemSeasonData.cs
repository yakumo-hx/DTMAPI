using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class StoreItemSeasonData : BeanBase
{
	public const int __ID__ = -677833964;

	public string ItemName { get; private set; }

	public ItemInfo ItemName_Ref { get; private set; }

	public int Storage { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public StoreItemSpawnData[] SeasonSpawnData { get; private set; }

	public bool UnlimitedStorage => Storage <= 0;

	public StoreItemSeasonData(JSONNode _json)
	{
		if (!_json["item_name"].IsString)
		{
			throw new SerializationException();
		}
		ItemName = _json["item_name"];
		if (!_json["storage"].IsNumber)
		{
			throw new SerializationException();
		}
		Storage = _json["storage"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		JSONNode jSONNode = _json["season_spawn_data"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		SeasonSpawnData = new StoreItemSpawnData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			StoreItemSpawnData storeItemSpawnData = StoreItemSpawnData.DeserializeStoreItemSpawnData(child);
			SeasonSpawnData[num++] = storeItemSpawnData;
		}
	}

	public StoreItemSeasonData(string item_name, int storage, bool default_unlock, StoreItemSpawnData[] season_spawn_data)
	{
		ItemName = item_name;
		Storage = storage;
		DefaultUnlock = default_unlock;
		SeasonSpawnData = season_spawn_data;
	}

	public static StoreItemSeasonData DeserializeStoreItemSeasonData(JSONNode _json)
	{
		return new StoreItemSeasonData(_json);
	}

	public override int GetTypeId()
	{
		return -677833964;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemName);
		StoreItemSpawnData[] seasonSpawnData = SeasonSpawnData;
		for (int i = 0; i < seasonSpawnData.Length; i++)
		{
			seasonSpawnData[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		StoreItemSpawnData[] seasonSpawnData = SeasonSpawnData;
		for (int i = 0; i < seasonSpawnData.Length; i++)
		{
			seasonSpawnData[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ ItemName:" + ItemName + ",Storage:" + Storage + ",DefaultUnlock:" + DefaultUnlock + ",SeasonSpawnData:" + StringUtil.CollectionToString(SeasonSpawnData) + ",}";
	}

	public bool IsFixedInSeason(int seasonIndex)
	{
		return GetSeasonSpawnData(seasonIndex).SpawnWeight <= 0f;
	}

	public StoreItemSpawnData GetSeasonSpawnData(int seasonIndex)
	{
		return SeasonSpawnData[GetValidSeasonIndex(seasonIndex)];
	}

	private int GetValidSeasonIndex(int seasonIndex)
	{
		return Mathf.Clamp(seasonIndex, 0, SeasonSpawnData.Length - 1);
	}
}
