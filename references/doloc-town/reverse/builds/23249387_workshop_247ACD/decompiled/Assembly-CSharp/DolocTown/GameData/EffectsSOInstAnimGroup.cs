using System;
using UnityEngine;

namespace DolocTown.GameData;

public class EffectsSOInstAnimGroup : EffectsSO
{
	[SerializeField]
	private InstAnimEffectType[] _instAnimGroup = Array.Empty<InstAnimEffectType>();

	public override void Raise(Vector2 ws)
	{
		InstAnimEffectType[] instAnimGroup = _instAnimGroup;
		foreach (InstAnimEffectType type in instAnimGroup)
		{
			DolocAPI.RaiseInstantAnimEffects(ws, type);
		}
	}

	public override void Raise(Vector2 ws, Vector2 dir)
	{
		InstAnimEffectType[] instAnimGroup = _instAnimGroup;
		foreach (InstAnimEffectType type in instAnimGroup)
		{
			DolocAPI.RaiseInstantAnimEffects(ws, type, dir);
		}
	}
}
