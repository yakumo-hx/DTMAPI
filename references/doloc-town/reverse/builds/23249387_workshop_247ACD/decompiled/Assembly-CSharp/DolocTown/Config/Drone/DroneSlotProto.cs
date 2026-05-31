using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class DroneSlotProto : BeanBase
{
	public const int __ID__ = -1423065048;

	public ComponentType SlotType { get; private set; }

	public string LockedComponentId { get; private set; }

	public bool IsVisual { get; private set; }

	public SlotVisualSuitableType VisualType { get; private set; }

	public Vector2Int VisualPivot { get; private set; }

	public DroneSlotProto(JSONNode _json)
	{
		if (!_json["slot_type"].IsNumber)
		{
			throw new SerializationException();
		}
		SlotType = (ComponentType)_json["slot_type"].AsInt;
		if (!_json["locked_component_id"].IsString)
		{
			throw new SerializationException();
		}
		LockedComponentId = _json["locked_component_id"];
		if (!_json["is_visual"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsVisual = _json["is_visual"];
		if (!_json["visual_type"].IsNumber)
		{
			throw new SerializationException();
		}
		VisualType = (SlotVisualSuitableType)_json["visual_type"].AsInt;
		if (!_json["visual_pivot"].IsObject)
		{
			throw new SerializationException();
		}
		VisualPivot = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["visual_pivot"]));
	}

	public DroneSlotProto(ComponentType slot_type, string locked_component_id, bool is_visual, SlotVisualSuitableType visual_type, Vector2Int visual_pivot)
	{
		SlotType = slot_type;
		LockedComponentId = locked_component_id;
		IsVisual = is_visual;
		VisualType = visual_type;
		VisualPivot = visual_pivot;
	}

	public static DroneSlotProto DeserializeDroneSlotProto(JSONNode _json)
	{
		return new DroneSlotProto(_json);
	}

	public override int GetTypeId()
	{
		return -1423065048;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SlotType:" + SlotType.ToString() + ",LockedComponentId:" + LockedComponentId + ",IsVisual:" + IsVisual + ",VisualType:" + VisualType.ToString() + ",VisualPivot:" + VisualPivot.ToString() + ",}";
	}

	public DroneSlotInfo GetSlotInfo()
	{
		return DolocConfig.Tables.TbDroneSlot.GetOrDefault(SlotType);
	}
}
