using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

internal class QueryCache<T> where T : struct
{
	private readonly Dictionary<string, T[]> buffer = new Dictionary<string, T[]>();

	private readonly List<string> queryHistory = new List<string>();

	private readonly int capacity;

	public QueryCache(int capacity)
	{
		this.capacity = Math.Max(1, capacity);
	}

	public void Cache(string query, T[] result)
	{
		if (!buffer.ContainsKey(query))
		{
			if (queryHistory.Count + 1 > capacity)
			{
				buffer.Remove(queryHistory[0]);
				queryHistory.RemoveAt(0);
			}
			buffer.Add(query, result);
			queryHistory.Add(query);
		}
	}

	public bool GetCache(string query, out T[] result)
	{
		return buffer.TryGetValue(query, out result);
	}
}
