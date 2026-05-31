using System;
using RedSaw;

namespace DolocTown;

public static class LutHelper
{
	public static int[] Sample(float[] probabilities, int totalCount)
	{
		if (probabilities == null || probabilities.Length == 0 || totalCount == 0)
		{
			return Array.Empty<int>();
		}
		int[] array = new int[probabilities.Length];
		for (int i = 0; i < totalCount; i++)
		{
			array[RandomUtils._RussianRoulette(probabilities)]++;
		}
		return array;
	}
}
