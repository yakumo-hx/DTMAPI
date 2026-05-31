using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncRange : EquipmentFuncEquipment
{
	public int HorizontalRange { get; private set; }

	public int VerticalRangeTop { get; private set; }

	public int VerticalRangeBottom { get; private set; }

	public EquipmentFuncRange(JSONNode _json)
		: base(_json)
	{
		if (!_json["horizontal_range"].IsNumber)
		{
			throw new SerializationException();
		}
		HorizontalRange = _json["horizontal_range"];
		if (!_json["vertical_range_top"].IsNumber)
		{
			throw new SerializationException();
		}
		VerticalRangeTop = _json["vertical_range_top"];
		if (!_json["vertical_range_bottom"].IsNumber)
		{
			throw new SerializationException();
		}
		VerticalRangeBottom = _json["vertical_range_bottom"];
	}

	public EquipmentFuncRange(int horizontal_range, int vertical_range_top, int vertical_range_bottom)
	{
		HorizontalRange = horizontal_range;
		VerticalRangeTop = vertical_range_top;
		VerticalRangeBottom = vertical_range_bottom;
	}

	public static EquipmentFuncRange DeserializeEquipmentFuncRange(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncSprinkler" => new EquipmentFuncSprinkler(_json), 
			"EquipmentFuncSprinklerManual" => new EquipmentFuncSprinklerManual(_json), 
			"EquipmentFuncFarmLight" => new EquipmentFuncFarmLight(_json), 
			"EquipmentFuncLightningArrester" => new EquipmentFuncLightningArrester(_json), 
			"EquipmentFuncAutomateBotStation" => new EquipmentFuncAutomateBotStation(_json), 
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
		return "{ HorizontalRange:" + HorizontalRange + ",VerticalRangeTop:" + VerticalRangeTop + ",VerticalRangeBottom:" + VerticalRangeBottom + ",}";
	}
}
