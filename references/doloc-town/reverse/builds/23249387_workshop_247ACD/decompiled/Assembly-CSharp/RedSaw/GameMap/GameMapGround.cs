using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RedSaw.GameMap;

public class GameMapGround
{
	private readonly struct Column
	{
		public readonly Vector2Int pos;

		private readonly int top;

		public int bottom => pos.y;

		public Column(Vector2Int pos, int height)
		{
			this.pos = pos;
			top = pos.y + height - 1;
		}

		public bool Contains(Column other)
		{
			if (bottom <= other.bottom)
			{
				return top >= other.top;
			}
			return false;
		}

		public bool Touch(Column other, int tolerance)
		{
			if (top < other.bottom || other.top < bottom)
			{
				return false;
			}
			int num = Mathf.Min(top, other.top);
			int num2 = Mathf.Max(bottom, other.bottom);
			return num - num2 + 1 >= tolerance;
		}
	}

	private readonly IGameMap map;

	private Column[][] allColumns;

	public GameMapGround(IGameMap map)
	{
		this.map = map;
		allColumns = GetAllColumns(map);
	}

	public Vector2Int[] GetJumpPoints(Vector2Int pos, int hRange, int vRange, int jumpTolerance = 3, int touchTolerance = 2)
	{
		Column currentColumn = GetCurrentColumn(map, pos, vRange);
		Column continues = new Column(pos, jumpTolerance);
		List<Vector2Int> list = new List<Vector2Int>();
		foreach (Column item in FindColumns(currentColumn, continues, hRange, vRange, 1, touchTolerance))
		{
			list.Add(item.pos);
		}
		foreach (Column item2 in FindColumns(currentColumn, continues, hRange, vRange, -1, touchTolerance))
		{
			list.Add(item2.pos);
		}
		return list.ToArray();
	}

	private IEnumerable<Column> FindColumns(Column tolerance, Column continues, int hRange, int vRange, int dir, int touchTolerance = 2, int step = 1)
	{
		bool shouldContinue = false;
		Column[] columnsFromX = GetColumnsFromX(tolerance.pos.x + step * dir);
		for (int i = 0; i < columnsFromX.Length; i++)
		{
			Column g = columnsFromX[i];
			if (g.Touch(tolerance, touchTolerance))
			{
				if (g.pos.y != 0 && Mathf.Abs(g.pos.y - tolerance.pos.y) <= vRange && DiagonalTest(tolerance, g))
				{
					yield return g;
				}
				if (!shouldContinue && g.Contains(continues))
				{
					shouldContinue = true;
				}
			}
		}
		if (!(step < hRange && shouldContinue))
		{
			yield break;
		}
		foreach (Column item in FindColumns(tolerance, continues, hRange, vRange, dir, touchTolerance, step + 1))
		{
			yield return item;
		}
	}

	private bool DiagonalTest(Column tolerance, Column g)
	{
		if (tolerance.bottom <= g.bottom)
		{
			return true;
		}
		int num = tolerance.bottom - g.bottom;
		if (num < 2)
		{
			return true;
		}
		return Mathf.Abs(tolerance.pos.x - g.pos.x) < num;
	}

	private Column[] GetColumnsFromX(int x)
	{
		if (x < 0 || x >= allColumns.Length)
		{
			return Array.Empty<Column>();
		}
		return allColumns[x];
	}

	private Column GetCurrentColumn(IGameMap gameMap, Vector2Int current, int vRange)
	{
		int num = current.y + 1;
		int num2 = vRange + current.y;
		int num3 = 1;
		for (int i = num; i <= num2; i++)
		{
			Vector2Int pos = new Vector2Int(current.x, i);
			if (gameMap.IsObstacle(pos))
			{
				return new Column(current, num3);
			}
			num3++;
		}
		return new Column(current, num3);
	}

	private Column[][] GetAllColumns(IGameMap map)
	{
		List<Column>[] array = new List<Column>[map.Width];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new List<Column>();
		}
		Vector2Int[] groundPositions = map.GroundPositions;
		for (int j = 0; j < groundPositions.Length; j++)
		{
			Vector2Int pt = groundPositions[j];
			array[pt.x].Add(GetColumnFromGroundPoint(map, pt));
		}
		return array.Select((List<Column> x) => x.ToArray()).ToArray();
	}

	private Column GetColumnFromGroundPoint(IGameMap map, Vector2Int pt)
	{
		int num = pt.y + 1;
		for (int i = num; i < map.Height; i++)
		{
			Vector2Int pos = new Vector2Int(pt.x, i);
			if (map.IsObstacle(pos))
			{
				return new Column(pt, pos.y - num + 1);
			}
		}
		return new Column(pt, map.Height - num + 1);
	}
}
