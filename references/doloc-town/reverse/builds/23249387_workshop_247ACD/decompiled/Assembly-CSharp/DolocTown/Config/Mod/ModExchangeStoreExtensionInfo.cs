using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Store;
using SimpleJSON;

namespace DolocTown.Config.Mod;

public sealed class ModExchangeStoreExtensionInfo : BeanBase
{
	public const int __ID__ = -45173723;

	public string Id { get; private set; }

	public ExchangeStoreInfo Id_Ref { get; private set; }

	public ExchangeStoreItemData[] ExtraItems { get; private set; }

	public ModExchangeStoreExtensionInfo(JSONNode _json)
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
		ExtraItems = new ExchangeStoreItemData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ExchangeStoreItemData exchangeStoreItemData = ExchangeStoreItemData.DeserializeExchangeStoreItemData(child);
			ExtraItems[num++] = exchangeStoreItemData;
		}
	}

	public ModExchangeStoreExtensionInfo(string id, ExchangeStoreItemData[] extra_items)
	{
		Id = id;
		ExtraItems = extra_items;
	}

	public static ModExchangeStoreExtensionInfo DeserializeModExchangeStoreExtensionInfo(JSONNode _json)
	{
		return new ModExchangeStoreExtensionInfo(_json);
	}

	public override int GetTypeId()
	{
		return -45173723;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Store.TbExchangeStore"] as TbExchangeStore).GetOrDefault(Id);
		ExchangeStoreItemData[] extraItems = ExtraItems;
		for (int i = 0; i < extraItems.Length; i++)
		{
			extraItems[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ExchangeStoreItemData[] extraItems = ExtraItems;
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
