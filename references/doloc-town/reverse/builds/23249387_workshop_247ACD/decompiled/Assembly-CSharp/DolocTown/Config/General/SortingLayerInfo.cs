using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.General;

public sealed class SortingLayerInfo : BeanBase
{
	public const int __ID__ = -1342731007;

	public string SortingLayerName { get; private set; }

	public int OrderInLayer { get; private set; }

	public float ZValue { get; private set; }

	public SortingLayerInfo(JSONNode _json)
	{
		if (!_json["sorting_layer_name"].IsString)
		{
			throw new SerializationException();
		}
		SortingLayerName = _json["sorting_layer_name"];
		if (!_json["order_in_layer"].IsNumber)
		{
			throw new SerializationException();
		}
		OrderInLayer = _json["order_in_layer"];
		if (!_json["z_value"].IsNumber)
		{
			throw new SerializationException();
		}
		ZValue = _json["z_value"];
	}

	public SortingLayerInfo(string sorting_layer_name, int order_in_layer, float z_value)
	{
		SortingLayerName = sorting_layer_name;
		OrderInLayer = order_in_layer;
		ZValue = z_value;
	}

	public static SortingLayerInfo DeserializeSortingLayerInfo(JSONNode _json)
	{
		return new SortingLayerInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1342731007;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SortingLayerName:" + SortingLayerName + ",OrderInLayer:" + OrderInLayer + ",ZValue:" + ZValue + ",}";
	}
}
