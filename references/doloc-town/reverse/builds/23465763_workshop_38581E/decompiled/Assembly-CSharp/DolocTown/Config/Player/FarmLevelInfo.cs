using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Room;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class FarmLevelInfo : BeanBase
{
	public const int __ID__ = -501853585;

	public int Level { get; private set; }

	public string Id { get; private set; }

	public int UpgradeCost { get; private set; }

	public string InitMarkPoint { get; private set; }

	public MarkPointInfo InitMarkPoint_Ref { get; private set; }

	public FarmLevelInfo(JSONNode _json)
	{
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["upgrade_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		UpgradeCost = _json["upgrade_cost"];
		if (!_json["init_mark_point"].IsString)
		{
			throw new SerializationException();
		}
		InitMarkPoint = _json["init_mark_point"];
	}

	public FarmLevelInfo(int level, string id, int upgrade_cost, string init_mark_point)
	{
		Level = level;
		Id = id;
		UpgradeCost = upgrade_cost;
		InitMarkPoint = init_mark_point;
	}

	public static FarmLevelInfo DeserializeFarmLevelInfo(JSONNode _json)
	{
		return new FarmLevelInfo(_json);
	}

	public override int GetTypeId()
	{
		return -501853585;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		InitMarkPoint_Ref = (_tables["Room.TbMarkPoint"] as TbMarkPoint).GetOrDefault(InitMarkPoint);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Level:" + Level + ",Id:" + Id + ",UpgradeCost:" + UpgradeCost + ",InitMarkPoint:" + InitMarkPoint + ",}";
	}
}
