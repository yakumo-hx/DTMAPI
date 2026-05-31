using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class ExchangeStoreInfo : BeanBase
{
	public const int __ID__ = -455352769;

	public string Id { get; private set; }

	public bool RefreshImmediatly { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string CraftText { get; private set; }

	public string CraftText_l10n_key { get; }

	public List<ExchangeStoreItemData> ItemList { get; private set; }

	public Dictionary<string, ExchangeStoreItemData> StoreItemMap { get; private set; } = new Dictionary<string, ExchangeStoreItemData>();


	public ExchangeStoreInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["refresh_immediatly"].IsBoolean)
		{
			throw new SerializationException();
		}
		RefreshImmediatly = _json["refresh_immediatly"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["craft_text"]["key"].IsString)
		{
			throw new SerializationException();
		}
		CraftText_l10n_key = _json["craft_text"]["key"];
		if (!_json["craft_text"]["text"].IsString)
		{
			throw new SerializationException();
		}
		CraftText = _json["craft_text"]["text"];
		JSONNode jSONNode = _json["item_list"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		ItemList = new List<ExchangeStoreItemData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ExchangeStoreItemData item = ExchangeStoreItemData.DeserializeExchangeStoreItemData(child);
			ItemList.Add(item);
		}
	}

	public ExchangeStoreInfo(string id, bool refresh_immediatly, string title, string craft_text, List<ExchangeStoreItemData> item_list)
	{
		Id = id;
		RefreshImmediatly = refresh_immediatly;
		Title = title;
		CraftText = craft_text;
		ItemList = item_list;
	}

	public static ExchangeStoreInfo DeserializeExchangeStoreInfo(JSONNode _json)
	{
		return new ExchangeStoreInfo(_json);
	}

	public override int GetTypeId()
	{
		return -455352769;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ExchangeStoreItemData item in ItemList)
		{
			item?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		CraftText = translator(CraftText_l10n_key, CraftText);
		foreach (ExchangeStoreItemData item in ItemList)
		{
			item?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",RefreshImmediatly:" + RefreshImmediatly + ",Title:" + Title + ",CraftText:" + CraftText + ",ItemList:" + StringUtil.CollectionToString(ItemList) + ",}";
	}

	private void PostResolve()
	{
		foreach (ExchangeStoreItemData item in ItemList)
		{
			StoreItemMap[item.ItemId] = item;
		}
	}
}
