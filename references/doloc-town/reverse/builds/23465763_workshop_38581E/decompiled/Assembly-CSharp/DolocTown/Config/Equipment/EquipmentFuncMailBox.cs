using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncMailBox : EquipmentFuncEquipment
{
	public const int __ID__ = -1756849310;

	public Vector2 TipOffset { get; private set; }

	public EquipmentFuncMailBox(JSONNode _json)
		: base(_json)
	{
		if (!_json["tip_offset"].IsObject)
		{
			throw new SerializationException();
		}
		TipOffset = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["tip_offset"]));
	}

	public EquipmentFuncMailBox(Vector2 tip_offset)
	{
		TipOffset = tip_offset;
	}

	public static EquipmentFuncMailBox DeserializeEquipmentFuncMailBox(JSONNode _json)
	{
		return new EquipmentFuncMailBox(_json);
	}

	public override int GetTypeId()
	{
		return -1756849310;
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
		return "{ TipOffset:" + TipOffset.ToString() + ",}";
	}
}
