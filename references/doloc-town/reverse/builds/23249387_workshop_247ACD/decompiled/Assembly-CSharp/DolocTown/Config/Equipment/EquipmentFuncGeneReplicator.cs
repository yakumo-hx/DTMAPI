using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncGeneReplicator : EquipmentFuncWorker
{
	public const int __ID__ = 167184390;

	public int TotalCapacity { get; private set; }

	public int LineCapacity { get; private set; }

	public int CopyInterval { get; private set; }

	public int ClearInterval { get; private set; }

	public EquipmentFuncGeneReplicator(JSONNode _json)
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
		if (!_json["copy_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		CopyInterval = _json["copy_interval"];
		if (!_json["clear_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		ClearInterval = _json["clear_interval"];
	}

	public EquipmentFuncGeneReplicator(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, int total_capacity, int line_capacity, int copy_interval, int clear_interval)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite)
	{
		TotalCapacity = total_capacity;
		LineCapacity = line_capacity;
		CopyInterval = copy_interval;
		ClearInterval = clear_interval;
	}

	public static EquipmentFuncGeneReplicator DeserializeEquipmentFuncGeneReplicator(JSONNode _json)
	{
		return new EquipmentFuncGeneReplicator(_json);
	}

	public override int GetTypeId()
	{
		return 167184390;
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
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",TotalCapacity:" + TotalCapacity + ",LineCapacity:" + LineCapacity + ",CopyInterval:" + CopyInterval + ",ClearInterval:" + ClearInterval + ",}";
	}
}
