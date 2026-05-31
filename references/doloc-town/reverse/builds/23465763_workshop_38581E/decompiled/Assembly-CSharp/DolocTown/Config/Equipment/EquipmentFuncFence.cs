using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFence : EquipmentFuncEquipment
{
	public const int __ID__ = -1938893185;

	public EquipmentFuncFence(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncFence()
	{
	}

	public static EquipmentFuncFence DeserializeEquipmentFuncFence(JSONNode _json)
	{
		return new EquipmentFuncFence(_json);
	}

	public override int GetTypeId()
	{
		return -1938893185;
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
		return "{ }";
	}
}
