using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(ParticleSystem))]
public class InstantParticleEffects : DolocRecyclableObject
{
	public ParticleSystem ps { get; private set; }

	public Action<InstantParticleEffects> recycle { get; set; }

	protected override void __Init()
	{
		base.__Init();
		ps = GetComponent<ParticleSystem>();
		ParticleSystem.MainModule main = ps.main;
		main.stopAction = ParticleSystemStopAction.Callback;
	}

	private void OnParticleSystemStopped()
	{
		recycle(this);
	}
}
