using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Weather;

public sealed class WeatherInfo : BeanBase
{
	public const int __ID__ = -641267832;

	private static WeatherInfo _default;

	public WeatherType Id { get; private set; }

	public float Sun { get; private set; }

	public float Water { get; private set; }

	public float Wind { get; private set; }

	public bool IsMalignantWeather { get; private set; }

	public bool IsRainy { get; private set; }

	public bool IsWindy { get; private set; }

	public SpriteAsset UiSpriteAsset { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public static WeatherInfo Default
	{
		get
		{
			if (_default != null)
			{
				return _default;
			}
			_default = DolocConfig.Tables.TbWeather.GetWeatherInfo(WeatherType.SUNNY);
			return _default;
		}
	}

	public WeatherInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (WeatherType)_json["id"].AsInt;
		if (!_json["sun"].IsNumber)
		{
			throw new SerializationException();
		}
		Sun = _json["sun"];
		if (!_json["water"].IsNumber)
		{
			throw new SerializationException();
		}
		Water = _json["water"];
		if (!_json["wind"].IsNumber)
		{
			throw new SerializationException();
		}
		Wind = _json["wind"];
		if (!_json["is_malignant_weather"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsMalignantWeather = _json["is_malignant_weather"];
		if (!_json["is_rainy"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsRainy = _json["is_rainy"];
		if (!_json["is_windy"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsWindy = _json["is_windy"];
		if (!_json["ui_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		UiSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["ui_sprite_asset"]));
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

	public WeatherInfo(WeatherType id, float sun, float water, float wind, bool is_malignant_weather, bool is_rainy, bool is_windy, SpriteAsset ui_sprite_asset, string title, string description)
	{
		Id = id;
		Sun = sun;
		Water = water;
		Wind = wind;
		IsMalignantWeather = is_malignant_weather;
		IsRainy = is_rainy;
		IsWindy = is_windy;
		UiSpriteAsset = ui_sprite_asset;
		Title = title;
		Description = description;
	}

	public static WeatherInfo DeserializeWeatherInfo(JSONNode _json)
	{
		return new WeatherInfo(_json);
	}

	public override int GetTypeId()
	{
		return -641267832;
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
		return "{ Id:" + Id.ToString() + ",Sun:" + Sun + ",Water:" + Water + ",Wind:" + Wind + ",IsMalignantWeather:" + IsMalignantWeather + ",IsRainy:" + IsRainy + ",IsWindy:" + IsWindy + ",UiSpriteAsset:" + UiSpriteAsset?.ToString() + ",Title:" + Title + ",Description:" + Description + ",}";
	}
}
