using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncSprinkler : EquipmentFuncAffector
{
	public const int __ID__ = 1354705334;

	public Vector2 EffectOffset { get; private set; }

	public SpriteAsset OffSprite { get; private set; }

	public EquipmentFuncSprinkler(JSONNode _json)
		: base(_json)
	{
		if (!_json["effect_offset"].IsObject)
		{
			throw new SerializationException();
		}
		EffectOffset = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["effect_offset"]));
		if (!_json["off_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		OffSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["off_sprite"]));
	}

	public EquipmentFuncSprinkler(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int work_interval, Vector2 effect_offset, SpriteAsset off_sprite)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom, work_interval)
	{
		EffectOffset = effect_offset;
		OffSprite = off_sprite;
	}

	public static EquipmentFuncSprinkler DeserializeEquipmentFuncSprinkler(JSONNode _json)
	{
		return new EquipmentFuncSprinkler(_json);
	}

	public override int GetTypeId()
	{
		return 1354705334;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",WorkInterval:" + base.WorkInterval + ",EffectOffset:" + EffectOffset.ToString() + ",OffSprite:" + OffSprite?.ToString() + ",}";
	}
}
