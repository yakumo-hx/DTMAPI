using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPlantBasin : EquipmentFuncPlantBasinBase
{
	public const int __ID__ = -1011241216;

	public EquipmentFuncPlantBasin(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncPlantBasin(string seed_type, Vector2Int supply_capacity, Vector2 center_offset, SpriteAsset sprite_wet_asset)
		: base(seed_type, supply_capacity, center_offset, sprite_wet_asset)
	{
	}

	public static EquipmentFuncPlantBasin DeserializeEquipmentFuncPlantBasin(JSONNode _json)
	{
		return new EquipmentFuncPlantBasin(_json);
	}

	public override int GetTypeId()
	{
		return -1011241216;
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
		return "{ SeedType:" + base.SeedType + ",SupplyCapacity:" + base.SupplyCapacity.ToString() + ",CenterOffset:" + base.CenterOffset.ToString() + ",SpriteWetAsset:" + base.SpriteWetAsset?.ToString() + ",}";
	}
}
