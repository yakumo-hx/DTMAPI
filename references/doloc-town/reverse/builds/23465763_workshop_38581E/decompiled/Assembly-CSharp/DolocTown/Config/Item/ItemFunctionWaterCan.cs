using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionWaterCan : ItemFunctionBase
{
	public const int __ID__ = 870072415;

	public int Capacity { get; private set; }

	public int Range { get; private set; }

	public ItemFunctionWaterCan(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["range"].IsNumber)
		{
			throw new SerializationException();
		}
		Range = _json["range"];
	}

	public ItemFunctionWaterCan(int capacity, int range)
	{
		Capacity = capacity;
		Range = range;
	}

	public static ItemFunctionWaterCan DeserializeItemFunctionWaterCan(JSONNode _json)
	{
		return new ItemFunctionWaterCan(_json);
	}

	public override int GetTypeId()
	{
		return 870072415;
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
		return "{ Capacity:" + Capacity + ",Range:" + Range + ",}";
	}
}
