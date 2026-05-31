using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class VegetationFuncDandelion : VegetationFuncGrowBase
{
	public const int __ID__ = 1619974204;

	public string EffectsName { get; private set; }

	public Vector2 EffectsOffset { get; private set; }

	public VegetationFuncDandelion(JSONNode _json)
		: base(_json)
	{
		if (!_json["effects_name"].IsString)
		{
			throw new SerializationException();
		}
		EffectsName = _json["effects_name"];
		if (!_json["effects_offset"].IsObject)
		{
			throw new SerializationException();
		}
		EffectsOffset = ExternalTypeUtil.Vector2Converter(CfgVector2.DeserializeCfgVector2(_json["effects_offset"]));
	}

	public VegetationFuncDandelion(Vector2Int growth_value, SpriteAssetArray level_sprites, string effects_name, Vector2 effects_offset)
		: base(growth_value, level_sprites)
	{
		EffectsName = effects_name;
		EffectsOffset = effects_offset;
	}

	public static VegetationFuncDandelion DeserializeVegetationFuncDandelion(JSONNode _json)
	{
		return new VegetationFuncDandelion(_json);
	}

	public override int GetTypeId()
	{
		return 1619974204;
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
		return "{ GrowthValue:" + base.GrowthValue.ToString() + ",LevelSprites:" + base.LevelSprites?.ToString() + ",EffectsName:" + EffectsName + ",EffectsOffset:" + EffectsOffset.ToString() + ",}";
	}
}
