using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncFishTankBase : EquipmentFuncCaseBase
{
	public int MetabolismThreshold { get; private set; }

	public int UpdateInterval { get; private set; }

	public int ProductCapacity { get; private set; }

	public SpriteAsset AlphaMask { get; private set; }

	public EquipmentFuncFishTankBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["metabolism_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		MetabolismThreshold = _json["metabolism_threshold"];
		if (!_json["update_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		UpdateInterval = _json["update_interval"];
		if (!_json["product_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		ProductCapacity = _json["product_capacity"];
		if (!_json["alpha_mask"].IsObject)
		{
			throw new SerializationException();
		}
		AlphaMask = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["alpha_mask"]));
	}

	public EquipmentFuncFishTankBase(int total_capacity, int line_capacity, int metabolism_threshold, int update_interval, int product_capacity, SpriteAsset alpha_mask)
		: base(total_capacity, line_capacity)
	{
		MetabolismThreshold = metabolism_threshold;
		UpdateInterval = update_interval;
		ProductCapacity = product_capacity;
		AlphaMask = alpha_mask;
	}

	public static EquipmentFuncFishTankBase DeserializeEquipmentFuncFishTankBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncFishTank" => new EquipmentFuncFishTank(_json), 
			"EquipmentFuncFishTankEcological" => new EquipmentFuncFishTankEcological(_json), 
			"EquipmentFuncFishTankElectricEel" => new EquipmentFuncFishTankElectricEel(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",MetabolismThreshold:" + MetabolismThreshold + ",UpdateInterval:" + UpdateInterval + ",ProductCapacity:" + ProductCapacity + ",AlphaMask:" + AlphaMask?.ToString() + ",}";
	}
}
