using System;
using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOInstPSGroup : EffectsSO
{
	[SerializeField]
	private InstantParticleEffectsType[] _instPSGroup = Array.Empty<InstantParticleEffectsType>();

	public override void Raise(Vector2 ws)
	{
		InstantParticleEffectsType[] instPSGroup = _instPSGroup;
		foreach (InstantParticleEffectsType type in instPSGroup)
		{
			DolocAPI.RaiseInstantPSEffects(ws, type);
		}
	}

	public override void Raise(Vector2 ws, Vector2 ws2)
	{
		Raise(ws);
	}
}
