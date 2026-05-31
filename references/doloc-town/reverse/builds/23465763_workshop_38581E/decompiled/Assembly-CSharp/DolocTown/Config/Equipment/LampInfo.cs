using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown.Config.Equipment;

public sealed class LampInfo : BeanBase
{
	public const int __ID__ = -484881594;

	public string Id { get; private set; }

	public float Intensity { get; private set; }

	public Color Color { get; private set; }

	public float Range { get; private set; }

	public Vector2 Offset { get; private set; }

	public Light2D.LightType LampType { get; private set; }

	public Color EmissionColor { get; private set; }

	public float EmissionIntensity { get; private set; }

	public SpriteAsset EmissionSpriteAsset { get; private set; }

	public SpriteAsset EmissionRevertSpriteAsset { get; private set; }

	public LampInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["intensity"].IsNumber)
		{
			throw new SerializationException();
		}
		Intensity = _json["intensity"];
		if (!_json["color"].IsObject)
		{
			throw new SerializationException();
		}
		Color = ExternalTypeUtil.ColorConverter(CfgColor.DeserializeCfgColor(_json["color"]));
		if (!_json["range"].IsNumber)
		{
			throw new SerializationException();
		}
		Range = _json["range"];
		if (!_json["offset"].IsObject)
		{
			throw new SerializationException();
		}
		Offset = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["offset"]));
		if (!_json["lamp_type"].IsNumber)
		{
			throw new SerializationException();
		}
		LampType = (Light2D.LightType)_json["lamp_type"].AsInt;
		if (!_json["emission_color"].IsObject)
		{
			throw new SerializationException();
		}
		EmissionColor = ExternalTypeUtil.ColorConverter(CfgHDRColor.DeserializeCfgHDRColor(_json["emission_color"]));
		if (!_json["emission_intensity"].IsNumber)
		{
			throw new SerializationException();
		}
		EmissionIntensity = _json["emission_intensity"];
		if (!_json["emission_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		EmissionSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["emission_sprite_asset"]));
		if (!_json["emission_revert_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		EmissionRevertSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["emission_revert_sprite_asset"]));
	}

	public LampInfo(string id, float intensity, Color color, float range, Vector2 offset, Light2D.LightType lamp_type, Color emission_color, float emission_intensity, SpriteAsset emission_sprite_asset, SpriteAsset emission_revert_sprite_asset)
	{
		Id = id;
		Intensity = intensity;
		Color = color;
		Range = range;
		Offset = offset;
		LampType = lamp_type;
		EmissionColor = emission_color;
		EmissionIntensity = emission_intensity;
		EmissionSpriteAsset = emission_sprite_asset;
		EmissionRevertSpriteAsset = emission_revert_sprite_asset;
	}

	public static LampInfo DeserializeLampInfo(JSONNode _json)
	{
		return new LampInfo(_json);
	}

	public override int GetTypeId()
	{
		return -484881594;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Intensity:" + Intensity + ",Color:" + Color.ToString() + ",Range:" + Range + ",Offset:" + Offset.ToString() + ",LampType:" + LampType.ToString() + ",EmissionColor:" + EmissionColor.ToString() + ",EmissionIntensity:" + EmissionIntensity + ",EmissionSpriteAsset:" + EmissionSpriteAsset?.ToString() + ",EmissionRevertSpriteAsset:" + EmissionRevertSpriteAsset?.ToString() + ",}";
	}
}
