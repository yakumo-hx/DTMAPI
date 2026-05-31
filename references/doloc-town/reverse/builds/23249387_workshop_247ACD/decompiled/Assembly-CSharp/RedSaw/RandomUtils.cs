using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedSaw;

public static class RandomUtils
{
	public static bool Dice(float value)
	{
		return UnityEngine.Random.value < value;
	}

	public static int Coin(float value = 0.5f)
	{
		if (!(UnityEngine.Random.value < value))
		{
			return -1;
		}
		return 1;
	}

	public static int Coin01(float value = 0.5f)
	{
		if (!(UnityEngine.Random.value < value))
		{
			return 0;
		}
		return 1;
	}

	public static int DiceCount(this Vector2Int range, int minCount = 1)
	{
		if (range.x >= range.y)
		{
			return Mathf.Max(range.x, minCount);
		}
		return Mathf.Max(minCount, UnityEngine.Random.Range(range.x, range.y + 1));
	}

	public static int DiceCount(int min, int max)
	{
		if (min >= max)
		{
			return Mathf.Max(min, max);
		}
		return UnityEngine.Random.Range(min, max + 1);
	}

	public static T ChoiceFrom<T>(params T[] values)
	{
		return values.Choice();
	}

	public static T Choice<T>(this T[] values)
	{
		if (values == null || values.Length == 0)
		{
			return default(T);
		}
		if (values.Length == 1)
		{
			return values[0];
		}
		return values[UnityEngine.Random.Range(0, values.Length)];
	}

	public static T Choice<T>(this List<T> values)
	{
		if (values == null || values.Count == 0)
		{
			return default(T);
		}
		if (values.Count == 1)
		{
			return values[0];
		}
		return values[UnityEngine.Random.Range(0, values.Count)];
	}

	public static T ChoiceMin<T>(this IEnumerable<T> values, Func<T, int> dstFunc)
	{
		T[] array = values?.ToArray() ?? Array.Empty<T>();
		if (array.Length == 0)
		{
			return default(T);
		}
		if (array.Length == 1)
		{
			return array[0];
		}
		int num = dstFunc(array[0]);
		T result = array[0];
		for (int i = 1; i < array.Length; i++)
		{
			int num2 = dstFunc(array[i]);
			if (num2 < num)
			{
				num = num2;
				result = array[i];
			}
		}
		return result;
	}

	public static T[] Choice<T>(T[] values, int count)
	{
		if (values == null || values.Length == 0)
		{
			return new T[0];
		}
		if (values.Length == 1)
		{
			return new T[1] { values[0] };
		}
		if (count <= 0)
		{
			return new T[0];
		}
		if (count >= values.Length)
		{
			return values;
		}
		for (int i = 0; i < values.Length; i++)
		{
			int num = UnityEngine.Random.Range(0, values.Length);
			int num2 = num;
			int num3 = i;
			T val = values[i];
			T val2 = values[num];
			values[num2] = val;
			values[num3] = val2;
		}
		T[] array = new T[count];
		for (int j = 0; j < count; j++)
		{
			array[j] = values[j];
		}
		return array;
	}

	public static T Choice<T>(this T[] values, int[] weights)
	{
		if (values == null || values.Length == 0)
		{
			return default(T);
		}
		if (values.Length == 1)
		{
			return values[0];
		}
		return values[_RussianRoulette(weights)];
	}

	public static T Choice<T>(this T[] values, float[] weights)
	{
		if (values == null || values.Length == 0)
		{
			return default(T);
		}
		if (values.Length == 1)
		{
			return values[0];
		}
		float[] array = new float[weights.Length];
		array[0] = weights[0];
		for (int i = 1; i < weights.Length; i++)
		{
			array[i] = array[i - 1] + weights[i];
		}
		double num = new System.Random().NextDouble() * (double)array.Last();
		for (int j = 0; j < array.Length; j++)
		{
			if (num < (double)array[j])
			{
				return values[j];
			}
		}
		return values[^1];
	}

