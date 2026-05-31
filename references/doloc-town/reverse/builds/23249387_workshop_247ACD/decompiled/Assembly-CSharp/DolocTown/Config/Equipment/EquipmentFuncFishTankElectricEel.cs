using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFishTankElectricEel : EquipmentFuncFishTankBase
{
	public const int __ID__ = 764777083;

	public int EnergyCapacity { get; private set; }

	public float EfficiencyPerFish { get; private set; }

	public EquipmentFuncFishTankElectricEel(JSONNode _json)
		: base(_json)
	{
		if (!_json["energy_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		EnergyCapacity = _json["energy_capacity"];
		if (!_json["efficiency_per_fish"].IsNumber)
		{
			throw new SerializationException();
		}
		EfficiencyPerFish = _json["efficiency_per_fish"];
	}

	public EquipmentFuncFishTankElectricEel(int total_capacity, int line_capacity, int metabolism_threshold, int update_interval, int product_capacity, SpriteAsset alpha_mask, int energy_capacity, float efficiency_per_fish)
		: base(total_capacity, line_capacity, metabolism_threshold, update_interval, product_capacity, alpha_mask)
	{
		EnergyCapacity = energy_capacity;
		EfficiencyPerFish = efficiency_per_fish;
	}

	public static EquipmentFuncFishTankElectricEel DeserializeEquipmentFuncFishTankElectricEel(JSONNode _json)
	{
		return new EquipmentFuncFishTankElectricEel(_json);
	}

	public override int GetTypeId()
	{
		return 764777083;
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
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",MetabolismThreshold:" + base.MetabolismThreshold + ",UpdateInterval:" + base.UpdateInterval + ",ProductCapacity:" + base.ProductCapacity + ",AlphaMask:" + base.AlphaMask?.ToString() + ",EnergyCapacity:" + EnergyCapacity + ",EfficiencyPerFish:" + EfficiencyPerFish + ",}";
	}
}
