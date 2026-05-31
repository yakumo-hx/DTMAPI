using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Equipment;
using DolocTown.GameData;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class ResourceInfo : BeanBase, ISpawnedItem
{
	public const int __ID__ = 100497340;

	public string Id { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public DungeonResourceType ResourceType { get; private set; }

	public ResourceTypeInfo ResourceType_Ref { get; private set; }

	public Vector2Int Size { get; private set; }

	public Vector2Int PixelOffset { get; private set; }

	public int[] SpawnMonths { get; private set; }

	public ResourceLevelData[] LevelDatas { get; private set; }

	public DecalSlotData[] FitSlotDatas { get; private set; }

	public DecalSlotData[] ContainedSlotDatas { get; private set; }

	public string ResinCollectorOutput { get; private set; }

	public ResinCollectorOutputInfo ResinCollectorOutput_Ref { get; private set; }

	public string SpawnId => Id;

	public int Volume => Width;

	public int Width => Size.x;

	public DecalSlot[] ContainedSlots { get; private set; }

	public DungeonResourceClass ResourceClass => ResourceType_Ref.ResourceClass;

	public int MaxLevel => LevelDatas.Length - 1;

	public TerrainLayerName TerrainLayer { get; private set; }

	public ResourceInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["resource_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ResourceType = (DungeonResourceType)_json["resource_type"].AsInt;
		if (!_json["size"].IsObject)
		{
			throw new SerializationException();
		}
		Size = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["size"]));
		if (!_json["pixel_offset"].IsObject)
		{
			throw new SerializationException();
		}
		PixelOffset = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["pixel_offset"]));
		JSONNode jSONNode = _json["spawn_months"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		SpawnMonths = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			SpawnMonths[num++] = num2;
		}
		JSONNode jSONNode2 = _json["level_datas"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		LevelDatas = new ResourceLevelData[count2];
		int num3 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			ResourceLevelData resourceLevelData = ResourceLevelData.DeserializeResourceLevelData(child2);
			LevelDatas[num3++] = resourceLevelData;
		}
		JSONNode jSONNode3 = _json["fit_slot_datas"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		FitSlotDatas = new DecalSlotData[count3];
		int num4 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			DecalSlotData decalSlotData = DecalSlotData.DeserializeDecalSlotData(child3);
			FitSlotDatas[num4++] = decalSlotData;
		}
		JSONNode jSONNode4 = _json["contained_slot_datas"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		ContainedSlotDatas = new DecalSlotData[count4];
		int num5 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			DecalSlotData decalSlotData2 = DecalSlotData.DeserializeDecalSlotData(child4);
			ContainedSlotDatas[num5++] = decalSlotData2;
		}
		if (!_json["resin_collector_output"].IsString)
		{
			throw new SerializationException();
		}
		ResinCollectorOutput = _json["resin_collector_output"];
	}

	public ResourceInfo(string id, bool default_unlock, DungeonResourceType resource_type, Vector2Int size, Vector2Int pixel_offset, int[] spawn_months, ResourceLevelData[] level_datas, DecalSlotData[] fit_slot_datas, DecalSlotData[] contained_slot_datas, string resin_collector_output)
	{
		Id = id;
		DefaultUnlock = default_unlock;
		ResourceType = resource_type;
		Size = size;
		PixelOffset = pixel_offset;
		SpawnMonths = spawn_months;
		LevelDatas = level_datas;
		FitSlotDatas = fit_slot_datas;
		ContainedSlotDatas = contained_slot_datas;
		ResinCollectorOutput = resin_collector_output;
	}

	public static ResourceInfo DeserializeResourceInfo(JSONNode _json)
	{
		return new ResourceInfo(_json);
	}

	public override int GetTypeId()
	{
		return 100497340;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ResourceType_Ref = (_tables["Resource.TbResourceType"] as TbResourceType).GetOrDefault(ResourceType);
		ResourceLevelData[] levelDatas = LevelDatas;
		for (int i = 0; i < levelDatas.Length; i++)
		{
			levelDatas[i]?.Resolve(_tables);
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
		ResinCollectorOutput_Ref = (_tables["Resource.TbResinCollectorOutput"] as TbResinCollectorOutput).GetOrDefault(ResinCollectorOutput);
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ResourceLevelData[] levelDatas = LevelDatas;
		for (int i = 0; i < levelDatas.Length; i++)
		{
			levelDatas[i]?.TranslateText(translator);
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
		return "{ Id:" + Id + ",DefaultUnlock:" + DefaultUnlock + ",ResourceType:" + ResourceType.ToString() + ",Size:" + Size.ToString() + ",PixelOffset:" + PixelOffset.ToString() + ",SpawnMonths:" + StringUtil.CollectionToString(SpawnMonths) + ",LevelDatas:" + StringUtil.CollectionToString(LevelDatas) + ",FitSlotDatas:" + StringUtil.CollectionToString(FitSlotDatas) + ",ContainedSlotDatas:" + StringUtil.CollectionToString(ContainedSlotDatas) + ",ResinCollectorOutput:" + ResinCollectorOutput + ",}";
	}

	private void PostResolve()
	{
		TerrainLayer = ResourceType_Ref.TerrainLayer.ConvertToEnumOrDefault<TerrainLayerName>();
		ContainedSlots = ContainedSlotDatas.ConvertDecalSlotData();
	}

	public ResourceLevelData GetLevelData(int level)
	{
		if (level < 0 || level > MaxLevel)
		{
			Debug.LogError($"等级<{level}>超出资源等级配置范围！");
		}
		return LevelDatas[Mathf.Clamp(level, 0, MaxLevel)];
	}

	public int RandomSkinIndex()
	{
		ResourceLevelData levelData = GetLevelData(0);
		return UnityEngine.Random.Range(0, levelData.Skins.Length);
	}

	public bool CheckMatchMonth(int month)
	{
		if (!SpawnMonths.IsNullOrEmpty())
		{
			return SpawnMonths.Contains(month);
		}
		return true;
	}
}
