using System;
using UnityEngine;

namespace DolocTown.GameData;

[Serializable]
public abstract class EffectsSO : IEffects
{
	public abstract void Raise(Vector2 ws);

	public abstract void Raise(Vector2 ws, Vector2 dir);
}