	public static T[] Shuffle<T>(this T[] array)
	{
		if (array == null || array.Length <= 1)
		{
			return array;
		}
		for (int i = 0; i < array.Length; i++)
		{
			int num = UnityEngine.Random.Range(0, array.Length);
			int num2 = num;
			int num3 = i;
			T val = array[i];
			T val2 = array[num];
			array[num2] = val;
			array[num3] = val2;
		}
		return array;
	}

	public static List<T> Shuffle<T>(this List<T> array)
	{
		if (array == null || array.Count <= 1)
		{
			return array;
		}
		for (int i = 0; i < array.Count; i++)
		{
			int num = UnityEngine.Random.Range(0, array.Count);
			int index = num;
			int index2 = i;
			T val = array[i];
			T val2 = array[num];
			T val4 = (array[index] = val);
			val4 = (array[index2] = val2);
		}
		return array;
	}

	public static T[] ShuffleByWeight<T>(this T[] array, Func<T, float> weightGetter)
	{
		System.Random random = new System.Random();
		if (weightGetter == null)
		{
			weightGetter = (T _) => (float)random.NextDouble();
		}
		return (from x in array
			select new
			{
				Value = x,
				Weight = weightGetter(x)
			} into x
			orderby x.Weight descending
			select x.Value).ToArray();
	}

	public static T[] RandomSample<T>(T[] array, int count)
	{
		if (array == null || array.Length == 0)
		{
			return new T[0];
		}
		if (array.Length == 1)
		{
			return new T[1] { array[0] };
		}
		if (count <= 0)
		{
			return new T[0];
		}
		if (count >= array.Length)
		{
			return array;
		}
		for (int i = 0; i < array.Length; i++)
		{
			int num = UnityEngine.Random.Range(0, array.Length);
			int num2 = num;
			int num3 = i;
			T val = array[i];
			T val2 = array[num];
			array[num2] = val;
			array[num3] = val2;
		}
		T[] array2 = new T[count];
		for (int j = 0; j < count; j++)
		{
			array2[j] = array[j];
		}
		return array2;
	}

	public static int RussianRoulette(int[] probabilities, out int value)
	{
		value = 0;
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
		value = UnityEngine.Random.Range(0, num);
		for (int j = 0; j < probabilities.Length; j++)
		{
			num2 += probabilities[j];
			if (value < num2)
			{
				return j;
			}
		}
		return 0;
	}

	public static int[] RussianRouletteMulti(float[] probabilities, int count)
	{
		if (probabilities == null || probabilities.Length == 0)
		{
			return null;
		}
		if (probabilities.Length == 1)
		{
			return new int[count];
		}
		int[] array = new int[count];
		for (int i = 0; i < count; i++)
		{
			float num = 0f;
			float value = UnityEngine.Random.value;
			bool flag = false;
			for (int j = 0; j < probabilities.Length; j++)
			{
				num += probabilities[j];
				if (value < num)
				{
					array[i] = j;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				array[i] = probabilities.Length - 1;
			}
		}
		return array;
	}

	public static int RussianRoulette(float[] probabilities, out float value)
	{
		value = 0f;
		if (probabilities == null || probabilities.Length == 0)
		{
			return -1;
		}
		if (probabilities.Length == 1)
		{
			return 0;
		}
		float num = 0f;
		value = UnityEngine.Random.value;
		for (int i = 0; i < probabilities.Length; i++)
		{
			num += probabilities[i];
			if (value < num)
			{
				return i;
			}
		}
		return probabilities.Length - 1;
	}

	public static int _RussianRoulette(int[] probabilities)
	{
		if (probabilities.Length == 1)
		{
			return 0;
		}
		int num = 0;
		int num2 = UnityEngine.Random.Range(0, probabilities.Sum());
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

	public static int _RussianRoulette(int[] probabilities, int value)
	{
		if (probabilities.Length == 1)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < probabilities.Length; i++)
		{
			num += probabilities[i];
			if (value < num)
			{
				return i;
			}
		}
		return 0;
	}

	public static int _RussianRoulette(float[] probabilities)
	{
		if (probabilities.Length == 1)
		{
			return 0;
		}
		float num = probabilities[0];
		for (int i = 1; i < probabilities.Length; i++)
		{
			num += probabilities[i];
		}
		float num2 = 0f;
		float num3 = UnityEngine.Random.value * num;
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
}
