using System;
using UnityEngine;

namespace DolocTown;

public class LinearSelector
{
	private int len;

	private int currentMaxIdx;

	private Action<int> callback;

	public int lastIndex { get; private set; }

	public int currentIndex { get; private set; }

	public int length => len;

	public LinearSelector(int count, Action<int> callback)
	{
		len = count;
		currentMaxIdx = count - 1;
		lastIndex = 0;
		currentIndex = 0;
		this.callback = callback;
	}

	public LinearSelector(int count)
	{
		currentMaxIdx = count - 1;
		lastIndex = 0;
		currentIndex = 0;
		callback = null;
	}

	public void setCount(int count)
	{
		len = count;
		currentMaxIdx = count - 1;
	}

	public void clearIndex()
	{
		lastIndex = currentIndex;
		currentIndex = 0;
	}

	public void setSelectionIndex(int idx, bool shouldCallback = true)
	{
		int num = currentIndex;
		currentIndex = Mathf.Clamp(idx, 0, length - 1);
		if (currentIndex != num && callback != null && shouldCallback)
		{
			lastIndex = num;
			callback(currentIndex);
		}
	}

	public void setCallback(Action<int> callback)
	{
		this.callback = callback;
	}

	public void moveNext()
	{
		if (len > 1)
		{
			lastIndex = currentIndex;
			if (currentIndex < currentMaxIdx)
			{
				currentIndex++;
				callback?.Invoke(currentIndex);
			}
			else
			{
				currentIndex = 0;
				callback?.Invoke(currentIndex);
			}
		}
	}

	public void moveLast()
	{
		if (len > 1)
		{
			lastIndex = currentIndex;
			if (currentIndex > 0)
			{
				currentIndex--;
				callback?.Invoke(currentIndex);
			}
			else
			{
				currentIndex = currentMaxIdx;
				callback?.Invoke(currentIndex);
			}
		}
	}
}
