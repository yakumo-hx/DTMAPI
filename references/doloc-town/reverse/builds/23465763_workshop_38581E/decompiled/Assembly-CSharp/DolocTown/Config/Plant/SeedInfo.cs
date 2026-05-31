using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class SeedInfo : BeanBase
{
	public const int __ID__ = -1382951134;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public string SeedType { get; private set; }

	public SeedTypeInfo SeedType_Ref { get; private set; }

	public string[] NatureGenes { get; private set; }

	public CropGeneInfo[] NatureGenes_Ref { get; private set; }

	public float HealthValue { get; private set; }

	public float DamageRate { get; private set; }

	public int[] GrowthMonths { get; private set; }

	public int TechPoint { get; private set; }

	public RangedItem[] CropOutputs { get; private set; }

	public int Lifespan { get; private set; }

	public int RepeatLevel { get; private set; }

	public CropLevelData[] LevelDatas { get; private set; }

	public int LevelCount => LevelDatas.Length;

	public int MatureLevel => LevelCount - 1;

	public SeedInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["seed_type"].IsString)
		{
			throw new SerializationException();
		}
		SeedType = _json["seed_type"];
		JSONNode jSONNode = _json["nature_genes"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		NatureGenes = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			NatureGenes[num++] = text;
		}
		if (!_json["health_value"].IsNumber)
		{
			throw new SerializationException();
		}
		HealthValue = _json["health_value"];
		if (!_json["damage_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		DamageRate = _json["damage_rate"];
		JSONNode jSONNode2 = _json["growth_months"];
		if (!jSONNode2.IsArray)
		{
			throw new SerializationException();
		}
		int count2 = jSONNode2.Count;
		GrowthMonths = new int[count2];
		int num2 = 0;
		foreach (JSONNode child2 in jSONNode2.Children)
		{
			if (!child2.IsNumber)
			{
				throw new SerializationException();
			}
			int num3 = child2;
			GrowthMonths[num2++] = num3;
		}
		if (!_json["tech_point"].IsNumber)
		{
			throw new SerializationException();
		}
		TechPoint = _json["tech_point"];
		JSONNode jSONNode3 = _json["crop_outputs"];
		if (!jSONNode3.IsArray)
		{
			throw new SerializationException();
		}
		int count3 = jSONNode3.Count;
		CropOutputs = new RangedItem[count3];
		int num4 = 0;
		foreach (JSONNode child3 in jSONNode3.Children)
		{
			if (!child3.IsObject)
			{
				throw new SerializationException();
			}
			RangedItem rangedItem = ExternalTypeUtil.RangedItemConverter(CfgRangedItem.DeserializeCfgRangedItem(child3));
			CropOutputs[num4++] = rangedItem;
		}
		if (!_json["lifespan"].IsNumber)
		{
			throw new SerializationException();
		}
		Lifespan = _json["lifespan"];
		if (!_json["repeat_level"].IsNumber)
		{
			throw new SerializationException();
		}
		RepeatLevel = _json["repeat_level"];
		JSONNode jSONNode4 = _json["level_datas"];
		if (!jSONNode4.IsArray)
		{
			throw new SerializationException();
		}
		int count4 = jSONNode4.Count;
		LevelDatas = new CropLevelData[count4];
		int num5 = 0;
		foreach (JSONNode child4 in jSONNode4.Children)
		{
			if (!child4.IsObject)
			{
				throw new SerializationException();
			}
			CropLevelData cropLevelData = CropLevelData.DeserializeCropLevelData(child4);
			LevelDatas[num5++] = cropLevelData;
		}
	}

	public SeedInfo(string id, string seed_type, string[] nature_genes, float health_value, float damage_rate, int[] growth_months, int tech_point, RangedItem[] crop_outputs, int lifespan, int repeat_level, CropLevelData[] level_datas)
	{
		Id = id;
		SeedType = seed_type;
		NatureGenes = nature_genes;
		HealthValue = health_value;
		DamageRate = damage_rate;
		GrowthMonths = growth_months;
		TechPoint = tech_point;
		CropOutputs = crop_outputs;
		Lifespan = lifespan;
		RepeatLevel = repeat_level;
		LevelDatas = level_datas;
	}

	public static SeedInfo DeserializeSeedInfo(JSONNode _json)
	{
		return new SeedInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1382951134;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
		SeedType_Ref = (_tables["Plant.TbSeedType"] as TbSeedType).GetOrDefault(SeedType);
		int num = NatureGenes.Length;
		TbCropGene tbCropGene = (TbCropGene)_tables["Plant.TbCropGene"];
		NatureGenes_Ref = new CropGeneInfo[num];
		for (int i = 0; i < num; i++)
		{
			NatureGenes_Ref[i] = tbCropGene.GetOrDefault(NatureGenes[i]);
		}
		CropLevelData[] levelDatas = LevelDatas;
		for (int j = 0; j < levelDatas.Length; j++)
		{
			levelDatas[j]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		CropLevelData[] levelDatas = LevelDatas;
		for (int i = 0; i < levelDatas.Length; i++)
		{
			levelDatas[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SeedType:" + SeedType + ",NatureGenes:" + StringUtil.CollectionToString(NatureGenes) + ",HealthValue:" + HealthValue + ",DamageRate:" + DamageRate + ",GrowthMonths:" + StringUtil.CollectionToString(GrowthMonths) + ",TechPoint:" + TechPoint + ",CropOutputs:" + StringUtil.CollectionToString(CropOutputs) + ",Lifespan:" + Lifespan + ",RepeatLevel:" + RepeatLevel + ",LevelDatas:" + StringUtil.CollectionToString(LevelDatas) + ",}";
	}

	public CropLevelData GetLevelData(int level)
	{
		return LevelDatas[Mathf.Clamp(level, 0, LevelDatas.Length - 1)];
	}

	public Sprite GetLevelSprite(int levelIndex, int skinIndex = 0)
	{
		SpriteAsset[] skins = GetLevelData(levelIndex).Skins;
		return skins[Mathf.Clamp(skinIndex, 0, skins.Length - 1)].Asset;
	}
}
