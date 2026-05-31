using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionPatch : ItemFunctionBase
{
	public const int __ID__ = -691963678;

	public float HealingAmount { get; private set; }

	public ItemFunctionPatch(JSONNode _json)
		: base(_json)
	{
		if (!_json["healing_amount"].IsNumber)
		{
			throw new SerializationException();
		}
		HealingAmount = _json["healing_amount"];
	}

	public ItemFunctionPatch(float healing_amount)
	{
		HealingAmount = healing_amount;
	}

	public static ItemFunctionPatch DeserializeItemFunctionPatch(JSONNode _json)
	{
		return new ItemFunctionPatch(_json);
	}

	public override int GetTypeId()
	{
		return -691963678;
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
		return "{ HealingAmount:" + HealingAmount + ",}";
	}
}
