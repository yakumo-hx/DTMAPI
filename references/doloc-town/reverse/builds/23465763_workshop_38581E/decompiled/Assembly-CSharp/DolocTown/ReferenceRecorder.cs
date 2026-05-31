using System.Collections.Generic;

namespace DolocTown;

public class ReferenceRecorder
{
	private readonly Dictionary<int, int> referenceCount = new Dictionary<int, int>();

	public void AddReference(int id)
	{
		if (!referenceCount.TryAdd(id, 1))
		{
			referenceCount[id]++;
		}
	}

	public int SelectMinimumReference()
	{
		if (referenceCount.Count == 0)
		{
			return -1;
		}
		int num = int.MaxValue;
		int result = -1;
		foreach (var (num4, num5) in referenceCount)
		{
			if (num5 < num)
			{
				num = num5;
				result = num4;
			}
		}
		return result;
	}
}
