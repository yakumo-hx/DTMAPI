using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncGeneExtractor : EquipmentFuncSynthesizerBase
{
	public const int __ID__ = -1046351139;

	public EquipmentFuncGeneExtractor(JSONNode _json)
		: base(_json)
	{
	}

	public EquipmentFuncGeneExtractor(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, string recipe_group_name, string dish_group_name, bool use_conversion_mode)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite, recipe_group_name, dish_group_name, use_conversion_mode)
	{
	}

	public static EquipmentFuncGeneExtractor DeserializeEquipmentFuncGeneExtractor(JSONNode _json)
	{
		return new EquipmentFuncGeneExtractor(_json);
	}

	public override int GetTypeId()
	{
		return -1046351139;
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
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",RecipeGroupName:" + base.RecipeGroupName + ",DishGroupName:" + base.DishGroupName + ",UseConversionMode:" + base.UseConversionMode + ",}";
	}
}
