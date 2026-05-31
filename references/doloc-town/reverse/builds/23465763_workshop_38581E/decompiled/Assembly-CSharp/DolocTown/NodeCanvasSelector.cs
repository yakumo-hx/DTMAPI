using System;
using UnityEngine;

namespace DolocTown;

public class NodeCanvasSelector
{
	private Func<Vector2Int, bool> containsPosition;

	private Vector2Int selectedPosition;

	private Vector2Int size;

	public Vector2Int currentSelection => selectedPosition;

	private Action<Vector2Int> callback { get; set; }

	private int dstToLeft => selectedPosition.x;

	private int dstToRight => size.x - selectedPosition.x;

	private int dstToTop => size.y - selectedPosition.y;

	private int dstToBottom => selectedPosition.y;

	public NodeCanvasSelector(Action<Vector2Int> cb)
	{
		callback = cb;
	}

	public void setModelData(Vector2Int range, Func<Vector2Int, bool> containsPosition, Vector2Int defaultPosition)
	{
		this.containsPosition = containsPosition;
		selectedPosition = defaultPosition;
		size = range;
	}

	private void moveCallback()
	{
		callback?.Invoke(selectedPosition);
	}

	public void setSelectedPosition(Vector2Int pos, bool shouldCallback)
	{
		if (pos.x >= 0 && pos.x < size.x && pos.y >= 0 && pos.y < size.y && pos != selectedPosition)
		{
			selectedPosition = pos;
			if (shouldCallback)
			{
				moveCallback();
			}
		}
	}

	private bool testPoint(Vector2Int pos)
	{
		if (containsPosition(pos))
		{
			selectedPosition = pos;
			moveCallback();
			return true;
		}
		return false;
	}

	private bool testHorizontal(Vector2Int pos)
	{
		if (testPoint(pos))
		{
			return true;
		}
		if (testPoint(new Vector2Int(pos.x + 1, pos.y)))
		{
			return true;
		}
		if (testPoint(new Vector2Int(pos.x - 1, pos.y)))
		{
			return true;
		}
		return false;
	}

	private bool testVertical(Vector2Int pos)
	{
		if (testPoint(pos))
		{
			return true;
		}
		if (testPoint(new Vector2Int(pos.x, pos.y + 1)))
		{
			return true;
		}
		if (testPoint(new Vector2Int(pos.x, pos.y - 1)))
		{
			return true;
		}
		return false;
	}

	public void moveUp()
	{
		int num = dstToTop;
		if (num <= 0)
		{
			return;
		}
		Vector2Int pos = selectedPosition;
		for (int i = 0; i < num; i++)
		{
			pos.y++;
			if (testPoint(pos))
			{
				break;
			}
		}
	}

	public void moveDown()
	{
		int num = dstToBottom;
		if (num <= 0)
		{
			return;
		}
		Vector2Int pos = selectedPosition;
		for (int i = 0; i < num; i++)
		{
			pos.y--;
			if (testPoint(pos))
			{
				break;
			}
		}
	}

	public void moveLeft()
	{
		int num = dstToLeft;
		if (num <= 0)
		{
			return;
		}
		Vector2Int pos = selectedPosition;
		for (int i = 0; i < num; i++)
		{
			pos.x--;
			if (testVertical(pos))
			{
				break;
			}
		}
	}

	public void moveRight()
	{
		int num = dstToRight;
		if (num <= 0)
		{
			return;
		}
		Vector2Int pos = selectedPosition;
		for (int i = 0; i < num; i++)
		{
			pos.x++;
			if (testVertical(pos))
			{
				break;
			}
		}
	}
}
