using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncShowCase : EquipmentFuncEquipment
{
	public const int __ID__ = 1043714751;

	public Vector2 IconOffset { get; private set; }

	public bool UseEffects { get; private set; }

	public SpriteAsset ForegroundSprite { get; private set; }

	public EquipmentFuncShowCase(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["icon_offset"];
		if (!jSONNode.IsObject)
		{
			throw new SerializationException();
		}
		if (!jSONNode["x"].IsNumber)
		{
			throw new SerializationException();
		}
		float x = jSONNode["x"];
		if (!jSONNode["y"].IsNumber)
		{
			throw new SerializationException();
		}
		float y = jSONNode["y"];
		IconOffset = new Vector2(x, y);
		if (!_json["use_effects"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseEffects = _json["use_effects"];
		if (!_json["foreground_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		ForegroundSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["foreground_sprite"]));
	}

	public EquipmentFuncShowCase(Vector2 icon_offset, bool use_effects, SpriteAsset foreground_sprite)
	{
		IconOffset = icon_offset;
		UseEffects = use_effects;
		ForegroundSprite = foreground_sprite;
	}

	public static EquipmentFuncShowCase DeserializeEquipmentFuncShowCase(JSONNode _json)
	{
		return new EquipmentFuncShowCase(_json);
	}

	public override int GetTypeId()
	{
		return 1043714751;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ IconOffset:" + IconOffset.ToString() + ",UseEffects:" + UseEffects + ",ForegroundSprite:" + ForegroundSprite?.ToString() + ",}";
	}
}
