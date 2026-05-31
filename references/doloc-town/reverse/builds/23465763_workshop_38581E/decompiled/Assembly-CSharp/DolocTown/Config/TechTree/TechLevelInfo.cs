using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.TechTree;

public sealed class TechLevelInfo : BeanBase
{
	public const int __ID__ = -476898494;

	public int Level { get; private set; }

	public int RequiredExp { get; private set; }

	public int RewardPoints { get; private set; }

	public TechLevelInfo(JSONNode _json)
	{
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["required_exp"].IsNumber)
		{
			throw new SerializationException();
		}
		RequiredExp = _json["required_exp"];
		if (!_json["reward_points"].IsNumber)
		{
			throw new SerializationException();
		}
		RewardPoints = _json["reward_points"];
	}

	public TechLevelInfo(int level, int required_exp, int reward_points)
	{
		Level = level;
		RequiredExp = required_exp;
		RewardPoints = reward_points;
	}

	public static TechLevelInfo DeserializeTechLevelInfo(JSONNode _json)
	{
		return new TechLevelInfo(_json);
	}

	public override int GetTypeId()
	{
		return -476898494;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Level:" + Level + ",RequiredExp:" + RequiredExp + ",RewardPoints:" + RewardPoints + ",}";
	}
}
