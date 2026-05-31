using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPlantBasinTree : EquipmentFuncEquipment
{
	public const int __ID__ = -512616770;

	public float FertilizerOffset { get; private set; }

	public EquipmentFuncPlantBasinTree(JSONNode _json)
		: base(_json)
	{
		if (!_json["fertilizer_offset"].IsNumber)
		{
			throw new SerializationException();
		}
		FertilizerOffset = _json["fertilizer_offset"];
	}

	public EquipmentFuncPlantBasinTree(float fertilizer_offset)
	{
		FertilizerOffset = fertilizer_offset;
	}

	public static EquipmentFuncPlantBasinTree DeserializeEquipmentFuncPlantBasinTree(JSONNode _json)
	{
		return new EquipmentFuncPlantBasinTree(_json);
	}

	public override int GetTypeId()
	{
		return -512616770;
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
		return "{ FertilizerOffset:" + FertilizerOffset + ",}";
	}
}
