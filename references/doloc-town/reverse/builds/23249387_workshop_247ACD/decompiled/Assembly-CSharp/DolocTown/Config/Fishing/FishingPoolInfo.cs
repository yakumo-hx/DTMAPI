using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Fishing;

public sealed class FishingPoolInfo : BeanBase
{
	public const int __ID__ = 1553013744;

	public string Id { get; private set; }

	public int[] RarityWeights { get; private set; }

	public float GarbageProbability { get; private set; }

	public List<string> Fishes { get; private set; }

	public List<FishInfo> Fishes_Ref { get; private set; }

	public FishingPoolInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["rarity_weights"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		RarityWeights = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			RarityWeights[num++] = num2;
		}
		if (!_json["garbage_probability"].IsNumber)
		{
			throw new SerializationException();
		}
		GarbageProbability = _json["garbage_probability"];
		JSONNode jSONNode2 = _json["fishes"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		Fishes = new List<string>(jSONNode2.Count);
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsString)
			{
				throw new SerializationException();
			}
			string item = child2;
			Fishes.Add(item);
		}
	}

	public FishingPoolInfo(string id, int[] rarity_weights, float garbage_probability, List<string> fishes)
	{
		Id = id;
		RarityWeights = rarity_weights;
		GarbageProbability = garbage_probability;
		Fishes = fishes;
	}

	public static FishingPoolInfo DeserializeFishingPoolInfo(JSONNode _json)
	{
		return new FishingPoolInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1553013744;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TbFish tbFish = (TbFish)_tables["Fishing.TbFish"];
		Fishes_Ref = new List<FishInfo>();
		foreach (string fish in Fishes)
		{
			Fishes_Ref.Add(tbFish.GetOrDefault(fish));
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",RarityWeights:" + StringUtil.CollectionToString(RarityWeights) + ",GarbageProbability:" + GarbageProbability + ",Fishes:" + StringUtil.CollectionToString(Fishes) + ",}";
	}
}
