using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncChair : EquipmentFuncEquipment
{
	public const int __ID__ = -1941586669;

	public Vector2 SitOffset { get; private set; }

	public Vector2 SitOffsetFlip { get; private set; }

	public string Chair { get; private set; }

	public ChairInfo Chair_Ref { get; private set; }

	public EquipmentFuncChair(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["sit_offset"];
		if (!jSONNode.IsObject)
		{
			throw new SerializationException();
		}
		if (!jSONNode["x"].IsNumber)
		{
			throw new SerializationException();
		}
		float x = jSONNode["x"];
		if (!jSONNode["y"].IsNumber)
		{
			throw new SerializationException();
		}
		float y = jSONNode["y"];
		SitOffset = new Vector2(x, y);
		JSONNode jSONNode2 = _json["sit_offset_flip"];
		if (!jSONNode2.IsObject)
		{
			throw new SerializationException();
		}
		if (!jSONNode2["x"].IsNumber)
		{
			throw new SerializationException();
		}
		float x2 = jSONNode2["x"];
		if (!jSONNode2["y"].IsNumber)
		{
			throw new SerializationException();
		}
		float y2 = jSONNode2["y"];
		SitOffsetFlip = new Vector2(x2, y2);
		if (!_json["chair"].IsString)
		{
			throw new SerializationException();
		}
		Chair = _json["chair"];
	}

	public EquipmentFuncChair(Vector2 sit_offset, Vector2 sit_offset_flip, string chair)
	{
		SitOffset = sit_offset;
		SitOffsetFlip = sit_offset_flip;
		Chair = chair;
	}

	public static EquipmentFuncChair DeserializeEquipmentFuncChair(JSONNode _json)
	{
		return new EquipmentFuncChair(_json);
	}

	public override int GetTypeId()
	{
		return -1941586669;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		Chair_Ref = (_tables["Equipment.TbChair"] as TbChair).GetOrDefault(Chair);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SitOffset:" + SitOffset.ToString() + ",SitOffsetFlip:" + SitOffsetFlip.ToString() + ",Chair:" + Chair + ",}";
	}
}
