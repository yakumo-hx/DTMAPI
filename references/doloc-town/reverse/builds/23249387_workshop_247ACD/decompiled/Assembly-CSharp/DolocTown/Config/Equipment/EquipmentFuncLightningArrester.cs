using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncLightningArrester : EquipmentFuncAffector
{
	public const int __ID__ = 1079858474;

	public int Power { get; private set; }

	public EquipmentFuncLightningArrester(JSONNode _json)
		: base(_json)
	{
		if (!_json["power"].IsNumber)
		{
			throw new SerializationException();
		}
		Power = _json["power"];
	}

	public EquipmentFuncLightningArrester(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int work_interval, int power)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom, work_interval)
	{
		Power = power;
	}

	public static EquipmentFuncLightningArrester DeserializeEquipmentFuncLightningArrester(JSONNode _json)
	{
		return new EquipmentFuncLightningArrester(_json);
	}

	public override int GetTypeId()
	{
		return 1079858474;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",WorkInterval:" + base.WorkInterval + ",Power:" + Power + ",}";
	}
}
