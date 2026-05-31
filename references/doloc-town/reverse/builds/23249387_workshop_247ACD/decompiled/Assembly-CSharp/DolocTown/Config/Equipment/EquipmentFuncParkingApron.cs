using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Store;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncParkingApron : EquipmentFuncCaseBase
{
	public const int __ID__ = 2058256476;

	public string StoreName { get; private set; }

	public StoreInfo StoreName_Ref { get; private set; }

	public List<int> PriceIncreasedMonths { get; private set; }

	public SpriteAsset ExpressDroneAsset { get; private set; }

	public float ExpressDroneOffset { get; private set; }

	public SpriteAsset SignalLightAsset { get; private set; }

	public SpriteAsset SignalLightMask { get; private set; }

	public float SignalLightOffset { get; private set; }

	public GoodsSpriteLevel[] GoodsSpriteLvs { get; private set; }

	public GoodsDurationLevel[] GoodsDurationLvs { get; private set; }

	public EquipmentFuncParkingApron(JSONNode _json)
		: base(_json)
	{
		if (!_json["store_name"].IsString)
		{
			throw new SerializationException();
		}
		StoreName = _json["store_name"];
		JSONNode jSONNode = _json["price_increased_months"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		PriceIncreasedMonths = new List<int>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int item = child;
			PriceIncreasedMonths.Add(item);
		}
		if (!_json["express_drone_asset"].IsObject)
		{
			throw new SerializationException();
		}
		ExpressDroneAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["express_drone_asset"]));
		if (!_json["express_drone_offset"].IsNumber)
		{
			throw new SerializationException();
		}
		ExpressDroneOffset = _json["express_drone_offset"];
		if (!_json["signal_light_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SignalLightAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["signal_light_asset"]));
		if (!_json["signal_light_mask"].IsObject)
		{
			throw new SerializationException();
		}
		SignalLightMask = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["signal_light_mask"]));
		if (!_json["signal_light_offset"].IsNumber)
		{
			throw new SerializationException();
		}
		SignalLightOffset = _json["signal_light_offset"];
		JSONNode jSONNode2 = _json["goods_sprite_lvs"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode2.Count;
		GoodsSpriteLvs = new GoodsSpriteLevel[count];
		int num = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			GoodsSpriteLevel goodsSpriteLevel = GoodsSpriteLevel.DeserializeGoodsSpriteLevel(child2);
			GoodsSpriteLvs[num++] = goodsSpriteLevel;
		}
		JSONNode jSONNode3 = _json["goods_duration_lvs"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode3.Count;
		GoodsDurationLvs = new GoodsDurationLevel[count2];
		int num2 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			GoodsDurationLevel goodsDurationLevel = GoodsDurationLevel.DeserializeGoodsDurationLevel(child3);
			GoodsDurationLvs[num2++] = goodsDurationLevel;
		}
	}

	public EquipmentFuncParkingApron(int total_capacity, int line_capacity, string store_name, List<int> price_increased_months, SpriteAsset express_drone_asset, float express_drone_offset, SpriteAsset signal_light_asset, SpriteAsset signal_light_mask, float signal_light_offset, GoodsSpriteLevel[] goods_sprite_lvs, GoodsDurationLevel[] goods_duration_lvs)
		: base(total_capacity, line_capacity)
	{
		StoreName = store_name;
		PriceIncreasedMonths = price_increased_months;
		ExpressDroneAsset = express_drone_asset;
		ExpressDroneOffset = express_drone_offset;
		SignalLightAsset = signal_light_asset;
		SignalLightMask = signal_light_mask;
		SignalLightOffset = signal_light_offset;
		GoodsSpriteLvs = goods_sprite_lvs;
		GoodsDurationLvs = goods_duration_lvs;
	}

	public static EquipmentFuncParkingApron DeserializeEquipmentFuncParkingApron(JSONNode _json)
	{
		return new EquipmentFuncParkingApron(_json);
	}

	public override int GetTypeId()
	{
		return 2058256476;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		StoreName_Ref = (_tables["Store.TbStore"] as TbStore).GetOrDefault(StoreName);
		GoodsSpriteLevel[] goodsSpriteLvs = GoodsSpriteLvs;
		for (int i = 0; i < goodsSpriteLvs.Length; i++)
		{
			goodsSpriteLvs[i]?.Resolve(_tables);
		}
		GoodsDurationLevel[] goodsDurationLvs = GoodsDurationLvs;
		for (int i = 0; i < goodsDurationLvs.Length; i++)
		{
			goodsDurationLvs[i]?.Resolve(_tables);
		}
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
		GoodsSpriteLevel[] goodsSpriteLvs = GoodsSpriteLvs;
		for (int i = 0; i < goodsSpriteLvs.Length; i++)
		{
			goodsSpriteLvs[i]?.TranslateText(translator);
		}
		GoodsDurationLevel[] goodsDurationLvs = GoodsDurationLvs;
		for (int i = 0; i < goodsDurationLvs.Length; i++)
		{
			goodsDurationLvs[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",StoreName:" + StoreName + ",PriceIncreasedMonths:" + StringUtil.CollectionToString(PriceIncreasedMonths) + ",ExpressDroneAsset:" + ExpressDroneAsset?.ToString() + ",ExpressDroneOffset:" + ExpressDroneOffset + ",SignalLightAsset:" + SignalLightAsset?.ToString() + ",SignalLightMask:" + SignalLightMask?.ToString() + ",SignalLightOffset:" + SignalLightOffset + ",GoodsSpriteLvs:" + StringUtil.CollectionToString(GoodsSpriteLvs) + ",GoodsDurationLvs:" + StringUtil.CollectionToString(GoodsDurationLvs) + ",}";
	}

	public Sprite GetGoodsLvSprite(int countThreshold)
	{
		if (TryGetGoodsSpriteLv(countThreshold, out var lv))
		{
			return lv.SpriteAsset.Asset;
		}
		return null;
	}

	private bool TryGetGoodsSpriteLv(int countThreshold, out GoodsSpriteLevel lv)
	{
		for (int num = GoodsSpriteLvs.Length - 1; num >= 0; num--)
		{
			if (countThreshold > GoodsSpriteLvs[num].CountThreshold)
			{
				lv = GoodsSpriteLvs[num];
				return true;
			}
		}
		lv = null;
		return false;
	}

	public int GetGoodsLvDuration(int moneyThreshold)
	{
		if (TryGetGoodsDurationLv(moneyThreshold, out var lv))
		{
			return lv.Duration;
		}
		return 0;
	}

	private bool TryGetGoodsDurationLv(int moneyThreshold, out GoodsDurationLevel lv)
	{
		for (int num = GoodsDurationLvs.Length - 1; num >= 0; num--)
		{
			if (moneyThreshold > GoodsDurationLvs[num].MoneyThreshold)
			{
				lv = GoodsDurationLvs[num];
				return true;
			}
		}
		lv = null;
		return false;
	}
}
