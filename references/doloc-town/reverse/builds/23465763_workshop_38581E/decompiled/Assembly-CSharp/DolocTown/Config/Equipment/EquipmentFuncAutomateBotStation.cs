using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncAutomateBotStation : EquipmentFuncRange
{
	public const int __ID__ = -1351481805;

	public int MotionPointProduce { get; private set; }

	public int Capacity { get; private set; }

	public Vector2[] ChargingPositions { get; private set; }

	public bool IsLarge { get; private set; }

	public string LampOff { get; private set; }

	public LampInfo LampOff_Ref { get; private set; }

	public string LampOn { get; private set; }

	public LampInfo LampOn_Ref { get; private set; }

	public EquipmentFuncAutomateBotStation(JSONNode _json)
		: base(_json)
	{
		if (!_json["motion_point_produce"].IsNumber)
		{
			throw new SerializationException();
		}
		MotionPointProduce = _json["motion_point_produce"];
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		JSONNode jSONNode = _json["charging_positions"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ChargingPositions = new Vector2[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			Vector2 vector = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(child));
			ChargingPositions[num++] = vector;
		}
		if (!_json["is_large"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsLarge = _json["is_large"];
		if (!_json["lamp_off"].IsString)
		{
			throw new SerializationException();
		}
		LampOff = _json["lamp_off"];
		if (!_json["lamp_on"].IsString)
		{
			throw new SerializationException();
		}
		LampOn = _json["lamp_on"];
	}

	public EquipmentFuncAutomateBotStation(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int motion_point_produce, int capacity, Vector2[] charging_positions, bool is_large, string lamp_off, string lamp_on)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		MotionPointProduce = motion_point_produce;
		Capacity = capacity;
		ChargingPositions = charging_positions;
		IsLarge = is_large;
		LampOff = lamp_off;
		LampOn = lamp_on;
	}

	public static EquipmentFuncAutomateBotStation DeserializeEquipmentFuncAutomateBotStation(JSONNode _json)
	{
		return new EquipmentFuncAutomateBotStation(_json);
	}

	public override int GetTypeId()
	{
		return -1351481805;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		LampOff_Ref = (_tables["Equipment.TbLamp"] as TbLamp).GetOrDefault(LampOff);
		LampOn_Ref = (_tables["Equipment.TbLamp"] as TbLamp).GetOrDefault(LampOn);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",MotionPointProduce:" + MotionPointProduce + ",Capacity:" + Capacity + ",ChargingPositions:" + StringUtil.CollectionToString(ChargingPositions) + ",IsLarge:" + IsLarge + ",LampOff:" + LampOff + ",LampOn:" + LampOn + ",}";
	}
}
