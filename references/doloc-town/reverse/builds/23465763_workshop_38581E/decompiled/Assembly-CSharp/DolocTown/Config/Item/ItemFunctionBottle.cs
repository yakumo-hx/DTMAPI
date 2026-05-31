using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionBottle : ItemFunctionBase
{
	public const int __ID__ = -363899796;

	public int Capacity { get; private set; }

	public ItemFunctionBottle(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
	}

	public ItemFunctionBottle(int capacity)
	{
		Capacity = capacity;
	}

	public static ItemFunctionBottle DeserializeItemFunctionBottle(JSONNode _json)
	{
		return new ItemFunctionBottle(_json);
	}

	public override int GetTypeId()
	{
		return -363899796;
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
		return "{ Capacity:" + Capacity + ",}";
	}
}
