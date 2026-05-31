using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncAnimalDecorator : EquipmentFuncEquipment
{
	public const int __ID__ = -1945591187;

	public int MoodContribution { get; private set; }

	public EquipmentFuncAnimalDecorator(JSONNode _json)
		: base(_json)
	{
		if (!_json["mood_contribution"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodContribution = _json["mood_contribution"];
	}

	public EquipmentFuncAnimalDecorator(int mood_contribution)
	{
		MoodContribution = mood_contribution;
	}

	public static EquipmentFuncAnimalDecorator DeserializeEquipmentFuncAnimalDecorator(JSONNode _json)
	{
		return new EquipmentFuncAnimalDecorator(_json);
	}

	public override int GetTypeId()
	{
		return -1945591187;
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
		return "{ MoodContribution:" + MoodContribution + ",}";
	}
}
