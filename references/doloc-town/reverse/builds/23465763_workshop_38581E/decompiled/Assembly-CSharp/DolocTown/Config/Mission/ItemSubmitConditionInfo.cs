using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class ItemSubmitConditionInfo : BeanBase
{
	public const int __ID__ = -1927903620;

	public string Id { get; private set; }

	public int SubmitCount { get; private set; }

	public bool ShouldCostItem { get; private set; }

	public List<string> ItemTypeFilter { get; private set; }

	public List<ItemMainTypeInfo> ItemTypeFilter_Ref { get; private set; }

	public List<string> ItemSubTypeFilter { get; private set; }

	public List<ItemSubTypeInfo> ItemSubTypeFilter_Ref { get; private set; }

	public List<string> ItemNameFilter { get; private set; }

	public List<ItemInfo> ItemNameFilter_Ref { get; private set; }

	public string GeneCondition { get; private set; }

	public ItemGeneConditionInfo GeneCondition_Ref { get; private set; }

	public ItemSubmitConditionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["submit_count"].IsNumber)
		{
			throw new SerializationException();
		}
		SubmitCount = _json["submit_count"];
		if (!_json["should_cost_item"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShouldCostItem = _json["should_cost_item"];
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
		if (!_json["gene_condition"].IsString)
		{
			throw new SerializationException();
		}
		GeneCondition = _json["gene_condition"];
	}

	public ItemSubmitConditionInfo(string id, int submit_count, bool should_cost_item, List<string> item_type_filter, List<string> item_sub_type_filter, List<string> item_name_filter, string gene_condition)
	{
		Id = id;
		SubmitCount = submit_count;
		ShouldCostItem = should_cost_item;
		ItemTypeFilter = item_type_filter;
		ItemSubTypeFilter = item_sub_type_filter;
		ItemNameFilter = item_name_filter;
		GeneCondition = gene_condition;
	}

	public static ItemSubmitConditionInfo DeserializeItemSubmitConditionInfo(JSONNode _json)
	{
		return new ItemSubmitConditionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1927903620;
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
		GeneCondition_Ref = (_tables["Mission.TbItemGeneCondition"] as TbItemGeneCondition).GetOrDefault(GeneCondition);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SubmitCount:" + SubmitCount + ",ShouldCostItem:" + ShouldCostItem + ",ItemTypeFilter:" + StringUtil.CollectionToString(ItemTypeFilter) + ",ItemSubTypeFilter:" + StringUtil.CollectionToString(ItemSubTypeFilter) + ",ItemNameFilter:" + StringUtil.CollectionToString(ItemNameFilter) + ",GeneCondition:" + GeneCondition + ",}";
	}

	public bool CheckCondition(DolocTown.Item item)
	{
		if (item == null)
		{
			return false;
		}
		bool flag = false;
		flag |= ItemNameFilter.Contains(item.name);
		flag |= ItemTypeFilter.Contains(item.type.Id);
		flag |= ItemSubTypeFilter.Contains(item.subType.Id);
		if (GeneCondition_Ref != null)
		{
			if (!(item is IHasGeneGroup hasGeneGroup))
			{
				return false;
			}
			flag &= hasGeneGroup.IsCloned == GeneCondition_Ref.IsCloned;
			flag &= hasGeneGroup.GeneCount >= GeneCondition_Ref.GeneMinCount;
			string[] geneFilter = GeneCondition_Ref.GeneFilter;
			foreach (string geneId in geneFilter)
			{
				flag &= hasGeneGroup.GeneGroup.ContainsGene(geneId);
			}
		}
		return flag;
	}
}
