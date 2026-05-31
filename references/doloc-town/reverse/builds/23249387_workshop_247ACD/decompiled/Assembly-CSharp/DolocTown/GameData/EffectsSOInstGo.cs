using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOInstGo : EffectsSO
{
	[SerializeField]
	private InstantGoEffectsType _instGoType;

	public override void Raise(Vector2 ws)
	{
		DolocAPI.RaiseInstantGoEffects(ws, _instGoType);
	}

	public override void Raise(Vector2 ws, Vector2 dir)
	{
		DolocAPI.RaiseInstantGoEffects(ws, _instGoType, dir);
	}
}
