using System;
using System.Collections.Generic;

namespace RedSaw.CommandLineInterface;

internal class LinearSelector
{
	private readonly List<string> optionsBuffer;

	private int currentIndex;

	private int TotalCount
	{
		get
		{
			if (optionsBuffer == null)
			{
				return 0;
			}
			return optionsBuffer.Count;
		}
	}

	public int SelectionIndex => currentIndex;

	public event Action<int> OnSelectionChanged;

	public LinearSelector()
	{
		optionsBuffer = new List<string>();
		currentIndex = -1;
	}

	public void LoadOptions(List<string> options)
	{
		optionsBuffer.Clear();
		optionsBuffer.AddRange(options);
		currentIndex = -1;
	}

	public bool GetCurrentSelection(out string selection)
	{
		selection = string.Empty;
		if (currentIndex == -1)
		{
			return false;
		}
		selection = optionsBuffer[currentIndex];
		return true;
	}

	public void MoveNext()
	{
		if (TotalCount != 0)
		{
			if (currentIndex == -1)
			{
				currentIndex = 0;
				this.OnSelectionChanged?.Invoke(currentIndex);
			}
			else
			{
				currentIndex = ((currentIndex < TotalCount - 1) ? (currentIndex + 1) : 0);
				this.OnSelectionChanged?.Invoke(currentIndex);
			}
		}
	}

	public void MoveLast()
	{
		if (TotalCount != 0)
		{
			if (currentIndex == -1)
			{
				currentIndex = TotalCount - 1;
				this.OnSelectionChanged?.Invoke(currentIndex);
			}
			else
			{
				currentIndex = ((currentIndex > 0) ? (currentIndex - 1) : (TotalCount - 1));
				this.OnSelectionChanged?.Invoke(currentIndex);
			}
		}
	}
}
