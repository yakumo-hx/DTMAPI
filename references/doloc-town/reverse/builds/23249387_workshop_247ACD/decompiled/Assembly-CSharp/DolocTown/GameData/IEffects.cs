using UnityEngine;

namespace DolocTown.GameData;

public interface IEffects
{
	static IEffects Empty => new EmptyEffects();

	void Raise(Vector2 ws);

	void Raise(Vector2 ws, Vector2 dir);
}
