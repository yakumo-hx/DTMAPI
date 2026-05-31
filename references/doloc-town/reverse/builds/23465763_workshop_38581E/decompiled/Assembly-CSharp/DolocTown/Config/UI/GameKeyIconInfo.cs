using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class GameKeyIconInfo : BeanBase
{
	public const int __ID__ = -788124294;

	public string DeviceName { get; private set; }

	public string Path { get; private set; }

	public string DevicePath { get; private set; }

	public SpriteAsset LargeIcon { get; private set; }

	public SpriteAsset SmallIcon { get; private set; }

	public string DefaultDisplayName { get; private set; }

	public GameKeyIconInfo(JSONNode _json)
	{
		if (!_json["device_name"].IsString)
		{
			throw new SerializationException();
		}
		DeviceName = _json["device_name"];
		if (!_json["path"].IsString)
		{
			throw new SerializationException();
		}
		Path = _json["path"];
		if (!_json["device_path"].IsString)
		{
			throw new SerializationException();
		}
		DevicePath = _json["device_path"];
		if (!_json["large_icon"].IsObject)
		{
			throw new SerializationException();
		}
		LargeIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["large_icon"]));
		if (!_json["small_icon"].IsObject)
		{
			throw new SerializationException();
		}
		SmallIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["small_icon"]));
		if (!_json["default_display_name"].IsString)
		{
			throw new SerializationException();
		}
		DefaultDisplayName = _json["default_display_name"];
	}

	public GameKeyIconInfo(string device_name, string path, string device_path, SpriteAsset large_icon, SpriteAsset small_icon, string default_display_name)
	{
		DeviceName = device_name;
		Path = path;
		DevicePath = device_path;
		LargeIcon = large_icon;
		SmallIcon = small_icon;
		DefaultDisplayName = default_display_name;
	}

	public static GameKeyIconInfo DeserializeGameKeyIconInfo(JSONNode _json)
	{
		return new GameKeyIconInfo(_json);
	}

	public override int GetTypeId()
	{
		return -788124294;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ DeviceName:" + DeviceName + ",Path:" + Path + ",DevicePath:" + DevicePath + ",LargeIcon:" + LargeIcon?.ToString() + ",SmallIcon:" + SmallIcon?.ToString() + ",DefaultDisplayName:" + DefaultDisplayName + ",}";
	}
}
