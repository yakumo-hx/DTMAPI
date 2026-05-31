using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Sound;
using SimpleJSON;

namespace DolocTown.Config.Room;

public sealed class SceneInfo : BeanBase
{
	public const int __ID__ = 1123317255;

	public string Id { get; private set; }

	public BackgroundHighLevel HighLevel { get; private set; }

	public BackgroundHighLevelInfo HighLevel_Ref { get; private set; }

	public string MapId { get; private set; }

	public MapTypeInfo MapId_Ref { get; private set; }

	public EnvironmentType BgmEnvironmentType { get; private set; }

	public bool SwitchBgmImmediately { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string SubTitle { get; private set; }

	public string SubTitle_l10n_key { get; }

	public SceneInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["high_level"].IsNumber)
		{
			throw new SerializationException();
		}
		HighLevel = (BackgroundHighLevel)_json["high_level"].AsInt;
		if (!_json["map_id"].IsString)
		{
			throw new SerializationException();
		}
		MapId = _json["map_id"];
		if (!_json["bgm_environment_type"].IsNumber)
		{
			throw new SerializationException();
		}
		BgmEnvironmentType = (EnvironmentType)_json["bgm_environment_type"].AsInt;
		if (!_json["switch_bgm_immediately"].IsBoolean)
		{
			throw new SerializationException();
		}
		SwitchBgmImmediately = _json["switch_bgm_immediately"];
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
		if (!_json["sub_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		SubTitle_l10n_key = _json["sub_title"]["key"];
		if (!_json["sub_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		SubTitle = _json["sub_title"]["text"];
	}

	public SceneInfo(string id, BackgroundHighLevel high_level, string map_id, EnvironmentType bgm_environment_type, bool switch_bgm_immediately, string title, string sub_title)
	{
		Id = id;
		HighLevel = high_level;
		MapId = map_id;
		BgmEnvironmentType = bgm_environment_type;
		SwitchBgmImmediately = switch_bgm_immediately;
		Title = title;
		SubTitle = sub_title;
	}

	public static SceneInfo DeserializeSceneInfo(JSONNode _json)
	{
		return new SceneInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1123317255;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		HighLevel_Ref = (_tables["Room.TbBackgroundHighLevel"] as TbBackgroundHighLevel).GetOrDefault(HighLevel);
		MapId_Ref = (_tables["Room.TbMapType"] as TbMapType).GetOrDefault(MapId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		SubTitle = translator(SubTitle_l10n_key, SubTitle);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",HighLevel:" + HighLevel.ToString() + ",MapId:" + MapId + ",BgmEnvironmentType:" + BgmEnvironmentType.ToString() + ",SwitchBgmImmediately:" + SwitchBgmImmediately + ",Title:" + Title + ",SubTitle:" + SubTitle + ",}";
	}
}
