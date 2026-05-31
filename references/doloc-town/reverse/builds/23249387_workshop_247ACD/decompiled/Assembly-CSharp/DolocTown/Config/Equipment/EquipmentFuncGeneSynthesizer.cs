using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncGeneSynthesizer : EquipmentFuncWorker
{
	public const int __ID__ = -980922367;

	public int Interval { get; private set; }

	public bool AllowCrossSpecies { get; private set; }

	public string DefaultCapsuleItem { get; private set; }

	public ItemInfo DefaultCapsuleItem_Ref { get; private set; }

	public EquipmentFuncGeneSynthesizer(JSONNode _json)
		: base(_json)
	{
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
		if (!_json["allow_cross_species"].IsBoolean)
		{
			throw new SerializationException();
		}
		AllowCrossSpecies = _json["allow_cross_species"];
		if (!_json["default_capsule_item"].IsString)
		{
			throw new SerializationException();
		}
		DefaultCapsuleItem = _json["default_capsule_item"];
	}

	public EquipmentFuncGeneSynthesizer(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, int interval, bool allow_cross_species, string default_capsule_item)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite)
	{
		Interval = interval;
		AllowCrossSpecies = allow_cross_species;
		DefaultCapsuleItem = default_capsule_item;
	}

	public static EquipmentFuncGeneSynthesizer DeserializeEquipmentFuncGeneSynthesizer(JSONNode _json)
	{
		return new EquipmentFuncGeneSynthesizer(_json);
	}

	public override int GetTypeId()
	{
		return -980922367;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		DefaultCapsuleItem_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(DefaultCapsuleItem);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",Interval:" + Interval + ",AllowCrossSpecies:" + AllowCrossSpecies + ",DefaultCapsuleItem:" + DefaultCapsuleItem + ",}";
	}
}
