using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Building;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionBuildingExterior : ItemFunctionBase
{
	public const int __ID__ = -1381566280;

	public string ExteriorId { get; private set; }

	public BuildingExteriorInfo ExteriorId_Ref { get; private set; }

	public ItemFunctionBuildingExterior(JSONNode _json)
		: base(_json)
	{
		if (!_json["exterior_id"].IsString)
		{
			throw new SerializationException();
		}
		ExteriorId = _json["exterior_id"];
	}

	public ItemFunctionBuildingExterior(string exterior_id)
	{
		ExteriorId = exterior_id;
	}

	public static ItemFunctionBuildingExterior DeserializeItemFunctionBuildingExterior(JSONNode _json)
	{
		return new ItemFunctionBuildingExterior(_json);
	}

	public override int GetTypeId()
	{
		return -1381566280;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		ExteriorId_Ref = (_tables["Building.TbBuildingExterior"] as TbBuildingExterior).GetOrDefault(ExteriorId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ ExteriorId:" + ExteriorId + ",}";
	}
}
