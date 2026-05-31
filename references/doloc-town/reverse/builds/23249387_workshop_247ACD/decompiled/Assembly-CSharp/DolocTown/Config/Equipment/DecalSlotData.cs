using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.GameData;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class DecalSlotData : BeanBase
{
	public const int __ID__ = -1251576587;

	public DecalSlotType SlotType { get; private set; }

	public Vector2Int PixelPivot { get; private set; }

	public DecalSlotData(JSONNode _json)
	{
		if (!_json["slot_type"].IsNumber)
		{
			throw new SerializationException();
		}
		SlotType = (DecalSlotType)_json["slot_type"].AsInt;
		if (!_json["pixel_pivot"].IsObject)
		{
			throw new SerializationException();
		}
		PixelPivot = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["pixel_pivot"]));
	}

	public DecalSlotData(DecalSlotType slot_type, Vector2Int pixel_pivot)
	{
		SlotType = slot_type;
		PixelPivot = pixel_pivot;
	}

	public static DecalSlotData DeserializeDecalSlotData(JSONNode _json)
	{
		return new DecalSlotData(_json);
	}

	public override int GetTypeId()
	{
		return -1251576587;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SlotType:" + SlotType.ToString() + ",PixelPivot:" + PixelPivot.ToString() + ",}";
	}
}
