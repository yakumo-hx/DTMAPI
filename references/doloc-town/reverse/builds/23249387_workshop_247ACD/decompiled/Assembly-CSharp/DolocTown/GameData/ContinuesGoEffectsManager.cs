using System;
using RedSaw;
using UnityEngine;

namespace DolocTown.GameData;

public class ContinuesGoEffectsManager
{
	private NashObjectPoolGroup<ContinuesGoEffectsType, ContinuesGoEffects> instPSGroup;

	public ContinuesGoEffectsManager(Transform container, int maxCount = 5)
	{
		CreateInstantPSGroup(container, maxCount);
	}

	public void Clear()
	{
		instPSGroup.RecycleAll();
	}

	private void CreateInstantPSGroup(Transform container, int maxCount)
	{
		instPSGroup = new NashObjectPoolGroup<ContinuesGoEffectsType, ContinuesGoEffects>(container, maxCount);
		foreach (object value in Enum.GetValues(typeof(ContinuesGoEffectsType)))
		{
			ContinuesGoEffectsType type = (ContinuesGoEffectsType)value;
			GameObject asset = DolocAPI.GetAsset<GameObject>("gm_effects_go_" + type.ToString().ToLower());
			NashObjectPool<ContinuesGoEffects> pool = instPSGroup.AppendPool(type, asset);
			pool.OnCreate = delegate(ContinuesGoEffects R)
			{
				R.recycle = pool.Recycle;
			};
		}
	}

	public ContinuesGoEffects Raise(Vector2 position, ContinuesGoEffectsType type, float duration = 10f)
	{
		ContinuesGoEffects continuesGoEffects = instPSGroup.Next(type);
		if ((object)continuesGoEffects != null)
		{
			continuesGoEffects.Play(position, duration);
			return continuesGoEffects;
		}
		return continuesGoEffects;
	}
}
