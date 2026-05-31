using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemAutomationTypeInfo : BeanBase
{
	public const int __ID__ = -2044837139;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public SpriteAsset SpriteAsset { get; private set; }

	public ItemAutomationTypeInfo(JSONNode _json)
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
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
	}

	public ItemAutomationTypeInfo(string id, string title, SpriteAsset ui_sprite_asset, SpriteAsset sprite_asset)
	{
		Id = id;
		Title = title;
		UiSpriteAsset = ui_sprite_asset;
		SpriteAsset = sprite_asset;
	}

	public static ItemAutomationTypeInfo DeserializeItemAutomationTypeInfo(JSONNode _json)
	{
		return new ItemAutomationTypeInfo(_json);
	}

	public override int GetTypeId()
	{
		return -2044837139;
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
		return "{ Id:" + Id + ",Title:" + Title + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",SpriteAsset:" + SpriteAsset?.ToString() + ",}";
	}
}
