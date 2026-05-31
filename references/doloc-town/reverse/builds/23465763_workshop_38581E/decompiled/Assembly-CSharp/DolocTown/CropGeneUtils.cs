using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config;
using DolocTown.Config.EnvOptimizer;
using DolocTown.Config.Plant;
using DolocTown.GameData;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public static class CropGeneUtils
{
	private const float ORIGIN_GROWTH_VALUE = 1f;

	private static readonly Dictionary<string, Type> InstanceTypes = ReflectionUtils.LoadIntoDict(typeof(CropGeneFunction).GetSubTypes());

	public static float CalcGrowthValue(this CropGeneFunction[] functions)
	{
		if (functions.IsNullOrEmpty())
		{
			return 1f;
		}
		float num = 1f;
		for (int i = 0; i < functions.Length; i++)
		{
			num = functions[i].HandleBuffData(num);
		}
		return num;
	}

	private static string GetInstanceName(CropGeneInfo proto)
	{
		if (proto == null)
		{
			return "CropGeneFunction";
		}
		return proto.Function.GetType().Name.Replace("CropGeneFuncProto", "CropGeneFunction");
	}

	public static CropGeneFunction CreateFunction(this CropGeneInfo proto)
	{
		if (proto == null)
		{
			return null;
		}
		if (!InstanceTypes.TryGetValue(GetInstanceName(proto), out var value))
		{
			Debug.LogWarning("未找到基因功能类型的实现：" + proto.Id);
			return null;
		}
		try
		{
			CropGeneFunction obj = Activator.CreateInstance(value, proto) as CropGeneFunction;
			if (obj == null)
			{
				Debug.LogError("无法创建基因功能实例：" + proto.Id + "，请检查其构造函数");
			}
			return obj;
		}
		catch (Exception arg)
		{
			Debug.LogError($"创建基因功能实例时发生异常：{proto.Id}\n{arg}");
			return null;
		}
	}

	public static CropGeneFunction[] CreateFunctions(this CropGeneInfo[] types)
	{
		return (from x in types.Distinct().ToArray()
			orderby DolocAPI.GetItemSortingOrder(x.CapsuleItem)
			select x into type
			select type.CreateFunction() into f
			where f != null
			select f).ToArray();
	}

	public static bool TryRollGene(this SeedInfo proto, out CropGeneInfo geneProto)
	{
		geneProto = null;
		if (proto == null)
		{
			return false;
		}
		if (DolocAPI.gameManager.gameInitConfig.ignoreCropGeneUnlock)
		{
			if (!DolocConfig.Tables.TbCropGeneMatrix.TryRollGene(proto.Id, out var geneId))
			{
				return false;
			}
			geneProto = DolocConfig.Tables.TbCropGene.GetOrDefault(geneId);
			return geneProto != null;
		}
		HashSet<string> hashSet = new HashSet<string>(DolocAPI.archiveHandle.farmData.unlockedGeneNames);
		hashSet.UnionWith(DolocConfig.Tables.TbCropGene.DefaultUnlockedGenes);
		if (!DolocConfig.Tables.TbCropGeneMatrix.TryRollGene(proto.Id, out var geneId2, hashSet.ToArray()))
		{
			return false;
		}
		geneProto = DolocConfig.Tables.TbCropGene.GetOrDefault(geneId2);
		return geneProto != null;
	}

	public static CropGeneInfo[] GetInheritedGenes(this CropGeneInfo[] parentGenes, GeneGroup naturalGeneGroup)
	{
		if (parentGenes.IsNullOrEmpty())
		{
			return Array.Empty<CropGeneInfo>();
		}
		HashSet<CropGeneInfo> hashSet = parentGenes.Where((CropGeneInfo x) => x != null).ToHashSet();
		if (hashSet.IsNullOrEmpty())
		{
			return Array.Empty<CropGeneInfo>();
		}
		int count = hashSet.Count;
		HashSet<CropGeneInfo> hashSet2 = new HashSet<CropGeneInfo>();
		CropGeneInfo[] array = hashSet.ToArray();
		foreach (CropGeneInfo cropGeneInfo in array)
		{
			if (naturalGeneGroup.ContainsGene(cropGeneInfo.Id) || DolocAPI.GlobalParameter.CompressGeneFixed.Contains(cropGeneInfo.Id))
			{
				hashSet2.Add(cropGeneInfo);
				hashSet.Remove(cropGeneInfo);
			}
		}
		if (hashSet.Count == 0)
		{
			return hashSet2.ToArray();
		}
		Vector2[] array2 = (DolocAPI.archiveHandle.IsEnvOptimizerComponentUnlocked(EnvOptimizerComponentType.VULTURE) ? DolocAPI.GlobalParameter.CompressGeneCountDistPost : DolocAPI.GlobalParameter.CompressGeneCountDistPre);
		Vector2 vector = array2[Mathf.Clamp(count - 1, 0, array2.Length - 1)];
		float num = Mathf.Clamp01(vector.x);
		float num2 = Mathf.Clamp01(vector.y);
		float value = UnityEngine.Random.value;
		int value2 = ((value < num) ? 1 : ((!(value < num + num2)) ? 3 : 2));
		value2 = Mathf.Clamp(value2, 0, count);
		int num3 = Mathf.Min(value2 - hashSet2.Count, hashSet.Count);
		for (int j = 0; j < num3; j++)
		{
			CropGeneInfo item = hashSet.ToArray().Choice(hashSet.Select((CropGeneInfo x) => x.CompressInheritWeight).ToArray());
			hashSet.Remove(item);
			hashSet2.Add(item);
			if (hashSet.Count == 0)
			{
				break;
			}
		}
		return hashSet2.ToArray();
	}

	public static CropGeneInfo[] SynthesizeGenes(this CropGeneInfo[] current, CropGeneInfo[] other)
	{
		List<CropGeneInfo> allGenes = new List<CropGeneInfo>();
		if (!current.IsNullOrEmpty())
		{
			allGenes.AddRange(current);
		}
		if (!other.IsNullOrEmpty())
		{
			allGenes.AddRange(other);
		}
		if (allGenes.Count == 0)
		{
			return Array.Empty<CropGeneInfo>();
		}
		HashSet<CropGeneInfo> hashSet = allGenes.Where((CropGeneInfo x) => x != null).ToHashSet();
		if (hashSet.IsNullOrEmpty())
		{
			return Array.Empty<CropGeneInfo>();
		}
		int count = allGenes.Count;
		HashSet<CropGeneInfo> hashSet2 = new HashSet<CropGeneInfo>();
		Vector2[] synthesisGeneCountDist = DolocAPI.GlobalParameter.SynthesisGeneCountDist;
		Vector2 vector = synthesisGeneCountDist[Mathf.Clamp(count - 1, 0, synthesisGeneCountDist.Length - 1)];
		float num = Mathf.Clamp01(vector.x);
		float num2 = Mathf.Clamp01(vector.y);
		float value = UnityEngine.Random.value;
		int value2 = ((value < num) ? 1 : ((!(value < num + num2)) ? 3 : 2));
		value2 = Mathf.Clamp(value2, 0, hashSet.Count);
		for (int i = 0; i < value2; i++)
		{
			CropGeneInfo item = hashSet.ToArray().Choice(hashSet.Select((CropGeneInfo x) => GetWeightSum(x.Id)).ToArray());
			hashSet.Remove(item);
			hashSet2.Add(item);
			if (hashSet.Count == 0)
			{
				break;
			}
		}
		return hashSet2.ToArray();
		int GetWeightSum(string id)
		{
			int num3 = 0;
			foreach (CropGeneInfo item2 in allGenes)
			{
				if (item2.Id == id)
				{
					num3 += item2.CompressInheritWeight;
				}
			}
			return num3;
		}
	}

	public static int CalcFinalOutput(this CropOutputData data)
	{
		int originCount = data.originCount;
		int num = Mathf.FloorToInt((float)data.originCount * data.finalCountMultiplication);
		originCount += num;
		originCount += data.finalCountAddition;
		return Mathf.Max(1, originCount);
	}

	public static float CalcFinalGrowthAddition(this GeneBuffData data, float sourceValue)
	{
		float num = Mathf.Max(0f, data.GrowthMultiplier);
		return sourceValue * num + data.GrowthAddition;
	}

	public static string GetDescription(this CropGeneInfo[] geneProtos, string[] natureGenes)
	{
		if (geneProtos.IsNullOrEmpty())
		{
			return string.Empty;
		}
		if (natureGenes?.ToHashSet() == null)
		{
			new HashSet<string>();
		}
		List<string> list = new List<string>();
		foreach (CropGeneInfo cropGeneInfo in geneProtos)
		{
			list.Add(DolocUtils.Format(DolocConfig.StaticTexts.ItemGeneDescFormat.ClearNoWrapSpace(), cropGeneInfo.Title, cropGeneInfo.Description.Colored(DolocUiColor.SLIENTCOLOR_GREY)));
		}
		return string.Join("\n", list);
	}
}
