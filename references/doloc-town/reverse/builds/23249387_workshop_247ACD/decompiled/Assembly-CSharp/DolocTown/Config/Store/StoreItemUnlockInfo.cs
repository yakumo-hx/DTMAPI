using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StoreItemUnlockInfo : BeanBase
{
	public const int __ID__ = -2063211527;

	public string Id { get; private set; }

	public StoreItemUnlockType ConditionType { get; private set; }

	public string StoreId { get; private set; }

	public string UnlockItemName { get; private set; }

	public ItemInfo UnlockItemName_Ref { get; private set; }

	public StoreItemUnlockInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["condition_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ConditionType = (StoreItemUnlockType)_json["condition_type"].AsInt;
		if (!_json["store_id"].IsString)
		{
			throw new SerializationException();
		}
		StoreId = _json["store_id"];
		if (!_json["unlock_item_name"].IsString)
		{
			throw new SerializationException();
		}
		UnlockItemName = _json["unlock_item_name"];
	}

	public StoreItemUnlockInfo(string id, StoreItemUnlockType condition_type, string store_id, string unlock_item_name)
	{
		Id = id;
		ConditionType = condition_type;
		StoreId = store_id;
		UnlockItemName = unlock_item_name;
	}

	public static StoreItemUnlockInfo DeserializeStoreItemUnlockInfo(JSONNode _json)
	{
		return new StoreItemUnlockInfo(_json);
	}

	public override int GetTypeId()
	{
		return -2063211527;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		UnlockItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(UnlockItemName);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",ConditionType:" + ConditionType.ToString() + ",StoreId:" + StoreId + ",UnlockItemName:" + UnlockItemName + ",}";
	}
}
