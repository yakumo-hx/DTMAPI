using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncSynthesizerBase : EquipmentFuncWorker
{
	public string RecipeGroupName { get; private set; }

	public RecipeGroupInfo RecipeGroupName_Ref { get; private set; }

	public string DishGroupName { get; private set; }

	public DishGroupInfo DishGroupName_Ref { get; private set; }

	public bool UseConversionMode { get; private set; }

	public EquipmentFuncSynthesizerBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["recipe_group_name"].IsString)
		{
			throw new SerializationException();
		}
		RecipeGroupName = _json["recipe_group_name"];
		if (!_json["dish_group_name"].IsString)
		{
			throw new SerializationException();
		}
		DishGroupName = _json["dish_group_name"];
		if (!_json["use_conversion_mode"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseConversionMode = _json["use_conversion_mode"];
	}

	public EquipmentFuncSynthesizerBase(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, string recipe_group_name, string dish_group_name, bool use_conversion_mode)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite)
	{
		RecipeGroupName = recipe_group_name;
		DishGroupName = dish_group_name;
		UseConversionMode = use_conversion_mode;
	}

	public static EquipmentFuncSynthesizerBase DeserializeEquipmentFuncSynthesizerBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncSynthesizer" => new EquipmentFuncSynthesizer(_json), 
			"EquipmentFuncSeedCompressor" => new EquipmentFuncSeedCompressor(_json), 
			"EquipmentFuncGeneExtractor" => new EquipmentFuncGeneExtractor(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		RecipeGroupName_Ref = (_tables["Recipe.TbRecipeGroup"] as TbRecipeGroup).GetOrDefault(RecipeGroupName);
		DishGroupName_Ref = (_tables["Recipe.TbDishGroup"] as TbDishGroup).GetOrDefault(DishGroupName);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",RecipeGroupName:" + RecipeGroupName + ",DishGroupName:" + DishGroupName + ",UseConversionMode:" + UseConversionMode + ",}";
	}
}
