using System.Linq;
using DolocTown.Config;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using UnityEngine;

namespace DolocTown;

public struct ResourceFellData
{
	public readonly bool levelMatch;

	public readonly int toolLevel;

	public readonly int Damage;

	public readonly string overrideSpawnLut;

	public readonly CountItem[] extraItems;

	public readonly Vector2 hitPoint;

	public readonly bool shouldCounterBack;

	public readonly bool shouldRaiseToolTip;

	public readonly bool Valid;

	public ResourceFellData(DungeonResource resource, ItemTool tool, Vector2 hitPoint)
	{
		this = default(ResourceFellData);
		ResourceLevelData currentLevelData = resource.currentLevelData;
		if (!CheckToolTypeMatch(currentLevelData, tool.ToolType))
		{
			Valid = false;
			return;
		}
		Valid = true;
		toolLevel = GetEffectiveLevel(tool, resource.Proto.ResourceClass);
		levelMatch = CheckToolLevelMatch(currentLevelData, tool.ToolType, toolLevel);
		Damage = tool.ChopNumber;
		this.hitPoint = hitPoint;
		ToolOverrideInfo orDefault = DolocConfig.Tables.TbToolOverride.GetOrDefault(tool.name);
		if (orDefault != null)
		{
			if (orDefault.OverrideSpawnLuts_Index.TryGetValue(resource.Proto.ResourceClass, out var value))
			{
				overrideSpawnLut = value.OverrideSpawnLut;
			}
			ToolExtraSpawnLutByResourceClass value3;
			if (orDefault.ExtraSpawnLutsByName_Index.TryGetValue(resource.Proto.Id, out var value2))
			{
				if (value2.TargetLevels.IsNullOrEmpty() || value2.TargetLevels.Contains(resource.currentLevel))
				{
					extraItems = value2.SpawnEntry.SpawnItems();
				}
			}
			else if (orDefault.ExtraSpawnLutsByClass_Index.TryGetValue(resource.Proto.ResourceClass, out value3) && (value3.TargetLevels.IsNullOrEmpty() || value3.TargetLevels.Contains(resource.currentLevel)))
			{
				extraItems = value3.SpawnEntry.SpawnItems();
			}
		}
		shouldCounterBack = true;
		shouldRaiseToolTip = true;
	}

	private int GetEffectiveLevel(ItemTool tool, DungeonResourceClass resourceClass)
	{
		ToolOverrideInfo orDefault = DolocConfig.Tables.TbToolOverride.GetOrDefault(tool.name);
		if (orDefault?.OverrideLevels_Index == null)
		{
			return tool.Level;
		}
		if (orDefault.OverrideLevels_Index.TryGetValue(resourceClass, out var value))
		{
			return value.OverrideLevel;
		}
		return tool.Level;
	}

	private bool CheckToolTypeMatch(ResourceLevelData currentLevelData, ToolType type)
	{
		int minLevel;
		return currentLevelData.GetTargetToolLevelByType(type, out minLevel);
	}

	private bool CheckToolLevelMatch(ResourceLevelData currentLevelData, ToolType type, int level)
	{
		if (!currentLevelData.GetTargetToolLevelByType(type, out var minLevel))
		{
			return false;
		}
		return level >= minLevel;
	}

	public ResourceFellData(bool levelMatch, int toolLevel, int damage, Vector2 hitPoint, bool shouldCounterBack, bool shouldRaiseToolTip, string overrideSpawnLut = "", CountItem[] extraItems = null)
	{
		Valid = true;
		this.levelMatch = levelMatch;
		this.toolLevel = toolLevel;
		Damage = damage;
		this.overrideSpawnLut = overrideSpawnLut;
		this.hitPoint = hitPoint;
		this.shouldCounterBack = shouldCounterBack;
		this.shouldRaiseToolTip = shouldRaiseToolTip;
		this.extraItems = extraItems;
	}
}
