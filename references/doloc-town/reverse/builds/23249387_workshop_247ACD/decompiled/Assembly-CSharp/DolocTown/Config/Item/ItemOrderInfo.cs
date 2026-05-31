using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemOrderInfo : BeanBase
{
	public const int __ID__ = 316061614;

	public string Id { get; private set; }

	public float CustomOrder { get; private set; }

	public float SortingOrder { get; set; }

	public ItemOrderInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["custom_order"].IsNumber)
		{
			throw new SerializationException();
		}
		CustomOrder = _json["custom_order"];
	}

	public ItemOrderInfo(string id, float custom_order)
	{
		Id = id;
		CustomOrder = custom_order;
	}

	public static ItemOrderInfo DeserializeItemOrderInfo(JSONNode _json)
	{
		return new ItemOrderInfo(_json);
	}

	public override int GetTypeId()
	{
		return 316061614;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",CustomOrder:" + CustomOrder + ",}";
	}
}
