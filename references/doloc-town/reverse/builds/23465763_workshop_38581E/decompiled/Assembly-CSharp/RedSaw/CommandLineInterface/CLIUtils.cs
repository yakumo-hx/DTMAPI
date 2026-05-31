using System;

namespace RedSaw.CommandLineInterface;

public static class CLIUtils
{
	public static string TimeInfo
	{
		get
		{
			DateTime now = DateTime.Now;
			return "[" + PadZero(now.Hour) + ":" + PadZero(now.Minute) + ":" + PadZero(now.Second) + "] ";
		}
	}

	private static int GetEditDistance(string X, string Y)
	{
		int length = X.Length;
		int length2 = Y.Length;
		int[][] array = new int[length + 1][];
		for (int i = 0; i < length + 1; i++)
		{
			array[i] = new int[length2 + 1];
		}
		for (int j = 1; j <= length; j++)
		{
			array[j][0] = j;
		}
		for (int k = 1; k <= length2; k++)
		{
			array[0][k] = k;
		}
		for (int l = 1; l <= length; l++)
		{
			for (int m = 1; m <= length2; m++)
			{
				int num = ((X[l - 1] != Y[m - 1]) ? 1 : 0);
				array[l][m] = Math.Min(Math.Min(array[l - 1][m] + 1, array[l][m - 1] + 1), array[l - 1][m - 1] + num);
			}
		}
		return array[length][length2];
	}

	public static float FindSimilarity(string x, string y)
	{
		if (x == null || y == null)
		{
			return 0f;
		}
		float num = Math.Max(x.Length, y.Length);
		if (num > 0f)
		{
			return (num - (float)GetEditDistance(x, y)) / num;
		}
		return 1f;
	}

	private static string PadZero(int value)
	{
		if (value >= 10)
		{
			return $"{value}";
		}
		return $"0{value}";
	}
}
