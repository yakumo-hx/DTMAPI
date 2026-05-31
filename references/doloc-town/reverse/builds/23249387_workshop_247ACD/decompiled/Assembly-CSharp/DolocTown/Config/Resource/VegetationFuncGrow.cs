using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class VegetationFuncGrow : VegetationFuncGrowBase
{
	public const int __ID__ = 318308869;

	public VegetationFuncGrow(JSONNode _json)
		: base(_json)
	{
	}

	public VegetationFuncGrow(Vector2Int growth_value, SpriteAssetArray level_sprites)
		: base(growth_value, level_sprites)
	{
	}

	public static VegetationFuncGrow DeserializeVegetationFuncGrow(JSONNode _json)
	{
		return new VegetationFuncGrow(_json);
	}

	public override int GetTypeId()
	{
		return 318308869;
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
		return "{ GrowthValue:" + base.GrowthValue.ToString() + ",LevelSprites:" + base.LevelSprites?.ToString() + ",}";
	}
}
