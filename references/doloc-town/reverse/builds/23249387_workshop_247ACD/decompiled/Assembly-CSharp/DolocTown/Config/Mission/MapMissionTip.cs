using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class MapMissionTip : BeanBase
{
	public const int __ID__ = -813571895;

	public MapMissionTipType TipType { get; private set; }

	public PositionTypeInfo TipType_Ref { get; private set; }

	public string TipArg { get; private set; }

	public MapMissionTip(JSONNode _json)
	{
		if (!_json["tip_type"].IsNumber)
		{
			throw new SerializationException();
		}
		TipType = (MapMissionTipType)_json["tip_type"].AsInt;
		if (!_json["tip_arg"].IsString)
		{
			throw new SerializationException();
		}
		TipArg = _json["tip_arg"];
	}

	public MapMissionTip(MapMissionTipType tip_type, string tip_arg)
	{
		TipType = tip_type;
		TipArg = tip_arg;
	}

	public static MapMissionTip DeserializeMapMissionTip(JSONNode _json)
	{
		return new MapMissionTip(_json);
	}

	public override int GetTypeId()
	{
		return -813571895;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TipType_Ref = (_tables["Mission.TbPositionType"] as TbPositionType).GetOrDefault(TipType);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ TipType:" + TipType.ToString() + ",TipArg:" + TipArg + ",}";
	}
}
