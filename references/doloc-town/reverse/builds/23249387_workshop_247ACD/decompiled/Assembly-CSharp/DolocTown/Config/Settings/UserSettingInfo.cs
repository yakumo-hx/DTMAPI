using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class UserSettingInfo : BeanBase
{
	public const int __ID__ = 254611080;

	public UserSettingType Id { get; private set; }

	public string Group { get; private set; }

	public UserSettingGroupInfo Group_Ref { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public SettingComponentBase Component { get; private set; }

	public UserSettingInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (UserSettingType)_json["id"].AsInt;
		if (!_json["group"].IsString)
		{
			throw new SerializationException();
		}
		Group = _json["group"];
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
		if (!_json["component"].IsObject)
		{
			throw new SerializationException();
		}
		Component = SettingComponentBase.DeserializeSettingComponentBase(_json["component"]);
	}

	public UserSettingInfo(UserSettingType id, string group, string title, SettingComponentBase component)
	{
		Id = id;
		Group = group;
		Title = title;
		Component = component;
	}

	public static UserSettingInfo DeserializeUserSettingInfo(JSONNode _json)
	{
		return new UserSettingInfo(_json);
	}

	public override int GetTypeId()
	{
		return 254611080;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Group_Ref = (_tables["Settings.TbUserSettingGroup"] as TbUserSettingGroup).GetOrDefault(Group);
		Component?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Component?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",Group:" + Group + ",Title:" + Title + ",Component:" + Component?.ToString() + ",}";
	}
}
