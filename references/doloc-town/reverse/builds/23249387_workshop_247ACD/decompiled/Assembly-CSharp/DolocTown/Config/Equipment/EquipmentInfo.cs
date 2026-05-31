using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.GameData;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentInfo : BeanBase
{
	public const int __ID__ = -2070962500;

	public string Id { get; private set; }

	public EquipmentType MenuType { get; private set; }

	public SpriteAsset SceneAsset { get; private set; }

	public SpriteAsset FlipAsset { get; private set; }

	public AnimatorAsset AnimatorAsset { get; private set; }

	public EquipmentEnvType EnvType { get; private set; }

	public EquipmentFitType FitType { get; private set; }

	public Vector2Int CoverSize { get; private set; }

	public string BaseEquipment { get; private set; }

	public bool DisplayLock { get; private set; }

	public bool ShowCompleteInfoLock { get; private set; }

	public EquipmentFuncEquipment Function { get; private set; }

	public ElectronicComponentProto ElectronicComponent { get; private set; }

	public WeatherDecoratorProto[] Processors { get; private set; }

	public DecalSlotData[] FitSlotDatas { get; private set; }

	public DecalSlotData[] ContainedSlotDatas { get; private set; }

	public Sprite Sprite => SceneAsset.Asset;

	public Sprite TurnSprite => FlipAsset.Asset;

	public bool Reversible => TurnSprite != null;

	public Sprite UiSprite => DolocAPI.GetItemSprite(Id);

	public RuntimeAnimatorController animatorController => AnimatorAsset.Asset;

	public bool isElectrical => ElectronicComponent != null;

	public bool IsAppliance => ElectronicComponent is EComProtoAppliance;

	public string Title => DolocAPI.GetItemTitle(Id);

	public string Description => DolocAPI.GetItemDescription(Id);

	public Vector2 SpriteSize
	{
		get
		{
			if (!(Sprite == null))
			{
				return Sprite.rect.size;
			}
			return Vector2.one;
		}
	}

	public Vector2 WorldSpriteSize => SpriteSize * 0.125f;

	public bool Display
	{
		get
		{
			if (!DolocAPI.QueryRecipe(Id, out var _))
			{
				return false;
			}
			if (!DolocAPI.QueryItemProto(Id, out var _))
			{
				return false;
			}
			if (!DolocAPI.archiveHandle.IsEquipmentUnlocked(Id))
			{
				return DisplayLock;
			}
			return true;
		}
	}

	public bool ShowCompleteInfo
	{
		get
		{
			if (!DolocAPI.QueryItemProto(Id, out var _))
			{
				return false;
			}
			return ShowCompleteInfoLock;
		}
	}

	public CountItem[] CostList
	{
		get
		{
			DolocAPI.QueryRecipe(Id, out var recipe);
			return recipe?.InputItems ?? Array.Empty<CountItem>();
		}
	}

	public bool isDecal => !FitSlotDatas.IsNullOrEmpty();

	public bool isDecalHost => !ContainedSlotDatas.IsNullOrEmpty();

	public DecalSlot[] FitSlots { get; private set; }

	public DecalSlot[] ContainedSlots { get; private set; }

	private float pivotX => Sprite.pivot.x / SpriteSize.x;

	private float pivotY => Sprite.pivot.y / SpriteSize.y;

	public EquipmentInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["menu_type"].IsNumber)
		{
			throw new SerializationException();
		}
		MenuType = (EquipmentType)_json["menu_type"].AsInt;
		if (!_json["scene_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SceneAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["scene_asset"]));
		if (!_json["flip_asset"].IsObject)
		{
			throw new SerializationException();
		}
		FlipAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["flip_asset"]));
		if (!_json["animator_asset"].IsObject)
		{
			throw new SerializationException();
		}
		AnimatorAsset = ExternalTypeUtil.AnimatorAssetConverter(CfgAnimatorAsset.DeserializeCfgAnimatorAsset(_json["animator_asset"]));
		if (!_json["env_type"].IsNumber)
		{
			throw new SerializationException();
		}
		EnvType = (EquipmentEnvType)_json["env_type"].AsInt;
		if (!_json["fit_type"].IsNumber)
		{
			throw new SerializationException();
		}
		FitType = (EquipmentFitType)_json["fit_type"].AsInt;
		if (!_json["cover_size"].IsObject)
		{
			throw new SerializationException();
		}
		CoverSize = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["cover_size"]));
		if (!_json["base_equipment"].IsString)
		{
			throw new SerializationException();
		}
		BaseEquipment = _json["base_equipment"];
		if (!_json["display_lock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DisplayLock = _json["display_lock"];
		if (!_json["show_complete_info_lock"].IsBoolean)
		{
			throw new SerializationException();
		}
		ShowCompleteInfoLock = _json["show_complete_info_lock"];
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = EquipmentFuncEquipment.DeserializeEquipmentFuncEquipment(_json["function"]);
		JSONNode jSONNode = _json["electronic_component"];
		if (jSONNode.Tag != JSONNodeType.None && jSONNode.Tag != JSONNodeType.NullValue)
		{
			if (!jSONNode.IsObject)
			{
				throw new SerializationException();
			}
			ElectronicComponent = ElectronicComponentProto.DeserializeElectronicComponentProto(jSONNode);
		}
		else
		{
			ElectronicComponent = null;
		}
		JSONNode jSONNode2 = _json["processors"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode2.Count;
		Processors = new WeatherDecoratorProto[count];
		int num = 0;
		foreach (JSONNode child in jSONNode2.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			WeatherDecoratorProto weatherDecoratorProto = WeatherDecoratorProto.DeserializeWeatherDecoratorProto(child);
			Processors[num++] = weatherDecoratorProto;
		}
		JSONNode jSONNode3 = _json["fit_slot_datas"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode3.Count;
		FitSlotDatas = new DecalSlotData[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode3.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			DecalSlotData decalSlotData = DecalSlotData.DeserializeDecalSlotData(child2);
			FitSlotDatas[num2++] = decalSlotData;
		}
		JSONNode jSONNode4 = _json["contained_slot_datas"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode4.Count;
		ContainedSlotDatas = new DecalSlotData[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode4.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			DecalSlotData decalSlotData2 = DecalSlotData.DeserializeDecalSlotData(child3);
			ContainedSlotDatas[num3++] = decalSlotData2;
		}
	}

	public EquipmentInfo(string id, EquipmentType menu_type, SpriteAsset scene_asset, SpriteAsset flip_asset, AnimatorAsset animator_asset, EquipmentEnvType env_type, EquipmentFitType fit_type, Vector2Int cover_size, string base_equipment, bool display_lock, bool show_complete_info_lock, EquipmentFuncEquipment function, ElectronicComponentProto electronic_component, WeatherDecoratorProto[] processors, DecalSlotData[] fit_slot_datas, DecalSlotData[] contained_slot_datas)
	{
		Id = id;
		MenuType = menu_type;
		SceneAsset = scene_asset;
		FlipAsset = flip_asset;
		AnimatorAsset = animator_asset;
		EnvType = env_type;
		FitType = fit_type;
		CoverSize = cover_size;
		BaseEquipment = base_equipment;
		DisplayLock = display_lock;
		ShowCompleteInfoLock = show_complete_info_lock;
		Function = function;
		ElectronicComponent = electronic_component;
		Processors = processors;
		FitSlotDatas = fit_slot_datas;
		ContainedSlotDatas = contained_slot_datas;
	}

	public static EquipmentInfo DeserializeEquipmentInfo(JSONNode _json)
	{
		return new EquipmentInfo(_json);
	}

	public override int GetTypeId()
	{
		return -2070962500;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Function?.Resolve(_tables);
		ElectronicComponent?.Resolve(_tables);
		WeatherDecoratorProto[] processors = Processors;
		for (int i = 0; i < processors.Length; i++)
		{
			processors[i]?.Resolve(_tables);
		}
		DecalSlotData[] fitSlotDatas = FitSlotDatas;
		for (int i = 0; i < fitSlotDatas.Length; i++)
		{
			fitSlotDatas[i]?.Resolve(_tables);
		}
		fitSlotDatas = ContainedSlotDatas;
		for (int i = 0; i < fitSlotDatas.Length; i++)
		{
			fitSlotDatas[i]?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Function?.TranslateText(translator);
		ElectronicComponent?.TranslateText(translator);
		WeatherDecoratorProto[] processors = Processors;
		for (int i = 0; i < processors.Length; i++)
		{
			processors[i]?.TranslateText(translator);
		}
		DecalSlotData[] fitSlotDatas = FitSlotDatas;
		for (int i = 0; i < fitSlotDatas.Length; i++)
		{
			fitSlotDatas[i]?.TranslateText(translator);
		}
		fitSlotDatas = ContainedSlotDatas;
		for (int i = 0; i < fitSlotDatas.Length; i++)
		{
			fitSlotDatas[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",MenuType:" + MenuType.ToString() + ",SceneAsset:" + SceneAsset?.ToString() + ",FlipAsset:" + FlipAsset?.ToString() + ",AnimatorAsset:" + AnimatorAsset?.ToString() + ",EnvType:" + EnvType.ToString() + ",FitType:" + FitType.ToString() + ",CoverSize:" + CoverSize.ToString() + ",BaseEquipment:" + BaseEquipment + ",DisplayLock:" + DisplayLock + ",ShowCompleteInfoLock:" + ShowCompleteInfoLock + ",Function:" + Function?.ToString() + ",ElectronicComponent:" + ElectronicComponent?.ToString() + ",Processors:" + StringUtil.CollectionToString(Processors) + ",FitSlotDatas:" + StringUtil.CollectionToString(FitSlotDatas) + ",ContainedSlotDatas:" + StringUtil.CollectionToString(ContainedSlotDatas) + ",}";
	}

	private void PostResolve()
	{
		FitSlots = (isDecal ? ConvertDecalSlotData(FitSlotDatas) : null);
		ContainedSlots = (isDecalHost ? ConvertDecalSlotData(ContainedSlotDatas) : null);
	}

	public IEnumerable<Vector2Int> GroundPositions(Vector2Int anchor)
	{
		int y = anchor.y - 1;
		for (int i = 0; i < CoverSize.x; i++)
		{
			yield return new Vector2Int(i + anchor.x, y);
		}
	}

	public Vector2Int[] GetCoveredPositionsBySprite(Vector2 scenePosition, Vector2 decalWorldPos)
	{
		return GetCoveredPositionsBySprite(GetCoveredTileLBRT(scenePosition, decalWorldPos));
	}

	public Vector2Int[] GetCoveredPositions(Vector2Int anchor, Vector2Int size)
	{
		return GetCoveredPositionsBySprite((anchor.x, anchor.y, anchor.x + size.x, anchor.y + size.y));
	}

	public Vector2Int[] GetCoveredPositionsBySprite((int, int, int, int) lbrt)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		var (i, _, _, _) = lbrt;
		for (; i < lbrt.Item3; i++)
		{
			for (int j = lbrt.Item2; j < lbrt.Item4; j++)
			{
				list.Add(new Vector2Int(i, j));
			}
		}
		if (list.Count == 0)
		{
			list.Add(new Vector2Int(lbrt.Item1, lbrt.Item2));
		}
		return list.ToArray();
	}

	public Vector2Int GetCoveredSize(Vector2 decalWorldPos)
	{
		if (isDecal)
		{
			return GetCoveredSizeBySprite(GetCoveredTileLBRT(Vector2.zero, decalWorldPos));
		}
		return CoverSize;
	}

	public Vector2Int GetCoveredSizeBySprite((int, int, int, int) lbrt)
	{
		return new Vector2Int(Mathf.Max(1, lbrt.Item3 - lbrt.Item1), Mathf.Max(1, lbrt.Item4 - lbrt.Item2));
	}

	public (int, int, int, int) GetCoveredTileLBRT(Vector2 scenePosition, Vector2 decalWorldPos)
	{
		float num = decalWorldPos.x - pivotX * Sprite.bounds.size.x;
		float num2 = decalWorldPos.x + (1f - pivotX) * Sprite.bounds.size.x;
		float num3 = decalWorldPos.y - pivotY * Sprite.bounds.size.y;
		float num4 = decalWorldPos.y + (1f - pivotY) * Sprite.bounds.size.y;
		int item = RoundToInt((num - scenePosition.x) / 1.5f, 0.6f);
		int item2 = RoundToInt((num2 - scenePosition.x) / 1.5f, 0.6f);
		int item3 = RoundToInt((num3 - scenePosition.y) / 1.5f, 0.6f);
		int item4 = RoundToInt((num4 - scenePosition.y) / 1.5f, 0.6f);
		return (item, item3, item2, item4);
	}

	private int RoundToInt(float f, float threshold)
	{
		if (!(f - (float)Mathf.FloorToInt(f) < threshold))
		{
			return Mathf.CeilToInt(f);
		}
		return Mathf.FloorToInt(f);
	}

	public Vector2Int GetAnchorTile(Vector2 scenePosition, Vector2 decalWorldPos)
	{
		return GetAnchorTile(GetCoveredTileLBRT(scenePosition, decalWorldPos));
	}

	public Vector2Int GetAnchorTile((int, int, int, int) lbrt)
	{
		return new Vector2Int(lbrt.Item1, lbrt.Item2);
	}

	public Vector2 GetWorldPosition(Vector2 slotWorldPos, Vector2 pixelPivot)
	{
		float x = slotWorldPos.x + (Sprite.pivot.x - pixelPivot.x) / SpriteSize.x * Sprite.bounds.size.x;
		float y = slotWorldPos.y + (Sprite.pivot.y - pixelPivot.y) / SpriteSize.y * Sprite.bounds.size.y;
		return new Vector2(x, y);
	}

	private DecalSlot[] ConvertDecalSlotData(DecalSlotData[] datas)
	{
		if (datas.Length == 0)
		{
			return null;
		}
		DecalSlot[] array = new DecalSlot[datas.Length];
		for (int i = 0; i < datas.Length; i++)
		{
			array[i] = new DecalSlot(i, datas[i].SlotType, datas[i].PixelPivot);
		}
		return array;
	}
}
