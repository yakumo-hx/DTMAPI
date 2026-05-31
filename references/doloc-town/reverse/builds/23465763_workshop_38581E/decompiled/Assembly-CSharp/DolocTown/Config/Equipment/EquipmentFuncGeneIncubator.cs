using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncGeneIncubator : EquipmentFuncWorker
{
	public const int __ID__ = -2050377862;

	public int TotalCapacity { get; private set; }

	public int LineCapacity { get; private set; }

	public int IncubationThreshold { get; private set; }

	public EquipmentFuncGeneIncubator(JSONNode _json)
		: base(_json)
	{
		if (!_json["total_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		TotalCapacity = _json["total_capacity"];
		if (!_json["line_capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		LineCapacity = _json["line_capacity"];
		if (!_json["incubation_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		IncubationThreshold = _json["incubation_threshold"];
	}

	public EquipmentFuncGeneIncubator(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, int total_capacity, int line_capacity, int incubation_threshold)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite)
	{
		TotalCapacity = total_capacity;
		LineCapacity = line_capacity;
		IncubationThreshold = incubation_threshold;
	}

	public static EquipmentFuncGeneIncubator DeserializeEquipmentFuncGeneIncubator(JSONNode _json)
	{
		return new EquipmentFuncGeneIncubator(_json);
	}

	public override int GetTypeId()
	{
		return -2050377862;
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
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",TotalCapacity:" + TotalCapacity + ",LineCapacity:" + LineCapacity + ",IncubationThreshold:" + IncubationThreshold + ",}";
	}
}
