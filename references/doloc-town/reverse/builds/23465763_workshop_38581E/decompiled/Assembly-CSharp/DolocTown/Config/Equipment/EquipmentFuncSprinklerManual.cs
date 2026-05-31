using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncSprinklerManual : EquipmentFuncAffector
{
	public const int __ID__ = -1986372324;

	public Vector2 EffectOffset { get; private set; }

	public int SprinklerCost { get; private set; }

	public EquipmentFuncSprinklerManual(JSONNode _json)
		: base(_json)
	{
		if (!_json["effect_offset"].IsObject)
		{
			throw new SerializationException();
		}
		EffectOffset = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["effect_offset"]));
		if (!_json["sprinkler_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		SprinklerCost = _json["sprinkler_cost"];
	}

	public EquipmentFuncSprinklerManual(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int work_interval, Vector2 effect_offset, int sprinkler_cost)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom, work_interval)
	{
		EffectOffset = effect_offset;
		SprinklerCost = sprinkler_cost;
	}

	public static EquipmentFuncSprinklerManual DeserializeEquipmentFuncSprinklerManual(JSONNode _json)
	{
		return new EquipmentFuncSprinklerManual(_json);
	}

	public override int GetTypeId()
	{
		return -1986372324;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",WorkInterval:" + base.WorkInterval + ",EffectOffset:" + EffectOffset.ToString() + ",SprinklerCost:" + SprinklerCost + ",}";
	}
}
