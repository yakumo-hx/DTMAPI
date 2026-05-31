using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Recipe;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncGarbageShredder : EquipmentFuncWorker
{
	public const int __ID__ = 1398314136;

	public bool InFarm { get; private set; }

	public int GoldCost { get; private set; }

	public float RecycleFactor { get; private set; }

	public string RecipeGroupName { get; private set; }

	public DismantleRecipeGroupInfo RecipeGroupName_Ref { get; private set; }

	public int Interval { get; private set; }

	public EquipmentFuncGarbageShredder(JSONNode _json)
		: base(_json)
	{
		if (!_json["in_farm"].IsBoolean)
		{
			throw new SerializationException();
		}
		InFarm = _json["in_farm"];
		if (!_json["gold_cost"].IsNumber)
		{
			throw new SerializationException();
		}
		GoldCost = _json["gold_cost"];
		if (!_json["recycle_factor"].IsNumber)
		{
			throw new SerializationException();
		}
		RecycleFactor = _json["recycle_factor"];
		if (!_json["recipe_group_name"].IsString)
		{
			throw new SerializationException();
		}
		RecipeGroupName = _json["recipe_group_name"];
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
	}

	public EquipmentFuncGarbageShredder(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, bool in_farm, int gold_cost, float recycle_factor, string recipe_group_name, int interval)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite)
	{
		InFarm = in_farm;
		GoldCost = gold_cost;
		RecycleFactor = recycle_factor;
		RecipeGroupName = recipe_group_name;
		Interval = interval;
	}

	public static EquipmentFuncGarbageShredder DeserializeEquipmentFuncGarbageShredder(JSONNode _json)
	{
		return new EquipmentFuncGarbageShredder(_json);
	}

	public override int GetTypeId()
	{
		return 1398314136;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		RecipeGroupName_Ref = (_tables["Recipe.TbDismantleRecipeGroup"] as TbDismantleRecipeGroup).GetOrDefault(RecipeGroupName);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",InFarm:" + InFarm + ",GoldCost:" + GoldCost + ",RecycleFactor:" + RecycleFactor + ",RecipeGroupName:" + RecipeGroupName + ",Interval:" + Interval + ",}";
	}
}
