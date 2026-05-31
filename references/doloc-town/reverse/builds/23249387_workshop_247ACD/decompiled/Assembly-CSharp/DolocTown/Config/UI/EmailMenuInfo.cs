using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class EmailMenuInfo : BeanBase
{
	public const int __ID__ = -1270031249;

	public EmailMenuType Id { get; private set; }

	public SpriteAsset IconAsset { get; private set; }

	public string MenuTitle { get; private set; }

	public string MenuTitle_l10n_key { get; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public EmailMenuInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (EmailMenuType)_json["id"].AsInt;
		if (!_json["icon_asset"].IsObject)
		{
			throw new SerializationException();
		}
		IconAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon_asset"]));
		if (!_json["menu_title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		MenuTitle_l10n_key = _json["menu_title"]["key"];
		if (!_json["menu_title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		MenuTitle = _json["menu_title"]["text"];
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
	}

	public EmailMenuInfo(EmailMenuType id, SpriteAsset icon_asset, string menu_title, string title)
	{
		Id = id;
		IconAsset = icon_asset;
		MenuTitle = menu_title;
		Title = title;
	}

	public static EmailMenuInfo DeserializeEmailMenuInfo(JSONNode _json)
	{
		return new EmailMenuInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1270031249;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		MenuTitle = translator(MenuTitle_l10n_key, MenuTitle);
		Title = translator(Title_l10n_key, Title);
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",IconAsset:" + IconAsset?.ToString() + ",MenuTitle:" + MenuTitle + ",Title:" + Title + ",}";
	}
}
