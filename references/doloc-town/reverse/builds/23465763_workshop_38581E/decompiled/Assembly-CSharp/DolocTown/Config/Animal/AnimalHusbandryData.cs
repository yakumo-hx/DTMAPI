using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class AnimalHusbandryData : BeanBase
{
	public const int __ID__ = -1975835906;

	public string Output { get; private set; }

	public ItemInfo Output_Ref { get; private set; }

	public Vector2Int OutputRange { get; private set; }

	public int Threshold { get; private set; }

	public string[] LimitedContributions { get; private set; }

	public HusbandryEnergyInfo[] LimitedContributions_Ref { get; private set; }

	public AnimalHusbandryData(JSONNode _json)
	{
		if (!_json["output"].IsString)
		{
			throw new SerializationException();
		}
		Output = _json["output"];
		if (!_json["output_range"].IsObject)
		{
			throw new SerializationException();
		}
		OutputRange = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["output_range"]));
		if (!_json["threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		Threshold = _json["threshold"];
		JSONNode jSONNode = _json["limited_contributions"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		LimitedContributions = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			LimitedContributions[num++] = text;
		}
	}

	public AnimalHusbandryData(string output, Vector2Int output_range, int threshold, string[] limited_contributions)
	{
		Output = output;
		OutputRange = output_range;
		Threshold = threshold;
		LimitedContributions = limited_contributions;
	}

	public static AnimalHusbandryData DeserializeAnimalHusbandryData(JSONNode _json)
	{
		return new AnimalHusbandryData(_json);
	}

	public override int GetTypeId()
	{
		return -1975835906;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Output_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Output);
		int num = LimitedContributions.Length;
		TbHusbandryEnergy tbHusbandryEnergy = (TbHusbandryEnergy)_tables["Animal.TbHusbandryEnergy"];
		LimitedContributions_Ref = new HusbandryEnergyInfo[num];
		for (int i = 0; i < num; i++)
		{
			LimitedContributions_Ref[i] = tbHusbandryEnergy.GetOrDefault(LimitedContributions[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Output:" + Output + ",OutputRange:" + OutputRange.ToString() + ",Threshold:" + Threshold + ",LimitedContributions:" + StringUtil.CollectionToString(LimitedContributions) + ",}";
	}
}
