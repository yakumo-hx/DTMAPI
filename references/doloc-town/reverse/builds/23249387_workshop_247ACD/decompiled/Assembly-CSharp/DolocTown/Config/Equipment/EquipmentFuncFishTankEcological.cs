using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFishTankEcological : EquipmentFuncFishTankBase
{
	public const int __ID__ = -1395579700;

	public int WorkDuration { get; private set; }

	public EquipmentFuncFishTankEcological(JSONNode _json)
		: base(_json)
	{
		if (!_json["work_duration"].IsNumber)
		{
			throw new SerializationException();
		}
		WorkDuration = _json["work_duration"];
	}

	public EquipmentFuncFishTankEcological(int total_capacity, int line_capacity, int metabolism_threshold, int update_interval, int product_capacity, SpriteAsset alpha_mask, int work_duration)
		: base(total_capacity, line_capacity, metabolism_threshold, update_interval, product_capacity, alpha_mask)
	{
		WorkDuration = work_duration;
	}

	public static EquipmentFuncFishTankEcological DeserializeEquipmentFuncFishTankEcological(JSONNode _json)
	{
		return new EquipmentFuncFishTankEcological(_json);
	}

	public override int GetTypeId()
	{
		return -1395579700;
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
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",MetabolismThreshold:" + base.MetabolismThreshold + ",UpdateInterval:" + base.UpdateInterval + ",ProductCapacity:" + base.ProductCapacity + ",AlphaMask:" + base.AlphaMask?.ToString() + ",WorkDuration:" + WorkDuration + ",}";
	}
}
