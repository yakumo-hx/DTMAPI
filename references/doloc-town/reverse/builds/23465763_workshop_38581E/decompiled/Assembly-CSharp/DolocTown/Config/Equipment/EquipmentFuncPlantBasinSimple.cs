using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPlantBasinSimple : EquipmentFuncPlantBasinBase
{
	public const int __ID__ = 1259834578;

	public int UseTimes { get; private set; }

	public EquipmentFuncPlantBasinSimple(JSONNode _json)
		: base(_json)
	{
		if (!_json["use_times"].IsNumber)
		{
			throw new SerializationException();
		}
		UseTimes = _json["use_times"];
	}

	public EquipmentFuncPlantBasinSimple(string seed_type, Vector2Int supply_capacity, Vector2 center_offset, SpriteAsset sprite_wet_asset, int use_times)
		: base(seed_type, supply_capacity, center_offset, sprite_wet_asset)
	{
		UseTimes = use_times;
	}

	public static EquipmentFuncPlantBasinSimple DeserializeEquipmentFuncPlantBasinSimple(JSONNode _json)
	{
		return new EquipmentFuncPlantBasinSimple(_json);
	}

	public override int GetTypeId()
	{
		return 1259834578;
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
		return "{ SeedType:" + base.SeedType + ",SupplyCapacity:" + base.SupplyCapacity.ToString() + ",CenterOffset:" + base.CenterOffset.ToString() + ",SpriteWetAsset:" + base.SpriteWetAsset?.ToString() + ",UseTimes:" + UseTimes + ",}";
	}
}
