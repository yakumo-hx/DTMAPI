using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Plant;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionGeneCapsule : ItemFunctionBase
{
	public const int __ID__ = -1830102414;

	public string[] Genes { get; private set; }

	public CropGeneInfo[] Genes_Ref { get; private set; }

	public ItemFunctionGeneCapsule(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["genes"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Genes = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			Genes[num++] = text;
		}
	}

	public ItemFunctionGeneCapsule(string[] genes)
	{
		Genes = genes;
	}

	public static ItemFunctionGeneCapsule DeserializeItemFunctionGeneCapsule(JSONNode _json)
	{
		return new ItemFunctionGeneCapsule(_json);
	}

	public override int GetTypeId()
	{
		return -1830102414;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		int num = Genes.Length;
		TbCropGene tbCropGene = (TbCropGene)_tables["Plant.TbCropGene"];
		Genes_Ref = new CropGeneInfo[num];
		for (int i = 0; i < num; i++)
		{
			Genes_Ref[i] = tbCropGene.GetOrDefault(Genes[i]);
		}
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Genes:" + StringUtil.CollectionToString(Genes) + ",}";
	}
}
