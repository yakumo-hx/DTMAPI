using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class BackpackLevelInfo : BeanBase
{
	public const int __ID__ = -1201041179;

	public int Level { get; private set; }

	public int Capacity { get; private set; }

	public int UpgradeCost { get; private set; }

	public BackpackLevelInfo(JSONNode _json)
	{
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["upgrade_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		UpgradeCost = _json["upgrade_cost"];
	}

	public BackpackLevelInfo(int level, int capacity, int upgrade_cost)
	{
		Level = level;
		Capacity = capacity;
		UpgradeCost = upgrade_cost;
	}

	public static BackpackLevelInfo DeserializeBackpackLevelInfo(JSONNode _json)
	{
		return new BackpackLevelInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1201041179;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Level:" + Level + ",Capacity:" + Capacity + ",UpgradeCost:" + UpgradeCost + ",}";
	}
}
