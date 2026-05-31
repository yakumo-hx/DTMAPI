using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneSlotInfo : BeanBase
{
	public const int __ID__ = -1431591314;

	public ComponentType Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public SpriteAsset DefaultIcon { get; private set; }

	public DroneSlotInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (ComponentType)_json["id"].AsInt;
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
		if (!_json["default_icon"].IsObject)
		{
			throw new SerializationException();
		}
		DefaultIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["default_icon"]));
	}

	public DroneSlotInfo(ComponentType id, string title, SpriteAsset default_icon)
	{
		Id = id;
		Title = title;
		DefaultIcon = default_icon;
	}

	public static DroneSlotInfo DeserializeDroneSlotInfo(JSONNode _json)
	{
		return new DroneSlotInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1431591314;
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
		return "{ Id:" + Id.ToString() + ",Title:" + Title + ",DefaultIcon:" + DefaultIcon?.ToString() + ",}";
	}
}
