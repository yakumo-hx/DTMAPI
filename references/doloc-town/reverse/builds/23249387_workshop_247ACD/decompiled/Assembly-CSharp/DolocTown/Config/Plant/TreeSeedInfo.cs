using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TreeSeedInfo : BeanBase
{
	public const int __ID__ = -1329117344;

	public string Id { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string ResourceId { get; private set; }

	public ResourceInfo ResourceId_Ref { get; private set; }

	public ResourceLevelData[] LevelDatas { get; private set; }

	public int RepeatLevel { get; private set; }

	public string ResinCollectorOutput { get; private set; }

	public ResinCollectorOutputInfo ResinCollectorOutput_Ref { get; private set; }

	public int MaxLevel => LevelDatas.Length - 1;

	public TreeSeedInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["title"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Title_l10n_key = _json["title"]["key"];
		if (!_json["title"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Title = _json["title"]["text"];
		if (!_json["resource_id"].IsString)
		{
			throw new SerializationException();
		}
		ResourceId = _json["resource_id"];
		JSONNode jSONNode = _json["level_datas"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		LevelDatas = new ResourceLevelData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			ResourceLevelData resourceLevelData = ResourceLevelData.DeserializeResourceLevelData(child);
			LevelDatas[num++] = resourceLevelData;
		}
		if (!_json["repeat_level"].IsNumber)
		{
			throw new SerializationException();
		}
		RepeatLevel = _json["repeat_level"];
		if (!_json["resin_collector_output"].IsString)
		{
			throw new SerializationException();
		}
		ResinCollectorOutput = _json["resin_collector_output"];
	}

	public TreeSeedInfo(string id, string title, string resource_id, ResourceLevelData[] level_datas, int repeat_level, string resin_collector_output)
	{
		Id = id;
		Title = title;
		ResourceId = resource_id;
		LevelDatas = level_datas;
		RepeatLevel = repeat_level;
		ResinCollectorOutput = resin_collector_output;
	}

	public static TreeSeedInfo DeserializeTreeSeedInfo(JSONNode _json)
	{
		return new TreeSeedInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1329117344;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ResourceId_Ref = (_tables["Resource.TbResource"] as TbResource).GetOrDefault(ResourceId);
		ResourceLevelData[] levelDatas = LevelDatas;
		for (int i = 0; i < levelDatas.Length; i++)
		{
			levelDatas[i]?.Resolve(_tables);
		}
		ResinCollectorOutput_Ref = (_tables["Resource.TbResinCollectorOutput"] as TbResinCollectorOutput).GetOrDefault(ResinCollectorOutput);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		Title = translator(Title_l10n_key, Title);
		ResourceLevelData[] levelDatas = LevelDatas;
		for (int i = 0; i < levelDatas.Length; i++)
		{
			levelDatas[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Title:" + Title + ",ResourceId:" + ResourceId + ",LevelDatas:" + StringUtil.CollectionToString(LevelDatas) + ",RepeatLevel:" + RepeatLevel + ",ResinCollectorOutput:" + ResinCollectorOutput + ",}";
	}

	public ResourceLevelData GetLevelData(int level)
	{
		if (level < 0 || level > MaxLevel)
		{
			Debug.LogError($"等级<{level}>超出资源等级配置范围！");
		}
		return LevelDatas[Mathf.Clamp(level, 0, MaxLevel)];
	}

	public int RandomSkinIndex(int randomSeed)
	{
		ResourceLevelData levelData = GetLevelData(0);
		return new System.Random(randomSeed).Next(0, levelData.Skins.Length);
	}

	public int[] GetRandomMaxGrowthValues(int randomSeed)
	{
		System.Random random = new System.Random(randomSeed);
		int[] array = new int[LevelDatas.Length];
		for (int i = 0; i < LevelDatas.Length; i++)
		{
			Vector2Int growthValue = LevelDatas[i].GrowthValue;
			array[i] = random.Next(growthValue.x, growthValue.y);
		}
		return array;
	}

	public int ValidateSkinIdx(int skinIdx)
	{
		ResourceLevelData levelData = GetLevelData(0);
		return Mathf.Clamp(skinIdx, 0, levelData.Skins.Length - 1);
	}
}
