using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

internal class InputHistory
{
	private readonly List<string> history;

	private readonly int capacity;

	private int lastIndex;

	public string[] History
	{
		get
		{
			return history.ToArray();
		}
		set
		{
			if (value == null || value.Length == 0)
			{
				return;
			}
			history.Clear();
			if (value.Length > capacity)
			{
				for (int i = 0; i < capacity; i++)
				{
					history.Add(value[i]);
				}
			}
			else
			{
				history.AddRange(value);
			}
			lastIndex = history.Count - 1;
		}
	}

	public string Last
	{
		get
		{
			if (history.Count == 0)
			{
				return string.Empty;
			}
			if (lastIndex < 0 || lastIndex >= history.Count)
			{
				lastIndex = history.Count - 1;
			}
			return history[lastIndex--];
		}
	}

	public string Next
	{
		get
		{
			if (history.Count == 0)
			{
				return string.Empty;
			}
			if (lastIndex >= history.Count || lastIndex < 0)
			{
				lastIndex = 0;
			}
			return history[lastIndex++];
		}
	}

	public InputHistory(int capacity)
	{
		lastIndex = 0;
		this.capacity = capacity;
		history = new List<string>();
	}

	public void Record(string input)
	{
		if (history.Count == capacity)
		{
			history.RemoveAt(0);
		}
		history.Add(input);
		lastIndex = history.Count - 1;
	}
}
