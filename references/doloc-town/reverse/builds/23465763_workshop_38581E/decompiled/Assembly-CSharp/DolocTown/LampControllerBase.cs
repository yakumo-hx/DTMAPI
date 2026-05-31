using DG.Tweening;
using DolocTown.Config.Equipment;
using UnityEngine;

namespace DolocTown;

public abstract class LampControllerBase
{
	public readonly LampInfo lampInfo;

	protected readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

	protected Tween noiseTween;

	protected virtual Sprite EmissionSprite => lampInfo.EmissionSpriteAsset.Asset;

	public LampControllerBase(LampInfo lampInfo)
	{
		this.lampInfo = lampInfo;
	}

	public void StopTween()
	{
		if (noiseTween != null)
		{
			noiseTween.Kill();
			noiseTween = null;
		}
	}

	public abstract void ToggleLight(bool value);

	public abstract void ToggleLight(bool value, float time);

	protected abstract void ToggleMat(bool value);
}
