using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFishTankExtension : EquipmentFuncEquipment
{
	public const int __ID__ = 1581652043;

	public int MetabolismIncrease { get; private set; }

	public int EnergyIncrease { get; private set; }

	public EquipmentFuncFishTankExtension(JSONNode _json)
		: base(_json)
	{
		if (!_json["metabolism_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		MetabolismIncrease = _json["metabolism_increase"];
		if (!_json["energy_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		EnergyIncrease = _json["energy_increase"];
	}

	public EquipmentFuncFishTankExtension(int metabolism_increase, int energy_increase)
	{
		MetabolismIncrease = metabolism_increase;
		EnergyIncrease = energy_increase;
	}

	public static EquipmentFuncFishTankExtension DeserializeEquipmentFuncFishTankExtension(JSONNode _json)
	{
		return new EquipmentFuncFishTankExtension(_json);
	}

	public override int GetTypeId()
	{
		return 1581652043;
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
		return "{ MetabolismIncrease:" + MetabolismIncrease + ",EnergyIncrease:" + EnergyIncrease + ",}";
	}
}
