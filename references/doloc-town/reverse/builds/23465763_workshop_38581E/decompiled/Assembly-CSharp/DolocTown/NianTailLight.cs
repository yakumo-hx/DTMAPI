using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class NianTailLight : DolocObject
{
	[SerializeField]
	private float shakeDuration = 0.3f;

	protected override void __Init()
	{
		base.__Init();
		Flicker(GetComponent<Light2D>());
	}

	public void OnReuse()
	{
		Flicker(GetComponent<Light2D>());
	}

	public void Flicker(Light2D light)
	{
		if (light == null)
		{
			return;
		}
		float originIntensity = light.intensity;
		Sequence sequence = DOTween.Sequence();
		sequence.Join(DOTween.To(() => light.intensity, delegate(float x)
		{
			light.intensity = x;
		}, 0f, shakeDuration).SetEase(Ease.OutQuad));
		sequence.Append(DOTween.To(() => light.intensity, delegate(float x)
		{
			light.intensity = x;
		}, originIntensity, shakeDuration).SetEase(Ease.InQuad));
		sequence.OnComplete(delegate
		{
			light.intensity = originIntensity;
			if (base.gameObject.activeSelf)
			{
				Flicker(light);
			}
		});
	}
}
