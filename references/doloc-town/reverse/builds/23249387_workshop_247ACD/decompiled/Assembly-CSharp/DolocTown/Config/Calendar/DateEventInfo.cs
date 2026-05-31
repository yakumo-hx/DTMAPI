using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Calendar;

public sealed class DateEventInfo : BeanBase
{
	public const int __ID__ = 2035740586;

	public string Id { get; private set; }

	public SpecialEventType Type { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public SpriteAsset IconAsset { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public DateEventInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["type"].IsNumber)
		{
			throw new SerializationException();
		}
		Type = (SpecialEventType)_json["type"].AsInt;
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["icon_asset"].IsObject)
		{
			throw new SerializationException();
		}
		IconAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon_asset"]));
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
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
	}

	public DateEventInfo(string id, SpecialEventType type, bool default_unlock, SpriteAsset icon_asset, string title, string description)
	{
		Id = id;
		Type = type;
		DefaultUnlock = default_unlock;
		IconAsset = icon_asset;
		Title = title;
		Description = description;
	}

	public static DateEventInfo DeserializeDateEventInfo(JSONNode _json)
	{
		return new DateEventInfo(_json);
	}

	public override int GetTypeId()
	{
		return 2035740586;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Type:" + Type.ToString() + ",DefaultUnlock:" + DefaultUnlock + ",IconAsset:" + IconAsset?.ToString() + ",Title:" + Title + ",Description:" + Description + ",}";
	}
}
