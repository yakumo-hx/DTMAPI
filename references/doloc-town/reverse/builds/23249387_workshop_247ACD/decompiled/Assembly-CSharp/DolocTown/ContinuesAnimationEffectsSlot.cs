using UnityEngine;

namespace DolocTown;

public class ContinuesAnimationEffectsSlot
{
	private readonly ContinuesAnimEffectType _effectType;

	private ContinuesAnimationEffects _continuesAnimationEffects;

	public ContinuesAnimationEffects Effects
	{
		get
		{
			if (_continuesAnimationEffects == null)
			{
				_continuesAnimationEffects = DolocAPI.effectProvider.RaiseContinuesAnim(_effectType);
			}
			return _continuesAnimationEffects;
		}
	}

	public ContinuesAnimationEffectsSlot(ContinuesAnimEffectType type)
	{
		_effectType = type;
	}

	public void PlayRandom()
	{
		ContinuesAnimationEffects effects = Effects;
		if (!(effects == null))
		{
			effects.Play(_effectType.ToString().ToLower(), Random.value);
		}
	}

	public void Recycle()
	{
		if (!(_continuesAnimationEffects == null))
		{
			DolocAPI.effectProvider.RecycleContinuesAnim(_continuesAnimationEffects);
			_continuesAnimationEffects = null;
		}
	}
}
