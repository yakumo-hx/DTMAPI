using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionBattery : ItemFunctionBase
{
	public const int __ID__ = 1203193895;

	public int Power { get; private set; }

	public ItemFunctionBattery(JSONNode _json)
		: base(_json)
	{
		if (!_json["power"].IsNumber)
		{
			throw new SerializationException();
		}
		Power = _json["power"];
	}

	public ItemFunctionBattery(int power)
	{
		Power = power;
	}

	public static ItemFunctionBattery DeserializeItemFunctionBattery(JSONNode _json)
	{
		return new ItemFunctionBattery(_json);
	}

	public override int GetTypeId()
	{
		return 1203193895;
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
		return "{ Power:" + Power + ",}";
	}
}
