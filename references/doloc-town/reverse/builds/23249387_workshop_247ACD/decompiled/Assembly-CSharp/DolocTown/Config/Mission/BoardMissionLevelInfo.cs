using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class BoardMissionLevelInfo : BeanBase
{
	public const int __ID__ = 180629002;

	public string MissionLv { get; private set; }

	public int Level { get; private set; }

	public int Exp { get; private set; }

	public BoardMissionLevelInfo(JSONNode _json)
	{
		if (!_json["mission_lv"].IsString)
		{
			throw new SerializationException();
		}
		MissionLv = _json["mission_lv"];
		if (!_json["level"].IsNumber)
		{
			throw new SerializationException();
		}
		Level = _json["level"];
		if (!_json["exp"].IsNumber)
		{
			throw new SerializationException();
		}
		Exp = _json["exp"];
	}

	public BoardMissionLevelInfo(string mission_lv, int level, int exp)
	{
		MissionLv = mission_lv;
		Level = level;
		Exp = exp;
	}

	public static BoardMissionLevelInfo DeserializeBoardMissionLevelInfo(JSONNode _json)
	{
		return new BoardMissionLevelInfo(_json);
	}

	public override int GetTypeId()
	{
		return 180629002;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ MissionLv:" + MissionLv + ",Level:" + Level + ",Exp:" + Exp + ",}";
	}
}
