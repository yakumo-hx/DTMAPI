using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class ItemPlaceConditionInfo : BeanBase
{
	public const int __ID__ = 1369794485;

	public string Id { get; private set; }

	public List<string> ItemTypeFilter { get; private set; }

	public List<ItemMainTypeInfo> ItemTypeFilter_Ref { get; private set; }

	public List<string> ItemSubTypeFilter { get; private set; }

	public List<ItemSubTypeInfo> ItemSubTypeFilter_Ref { get; private set; }

	public List<string> ItemNameFilter { get; private set; }

	public List<ItemInfo> ItemNameFilter_Ref { get; private set; }

	public ItemPlaceConditionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["item_type_filter"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		ItemTypeFilter = new List<string>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string item = child;
			ItemTypeFilter.Add(item);
		}
		JSONNode jSONNode2 = _json["item_sub_type_filter"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		ItemSubTypeFilter = new List<string>(jSONNode2.Count);
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string item2 = child2;
			ItemSubTypeFilter.Add(item2);
		}
		JSONNode jSONNode3 = _json["item_name_filter"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		ItemNameFilter = new List<string>(jSONNode3.Count);
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsString)
			{
				throw new SerializationException();
			}
			string item3 = child3;
			ItemNameFilter.Add(item3);
		}
	}

	public ItemPlaceConditionInfo(string id, List<string> item_type_filter, List<string> item_sub_type_filter, List<string> item_name_filter)
	{
		Id = id;
		ItemTypeFilter = item_type_filter;
		ItemSubTypeFilter = item_sub_type_filter;
		ItemNameFilter = item_name_filter;
	}

	public static ItemPlaceConditionInfo DeserializeItemPlaceConditionInfo(JSONNode _json)
	{
		return new ItemPlaceConditionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1369794485;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TbItemMainType tbItemMainType = (TbItemMainType)_tables["Item.TbItemMainType"];
		ItemTypeFilter_Ref = new List<ItemMainTypeInfo>();
		foreach (string item in ItemTypeFilter)
		{
			ItemTypeFilter_Ref.Add(tbItemMainType.GetOrDefault(item));
		}
		TbItemSubType tbItemSubType = (TbItemSubType)_tables["Item.TbItemSubType"];
		ItemSubTypeFilter_Ref = new List<ItemSubTypeInfo>();
		foreach (string item2 in ItemSubTypeFilter)
		{
			ItemSubTypeFilter_Ref.Add(tbItemSubType.GetOrDefault(item2));
		}
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		ItemNameFilter_Ref = new List<ItemInfo>();
		foreach (string item3 in ItemNameFilter)
		{
			ItemNameFilter_Ref.Add(tbItem.GetOrDefault(item3));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ItemTypeFilter:" + StringUtil.CollectionToString(ItemTypeFilter) + ",ItemSubTypeFilter:" + StringUtil.CollectionToString(ItemSubTypeFilter) + ",ItemNameFilter:" + StringUtil.CollectionToString(ItemNameFilter) + ",}";
	}

	public bool CheckCondition(DolocTown.Item item)
	{
		if (item == null)
		{
			return false;
		}
		if (!ItemNameFilter.Contains(item.name) && !ItemTypeFilter.Contains(item.type.Id))
		{
			return ItemSubTypeFilter.Contains(item.subType.Id);
		}
		return true;
	}
}
