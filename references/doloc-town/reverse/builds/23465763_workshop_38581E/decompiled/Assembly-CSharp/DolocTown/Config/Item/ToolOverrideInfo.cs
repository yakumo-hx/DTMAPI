using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ToolOverrideInfo : BeanBase
{
	public readonly Dictionary<DungeonResourceClass, ToolOverrideLevel> OverrideLevels_Index = new Dictionary<DungeonResourceClass, ToolOverrideLevel>();

	public readonly Dictionary<DungeonResourceClass, ToolOverrideSpawnLut> OverrideSpawnLuts_Index = new Dictionary<DungeonResourceClass, ToolOverrideSpawnLut>();

	public readonly Dictionary<string, ToolExtraSpawnLutByResourceName> ExtraSpawnLutsByName_Index = new Dictionary<string, ToolExtraSpawnLutByResourceName>();

	public readonly Dictionary<DungeonResourceClass, ToolExtraSpawnLutByResourceClass> ExtraSpawnLutsByClass_Index = new Dictionary<DungeonResourceClass, ToolExtraSpawnLutByResourceClass>();

	public const int __ID__ = 2012900813;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public ToolOverrideLevel[] OverrideLevels { get; private set; }

	public ToolOverrideSpawnLut[] OverrideSpawnLuts { get; private set; }

	public ToolExtraSpawnLutByResourceName[] ExtraSpawnLutsByName { get; private set; }

	public ToolExtraSpawnLutByResourceClass[] ExtraSpawnLutsByClass { get; private set; }

	public ToolOverrideInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["override_levels"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		OverrideLevels = new ToolOverrideLevel[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ToolOverrideLevel toolOverrideLevel = ToolOverrideLevel.DeserializeToolOverrideLevel(child);
			OverrideLevels[num++] = toolOverrideLevel;
		}
		ToolOverrideLevel[] overrideLevels = OverrideLevels;
		foreach (ToolOverrideLevel toolOverrideLevel2 in overrideLevels)
		{
			OverrideLevels_Index.Add(toolOverrideLevel2.TargetResourceClass, toolOverrideLevel2);
		}
		JSONNode jSONNode2 = _json["override_spawn_luts"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		OverrideSpawnLuts = new ToolOverrideSpawnLut[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsObject)
			{
				throw new SerializationException();
			}
			ToolOverrideSpawnLut toolOverrideSpawnLut = ToolOverrideSpawnLut.DeserializeToolOverrideSpawnLut(child2);
			OverrideSpawnLuts[num2++] = toolOverrideSpawnLut;
		}
		ToolOverrideSpawnLut[] overrideSpawnLuts = OverrideSpawnLuts;
		foreach (ToolOverrideSpawnLut toolOverrideSpawnLut2 in overrideSpawnLuts)
		{
			OverrideSpawnLuts_Index.Add(toolOverrideSpawnLut2.TargetResourceClass, toolOverrideSpawnLut2);
		}
		JSONNode jSONNode3 = _json["extra_spawn_luts_by_name"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		ExtraSpawnLutsByName = new ToolExtraSpawnLutByResourceName[count3];
		int num3 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			ToolExtraSpawnLutByResourceName toolExtraSpawnLutByResourceName = ToolExtraSpawnLutByResourceName.DeserializeToolExtraSpawnLutByResourceName(child3);
			ExtraSpawnLutsByName[num3++] = toolExtraSpawnLutByResourceName;
		}
		ToolExtraSpawnLutByResourceName[] extraSpawnLutsByName = ExtraSpawnLutsByName;
		foreach (ToolExtraSpawnLutByResourceName toolExtraSpawnLutByResourceName2 in extraSpawnLutsByName)
		{
			ExtraSpawnLutsByName_Index.Add(toolExtraSpawnLutByResourceName2.TargetResourceName, toolExtraSpawnLutByResourceName2);
		}
		JSONNode jSONNode4 = _json["extra_spawn_luts_by_class"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		ExtraSpawnLutsByClass = new ToolExtraSpawnLutByResourceClass[count4];
		int num4 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			ToolExtraSpawnLutByResourceClass toolExtraSpawnLutByResourceClass = ToolExtraSpawnLutByResourceClass.DeserializeToolExtraSpawnLutByResourceClass(child4);
			ExtraSpawnLutsByClass[num4++] = toolExtraSpawnLutByResourceClass;
		}
		ToolExtraSpawnLutByResourceClass[] extraSpawnLutsByClass = ExtraSpawnLutsByClass;
		foreach (ToolExtraSpawnLutByResourceClass toolExtraSpawnLutByResourceClass2 in extraSpawnLutsByClass)
		{
			ExtraSpawnLutsByClass_Index.Add(toolExtraSpawnLutByResourceClass2.TargetResourceClass, toolExtraSpawnLutByResourceClass2);
		}
	}

	public ToolOverrideInfo(string id, ToolOverrideLevel[] override_levels, ToolOverrideSpawnLut[] override_spawn_luts, ToolExtraSpawnLutByResourceName[] extra_spawn_luts_by_name, ToolExtraSpawnLutByResourceClass[] extra_spawn_luts_by_class)
	{
		Id = id;
		OverrideLevels = override_levels;
		ToolOverrideLevel[] overrideLevels = OverrideLevels;
		foreach (ToolOverrideLevel toolOverrideLevel in overrideLevels)
		{
			OverrideLevels_Index.Add(toolOverrideLevel.TargetResourceClass, toolOverrideLevel);
		}
		OverrideSpawnLuts = override_spawn_luts;
		ToolOverrideSpawnLut[] overrideSpawnLuts = OverrideSpawnLuts;
		foreach (ToolOverrideSpawnLut toolOverrideSpawnLut in overrideSpawnLuts)
		{
			OverrideSpawnLuts_Index.Add(toolOverrideSpawnLut.TargetResourceClass, toolOverrideSpawnLut);
		}
		ExtraSpawnLutsByName = extra_spawn_luts_by_name;
		ToolExtraSpawnLutByResourceName[] extraSpawnLutsByName = ExtraSpawnLutsByName;
		foreach (ToolExtraSpawnLutByResourceName toolExtraSpawnLutByResourceName in extraSpawnLutsByName)
		{
			ExtraSpawnLutsByName_Index.Add(toolExtraSpawnLutByResourceName.TargetResourceName, toolExtraSpawnLutByResourceName);
		}
		ExtraSpawnLutsByClass = extra_spawn_luts_by_class;
		ToolExtraSpawnLutByResourceClass[] extraSpawnLutsByClass = ExtraSpawnLutsByClass;
		foreach (ToolExtraSpawnLutByResourceClass toolExtraSpawnLutByResourceClass in extraSpawnLutsByClass)
		{
			ExtraSpawnLutsByClass_Index.Add(toolExtraSpawnLutByResourceClass.TargetResourceClass, toolExtraSpawnLutByResourceClass);
		}
	}

	public static ToolOverrideInfo DeserializeToolOverrideInfo(JSONNode _json)
	{
		return new ToolOverrideInfo(_json);
	}

	public override int GetTypeId()
	{
		return 2012900813;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		ToolOverrideLevel[] overrideLevels = OverrideLevels;
		for (int i = 0; i < overrideLevels.Length; i++)
		{
			overrideLevels[i]?.Resolve(_tables);
		}
		ToolOverrideSpawnLut[] overrideSpawnLuts = OverrideSpawnLuts;
		for (int i = 0; i < overrideSpawnLuts.Length; i++)
		{
			overrideSpawnLuts[i]?.Resolve(_tables);
		}
		ToolExtraSpawnLutByResourceName[] extraSpawnLutsByName = ExtraSpawnLutsByName;
		for (int i = 0; i < extraSpawnLutsByName.Length; i++)
		{
			extraSpawnLutsByName[i]?.Resolve(_tables);
		}
		ToolExtraSpawnLutByResourceClass[] extraSpawnLutsByClass = ExtraSpawnLutsByClass;
		for (int i = 0; i < extraSpawnLutsByClass.Length; i++)
		{
			extraSpawnLutsByClass[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ToolOverrideLevel[] overrideLevels = OverrideLevels;
		for (int i = 0; i < overrideLevels.Length; i++)
		{
			overrideLevels[i]?.TranslateText(translator);
		}
		ToolOverrideSpawnLut[] overrideSpawnLuts = OverrideSpawnLuts;
		for (int i = 0; i < overrideSpawnLuts.Length; i++)
		{
			overrideSpawnLuts[i]?.TranslateText(translator);
		}
		ToolExtraSpawnLutByResourceName[] extraSpawnLutsByName = ExtraSpawnLutsByName;
		for (int i = 0; i < extraSpawnLutsByName.Length; i++)
		{
			extraSpawnLutsByName[i]?.TranslateText(translator);
		}
		ToolExtraSpawnLutByResourceClass[] extraSpawnLutsByClass = ExtraSpawnLutsByClass;
		for (int i = 0; i < extraSpawnLutsByClass.Length; i++)
		{
			extraSpawnLutsByClass[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",OverrideLevels:" + StringUtil.CollectionToString(OverrideLevels) + ",OverrideSpawnLuts:" + StringUtil.CollectionToString(OverrideSpawnLuts) + ",ExtraSpawnLutsByName:" + StringUtil.CollectionToString(ExtraSpawnLutsByName) + ",ExtraSpawnLutsByClass:" + StringUtil.CollectionToString(ExtraSpawnLutsByClass) + ",}";
	}
}
