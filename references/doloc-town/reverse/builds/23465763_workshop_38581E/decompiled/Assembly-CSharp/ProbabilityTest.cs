using System;
using System.Collections.Generic;
using DolocTown;
using RedSaw;
using UnityEngine;

public class ProbabilityTest : MonoBehaviour
{
	[Serializable]
	private struct LutItem
	{
		[SerializeField]
		public string name;

		[SerializeField]
		public int probability;
	}

	[SerializeField]
	private List<LutItem> items;

	[SerializeField]
	private bool isMultiTiems;

	[SerializeField]
	private int generateCount = 5;

	[SerializeField]
	private int generateTimes = 1000;

	private bool ValidateLutItems(List<LutItem> items)
	{
		if (items == null || items.Count == 0)
		{
			return false;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (LutItem item in items)
		{
			if (item.probability <= 0)
			{
				return false;
			}
			if (item.name.IsNullOrEmpty())
			{
				return false;
			}
			if (!hashSet.Add(item.name))
			{
				return false;
			}
		}
		return true;
	}

	public void Test()
	{
		if (!ValidateLutItems(items))
		{
			Debug.LogError("配置项目不合法");
			return;
		}
		int[] array = new int[items.Count];
		string[] array2 = new string[items.Count];
		for (int i = 0; i < items.Count; i++)
		{
			array[i] = items[i].probability;
			array2[i] = items[i].name;
		}
		if (isMultiTiems)
		{
			GenerateMultiTimes(array, array2, generateCount, generateTimes);
		}
		else
		{
			GenerateSingleTime(array, array2, generateCount);
		}
	}

	private void GenerateSingleTime(int[] probabilities, string[] names, int count)
	{
		int[] array = new int[probabilities.Length];
		for (int i = 0; i < count; i++)
		{
			int value;
			int num = RandomUtils.RussianRoulette(probabilities, out value);
			array[num]++;
		}
		string text = string.Empty;
		for (int j = 0; j < array.Length; j++)
		{
			text += $"{names[j]}x{array[j]}  ";
		}
		Debug.Log(text);
	}

	private void GenerateMultiTimes(int[] probability, string[] names, int count, int times)
	{
		int[] array = new int[probability.Length];
		for (int i = 0; i < times; i++)
		{
			for (int j = 0; j < count; j++)
			{
				int value;
				int num = RandomUtils.RussianRoulette(probability, out value);
				array[num]++;
			}
		}
		int num2 = 0;
		foreach (int num3 in probability)
		{
			num2 += num3;
		}
		float[] array2 = new float[probability.Length];
		for (int k = 0; k < probability.Length; k++)
		{
			array2[k] = (float)probability[k] / (float)num2;
		}
		int num4 = count * times;
		float[] array3 = new float[array.Length];
		for (int l = 0; l < array.Length; l++)
		{
			array3[l] = (float)array[l] / (float)num4;
		}
		string text = string.Empty;
		for (int m = 0; m < array.Length; m++)
		{
			text += $"{names[m]} x {array[m]} (期望占比:{array2[m]:P}) (真实占比:{array3[m]:P})\n";
		}
		Debug.Log(text);
	}
}
