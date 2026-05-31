using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Store;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModStoreExtensionInfo : BeanBase
{
	public readonly Dictionary<string, StoreItemSeasonData> ExtraItems_Index = new Dictionary<string, StoreItemSeasonData>();

	public const int __ID__ = -2015731038;

	public string Id { get; private set; }

	public StoreInfo Id_Ref { get; private set; }

	public StoreItemSeasonData[] ExtraItems { get; private set; }

	public ModStoreExtensionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["extra_items"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ExtraItems = new StoreItemSeasonData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			StoreItemSeasonData storeItemSeasonData = StoreItemSeasonData.DeserializeStoreItemSeasonData(child);
			ExtraItems[num++] = storeItemSeasonData;
		}
		StoreItemSeasonData[] extraItems = ExtraItems;
		foreach (StoreItemSeasonData storeItemSeasonData2 in extraItems)
		{
			ExtraItems_Index.Add(storeItemSeasonData2.ItemName, storeItemSeasonData2);
		}
	}

	public ModStoreExtensionInfo(string id, StoreItemSeasonData[] extra_items)
	{
		Id = id;
		ExtraItems = extra_items;
		StoreItemSeasonData[] extraItems = ExtraItems;
		foreach (StoreItemSeasonData storeItemSeasonData in extraItems)
		{
			ExtraItems_Index.Add(storeItemSeasonData.ItemName, storeItemSeasonData);
		}
	}

	public static ModStoreExtensionInfo DeserializeModStoreExtensionInfo(JSONNode _json)
	{
		return new ModStoreExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -2015731038;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Store.TbStore"] as TbStore).GetOrDefault(Id);
		StoreItemSeasonData[] extraItems = ExtraItems;
		for (int i = 0; i < extraItems.Length; i++)
		{
			extraItems[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		StoreItemSeasonData[] extraItems = ExtraItems;
		for (int i = 0; i < extraItems.Length; i++)
		{
			extraItems[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ExtraItems:" + StringUtil.CollectionToString(ExtraItems) + ",}";
	}
}
