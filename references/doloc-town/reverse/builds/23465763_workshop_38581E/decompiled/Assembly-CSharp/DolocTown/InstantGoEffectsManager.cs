using System;
using RedSaw;
using UnityEngine;

namespace DolocTown;

public class InstantGoEffectsManager
{
	private NashObjectPoolGroup<InstantGoEffectsType, InstantGoEffects> instPSGroup;

	public InstantGoEffectsManager(Transform container, int maxCount = 5)
	{
		CreatePoolGroup(container, maxCount);
	}

	public void Clear()
	{
		instPSGroup.RecycleAll();
	}

	private void CreatePoolGroup(Transform container, int maxCount)
	{
		instPSGroup = new NashObjectPoolGroup<InstantGoEffectsType, InstantGoEffects>(container, maxCount);
		foreach (object value in Enum.GetValues(typeof(InstantGoEffectsType)))
		{
			InstantGoEffectsType type = (InstantGoEffectsType)value;
			GameObject asset = DolocAPI.GetAsset<GameObject>("gm_effects_go_" + type.ToString().ToLower());
			NashObjectPool<InstantGoEffects> pool = instPSGroup.AppendPool(type, asset);
			pool.OnCreate = delegate(InstantGoEffects R)
			{
				R.Recycle = pool.Recycle;
			};
		}
	}

	public InstantGoEffects GetInstantGoEffects(InstantGoEffectsType type)
	{
		return instPSGroup.Next(type);
	}

	public void Raise(Vector2 position, InstantGoEffectsType effectsType)
	{
		InstantGoEffects instantGoEffects = instPSGroup.Next(effectsType);
		if (!(instantGoEffects == null))
		{
			instantGoEffects.transform.position = position;
			instantGoEffects.transform.rotation = Quaternion.identity;
			instantGoEffects.Raise();
		}
	}

	public void Raise(Vector2 position, InstantGoEffectsType effectsType, Vector2 dir)
	{
		InstantGoEffects instantGoEffects = instPSGroup.Next(effectsType);
		if (!(instantGoEffects == null))
		{
			instantGoEffects.transform.position = position;
			instantGoEffects.transform.rotation = dir.GetRotation();
			instantGoEffects.Raise();
		}
	}
}
