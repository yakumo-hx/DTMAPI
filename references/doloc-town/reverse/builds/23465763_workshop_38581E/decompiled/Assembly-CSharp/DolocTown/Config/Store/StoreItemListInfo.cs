using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StoreItemListInfo : BeanBase
{
	public readonly Dictionary<string, StoreItemSeasonData> ItemList_Index = new Dictionary<string, StoreItemSeasonData>();

	public const int __ID__ = -1766537709;

	public string Id { get; private set; }

	public List<StoreItemSeasonData> ItemList { get; private set; }

	public StoreItemListInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["item_list"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		ItemList = new List<StoreItemSeasonData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			StoreItemSeasonData item = StoreItemSeasonData.DeserializeStoreItemSeasonData(child);
			ItemList.Add(item);
		}
		foreach (StoreItemSeasonData item2 in ItemList)
		{
			ItemList_Index.Add(item2.ItemName, item2);
		}
	}

	public StoreItemListInfo(string id, List<StoreItemSeasonData> item_list)
	{
		Id = id;
		ItemList = item_list;
		foreach (StoreItemSeasonData item in ItemList)
		{
			ItemList_Index.Add(item.ItemName, item);
		}
	}

	public static StoreItemListInfo DeserializeStoreItemListInfo(JSONNode _json)
	{
		return new StoreItemListInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1766537709;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (StoreItemSeasonData item in ItemList)
		{
			item?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (StoreItemSeasonData item in ItemList)
		{
			item?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ItemList:" + StringUtil.CollectionToString(ItemList) + ",}";
	}
}
