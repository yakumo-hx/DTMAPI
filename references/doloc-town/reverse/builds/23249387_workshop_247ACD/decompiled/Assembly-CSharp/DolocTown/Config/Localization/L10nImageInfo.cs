using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Localization;

public sealed class L10nImageInfo : BeanBase
{
	public const int __ID__ = 1078316817;

	public string ImgId { get; private set; }

	public string LanguageId { get; private set; }

	public LocalizationInfo LanguageId_Ref { get; private set; }

	public SpriteAsset SpriteAsset { get; private set; }

	public L10nImageInfo(JSONNode _json)
	{
		if (!_json["img_id"].IsString)
		{
			throw new SerializationException();
		}
		ImgId = _json["img_id"];
		if (!_json["language_id"].IsString)
		{
			throw new SerializationException();
		}
		LanguageId = _json["language_id"];
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
	}

	public L10nImageInfo(string img_id, string language_id, SpriteAsset sprite_asset)
	{
		ImgId = img_id;
		LanguageId = language_id;
		SpriteAsset = sprite_asset;
	}

	public static L10nImageInfo DeserializeL10nImageInfo(JSONNode _json)
	{
		return new L10nImageInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1078316817;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		LanguageId_Ref = (_tables["Localization.TbLocalization"] as TbLocalization).GetOrDefault(LanguageId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ ImgId:" + ImgId + ",LanguageId:" + LanguageId + ",SpriteAsset:" + SpriteAsset?.ToString() + ",}";
	}
}
