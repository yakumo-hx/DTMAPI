using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFishTank : EquipmentFuncFishTankBase
{
	public const int __ID__ = 850690644;

	public int EnergyCapacity { get; private set; }

	public EquipmentFuncFishTank(JSONNode _json)
		: base(_json)
	{
		if (!_json["energy_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		EnergyCapacity = _json["energy_capacity"];
	}

	public EquipmentFuncFishTank(int total_capacity, int line_capacity, int metabolism_threshold, int update_interval, int product_capacity, SpriteAsset alpha_mask, int energy_capacity)
		: base(total_capacity, line_capacity, metabolism_threshold, update_interval, product_capacity, alpha_mask)
	{
		EnergyCapacity = energy_capacity;
	}

	public static EquipmentFuncFishTank DeserializeEquipmentFuncFishTank(JSONNode _json)
	{
		return new EquipmentFuncFishTank(_json);
	}

	public override int GetTypeId()
	{
		return 850690644;
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
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",MetabolismThreshold:" + base.MetabolismThreshold + ",UpdateInterval:" + base.UpdateInterval + ",ProductCapacity:" + base.ProductCapacity + ",AlphaMask:" + base.AlphaMask?.ToString() + ",EnergyCapacity:" + EnergyCapacity + ",}";
	}
}
