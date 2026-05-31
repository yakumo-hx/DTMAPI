namespace DolocTown.GameData;

public static class MissionUtils
{
	public static string Label(this StringCompareMethod method)
	{
		return method switch
		{
			StringCompareMethod.EqualTo => "==", 
			StringCompareMethod.NotEqualTo => "!=", 
			StringCompareMethod.Contains => "包含", 
			_ => "??", 
		};
	}

	public static bool CompareString(string a, string b, StringCompareMethod method)
	{
		switch (method)
		{
		case StringCompareMethod.EqualTo:
			return a == b;
		case StringCompareMethod.NotEqualTo:
			return a != b;
		case StringCompareMethod.Contains:
			if (!a.Contains(b))
			{
				return b.Contains(a);
			}
			return true;
		default:
			return false;
		}
	}
}
