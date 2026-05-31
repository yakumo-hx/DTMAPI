using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using DolocTown.Config.TechTree;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class ResourceLevelData : BeanBase
{
	public const int __ID__ = -622050560;

	public int MaxHealth { get; private set; }

	public Vector2Int GrowthValue { get; private set; }

	public ItemSpawnEntry DropSpawnEntry { get; private set; }

	public TechPointAdder[] TechPoints { get; private set; }

	public int BulletLevelConstraint { get; private set; }

	public ToolConstraint[] ToolConstraints { get; private set; }

	public SpriteAsset[] Skins { get; private set; }

	public PrefabAsset[] SubPrefabs { get; private set; }

	public bool CanGrow => GrowthValue.y > 0;

	public ResourceLevelData(JSONNode _json)
	{
		if (!_json["max_health"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxHealth = _json["max_health"];
		if (!_json["growth_value"].IsObject)
		{
			throw new SerializationException();
		}
		GrowthValue = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["growth_value"]));
		if (!_json["drop_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		DropSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["drop_spawn_entry"]);
		JSONNode jSONNode = _json["tech_points"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		TechPoints = new TechPointAdder[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TechPointAdder techPointAdder = TechPointAdder.DeserializeTechPointAdder(child);
			TechPoints[num++] = techPointAdder;
		}
		if (!_json["bullet_level_constraint"].IsNumber)
		{
			throw new SerializationException();
		}
		BulletLevelConstraint = _json["bullet_level_constraint"];
		JSONNode jSONNode2 = _json["tool_constraints"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		ToolConstraints = new ToolConstraint[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			ToolConstraint toolConstraint = ToolConstraint.DeserializeToolConstraint(child2);
			ToolConstraints[num2++] = toolConstraint;
		}
		JSONNode jSONNode3 = _json["skins"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		Skins = new SpriteAsset[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			SpriteAsset spriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(child3));
			Skins[num3++] = spriteAsset;
		}
		JSONNode jSONNode4 = _json["sub_prefabs"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		SubPrefabs = new PrefabAsset[count4];
		int num4 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			PrefabAsset prefabAsset = ExternalTypeUtil.PrefabAssetConverter(CfgPrefabAsset.DeserializeCfgPrefabAsset(child4));
			SubPrefabs[num4++] = prefabAsset;
		}
	}

	public ResourceLevelData(int max_health, Vector2Int growth_value, ItemSpawnEntry drop_spawn_entry, TechPointAdder[] tech_points, int bullet_level_constraint, ToolConstraint[] tool_constraints, SpriteAsset[] skins, PrefabAsset[] sub_prefabs)
	{
		MaxHealth = max_health;
		GrowthValue = growth_value;
		DropSpawnEntry = drop_spawn_entry;
		TechPoints = tech_points;
		BulletLevelConstraint = bullet_level_constraint;
		ToolConstraints = tool_constraints;
		Skins = skins;
		SubPrefabs = sub_prefabs;
	}

	public static ResourceLevelData DeserializeResourceLevelData(JSONNode _json)
	{
		return new ResourceLevelData(_json);
	}

	public override int GetTypeId()
	{
		return -622050560;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		DropSpawnEntry?.Resolve(_tables);
		TechPointAdder[] techPoints = TechPoints;
		for (int i = 0; i < techPoints.Length; i++)
		{
			techPoints[i]?.Resolve(_tables);
		}
		ToolConstraint[] toolConstraints = ToolConstraints;
		for (int i = 0; i < toolConstraints.Length; i++)
		{
			toolConstraints[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		DropSpawnEntry?.TranslateText(translator);
		TechPointAdder[] techPoints = TechPoints;
		for (int i = 0; i < techPoints.Length; i++)
		{
			techPoints[i]?.TranslateText(translator);
		}
		ToolConstraint[] toolConstraints = ToolConstraints;
		for (int i = 0; i < toolConstraints.Length; i++)
		{
			toolConstraints[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ MaxHealth:" + MaxHealth + ",GrowthValue:" + GrowthValue.ToString() + ",DropSpawnEntry:" + DropSpawnEntry?.ToString() + ",TechPoints:" + StringUtil.CollectionToString(TechPoints) + ",BulletLevelConstraint:" + BulletLevelConstraint + ",ToolConstraints:" + StringUtil.CollectionToString(ToolConstraints) + ",Skins:" + StringUtil.CollectionToString(Skins) + ",SubPrefabs:" + StringUtil.CollectionToString(SubPrefabs) + ",}";
	}

	public bool GetTargetToolLevelByType(ToolType toolType, out int minLevel)
	{
		minLevel = -1;
		ToolConstraint[] toolConstraints = ToolConstraints;
		foreach (ToolConstraint toolConstraint in toolConstraints)
		{
			if (toolType == toolConstraint.ToolType)
			{
				minLevel = toolConstraint.ToolLevel;
				return true;
			}
		}
		return false;
	}

	public Sprite GetCurrentSkin(int skinIndex)
	{
		int num = Skins.Length - 1;
		if (skinIndex < 0 || skinIndex > num)
		{
			Debug.LogError($"皮肤索引<{skinIndex}>超出资源配置范围!");
			if (num < 0)
			{
				return null;
			}
		}
		int num2 = Mathf.Clamp(skinIndex, 0, num);
		return Skins[num2].Asset;
	}
}
