using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class GridSelector
{
	private Vector2Int currentCoord;

	public List<int> rowLengths = new List<int>();

	public List<int> indexBuffer = new List<int>();

	public Alignment alignment;

	private Action<int> callback;

	public int currentIndex { get; private set; }

	public int maxRow => rowLengths.Count - 1;

	public int maxIndex => indexBuffer[^1] - 1;

	public int currentRow => CalRow(currentIndex);

	public GridSelector(Alignment alignment = Alignment.Left)
	{
		this.alignment = alignment;
	}

	public void Init(Action<int> callback, Alignment alignment = Alignment.Left)
	{
		Clear();
		this.callback = callback;
		this.alignment = alignment;
	}

	public void Init(int total, int colum, Action<int> callback, Alignment alignment = Alignment.Left)
	{
		Init(callback, alignment);
		AddLengths(total, colum);
	}

	public void AddLengths(int total, int colum)
	{
		int num = Mathf.CeilToInt((float)total / (float)colum);
		for (int i = 0; i < num; i++)
		{
			if (i == num - 1)
			{
				int num2 = total % colum;
				int num3 = ((num2 == 0) ? colum : num2);
				rowLengths.Add(num3);
				indexBuffer.Add(indexBuffer[^1] + num3);
			}
			else
			{
				rowLengths.Add(colum);
				indexBuffer.Add(indexBuffer[^1] + colum);
			}
		}
	}

	public void Clear()
	{
		currentIndex = 0;
		rowLengths.Clear();
		indexBuffer.Clear();
		indexBuffer.Add(0);
		callback = null;
	}

	public void SetAlignment(Alignment alignment)
	{
		this.alignment = alignment;
	}

	public void SetSelectionIndex(int idx, bool shouldCallback = true)
	{
		int num = currentIndex;
		currentIndex = Mathf.Clamp(idx, 0, maxIndex);
		if (currentIndex != num && shouldCallback)
		{
			callback?.Invoke(currentIndex);
		}
	}

	public void MoveLeft()
	{
		currentIndex = ((currentIndex > 0) ? (currentIndex - 1) : maxIndex);
		callback?.Invoke(currentIndex);
	}

	public void MoveRight()
	{
		currentIndex = ((currentIndex < maxIndex) ? (currentIndex + 1) : 0);
		callback?.Invoke(currentIndex);
	}

	public void MoveDown()
	{
		int num = CalRow(currentIndex);
		int index = ((num != maxRow) ? (num + 1) : 0);
		int num2 = rowLengths[num];
		int num3 = rowLengths[index];
		int num4 = currentIndex - indexBuffer[num];
		switch (alignment)
		{
		case Alignment.Center:
		{
			int num5 = Mathf.RoundToInt((float)num4 / (float)num2 * (float)num3);
			currentIndex = num5 + indexBuffer[index];
			break;
		}
		case Alignment.Left:
		{
			int num5 = Mathf.Min(num4, num3 - 1);
			currentIndex = num5 + indexBuffer[index];
			break;
		}
		}
		currentIndex = Mathf.Clamp(currentIndex, 0, maxIndex);
		callback?.Invoke(currentIndex);
	}

	public void MoveUp()
	{
		int num = CalRow(currentIndex);
		int index = ((num == 0) ? maxRow : (num - 1));
		int num2 = rowLengths[num];
		int num3 = rowLengths[index];
		int num4 = currentIndex - indexBuffer[num];
		switch (alignment)
		{
		case Alignment.Center:
		{
			int num5 = Mathf.RoundToInt((float)num4 / (float)num2 * (float)num3);
			currentIndex = num5 + indexBuffer[index];
			break;
		}
		case Alignment.Left:
		{
			int num5 = Mathf.Min(num4, num3 - 1);
			currentIndex = num5 + indexBuffer[index];
			break;
		}
		}
		currentIndex = Mathf.Clamp(currentIndex, 0, maxIndex);
		callback?.Invoke(currentIndex);
	}

	private int CalRow(int idx)
	{
		for (int num = indexBuffer.Count - 1; num >= 0; num--)
		{
			if (idx >= indexBuffer[num])
			{
				return num;
			}
		}
		return 0;
	}
}
