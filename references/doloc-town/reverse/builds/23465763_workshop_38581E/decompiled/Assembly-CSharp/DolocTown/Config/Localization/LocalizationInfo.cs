using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class LocalizationInfo : BeanBase
{
	public const int __ID__ = -239181412;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string DialogueL10nId { get; private set; }

	public string SteamL10nId { get; private set; }

	public TmpFontAsset TmpFont { get; private set; }

	public FontAsset Font { get; private set; }

	public TmpFontAsset TmpFont12px { get; private set; }

	public FontAsset Font12px { get; private set; }

	public TmpFontAsset TmpFont10px { get; private set; }

	public FontAsset Font10px { get; private set; }

	public LocalizationInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"];
		if (!_json["dialogue_l10n_id"].IsString)
		{
			throw new SerializationException();
		}
		DialogueL10nId = _json["dialogue_l10n_id"];
		if (!_json["steam_l10n_id"].IsString)
		{
			throw new SerializationException();
		}
		SteamL10nId = _json["steam_l10n_id"];
		if (!_json["tmp_font"].IsObject)
		{
			throw new SerializationException();
		}
		TmpFont = ExternalTypeUtil.TmpFontAssetConverter(CfgTmpFontAsset.DeserializeCfgTmpFontAsset(_json["tmp_font"]));
		if (!_json["font"].IsObject)
		{
			throw new SerializationException();
		}
		Font = ExternalTypeUtil.FontAssetConverter(CfgFontAsset.DeserializeCfgFontAsset(_json["font"]));
		if (!_json["tmp_font_12px"].IsObject)
		{
			throw new SerializationException();
		}
		TmpFont12px = ExternalTypeUtil.TmpFontAssetConverter(CfgTmpFontAsset.DeserializeCfgTmpFontAsset(_json["tmp_font_12px"]));
		if (!_json["font_12px"].IsObject)
		{
			throw new SerializationException();
		}
		Font12px = ExternalTypeUtil.FontAssetConverter(CfgFontAsset.DeserializeCfgFontAsset(_json["font_12px"]));
		if (!_json["tmp_font_10px"].IsObject)
		{
			throw new SerializationException();
		}
		TmpFont10px = ExternalTypeUtil.TmpFontAssetConverter(CfgTmpFontAsset.DeserializeCfgTmpFontAsset(_json["tmp_font_10px"]));
		if (!_json["font_10px"].IsObject)
		{
			throw new SerializationException();
		}
		Font10px = ExternalTypeUtil.FontAssetConverter(CfgFontAsset.DeserializeCfgFontAsset(_json["font_10px"]));
	}

	public LocalizationInfo(string id, string title, string dialogue_l10n_id, string steam_l10n_id, TmpFontAsset tmp_font, FontAsset font, TmpFontAsset tmp_font_12px, FontAsset font_12px, TmpFontAsset tmp_font_10px, FontAsset font_10px)
	{
		Id = id;
		Title = title;
		DialogueL10nId = dialogue_l10n_id;
		SteamL10nId = steam_l10n_id;
		TmpFont = tmp_font;
		Font = font;
		TmpFont12px = tmp_font_12px;
		Font12px = font_12px;
		TmpFont10px = tmp_font_10px;
		Font10px = font_10px;
	}

	public static LocalizationInfo DeserializeLocalizationInfo(JSONNode _json)
	{
		return new LocalizationInfo(_json);
	}

	public override int GetTypeId()
	{
		return -239181412;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",DialogueL10nId:" + DialogueL10nId + ",SteamL10nId:" + SteamL10nId + ",TmpFont:" + TmpFont?.ToString() + ",Font:" + Font?.ToString() + ",TmpFont12px:" + TmpFont12px?.ToString() + ",Font12px:" + Font12px?.ToString() + ",TmpFont10px:" + TmpFont10px?.ToString() + ",Font10px:" + Font10px?.ToString() + ",}";
	}
}
