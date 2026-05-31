using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncLivestockNursery : EquipmentFuncEquipment
{
	public const int __ID__ = 1846265356;

	public SpriteAsset SpriteInUse { get; private set; }

	public EquipmentFuncLivestockNursery(JSONNode _json)
		: base(_json)
	{
		if (!_json["sprite_in_use"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteInUse = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_in_use"]));
	}

	public EquipmentFuncLivestockNursery(SpriteAsset sprite_in_use)
	{
		SpriteInUse = sprite_in_use;
	}

	public static EquipmentFuncLivestockNursery DeserializeEquipmentFuncLivestockNursery(JSONNode _json)
	{
		return new EquipmentFuncLivestockNursery(_json);
	}

	public override int GetTypeId()
	{
		return 1846265356;
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
		return "{ SpriteInUse:" + SpriteInUse?.ToString() + ",}";
	}
}
