using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncAffector : EquipmentFuncRange
{
	public int WorkInterval { get; private set; }

	public EquipmentFuncAffector(JSONNode _json)
		: base(_json)
	{
		if (!_json["work_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		WorkInterval = _json["work_interval"];
	}

	public EquipmentFuncAffector(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int work_interval)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom)
	{
		WorkInterval = work_interval;
	}

	public static EquipmentFuncAffector DeserializeEquipmentFuncAffector(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncSprinkler" => new EquipmentFuncSprinkler(_json), 
			"EquipmentFuncSprinklerManual" => new EquipmentFuncSprinklerManual(_json), 
			"EquipmentFuncFarmLight" => new EquipmentFuncFarmLight(_json), 
			"EquipmentFuncLightningArrester" => new EquipmentFuncLightningArrester(_json), 
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",WorkInterval:" + WorkInterval + ",}";
	}

	public static Vector2Int[] GetAffectedPositions(Vector2Int anchor, Vector2Int size, int hRange, int vRangeTop, int vRangeBottom)
	{
		int num = anchor.x - hRange;
		int num2 = anchor.y - vRangeBottom;
		int num3 = hRange * 2 + size.x;
		int num4 = vRangeBottom + vRangeTop + size.y;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < num4; j++)
			{
				list.Add(new Vector2Int(num + i, num2 + j));
			}
		}
		return list.ToArray();
	}

	public Vector2Int[] GetAffectedPositions(Vector2Int anchor, Vector2Int size)
	{
		int num = anchor.x - base.HorizontalRange;
		int num2 = anchor.y - base.VerticalRangeBottom;
		int num3 = base.HorizontalRange * 2 + size.x;
		int num4 = base.VerticalRangeBottom + base.VerticalRangeTop + size.y;
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = 0; i < num3; i++)
		{
			for (int j = 0; j < num4; j++)
			{
				list.Add(new Vector2Int(num + i, num2 + j));
			}
		}
		return list.ToArray();
	}
}
