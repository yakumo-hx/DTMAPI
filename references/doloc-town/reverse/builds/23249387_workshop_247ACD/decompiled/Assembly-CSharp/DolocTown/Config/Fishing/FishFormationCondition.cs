using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class FishFormationCondition : BeanBase
{
	public const int __ID__ = 2021436422;

	public string FishId { get; private set; }

	public Vector2Int CountRange { get; private set; }

	public int Index { get; private set; }

	public FishFormationCondition(JSONNode _json)
	{
		if (!_json["fish_id"].IsString)
		{
			throw new SerializationException();
		}
		FishId = _json["fish_id"];
		if (!_json["count_range"].IsObject)
		{
			throw new SerializationException();
		}
		CountRange = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["count_range"]));
		if (!_json["index"].IsNumber)
		{
			throw new SerializationException();
		}
		Index = _json["index"];
	}

	public FishFormationCondition(string fish_id, Vector2Int count_range, int index)
	{
		FishId = fish_id;
		CountRange = count_range;
		Index = index;
	}

	public static FishFormationCondition DeserializeFishFormationCondition(JSONNode _json)
	{
		return new FishFormationCondition(_json);
	}

	public override int GetTypeId()
	{
		return 2021436422;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ FishId:" + FishId + ",CountRange:" + CountRange.ToString() + ",Index:" + Index + ",}";
	}
}
