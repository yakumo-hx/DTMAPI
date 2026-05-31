using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncCaseBase : EquipmentFuncEquipment
{
	public int TotalCapacity { get; private set; }

	public int LineCapacity { get; private set; }

	public EquipmentFuncCaseBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["total_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		TotalCapacity = _json["total_capacity"];
		if (!_json["line_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		LineCapacity = _json["line_capacity"];
	}

	public EquipmentFuncCaseBase(int total_capacity, int line_capacity)
	{
		TotalCapacity = total_capacity;
		LineCapacity = line_capacity;
	}

	public static EquipmentFuncCaseBase DeserializeEquipmentFuncCaseBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncCase" => new EquipmentFuncCase(_json), 
			"EquipmentFuncStorageShelf" => new EquipmentFuncStorageShelf(_json), 
			"EquipmentFuncParkingApron" => new EquipmentFuncParkingApron(_json), 
			"EquipmentFuncPlantAnalyzer" => new EquipmentFuncPlantAnalyzer(_json), 
			"EquipmentFuncFishTank" => new EquipmentFuncFishTank(_json), 
			"EquipmentFuncFishTankEcological" => new EquipmentFuncFishTankEcological(_json), 
			"EquipmentFuncFishTankElectricEel" => new EquipmentFuncFishTankElectricEel(_json), 
			"EquipmentFuncFishIncubator" => new EquipmentFuncFishIncubator(_json), 
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
		return "{ TotalCapacity:" + TotalCapacity + ",LineCapacity:" + LineCapacity + ",}";
	}
}
