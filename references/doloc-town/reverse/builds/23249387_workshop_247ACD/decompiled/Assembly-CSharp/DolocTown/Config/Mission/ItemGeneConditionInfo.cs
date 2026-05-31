using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Plant;
using SimpleJSON;

namespace DolocTown.Config.Mission;

public sealed class ItemGeneConditionInfo : BeanBase
{
	public const int __ID__ = 1271285087;

	public string Id { get; private set; }

	public int GeneMinCount { get; private set; }

	public bool IsCloned { get; private set; }

	public string[] GeneFilter { get; private set; }

	public CropGeneInfo[] GeneFilter_Ref { get; private set; }

	public ItemGeneConditionInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["gene_min_count"].IsNumber)
		{
			throw new SerializationException();
		}
		GeneMinCount = _json["gene_min_count"];
		if (!_json["is_cloned"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsCloned = _json["is_cloned"];
		JSONNode jSONNode = _json["gene_filter"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		GeneFilter = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			GeneFilter[num++] = text;
		}
	}

	public ItemGeneConditionInfo(string id, int gene_min_count, bool is_cloned, string[] gene_filter)
	{
		Id = id;
		GeneMinCount = gene_min_count;
		IsCloned = is_cloned;
		GeneFilter = gene_filter;
	}

	public static ItemGeneConditionInfo DeserializeItemGeneConditionInfo(JSONNode _json)
	{
		return new ItemGeneConditionInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1271285087;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		int num = GeneFilter.Length;
		TbCropGene tbCropGene = (TbCropGene)_tables["Plant.TbCropGene"];
		GeneFilter_Ref = new CropGeneInfo[num];
		for (int i = 0; i < num; i++)
		{
			GeneFilter_Ref[i] = tbCropGene.GetOrDefault(GeneFilter[i]);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",GeneMinCount:" + GeneMinCount + ",IsCloned:" + IsCloned + ",GeneFilter:" + StringUtil.CollectionToString(GeneFilter) + ",}";
	}
}
