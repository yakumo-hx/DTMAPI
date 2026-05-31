using System;
using System.Collections.Generic;
using RedSaw;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class TbHusbandry
{
	public struct ContributionInfo
	{
		public string outputId;

		public int contribution;
	}

	private readonly Dictionary<string, HusbandryInfo> _dataMap;

	private readonly List<HusbandryInfo> _dataList;

	private readonly Dictionary<string, AnimalHusbandryData> _husbandryInfosCache = new Dictionary<string, AnimalHusbandryData>();

	private Dictionary<string, Dictionary<string, ContributionInfo[]>> _reverseMap;

	public Dictionary<string, HusbandryInfo> DataMap => _dataMap;

	public List<HusbandryInfo> DataList => _dataList;

	public HusbandryInfo this[string key] => _dataMap[key];

	public TbHusbandry(JSONNode _json)
	{
		_dataMap = new Dictionary<string, HusbandryInfo>();
		_dataList = new List<HusbandryInfo>();
		foreach (JSONNode child in _json.Children)
		{
			HusbandryInfo husbandryInfo = HusbandryInfo.DeserializeHusbandryInfo(child);
			if (_dataMap.TryAdd(husbandryInfo.Id, husbandryInfo))
			{
				_dataList.Add(husbandryInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + husbandryInfo.Id + " in table: TbHusbandry");
			}
		}
	}

	public HusbandryInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public HusbandryInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (HusbandryInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (HusbandryInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void InitReverseMap()
	{
		_reverseMap = new Dictionary<string, Dictionary<string, ContributionInfo[]>>();
		foreach (KeyValuePair<string, HusbandryInfo> item in DataMap)
		{
			item.Deconstruct(out var key, out var value);
			string key2 = key;
			List<AnimalHusbandryData> husbandryDatas = value.HusbandryDatas;
			_reverseMap[key2] = GetReverseInfo(husbandryDatas);
		}
	}

	public bool TryGetThreshold(string animalId, string outputId, out int threshold)
	{
		string key = animalId + "-" + outputId;
		if (_husbandryInfosCache.TryGetValue(key, out var value))
		{
			threshold = value.Threshold;
			return true;
		}
		threshold = 0;
		if (!DataMap.TryGetValue(animalId, out var value2))
		{
			return false;
		}
		foreach (AnimalHusbandryData husbandryData in value2.HusbandryDatas)
		{
			if (!(husbandryData.Output != outputId))
			{
				threshold = husbandryData.Threshold;
				_husbandryInfosCache[key] = husbandryData;
				return true;
			}
		}
		return false;
	}

	public CountItem[] GenOutput(string animalId, string outputId)
	{
		string key = animalId + "-" + outputId;
		if (_husbandryInfosCache.TryGetValue(key, out var value))
		{
			return GenOutput(value);
		}
		if (!DataMap.TryGetValue(animalId, out var value2))
		{
			return Array.Empty<CountItem>();
		}
		foreach (AnimalHusbandryData husbandryData in value2.HusbandryDatas)
		{
			if (!(husbandryData.Output != outputId))
			{
				_husbandryInfosCache[key] = husbandryData;
				return GenOutput(husbandryData);
			}
		}
		Debug.LogError("Animal.TbHusbandryInfos.GenOutput: 未知的小动物产出信息 " + animalId + " - " + outputId);
		return Array.Empty<CountItem>();
	}

	private CountItem[] GenOutput(AnimalHusbandryData info)
	{
		if (!DolocAPI.QueryItemProto(info.Output, out var proto))
		{
			Debug.LogError("Animal.TbHusbandryInfos.GenOutput: 未知的道具" + info.Output);
			return Array.Empty<CountItem>();
		}
		return new CountItem[1]
		{
			new CountItem(proto.Id, info.OutputRange.DiceCount())
		};
	}

	public ContributionInfo[] GetContributionInfos(string animalId, string inputItem)
	{
		if (_reverseMap == null)
		{
			InitReverseMap();
		}
		if (!_reverseMap.TryGetValue(animalId, out var value))
		{
			return Array.Empty<ContributionInfo>();
		}
		if (value.TryGetValue(inputItem, out var value2))
		{
			return value2;
		}
		return Array.Empty<ContributionInfo>();
	}

	private static Dictionary<string, ContributionInfo[]> GetReverseInfo(List<AnimalHusbandryData> infos)
	{
		Dictionary<string, List<ContributionInfo>> dictionary = new Dictionary<string, List<ContributionInfo>>();
		foreach (AnimalHusbandryData info in infos)
		{
			HusbandryEnergyInfo[] array = (info.LimitedContributions.IsNullOrEmpty() ? DolocConfig.Tables.TbHusbandryEnergy.DataList.ToArray() : info.LimitedContributions_Ref);
			foreach (HusbandryEnergyInfo husbandryEnergyInfo in array)
			{
				string id = husbandryEnergyInfo.Id;
				dictionary.TryAdd(id, new List<ContributionInfo>());
				ContributionInfo contributionInfo = default(ContributionInfo);
				contributionInfo.outputId = info.Output;
				contributionInfo.contribution = husbandryEnergyInfo.Contribution;
				ContributionInfo item = contributionInfo;
				dictionary[id].Add(item);
			}
		}
		Dictionary<string, ContributionInfo[]> dictionary2 = new Dictionary<string, ContributionInfo[]>();
		foreach (KeyValuePair<string, List<ContributionInfo>> item2 in dictionary)
		{
			dictionary2[item2.Key] = item2.Value.ToArray();
		}
		return dictionary2;
	}
}
