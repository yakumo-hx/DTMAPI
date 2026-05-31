using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOTwist : EffectsSO
{
	[SerializeField]
	[Range(0.1f, 5f)]
	private float duration = 1.5f;

	public override void Raise(Vector2 ws)
	{
		DolocAPI.RaiseScreenTwist(ws, duration);
	}

	public override void Raise(Vector2 ws, Vector2 dir)
	{
		DolocAPI.RaiseScreenTwist(ws, duration);
	}
}
