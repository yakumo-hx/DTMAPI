using System.Collections.Generic;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public interface IFeeder : IAnimalInteractable
{
	bool IsFeederEmpty { get; }

	int FeederPriority { get; }

	int TakeFeeds(int require, out string name);

	int CompareTo(Vector2Int animalPosition, IFeeder another)
	{
		if (another.FeederPriority == FeederPriority)
		{
			int num = animalPosition.ManhattenDistance(AnimalInteractablePosition) + AnimalCounter;
			int value = animalPosition.ManhattenDistance(another.AnimalInteractablePosition) + another.AnimalCounter;
			return num.CompareTo(value);
		}
		return another.FeederPriority.CompareTo(FeederPriority);
	}

	Vector2Int GetFeederTouchPosition(Room room, int animalWidth, Vector2Int animalPosition)
	{
		List<Vector2Int> list = new List<Vector2Int>();
		Vector2Int animalInteractablePosition = AnimalInteractablePosition;
		for (int i = -animalWidth; i < AnimalInteractableWidth; i++)
		{
			list.Add(new Vector2Int(animalInteractablePosition.x + i, animalInteractablePosition.y));
		}
		list.RemoveAll((Vector2Int pos) => !AnimalUtils.IsPositionWalkable(room, pos, animalWidth));
		if (list.Count == 0)
		{
			return animalInteractablePosition;
		}
		return list.ChoiceMin((Vector2Int pos) => pos.ManhattenDistance(animalPosition));
	}
}
