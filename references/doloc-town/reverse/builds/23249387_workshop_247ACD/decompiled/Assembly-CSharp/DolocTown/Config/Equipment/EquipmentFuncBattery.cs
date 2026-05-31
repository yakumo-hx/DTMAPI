using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncBattery : EquipmentFuncEquipment
{
	public const int __ID__ = 1375942875;

	public SpriteAssetArray Appearance { get; private set; }

	public EquipmentFuncBattery(JSONNode _json)
		: base(_json)
	{
		if (!_json["appearance"].IsObject)
		{
			throw new SerializationException();
		}
		Appearance = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["appearance"]));
	}

	public EquipmentFuncBattery(SpriteAssetArray appearance)
	{
		Appearance = appearance;
	}

	public static EquipmentFuncBattery DeserializeEquipmentFuncBattery(JSONNode _json)
	{
		return new EquipmentFuncBattery(_json);
	}

	public override int GetTypeId()
	{
		return 1375942875;
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
		return "{ Appearance:" + Appearance?.ToString() + ",}";
	}
}
