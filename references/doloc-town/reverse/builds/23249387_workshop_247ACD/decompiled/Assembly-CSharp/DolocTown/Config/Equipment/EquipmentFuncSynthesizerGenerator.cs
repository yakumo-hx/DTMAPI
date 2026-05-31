using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncSynthesizerGenerator : EquipmentFuncWorker
{
	public const int __ID__ = 1832972829;

	public string OutputItemName { get; private set; }

	public ItemInfo OutputItemName_Ref { get; private set; }

	public int Interval { get; private set; }

	public EquipmentFuncSynthesizerGenerator(JSONNode _json)
		: base(_json)
	{
		if (!_json["output_item_name"].IsString)
		{
			throw new SerializationException();
		}
		OutputItemName = _json["output_item_name"];
		if (!_json["interval"].IsNumber)
		{
			throw new SerializationException();
		}
		Interval = _json["interval"];
	}

	public EquipmentFuncSynthesizerGenerator(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite, string output_item_name, int interval)
		: base(worker_renderer_name, worker_renderer_name_ex, worker_renderer_name_lowpower, work_sprites, idle_sprite)
	{
		OutputItemName = output_item_name;
		Interval = interval;
	}

	public static EquipmentFuncSynthesizerGenerator DeserializeEquipmentFuncSynthesizerGenerator(JSONNode _json)
	{
		return new EquipmentFuncSynthesizerGenerator(_json);
	}

	public override int GetTypeId()
	{
		return 1832972829;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		OutputItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(OutputItemName);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ WorkerRendererName:" + base.WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + base.WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + base.WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + base.WorkSprites?.ToString() + ",IdleSprite:" + base.IdleSprite?.ToString() + ",OutputItemName:" + OutputItemName + ",Interval:" + Interval + ",}";
	}
}
