using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Equipment;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class VegetationFuncGrowLuminous : VegetationFuncGrowBase
{
	public const int __ID__ = 1010508713;

	public string Lamp { get; private set; }

	public LampInfo Lamp_Ref { get; private set; }

	public SwitchScheduleAsset SwitchScheduleAsseet { get; private set; }

	public VegetationFuncGrowLuminous(JSONNode _json)
		: base(_json)
	{
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
	}

	public VegetationFuncGrowLuminous(Vector2Int growth_value, SpriteAssetArray level_sprites, string lamp, SwitchScheduleAsset switch_schedule_asseet)
		: base(growth_value, level_sprites)
	{
		Lamp = lamp;
		SwitchScheduleAsseet = switch_schedule_asseet;
	}

	public static VegetationFuncGrowLuminous DeserializeVegetationFuncGrowLuminous(JSONNode _json)
	{
		return new VegetationFuncGrowLuminous(_json);
	}

	public override int GetTypeId()
	{
		return 1010508713;
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
		return "{ GrowthValue:" + base.GrowthValue.ToString() + ",LevelSprites:" + base.LevelSprites?.ToString() + ",Lamp:" + Lamp + ",SwitchScheduleAsseet:" + SwitchScheduleAsseet?.ToString() + ",}";
	}
}
