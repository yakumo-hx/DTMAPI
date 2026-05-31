using UnityEngine;

namespace DolocTown;

public interface IAnimalInteractable
{
	Vector2Int AnimalInteractablePosition { get; }

	Vector2 AnimalInteractablePositionWS { get; }

	int AnimalInteractableWidth { get; }

	bool AnimalInteractableIsValid { get; }

	int AnimalCounter { get; set; }

	int Distance(Vector2Int target)
	{
		return Mathf.Abs(AnimalInteractablePosition.x - target.x) + Mathf.Abs(AnimalInteractablePosition.y - target.y);
	}
}
