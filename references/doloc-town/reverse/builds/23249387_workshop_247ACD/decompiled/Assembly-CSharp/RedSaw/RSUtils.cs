using System.Collections.Generic;
using UnityEngine;

namespace RedSaw;

public static class RSUtils
{
	public static float Dice()
	{
		return Random.value;
	}

	public static bool Dice(float value)
	{
		return Random.value < value;
	}

	public static int RussianRoulette(Vector4 probabilitites)
	{
		return RussianRoulette(new float[4] { probabilitites.x, probabilitites.y, probabilitites.z, probabilitites.w });
	}

	public static int RussianRoulette(float[] probabilities)
	{
		int num = -1;
		int num2 = probabilities.Length - 1;
		float[] array = new float[probabilities.Length];
		probabilities.CopyTo(array, 0);
		for (int i = 1; i < array.Length; i++)
		{
			array[i] += array[i - 1];
		}
		float num3 = Dice();
		while (num2 - num != 1)
		{
			int num4 = (num2 + num) / 2;
			if (num3 > array[num4])
			{
				num = num4;
			}
			else
			{
				num2 = num4;
			}
		}
		return num2;
	}

	public static int RussianRoulette(int[] probabilities)
	{
		if (probabilities == null || probabilities.Length == 0)
		{
			return -1;
		}
		if (probabilities.Length == 1)
		{
			return 0;
		}
		int num = probabilities[0];
		for (int i = 1; i < probabilities.Length; i++)
		{
			num += probabilities[i];
		}
		int num2 = 0;
		int num3 = Random.Range(0, num);
		for (int j = 0; j < probabilities.Length; j++)
		{
			num2 += probabilities[j];
			if (num3 < num2)
			{
				return j;
			}
		}
		return 0;
	}

	public static int _RussianRoulette(int[] probabilities)
	{
		if (probabilities.Length == 1)
		{
			return 0;
		}
		int num = probabilities[0];
		for (int i = 1; i < probabilities.Length; i++)
		{
			num += probabilities[i];
		}
		int num2 = 0;
		int num3 = Random.Range(0, num);
		for (int j = 0; j < probabilities.Length; j++)
		{
			num2 += probabilities[j];
			if (num3 < num2)
			{
				return j;
			}
		}
		return 0;
	}

	public static int _RussianRoulette(ushort[] probabilities)
	{
		if (probabilities.Length == 1)
		{
			return 0;
		}
		int maxExclusive = SumProbabilities(probabilities);
		int num = 0;
		int num2 = Random.Range(0, maxExclusive);
		for (int i = 0; i < probabilities.Length; i++)
		{
			num += probabilities[i];
			if (num2 < num)
			{
				return i;
			}
		}
		return 0;
	}

	public static int _RussianRoulette(ushort[] probabilities, int total)
	{
		if (probabilities.Length == 1)
		{
			return 0;
		}
		int num = 0;
		int num2 = Random.Range(0, total);
		for (int i = 0; i < probabilities.Length; i++)
		{
			num += probabilities[i];
			if (num2 < num)
			{
				return i;
			}
		}
		return 0;
	}

	public static int SumProbabilities(ushort[] probabilities)
	{
		if (probabilities == null || probabilities.Length == 0)
		{
			return 0;
		}
		if (probabilities.Length == 1)
		{
			return probabilities[0];
		}
		int num = 0;
		for (int i = 0; i < probabilities.Length; i++)
		{
			num += probabilities[i];
		}
		return num;
	}

	public static bool LayerMaskCheck(GameObject obj, LayerMask layerMask)
	{
		return (layerMask.value & (1 << obj.layer)) > 0;
	}

	public static bool LayerMaskCheck(int layer, LayerMask layerMask)
	{
		return (layerMask.value & (1 << layer)) > 0;
	}

	public static Dictionary<string, T> loadIntoDictionary<T>(T[] values) where T : Object
	{
		Dictionary<string, T> dictionary = new Dictionary<string, T>();
		foreach (T val in values)
		{
			if (!(val == null))
			{
				dictionary.Add(val.name, val);
			}
		}
		return dictionary;
	}
}
