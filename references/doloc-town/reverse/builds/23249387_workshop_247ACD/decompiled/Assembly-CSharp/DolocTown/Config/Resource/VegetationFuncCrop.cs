using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class VegetationFuncCrop : VegetationFuncGrowBase
{
	public const int __ID__ = 318189698;

	public VegetationFuncCrop(JSONNode _json)
		: base(_json)
	{
	}

	public VegetationFuncCrop(Vector2Int growth_value, SpriteAssetArray level_sprites)
		: base(growth_value, level_sprites)
	{
	}

	public static VegetationFuncCrop DeserializeVegetationFuncCrop(JSONNode _json)
	{
		return new VegetationFuncCrop(_json);
	}

	public override int GetTypeId()
	{
		return 318189698;
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
