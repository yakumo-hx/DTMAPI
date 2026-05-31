using System.Collections.Generic;
using UnityEngine;

namespace DolocTown;

public class AnimalPathFinderForRoom
{
	private readonly Room room;

	private readonly int maxWidth;

	private readonly IAnimalPathFinder[] pathFinders;

	public AnimalPathFinderForRoom(Room room, int maxWidth = 4)
	{
		this.room = room;
		this.maxWidth = Mathf.Max(2, maxWidth);
		pathFinders = new IAnimalPathFinder[maxWidth];
		pathFinders[0] = new AnimalPathFinder(room);
		for (int i = 1; i < maxWidth; i++)
		{
			pathFinders[i] = new AnimalPathFinderWidth(room, i + 1);
		}
	}

	private IAnimalPathFinder GetAnimalPathFinder(int width)
	{
		if (width < 1 || width > maxWidth)
		{
			return null;
		}
		return pathFinders[width - 1];
	}

	public Vector2Int[] FindPath(Vector2Int from, Vector2Int to, int width = 1)
	{
		if (HorizontalCheck(from, to, width, out var path))
		{
			return path;
		}
		IAnimalPathFinder animalPathFinder = GetAnimalPathFinder(width);
		if (animalPathFinder == null)
		{
			Debug.LogWarning("AnimalPathFinderForRoom: Invalid width for path finder: " + width);
			return null;
		}
		return animalPathFinder.FindPath(from, to);
	}

	private bool HorizontalCheck(Vector2Int from, Vector2Int to, int width, out Vector2Int[] path)
	{
		path = null;
		if (from.y != to.y)
		{
			return false;
		}
		int num = ((from.x < to.x) ? 1 : (-1));
		List<Vector2Int> list = new List<Vector2Int>();
		for (int i = from.x; i != to.x; i += num)
		{
			Vector2Int vector2Int = new Vector2Int(i, from.y);
			if (!AnimalUtils.IsPositionWalkable(room, vector2Int, width))
			{
				return false;
			}
			list.Add(vector2Int);
		}
		path = list.ToArray();
		return true;
	}

	public void OnEnvChanged()
	{
		IAnimalPathFinder[] array = pathFinders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnEnvChanged();
		}
	}
}
