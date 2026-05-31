using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFarmLight : EquipmentFuncAffector
{
	public const int __ID__ = -1207489074;

	public string Lamp { get; private set; }

	public LampInfo Lamp_Ref { get; private set; }

	public SpriteAsset OffSprite { get; private set; }

	public EquipmentFuncFarmLight(JSONNode _json)
		: base(_json)
	{
		if (!_json["lamp"].IsString)
		{
			throw new SerializationException();
		}
		Lamp = _json["lamp"];
		if (!_json["off_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		OffSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["off_sprite"]));
	}

	public EquipmentFuncFarmLight(int horizontal_range, int vertical_range_top, int vertical_range_bottom, int work_interval, string lamp, SpriteAsset off_sprite)
		: base(horizontal_range, vertical_range_top, vertical_range_bottom, work_interval)
	{
		Lamp = lamp;
		OffSprite = off_sprite;
	}

	public static EquipmentFuncFarmLight DeserializeEquipmentFuncFarmLight(JSONNode _json)
	{
		return new EquipmentFuncFarmLight(_json);
	}

	public override int GetTypeId()
	{
		return -1207489074;
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
		return "{ HorizontalRange:" + base.HorizontalRange + ",VerticalRangeTop:" + base.VerticalRangeTop + ",VerticalRangeBottom:" + base.VerticalRangeBottom + ",WorkInterval:" + base.WorkInterval + ",Lamp:" + Lamp + ",OffSprite:" + OffSprite?.ToString() + ",}";
	}
}
