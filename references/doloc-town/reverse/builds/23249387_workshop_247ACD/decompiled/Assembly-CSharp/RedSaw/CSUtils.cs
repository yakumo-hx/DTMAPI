using System;
using System.Collections.Generic;

namespace RedSaw;

public static class CSUtils
{
	public static bool HasIntersectionWith<T>(this HashSet<T> A, HashSet<T> B)
	{
		if (A.Count > B.Count)
		{
			return B.Overlaps(A);
		}
		return A.Overlaps(B);
	}

	public static bool Contains<T>(this Enum value, T flag) where T : Enum
	{
		int num = Convert.ToInt32(value);
		int num2 = Convert.ToInt32(flag);
		return (num & num2) == num2;
	}

	public static T[] Sort<T>(this T[] values) where T : Enum
	{
		Array.Sort(values, (T a, T b) => a.GetHashCode().CompareTo(b.GetHashCode()));
		return values;
	}
}
