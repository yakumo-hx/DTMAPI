using UnityEngine;

namespace DolocTown;

public interface IMonsterInteractable
{
	void OnMonsterTouch(Vector2 interactPosition);

	void OnMonsterDisTouch(Vector2 interactPosition);
}
