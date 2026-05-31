using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModItemSpawnExtensionInfo : BeanBase
{
	public readonly Dictionary<string, ItemSpawnData> ExtraItems_Index = new Dictionary<string, ItemSpawnData>();

	public const int __ID__ = 2076344251;

	public string Id { get; private set; }

	public ItemSpawnInfo Id_Ref { get; private set; }

	public ItemSpawnData[] ExtraItems { get; private set; }

	public ModItemSpawnExtensionInfo(JSONNode _json)
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
		ExtraItems = new ItemSpawnData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ItemSpawnData itemSpawnData = ItemSpawnData.DeserializeItemSpawnData(child);
			ExtraItems[num++] = itemSpawnData;
		}
		ItemSpawnData[] extraItems = ExtraItems;
		foreach (ItemSpawnData itemSpawnData2 in extraItems)
		{
			ExtraItems_Index.Add(itemSpawnData2.ItemName, itemSpawnData2);
		}
	}

	public ModItemSpawnExtensionInfo(string id, ItemSpawnData[] extra_items)
	{
		Id = id;
		ExtraItems = extra_items;
		ItemSpawnData[] extraItems = ExtraItems;
		foreach (ItemSpawnData itemSpawnData in extraItems)
		{
			ExtraItems_Index.Add(itemSpawnData.ItemName, itemSpawnData);
		}
	}

	public static ModItemSpawnExtensionInfo DeserializeModItemSpawnExtensionInfo(JSONNode _json)
	{
		return new ModItemSpawnExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 2076344251;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItemSpawn"] as TbItemSpawn).GetOrDefault(Id);
		ItemSpawnData[] extraItems = ExtraItems;
		for (int i = 0; i < extraItems.Length; i++)
		{
			extraItems[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ItemSpawnData[] extraItems = ExtraItems;
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
