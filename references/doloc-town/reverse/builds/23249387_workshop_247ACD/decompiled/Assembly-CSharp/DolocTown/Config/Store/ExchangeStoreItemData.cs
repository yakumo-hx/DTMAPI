using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class ExchangeStoreItemData : BeanBase
{
	public const int __ID__ = 1995007598;

	public RangedItem RangedItem { get; private set; }

	public int Storage { get; private set; }

	public int GoldCost { get; private set; }

	public CountItem[] ItemCosts { get; private set; }

	public string PreConditionItemName { get; private set; }

	public ItemInfo PreConditionItemName_Ref { get; private set; }

	public int PreConditionItemCount { get; private set; }

	public int TechPoint { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public string ItemId => RangedItem.itemName;

	public ItemInfo ItemId_Ref => RangedItem.itemRef;

	public bool HasPreCondition => !PreConditionItemName.IsNullOrEmpty();

	public bool UnlockOnInit
	{
		get
		{
			if (DefaultUnlock)
			{
				return !HasPreCondition;
			}
			return false;
		}
	}

	public bool Unlimited => Storage <= 0;

	public ExchangeStoreItemData(JSONNode _json)
	{
		if (!_json["ranged_item"].IsObject)
		{
			throw new SerializationException();
		}
		RangedItem = ExternalTypeUtil.RangedItemConverter(CfgRangedItem.DeserializeCfgRangedItem(_json["ranged_item"]));
		if (!_json["storage"].IsNumber)
		{
			throw new SerializationException();
		}
		Storage = _json["storage"];
		if (!_json["gold_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		GoldCost = _json["gold_cost"];
		JSONNode jSONNode = _json["item_costs"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ItemCosts = new CountItem[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			CountItem countItem = ExternalTypeUtil.CountItemConverter(CfgCountItem.DeserializeCfgCountItem(child));
			ItemCosts[num++] = countItem;
		}
		if (!_json["pre_condition_item_name"].IsString)
		{
			throw new SerializationException();
		}
		PreConditionItemName = _json["pre_condition_item_name"];
		if (!_json["pre_condition_item_count"].IsNumber)
		{
			throw new SerializationException();
		}
		PreConditionItemCount = _json["pre_condition_item_count"];
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = _json["tech_point"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
	}

	public ExchangeStoreItemData(RangedItem ranged_item, int storage, int gold_cost, CountItem[] item_costs, string pre_condition_item_name, int pre_condition_item_count, int tech_point, bool default_unlock)
	{
		RangedItem = ranged_item;
		Storage = storage;
		GoldCost = gold_cost;
		ItemCosts = item_costs;
		PreConditionItemName = pre_condition_item_name;
		PreConditionItemCount = pre_condition_item_count;
		TechPoint = tech_point;
		DefaultUnlock = default_unlock;
	}

	public static ExchangeStoreItemData DeserializeExchangeStoreItemData(JSONNode _json)
	{
		return new ExchangeStoreItemData(_json);
	}

	public override int GetTypeId()
	{
		return 1995007598;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		PreConditionItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(PreConditionItemName);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ RangedItem:" + RangedItem.ToString() + ",Storage:" + Storage + ",GoldCost:" + GoldCost + ",ItemCosts:" + StringUtil.CollectionToString(ItemCosts) + ",PreConditionItemName:" + PreConditionItemName + ",PreConditionItemCount:" + PreConditionItemCount + ",TechPoint:" + TechPoint + ",DefaultUnlock:" + DefaultUnlock + ",}";
	}
}
