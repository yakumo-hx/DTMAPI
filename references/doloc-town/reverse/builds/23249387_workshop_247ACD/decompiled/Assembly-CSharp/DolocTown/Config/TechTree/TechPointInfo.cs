using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.TechTree;

public sealed class TechPointInfo : BeanBase
{
	public readonly Dictionary<int, TechLevelInfo> LevelMap_Index = new Dictionary<int, TechLevelInfo>();

	public const int __ID__ = -1551110322;

	public readonly List<int> TotalExpForLevel = new List<int>();

	public TechPointType Id { get; private set; }

	public List<TechLevelInfo> LevelMap { get; private set; }

	public string Title { get; private set; }

	public string Title_l10n_key { get; }

	public string Description { get; private set; }

	public string Description_l10n_key { get; }

	public SpriteAsset Icon { get; private set; }

	public bool UseLevelTip { get; private set; }

	public int Order { get; private set; }

	public int MaxLevel => LevelMap.Count;

	public int MaxExp { get; private set; }

	public TechPointInfo(JSONNode _json)
	{
		if (!_json["id"].IsNumber)
		{
			throw new SerializationException();
		}
		Id = (TechPointType)_json["id"].AsInt;
		JSONNode jSONNode = _json["level_map"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		LevelMap = new List<TechLevelInfo>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			TechLevelInfo item = TechLevelInfo.DeserializeTechLevelInfo(child);
			LevelMap.Add(item);
		}
		foreach (TechLevelInfo item2 in LevelMap)
		{
			LevelMap_Index.Add(item2.Level, item2);
		}
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
		if (!_json["description"]["key"].IsString)
		{
			throw new SerializationException();
		}
		Description_l10n_key = _json["description"]["key"];
		if (!_json["description"]["text"].IsString)
		{
			throw new SerializationException();
		}
		Description = _json["description"]["text"];
		if (!_json["icon"].IsObject)
		{
			throw new SerializationException();
		}
		Icon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["icon"]));
		if (!_json["use_level_tip"].IsBoolean)
		{
			throw new SerializationException();
		}
		UseLevelTip = _json["use_level_tip"];
		if (!_json["order"].IsNumber)
		{
			throw new SerializationException();
		}
		Order = _json["order"];
	}

	public TechPointInfo(TechPointType id, List<TechLevelInfo> level_map, string title, string description, SpriteAsset icon, bool use_level_tip, int order)
	{
		Id = id;
		LevelMap = level_map;
		foreach (TechLevelInfo item in LevelMap)
		{
			LevelMap_Index.Add(item.Level, item);
		}
		Title = title;
		Description = description;
		Icon = icon;
		UseLevelTip = use_level_tip;
		Order = order;
	}

	public static TechPointInfo DeserializeTechPointInfo(JSONNode _json)
	{
		return new TechPointInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1551110322;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TechLevelInfo item in LevelMap)
		{
			item?.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TechLevelInfo item in LevelMap)
		{
			item?.TranslateText(translator);
		}
		Title = translator(Title_l10n_key, Title);
		Description = translator(Description_l10n_key, Description);
	}

	public override string ToString()
	{
		return "{ Id:" + Id.ToString() + ",LevelMap:" + StringUtil.CollectionToString(LevelMap) + ",Title:" + Title + ",Description:" + Description + ",Icon:" + Icon?.ToString() + ",UseLevelTip:" + UseLevelTip + ",Order:" + Order + ",}";
	}

	private void PostResolve()
	{
		int num = 0;
		TotalExpForLevel.Add(0);
		for (int i = 0; i < MaxLevel; i++)
		{
			if (!LevelMap_Index.TryGetValue(i, out var value))
			{
				Debug.LogError($"<{Id}>等级数据不合法：请从零开始递增！");
				continue;
			}
			num += value.RequiredExp;
			TotalExpForLevel.Add(num);
		}
		MaxExp = TotalExpForLevel.Last();
	}

	public void GetLevelAndExp(int totalExp, out int currentLevel, out int currentExp)
	{
		currentExp = (currentLevel = 0);
		for (int num = MaxLevel; num >= 0; num--)
		{
			if (totalExp >= TotalExpForLevel[num])
			{
				currentExp = totalExp - TotalExpForLevel[num];
				currentLevel = num;
				break;
			}
		}
	}
}
