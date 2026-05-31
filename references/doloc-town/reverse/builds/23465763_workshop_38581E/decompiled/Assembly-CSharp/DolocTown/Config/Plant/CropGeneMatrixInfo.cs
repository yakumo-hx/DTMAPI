using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using RedSaw;
using SimpleJSON;

namespace DolocTown.Config.Plant;

public sealed class CropGeneMatrixInfo : BeanBase
{
	public const int __ID__ = -1558936489;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public Dictionary<string, int> GeneMap { get; private set; }

	public CropGeneMatrixInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["gene_map"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		GeneMap = new Dictionary<string, int>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child[0].IsString)
			{
				throw new SerializationException();
			}
			string key = child[0];
			if (!child[1].IsNumber)
			{
				throw new SerializationException();
			}
			int value = child[1];
			GeneMap.Add(key, value);
		}
	}

	public CropGeneMatrixInfo(string id, Dictionary<string, int> gene_map)
	{
		Id = id;
		GeneMap = gene_map;
	}

	public static CropGeneMatrixInfo DeserializeCropGeneMatrixInfo(JSONNode _json)
	{
		return new CropGeneMatrixInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1558936489;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",GeneMap:" + StringUtil.CollectionToString(GeneMap) + ",}";
	}

	public bool TryRollGene(out string geneId)
	{
		geneId = string.Empty;
		string[] array = GeneMap.Keys.ToArray();
		int value;
		int num = RandomUtils.RussianRoulette(GeneMap.Values.ToArray(), out value);
		if (num < 0 || num >= array.Length)
		{
			return false;
		}
		geneId = array[num];
		return true;
	}

	public bool TryRollGene(out string geneId, string[] availableGeneNames)
	{
		geneId = null;
		if (availableGeneNames.IsNullOrEmpty())
		{
			return false;
		}
		Dictionary<string, int> dictionary = GeneMap.Where((KeyValuePair<string, int> kv) => availableGeneNames.Contains(kv.Key)).ToDictionary((KeyValuePair<string, int> kv) => kv.Key, (KeyValuePair<string, int> kv) => kv.Value);
		if (dictionary.Count == 0)
		{
			return false;
		}
		string[] array = dictionary.Keys.ToArray();
		int value;
		int num = RandomUtils.RussianRoulette(dictionary.Values.ToArray(), out value);
		if (num < 0 || num >= array.Length)
		{
			return false;
		}
		geneId = array[num];
		return true;
	}
}
