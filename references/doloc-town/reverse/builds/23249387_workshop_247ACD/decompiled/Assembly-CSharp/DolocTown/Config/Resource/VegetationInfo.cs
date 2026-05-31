using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class VegetationInfo : BeanBase
{
	public const int __ID__ = 70646972;

	public string Id { get; private set; }

	public VegetationType Type { get; private set; }

	public bool DefaultUnlock { get; private set; }

	public Vector2Int Size { get; private set; }

	public int[] SpawnMonths { get; private set; }

	public int[] GrowingMonths { get; private set; }

	public ToolConstraint[] ToolConstraints { get; private set; }

	public ItemSpawnEntry DropSpawnEntry { get; private set; }

	public VegetationFuncBase Function { get; private set; }

	public int Width => Size.x;

	public VegetationInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["type"].IsNumber)
		{
			throw new SerializationException();
		}
		Type = (VegetationType)_json["type"].AsInt;
		if (!_json["default_unlock"].IsBoolean)
		{
			throw new SerializationException();
		}
		DefaultUnlock = _json["default_unlock"];
		if (!_json["size"].IsObject)
		{
			throw new SerializationException();
		}
		Size = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["size"]));
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
		JSONNode jSONNode2 = _json["growing_months"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		GrowingMonths = new int[count2];
		int num3 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsNumber)
			{
				throw new SerializationException();
			}
			int num4 = child2;
			GrowingMonths[num3++] = num4;
		}
		JSONNode jSONNode3 = _json["tool_constraints"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		ToolConstraints = new ToolConstraint[count3];
		int num5 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			ToolConstraint toolConstraint = ToolConstraint.DeserializeToolConstraint(child3);
			ToolConstraints[num5++] = toolConstraint;
		}
		if (!_json["drop_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		DropSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["drop_spawn_entry"]);
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = VegetationFuncBase.DeserializeVegetationFuncBase(_json["function"]);
	}

	public VegetationInfo(string id, VegetationType type, bool default_unlock, Vector2Int size, int[] spawn_months, int[] growing_months, ToolConstraint[] tool_constraints, ItemSpawnEntry drop_spawn_entry, VegetationFuncBase function)
	{
		Id = id;
		Type = type;
		DefaultUnlock = default_unlock;
		Size = size;
		SpawnMonths = spawn_months;
		GrowingMonths = growing_months;
		ToolConstraints = tool_constraints;
		DropSpawnEntry = drop_spawn_entry;
		Function = function;
	}

	public static VegetationInfo DeserializeVegetationInfo(JSONNode _json)
	{
		return new VegetationInfo(_json);
	}

	public override int GetTypeId()
	{
		return 70646972;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ToolConstraint[] toolConstraints = ToolConstraints;
		for (int i = 0; i < toolConstraints.Length; i++)
		{
			toolConstraints[i]?.Resolve(_tables);
		}
		DropSpawnEntry?.Resolve(_tables);
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ToolConstraint[] toolConstraints = ToolConstraints;
		for (int i = 0; i < toolConstraints.Length; i++)
		{
			toolConstraints[i]?.TranslateText(translator);
		}
		DropSpawnEntry?.TranslateText(translator);
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Type:" + Type.ToString() + ",DefaultUnlock:" + DefaultUnlock + ",Size:" + Size.ToString() + ",SpawnMonths:" + StringUtil.CollectionToString(SpawnMonths) + ",GrowingMonths:" + StringUtil.CollectionToString(GrowingMonths) + ",ToolConstraints:" + StringUtil.CollectionToString(ToolConstraints) + ",DropSpawnEntry:" + DropSpawnEntry?.ToString() + ",Function:" + Function?.ToString() + ",}";
	}

	public bool CheckMatchMonth(int month)
	{
		if (!SpawnMonths.IsNullOrEmpty())
		{
			return SpawnMonths.Contains(month);
		}
		return true;
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
}
