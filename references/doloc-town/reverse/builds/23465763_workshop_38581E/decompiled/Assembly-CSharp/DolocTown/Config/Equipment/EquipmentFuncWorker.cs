using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncWorker : EquipmentFuncEquipment
{
	public WorkerRendererName WorkerRendererName { get; private set; }

	public WorkerRendererName WorkerRendererNameEx { get; private set; }

	public WorkerRendererName WorkerRendererNameLowpower { get; private set; }

	public SpriteAssetArray WorkSprites { get; private set; }

	public SpriteAsset IdleSprite { get; private set; }

	public EquipmentFuncWorker(JSONNode _json)
		: base(_json)
	{
		if (!_json["worker_renderer_name"].IsNumber)
		{
			throw new SerializationException();
		}
		WorkerRendererName = (WorkerRendererName)_json["worker_renderer_name"].AsInt;
		if (!_json["worker_renderer_name_ex"].IsNumber)
		{
			throw new SerializationException();
		}
		WorkerRendererNameEx = (WorkerRendererName)_json["worker_renderer_name_ex"].AsInt;
		if (!_json["worker_renderer_name_lowpower"].IsNumber)
		{
			throw new SerializationException();
		}
		WorkerRendererNameLowpower = (WorkerRendererName)_json["worker_renderer_name_lowpower"].AsInt;
		if (!_json["work_sprites"].IsObject)
		{
			throw new SerializationException();
		}
		WorkSprites = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["work_sprites"]));
		if (!_json["idle_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		IdleSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["idle_sprite"]));
	}

	public EquipmentFuncWorker(WorkerRendererName worker_renderer_name, WorkerRendererName worker_renderer_name_ex, WorkerRendererName worker_renderer_name_lowpower, SpriteAssetArray work_sprites, SpriteAsset idle_sprite)
	{
		WorkerRendererName = worker_renderer_name;
		WorkerRendererNameEx = worker_renderer_name_ex;
		WorkerRendererNameLowpower = worker_renderer_name_lowpower;
		WorkSprites = work_sprites;
		IdleSprite = idle_sprite;
	}

	public static EquipmentFuncWorker DeserializeEquipmentFuncWorker(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncSynthesizerGenerator" => new EquipmentFuncSynthesizerGenerator(_json), 
			"EquipmentFuncSynthesizer" => new EquipmentFuncSynthesizer(_json), 
			"EquipmentFuncSeedCompressor" => new EquipmentFuncSeedCompressor(_json), 
			"EquipmentFuncGeneExtractor" => new EquipmentFuncGeneExtractor(_json), 
			"EquipmentFuncGeneIncubator" => new EquipmentFuncGeneIncubator(_json), 
			"EquipmentFuncGeneReplicator" => new EquipmentFuncGeneReplicator(_json), 
			"EquipmentFuncGeneSynthesizer" => new EquipmentFuncGeneSynthesizer(_json), 
			"EquipmentFuncGarbageShredder" => new EquipmentFuncGarbageShredder(_json), 
			_ => throw new SerializationException(), 
		};
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
		return "{ WorkerRendererName:" + WorkerRendererName.ToString() + ",WorkerRendererNameEx:" + WorkerRendererNameEx.ToString() + ",WorkerRendererNameLowpower:" + WorkerRendererNameLowpower.ToString() + ",WorkSprites:" + WorkSprites?.ToString() + ",IdleSprite:" + IdleSprite?.ToString() + ",}";
	}

	public Sprite GetWorkSprite(int workScale)
	{
		if (WorkSprites.refs.IsNullOrEmpty())
		{
			return null;
		}
		int num = Mathf.Clamp(workScale, 0, WorkSprites.refs.Length - 1);
		return WorkSprites.refs[num].Asset;
	}
}
