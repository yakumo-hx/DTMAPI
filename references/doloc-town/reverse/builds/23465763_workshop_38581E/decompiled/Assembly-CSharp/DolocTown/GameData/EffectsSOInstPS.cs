using System;
using System.Collections.Generic;
using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOInstPS : EffectsSO
{
	[SerializeField]
	private string typeName;

	private IEnumerable<string> availablePSNames => Enum.GetNames(typeof(InstantParticleEffectsType));

	public override void Raise(Vector2 ws)
	{
		if (Enum.TryParse<InstantParticleEffectsType>(typeName, ignoreCase: true, out var result))
		{
			DolocAPI.effectProvider.RaiseInstPS(ws, result);
		}
	}

	public override void Raise(Vector2 ws, Vector2 dir)
	{
		Raise(ws);
	}
}
