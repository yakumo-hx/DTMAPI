using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncPlantAnalyzer : EquipmentFuncCaseBase
{
	public const int __ID__ = -293754753;

	public EquipmentFuncPlantAnalyzer(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncPlantAnalyzer(int total_capacity, int line_capacity)
		: base(total_capacity, line_capacity)
	{
	}

	public static EquipmentFuncPlantAnalyzer DeserializeEquipmentFuncPlantAnalyzer(JSONNode _json)
	{
		return new EquipmentFuncPlantAnalyzer(_json);
	}

	public override int GetTypeId()
	{
		return -293754753;
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
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",}";
	}
}
