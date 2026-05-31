using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.UI;

public sealed class MainMenuInfo : BeanBase
{
	public const int __ID__ = -1676014400;

	public MenuType Id { get; private set; }

	public SpriteAsset IconAsset { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public MainMenuInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (MenuType)_json["id"].AsInt;
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
	}

	public MainMenuInfo(MenuType id, SpriteAsset icon_asset, string title)
	{
		Id = id;
		IconAsset = icon_asset;
		Title = title;
	}

	public static MainMenuInfo DeserializeMainMenuInfo(JSONNode _json)
	{
		return new MainMenuInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1676014400;
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
		return "{ Id:" + Id.ToString() + ",IconAsset:" + IconAsset?.ToString() + ",Title:" + Title + ",}";
	}
}
