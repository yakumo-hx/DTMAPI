using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.NPC;

public sealed class LikingLevelMapInfo : BeanBase
{
	public const int __ID__ = -1604483801;

	public int Level { get; private set; }

	public float Value { get; private set; }

	public LikingLevelMapInfo(JSONNode _json)
	{
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["value"].IsNumber)
		{
			throw new SerializationException();
		}
		Value = _json["value"];
	}

	public LikingLevelMapInfo(int level, float value)
	{
		Level = level;
		Value = value;
	}

	public static LikingLevelMapInfo DeserializeLikingLevelMapInfo(JSONNode _json)
	{
		return new LikingLevelMapInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1604483801;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Level:" + Level + ",Value:" + Value + ",}";
	}
}
