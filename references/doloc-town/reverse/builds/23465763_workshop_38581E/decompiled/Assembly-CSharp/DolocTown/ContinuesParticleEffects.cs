using System;
using UnityEngine;

namespace DolocTown;

[RequireComponent(typeof(ParticleSystem))]
public class ContinuesParticleEffects : DolocRecyclableObject
{
	private ParticleSystem ps;

	private bool isRecycled;

	public Action<ContinuesParticleEffects> recycle { get; set; }

	protected override void __Init()
	{
		base.__Init();
		ps = GetComponent<ParticleSystem>();
		ParticleSystem.MainModule main = ps.main;
		main.playOnAwake = false;
		main.stopAction = ParticleSystemStopAction.Callback;
	}

	public override void OnRecycle()
	{
		base.OnRecycle();
		isRecycled = true;
		ps.Stop();
	}

	public override void OnCreated()
	{
		base.OnCreated();
		isRecycled = false;
	}

	public override void OnReuse()
	{
		base.OnReuse();
		isRecycled = false;
	}

	public void Play(float duration)
	{
		ps.Stop();
		ParticleSystem.MainModule main = ps.main;
		main.duration = duration;
		ps.Play();
	}

	public void Play()
	{
		ps.Stop();
		ParticleSystem.MainModule main = ps.main;
		main.loop = true;
		ps.Play();
	}

	public void Stop()
	{
		ps.Stop();
	}

	public void Recycle()
	{
		if (!isRecycled)
		{
			recycle?.Invoke(this);
		}
	}

	private void OnParticleSystemStopped()
	{
		Recycle();
	}
}
