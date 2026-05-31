using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Settings;

public sealed class UserSettingGroupInfo : BeanBase
{
	public const int __ID__ = -1803753933;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string DevicePaths { get; private set; }

	public UserSettingGroupInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
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
		if (!_json["device_paths"].IsString)
		{
			throw new SerializationException();
		}
		DevicePaths = _json["device_paths"];
	}

	public UserSettingGroupInfo(string id, string title, string device_paths)
	{
		Id = id;
		Title = title;
		DevicePaths = device_paths;
	}

	public static UserSettingGroupInfo DeserializeUserSettingGroupInfo(JSONNode _json)
	{
		return new UserSettingGroupInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1803753933;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",DevicePaths:" + DevicePaths + ",}";
	}
}
