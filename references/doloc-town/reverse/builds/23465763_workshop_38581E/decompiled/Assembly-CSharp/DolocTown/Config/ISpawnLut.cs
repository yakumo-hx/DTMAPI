using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown.Config.Global;
using UnityEngine;

namespace DolocTown.Config;

public interface ISpawnLut
{
	int Ceiling { get; }

	List<SpawnData> SpawnDataList { get; }

	float[] SpawnWeights
	{
		get
		{
			float[] array = new float[SpawnDataList.Count];
			for (int i = 0; i < SpawnDataList.Count; i++)
			{
				array[i] = SpawnDataList[i].SpawnWeight;
			}
			return array;
		}
	}

	int GetCeiling()
	{
		int num = 0;
		foreach (SpawnData spawnData in SpawnDataList)
		{
			if (spawnData.Unlimited)
			{
				return int.MaxValue;
			}
			num += spawnData.MaxCount;
		}
		return num;
	}

	Dictionary<T, int> SpawnInternal<T>(int totalCount, Func<T, bool> spawnFilter, string debugInfo = "") where T : SpawnData
	{
		Dictionary<T, int> res = new Dictionary<T, int>();
		T[] array = (from x in SpawnDataList.OfType<T>()
			where spawnFilter?.Invoke(x) ?? true
			select x).ToArray();
		T[] array2;
		if (totalCount >= Ceiling)
		{
			array2 = array;
			foreach (T val in array2)
			{
				res[val] = val.MaxCount;
			}
			if (totalCount > Ceiling)
			{
				Debug.LogWarning(debugInfo + ": 生成数量大于查找表数量上限<" + GetType().Name + ">");
			}
			ClearEmpty();
			return res;
		}
		int num = 0;
		array2 = array;
		foreach (T val2 in array2)
		{
			res[val2] = 0;
			if (num + val2.MinCount >= totalCount)
			{
				res[val2] += totalCount - num;
				if (num + val2.MinCount > totalCount)
				{
					Debug.LogWarning(debugInfo + ": 生成数量小于查找表数量下限<" + GetType().Name + ">");
				}
				return res;
			}
			num += val2.MinCount;
			res[val2] += val2.MinCount;
		}
		int num2 = totalCount - num;
		if (num2 == 0)
		{
			return res;
		}
		float[] array3 = new float[array.Length];
		array3[0] = array[0].SpawnWeight;
		for (int j = 1; j < array.Length; j++)
		{
			array3[j] = array3[j - 1] + array[j].SpawnWeight;
		}
		System.Random random = new System.Random();
		while (num2-- > 0)
		{
			double num3 = random.NextDouble() * (double)array3.Last();
			for (int k = 0; k < array3.Length; k++)
			{
				if (num3 < (double)array3[k])
				{
					int num4 = (array[k].Unlimited ? int.MaxValue : array[k].MaxCount);
					if (res[array[k]] < num4)
					{
						res[array[k]]++;
						break;
					}
				}
			}
		}
		ClearEmpty();
		return res;
		void ClearEmpty()
		{
			T[] array4 = res.Keys.ToArray();
			foreach (T key in array4)
			{
				if (res[key] == 0)
				{
					res.Remove(key);
				}
			}
		}
	}

	List<(T, int)> SpawnByFloorOrderedInternal<T>(Func<T, bool> spawnFilter) where T : SpawnData
	{
		List<(T, int)> list = new List<(T, int)>();
		T[] array = (from x in SpawnDataList.OfType<T>()
			where spawnFilter?.Invoke(x) ?? true
			select x).ToArray();
		foreach (T val in array)
		{
			if (val.MinCount > 0)
			{
				list.Add((val, val.MinCount));
			}
		}
		return list;
	}

	bool SpawnSingleInternal<T>(Dictionary<string, int> currentCount, out T spawnData, Func<T, bool> spawnFilter) where T : SpawnData
	{
		T[] array = (from x in SpawnDataList.OfType<T>()
			where spawnFilter?.Invoke(x) ?? true
			select x).ToArray();
		spawnData = null;
		List<string> spawnIds = new List<string>();
		T[] array2 = array;
		foreach (T val in array2)
		{
			if (currentCount == null || !currentCount.TryGetValue(val.SpawnId, out var value) || val.Unlimited || value < val.MaxCount)
			{
				spawnIds.Add(val.SpawnId);
			}
		}
		if (spawnIds.Count == 0)
		{
			return false;
		}
		T[] array3 = array.Where((T x) => spawnIds.Contains(x.SpawnId)).ToArray();
		float[] array4 = new float[array3.Length];
		array4[0] = array3[0].SpawnWeight;
		for (int j = 1; j < array3.Length; j++)
		{
			array4[j] = array4[j - 1] + array3[j].SpawnWeight;
		}
		double num = new System.Random().NextDouble() * (double)array4.Last();
		for (int k = 0; k < array4.Length; k++)
		{
			if (num < (double)array4[k])
			{
				spawnData = array3[k];
				return true;
			}
		}
		return false;
	}
}
