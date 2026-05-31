using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.General;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class StoreInfo : BeanBase
{
	public const int __ID__ = -767444958;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public int InitMoney { get; private set; }

	public DolocTown.Config.General.RangeInt[] InitMoneyRange { get; private set; }

	public string ItemRecords { get; private set; }

	public StoreItemListInfo ItemRecords_Ref { get; private set; }

	public string PriceScale { get; private set; }

	public StorePriceScaleInfo PriceScale_Ref { get; private set; }

	public StoreSeasonData[] SeasonDatas { get; private set; }

	public bool UnlimitedMoney => InitMoney <= 0;

	public StoreInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
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
		if (!_json["init_money"].IsNumber)
		{
			throw new SerializationException();
		}
		InitMoney = _json["init_money"];
		JSONNode jSONNode = _json["init_money_range"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		InitMoneyRange = new DolocTown.Config.General.RangeInt[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			DolocTown.Config.General.RangeInt rangeInt = DolocTown.Config.General.RangeInt.DeserializeRangeInt(child);
			InitMoneyRange[num++] = rangeInt;
		}
		if (!_json["item_records"].IsString)
		{
			throw new SerializationException();
		}
		ItemRecords = _json["item_records"];
		if (!_json["price_scale"].IsString)
		{
			throw new SerializationException();
		}
		PriceScale = _json["price_scale"];
		JSONNode jSONNode2 = _json["season_datas"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		SeasonDatas = new StoreSeasonData[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			StoreSeasonData storeSeasonData = StoreSeasonData.DeserializeStoreSeasonData(child2);
			SeasonDatas[num2++] = storeSeasonData;
		}
	}

	public StoreInfo(string id, string title, int init_money, DolocTown.Config.General.RangeInt[] init_money_range, string item_records, string price_scale, StoreSeasonData[] season_datas)
	{
		Id = id;
		Title = title;
		InitMoney = init_money;
		InitMoneyRange = init_money_range;
		ItemRecords = item_records;
		PriceScale = price_scale;
		SeasonDatas = season_datas;
	}

	public static StoreInfo DeserializeStoreInfo(JSONNode _json)
	{
		return new StoreInfo(_json);
	}

	public override int GetTypeId()
	{
		return -767444958;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		DolocTown.Config.General.RangeInt[] initMoneyRange = InitMoneyRange;
		for (int i = 0; i < initMoneyRange.Length; i++)
		{
			initMoneyRange[i]?.Resolve(_tables);
		}
		ItemRecords_Ref = (_tables["Store.TbStoreItemList"] as TbStoreItemList).GetOrDefault(ItemRecords);
		PriceScale_Ref = (_tables["Store.TbStorePriceScale"] as TbStorePriceScale).GetOrDefault(PriceScale);
		StoreSeasonData[] seasonDatas = SeasonDatas;
		for (int i = 0; i < seasonDatas.Length; i++)
		{
			seasonDatas[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		DolocTown.Config.General.RangeInt[] initMoneyRange = InitMoneyRange;
		for (int i = 0; i < initMoneyRange.Length; i++)
		{
			initMoneyRange[i]?.TranslateText(translator);
		}
		StoreSeasonData[] seasonDatas = SeasonDatas;
		for (int i = 0; i < seasonDatas.Length; i++)
		{
			seasonDatas[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",InitMoney:" + InitMoney + ",InitMoneyRange:" + StringUtil.CollectionToString(InitMoneyRange) + ",ItemRecords:" + ItemRecords + ",PriceScale:" + PriceScale + ",SeasonDatas:" + StringUtil.CollectionToString(SeasonDatas) + ",}";
	}

	public int GetRandomSlotCountBySeason(int seasonIndex)
	{
		return GetSeasonData(seasonIndex).RandomSlotsRange.RandomCount;
	}

	public StoreSeasonData GetSeasonData(int seasonIndex)
	{
		return SeasonDatas[GetValidSeasonIndex(seasonIndex)];
	}

	private int GetValidSeasonIndex(int seasonIndex)
	{
		return Mathf.Clamp(seasonIndex, 0, SeasonDatas.Length - 1);
	}
}
