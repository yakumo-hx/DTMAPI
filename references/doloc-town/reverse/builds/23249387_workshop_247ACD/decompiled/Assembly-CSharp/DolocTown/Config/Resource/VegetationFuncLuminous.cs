using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Equipment;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class VegetationFuncLuminous : VegetationFuncBase
{
	public const int __ID__ = -1391739018;

	public SpriteAsset SpriteAsset { get; private set; }

	public string Lamp { get; private set; }

	public LampInfo Lamp_Ref { get; private set; }

	public SwitchScheduleAsset SwitchScheduleAsseet { get; private set; }

	public PrefabAsset IdleEffect { get; private set; }

	public string TouchEffect { get; private set; }

	public Vector2 EffectsOffset { get; private set; }

	public VegetationFuncLuminous(JSONNode _json)
		: base(_json)
	{
		if (!_json["sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_asset"]));
		if (!_json["lamp"].IsString)
		{
			throw new SerializationException();
		}
		Lamp = _json["lamp"];
		if (!_json["switch_schedule_asseet"].IsObject)
		{
			throw new SerializationException();
		}
		SwitchScheduleAsseet = ExternalTypeUtil.SwitchScheduleAssetConverter(CfgSwitchScheduleAsset.DeserializeCfgSwitchScheduleAsset(_json["switch_schedule_asseet"]));
		if (!_json["idle_effect"].IsObject)
		{
			throw new SerializationException();
		}
		IdleEffect = ExternalTypeUtil.PrefabAssetConverter(CfgPrefabAsset.DeserializeCfgPrefabAsset(_json["idle_effect"]));
		if (!_json["touch_effect"].IsString)
		{
			throw new SerializationException();
		}
		TouchEffect = _json["touch_effect"];
		if (!_json["effects_offset"].IsObject)
		{
			throw new SerializationException();
		}
		EffectsOffset = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["effects_offset"]));
	}

	public VegetationFuncLuminous(SpriteAsset sprite_asset, string lamp, SwitchScheduleAsset switch_schedule_asseet, PrefabAsset idle_effect, string touch_effect, Vector2 effects_offset)
	{
		SpriteAsset = sprite_asset;
		Lamp = lamp;
		SwitchScheduleAsseet = switch_schedule_asseet;
		IdleEffect = idle_effect;
		TouchEffect = touch_effect;
		EffectsOffset = effects_offset;
	}

	public static VegetationFuncLuminous DeserializeVegetationFuncLuminous(JSONNode _json)
	{
		return new VegetationFuncLuminous(_json);
	}

	public override int GetTypeId()
	{
		return -1391739018;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		Lamp_Ref = (_tables["Equipment.TbLamp"] as TbLamp).GetOrDefault(Lamp);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpriteAsset:" + SpriteAsset?.ToString() + ",Lamp:" + Lamp + ",SwitchScheduleAsseet:" + SwitchScheduleAsseet?.ToString() + ",IdleEffect:" + IdleEffect?.ToString() + ",TouchEffect:" + TouchEffect + ",EffectsOffset:" + EffectsOffset.ToString() + ",}";
	}
}
