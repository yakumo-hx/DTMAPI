using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RedSaw;

[JsonObject(MemberSerialization.OptIn)]
public class IndexCounter
{
	[JsonProperty]
	private readonly List<int> free = new List<int>();

	[JsonProperty]
	private int counter;

	public int NextIndex
	{
		get
		{
			if (free.Count > 0)
			{
				int result = free[0];
				free.RemoveAt(0);
				return result;
			}
			return counter++;
		}
	}

	public IndexCounter()
	{
	}

	[JsonConstructor]
	protected IndexCounter(List<int> free, int counter)
	{
		this.counter = counter;
		this.free = free;
	}

	public void Remove(int index)
	{
		if (index >= 0 && !IsRecycled(index))
		{
			free.Add(index);
		}
	}

	public bool IsRecycled(int index)
	{
		if (index < 0)
		{
			return false;
		}
		if (free.Count > 0)
		{
			return free.Any((int t) => t == index);
		}
		return false;
	}

	public void Clear()
	{
		counter = 0;
		free.Clear();
	}
}
