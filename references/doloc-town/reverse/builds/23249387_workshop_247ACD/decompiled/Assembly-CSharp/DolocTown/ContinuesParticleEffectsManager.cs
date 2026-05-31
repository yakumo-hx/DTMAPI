using System;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class ContinuesParticleEffectsManager
{
	private NashObjectPoolGroup<ContinuesParticleEffectsType, ContinuesParticleEffects> instPSGroup;

	private bool isClear = true;

	public ContinuesParticleEffectsManager(Transform container, int maxCount = 5)
	{
		CreateInstantPSGroup(container, maxCount);
	}

	private void CreateInstantPSGroup(Transform container, int maxCount)
	{
		instPSGroup = new NashObjectPoolGroup<ContinuesParticleEffectsType, ContinuesParticleEffects>(container, maxCount);
		foreach (object value in Enum.GetValues(typeof(ContinuesParticleEffectsType)))
		{
			ContinuesParticleEffectsType type = (ContinuesParticleEffectsType)value;
			GameObject asset = DolocAPI.GetAsset<GameObject>("gm_effects_psc_" + type.ToString().ToLower());
			if (!(asset == null))
			{
				NashObjectPool<ContinuesParticleEffects> pool = instPSGroup.AppendPool(type, asset);
				pool.OnCreate = delegate(ContinuesParticleEffects R)
				{
					R.recycle = pool.Recycle;
				};
			}
		}
	}

	public ContinuesParticleEffects Raise(Vector2 position, ContinuesParticleEffectsType type, float duration)
	{
		ContinuesParticleEffects continuesParticleEffects = instPSGroup.Next(type);
		if (continuesParticleEffects != null)
		{
			if (isClear)
			{
				isClear = false;
			}
			continuesParticleEffects.transform.position = position;
			continuesParticleEffects.Play(duration);
		}
		return continuesParticleEffects;
	}

	public ContinuesParticleEffects Raise(Vector2 position, ContinuesParticleEffectsType type)
	{
		ContinuesParticleEffects continuesParticleEffects = instPSGroup.Next(type);
		if (continuesParticleEffects == null)
		{
			return null;
		}
		if (isClear)
		{
			isClear = false;
		}
		continuesParticleEffects.transform.position = position;
		continuesParticleEffects.Play();
		return continuesParticleEffects;
	}

	public void Clear()
	{
		if (!isClear)
		{
			isClear = true;
			Debug.Log("清空持续特效");
			instPSGroup.RecycleAll();
		}
	}
}
