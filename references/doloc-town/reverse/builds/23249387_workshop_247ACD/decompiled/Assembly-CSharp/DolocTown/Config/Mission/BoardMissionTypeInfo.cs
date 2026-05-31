using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class BoardMissionTypeInfo : BeanBase
{
	public const int __ID__ = -63190960;

	public string Id { get; private set; }

	public BoardMissionType MissionType { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string MissionTipFormat { get; private set; }

	public string MissionTipFormat_l10n_key { get; }

	public BoardMissionTypeInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["mission_type"].IsNumber)
		{
			throw new SerializationException();
		}
		MissionType = (BoardMissionType)_json["mission_type"].AsInt;
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["mission_tip_format"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MissionTipFormat_l10n_key = _json["mission_tip_format"]["key"];
		if (!_json["mission_tip_format"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MissionTipFormat = _json["mission_tip_format"]["text"];
	}

	public BoardMissionTypeInfo(string id, BoardMissionType mission_type, string title, string mission_tip_format)
	{
		Id = id;
		MissionType = mission_type;
		Title = title;
		MissionTipFormat = mission_tip_format;
	}

	public static BoardMissionTypeInfo DeserializeBoardMissionTypeInfo(JSONNode _json)
	{
		return new BoardMissionTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -63190960;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		MissionTipFormat = translator(MissionTipFormat_l10n_key, MissionTipFormat);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MissionType:" + MissionType.ToString() + ",Title:" + Title + ",MissionTipFormat:" + MissionTipFormat + ",}";
	}
}
