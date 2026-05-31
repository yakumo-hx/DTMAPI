using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFarmingGun : ItemFunctionBase
{
	public const int __ID__ = 1063333210;

	public int Capacity { get; private set; }

	public Vector2Int Area { get; private set; }

	public Vector2Int Offset { get; private set; }

	public ItemFunctionFarmingGun(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["area"].IsObject)
		{
			throw new SerializationException();
		}
		Area = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["area"]));
		if (!_json["offset"].IsObject)
		{
			throw new SerializationException();
		}
		Offset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["offset"]));
	}

	public ItemFunctionFarmingGun(int capacity, Vector2Int area, Vector2Int offset)
	{
		Capacity = capacity;
		Area = area;
		Offset = offset;
	}

	public static ItemFunctionFarmingGun DeserializeItemFunctionFarmingGun(JSONNode _json)
	{
		return new ItemFunctionFarmingGun(_json);
	}

	public override int GetTypeId()
	{
		return 1063333210;
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
		return "{ Capacity:" + Capacity + ",Area:" + Area.ToString() + ",Offset:" + Offset.ToString() + ",}";
	}
}
