using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Animal;

public sealed class HusbandryInfo : BeanBase
{
	public readonly Dictionary<string, AnimalHusbandryData> HusbandryDatas_Index = new Dictionary<string, AnimalHusbandryData>();

	public const int __ID__ = 1644019486;

	public string Id { get; private set; }

	public List<AnimalHusbandryData> HusbandryDatas { get; private set; }

	public HusbandryInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["husbandry_datas"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		HusbandryDatas = new List<AnimalHusbandryData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			AnimalHusbandryData item = AnimalHusbandryData.DeserializeAnimalHusbandryData(child);
			HusbandryDatas.Add(item);
		}
		foreach (AnimalHusbandryData husbandryData in HusbandryDatas)
		{
			HusbandryDatas_Index.Add(husbandryData.Output, husbandryData);
		}
	}

	public HusbandryInfo(string id, List<AnimalHusbandryData> husbandry_datas)
	{
		Id = id;
		HusbandryDatas = husbandry_datas;
		foreach (AnimalHusbandryData husbandryData in HusbandryDatas)
		{
			HusbandryDatas_Index.Add(husbandryData.Output, husbandryData);
		}
	}

	public static HusbandryInfo DeserializeHusbandryInfo(JSONNode _json)
	{
		return new HusbandryInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1644019486;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AnimalHusbandryData husbandryData in HusbandryDatas)
		{
			husbandryData?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AnimalHusbandryData husbandryData in HusbandryDatas)
		{
			husbandryData?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",HusbandryDatas:" + StringUtil.CollectionToString(HusbandryDatas) + ",}";
	}
}
