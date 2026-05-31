using System;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class InstantParticleEffectsManager
{
	private readonly string defaultSortingLayer;

	private NashObjectPoolGroup<InstantParticleEffectsType, InstantParticleEffects> instPSGroup;

	public InstantParticleEffectsManager(Transform container, int maxCount = 5, string defaultLayer = "GroundFront")
	{
		CreateInstantPSGroup(container, maxCount);
		defaultSortingLayer = defaultLayer;
	}

	public void Clear()
	{
		instPSGroup.RecycleAll();
	}

	private void CreateInstantPSGroup(Transform container, int maxCount)
	{
		instPSGroup = new NashObjectPoolGroup<InstantParticleEffectsType, InstantParticleEffects>(container, maxCount);
		foreach (object value in Enum.GetValues(typeof(InstantParticleEffectsType)))
		{
			InstantParticleEffectsType type = (InstantParticleEffectsType)value;
			GameObject asset = DolocAPI.GetAsset<GameObject>("gm_effects_ps_" + type.ToString().ToLower());
			NashObjectPool<InstantParticleEffects> pool = instPSGroup.AppendPool(type, asset);
			pool.OnCreate = delegate(InstantParticleEffects R)
			{
				R.recycle = pool.Recycle;
			};
		}
	}

	public void Raise(Vector2 position, InstantParticleEffectsType type)
	{
		InstantParticleEffects instantParticleEffects = instPSGroup.Next(type);
		if (instantParticleEffects != null)
		{
			instantParticleEffects.transform.position = position;
			ParticleSystemRenderer component = instantParticleEffects.ps.GetComponent<ParticleSystemRenderer>();
			component.sortingLayerName = defaultSortingLayer;
			component.sortingOrder = 1;
			instantParticleEffects.ps.Play();
		}
	}

	public void Raise(Vector2 position, InstantParticleEffectsType type, string sortingLayer, int sortingOrder)
	{
		InstantParticleEffects instantParticleEffects = instPSGroup.Next(type);
		if (!(instantParticleEffects == null))
		{
			instantParticleEffects.transform.position = position;
			ParticleSystemRenderer component = instantParticleEffects.ps.GetComponent<ParticleSystemRenderer>();
			component.sortingLayerName = sortingLayer;
			component.sortingOrder = sortingOrder;
			instantParticleEffects.ps.Play();
		}
	}
}
