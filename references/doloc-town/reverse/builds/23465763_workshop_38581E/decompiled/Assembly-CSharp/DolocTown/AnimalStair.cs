using UnityEngine;

namespace DolocTown;

public readonly struct AnimalStair
{
	public readonly Vector2Int start;

	public readonly int length;

	public readonly Vector2Int center;

	public int EndPointX => start.x + length;

	public RectInt Area => new RectInt(start.x, start.y, length, 1);

	public bool IsValid => length > 0;

	public AnimalStair(Vector2Int start, int length)
	{
		this.start = start;
		this.length = length;
		center = new Vector2Int(start.x + length / 2, start.y);
	}

	public bool Contains(Vector2Int pos)
	{
		if (pos.y != start.y)
		{
			return false;
		}
		if (pos.x >= start.x)
		{
			return pos.x < EndPointX;
		}
		return false;
	}

	public bool Contains(AnimalStair other)
	{
		if (other.start.y != start.y)
		{
			return false;
		}
		if (start.x <= other.start.x)
		{
			return EndPointX >= other.EndPointX;
		}
		return false;
	}

	public bool IsConnected(AnimalStair other)
	{
		if (other.start.y != start.y)
		{
			return false;
		}
		if (start.x != other.start.x + other.length)
		{
			return other.start.x == start.x + length;
		}
		return true;
	}

	public AnimalStair Connect(AnimalStair other)
	{
		return new AnimalStair(new Vector2Int(Mathf.Min(start.x, other.start.x), start.y), length + other.length);
	}

	public bool _Touch(AnimalStair other, int threshold = 0)
	{
		return CalculateAdjacentArea(this, other) >= threshold;
	}

	public static int CalculateAdjacentArea(AnimalStair a, AnimalStair b)
	{
		int x = a.start.x;
		int a2 = a.start.x + a.length;
		int x2 = b.start.x;
		int b2 = b.start.x + b.length;
		int num = Mathf.Max(x, x2);
		return Mathf.Min(a2, b2) - num;
	}

	public bool Touch(AnimalStair other, int threshold = 0)
	{
		if (Mathf.Abs(start.y - other.start.y) != 1)
		{
			return false;
		}
		return _Touch(other, threshold);
	}

	public static bool operator ==(AnimalStair L, AnimalStair R)
	{
		if (L.start == R.start)
		{
			return L.length == R.length;
		}
		return false;
	}

	public static bool operator !=(AnimalStair L, AnimalStair R)
	{
		if (!(L.start != R.start))
		{
			return L.length != R.length;
		}
		return true;
	}
}
