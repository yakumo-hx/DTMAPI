using UnityEngine;

namespace DolocTown;

public interface IAnimalPathFinder
{
	void OnEnvChanged();

	Vector2Int[] FindPath(Vector2Int from, Vector2Int to);
}
