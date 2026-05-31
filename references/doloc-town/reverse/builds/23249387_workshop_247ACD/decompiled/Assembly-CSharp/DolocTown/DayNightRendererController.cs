using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DolocTown;

public class DayNightRendererController
{
	private Sun sun;

	private Volume volume;

	private Sequence ppmSequence;

	private Sequence sunSequence;

	public DayNightRendererController(Sun sun, Volume volume)
	{
		this.sun = sun;
		this.volume = volume;
	}

	public void TransitSun(Color sunColor, float sunIntensity, float transitDuration)
	{
		bool flag = transitDuration > 0f;
		sunSequence?.Kill();
		sunSequence = (flag ? DOTween.Sequence() : null);
		if (flag)
		{
			sunSequence.Join(DOTween.To(() => sun.color, delegate(Color x)
			{
				sun.color = x;
			}, sunColor, transitDuration));
			sunSequence.Join(DOTween.To(() => sun.intensity, delegate(float x)
			{
				sun.intensity = x;
			}, sunIntensity, transitDuration));
		}
		else
		{
			sun.color = sunColor;
			sun.intensity = sunIntensity;
		}
	}

	public void TransitPPM(DayNightRenderSettings settings, bool shouldTransit)
	{
		float duration = (shouldTransit ? 10f : 0f);
		ppmSequence?.Kill();
		ppmSequence = (shouldTransit ? DOTween.Sequence() : null);
		if (volume.profile.TryGet<Vignette>(out var vignette))
		{
			if (shouldTransit)
			{
				ppmSequence.Join(DOTween.To(() => vignette.intensity.value, delegate(float x)
				{
					vignette.intensity.value = x;
				}, settings.vignetteIntensity, duration));
				ppmSequence.Join(DOTween.To(() => vignette.smoothness.value, delegate(float x)
				{
					vignette.smoothness.value = x;
				}, settings.vignetteSmoothness, duration));
			}
			else
			{
				vignette.intensity.value = settings.vignetteIntensity;
				vignette.smoothness.value = settings.vignetteSmoothness;
			}
		}
		if (volume.profile.TryGet<SplitToning>(out var splitToning))
		{
			splitToning.shadows.overrideState = settings.splitToningShadow.a > 0f;
			splitToning.highlights.overrideState = settings.splitToningHighlight.a > 0f;
			splitToning.balance.overrideState = splitToning.shadows.overrideState ^ splitToning.highlights.overrideState;
			if (shouldTransit)
			{
				ppmSequence.Join(DOTween.To(() => splitToning.shadows.value, delegate(Color x)
				{
					splitToning.shadows.value = x;
				}, settings.splitToningShadow, duration));
				ppmSequence.Join(DOTween.To(() => splitToning.highlights.value, delegate(Color x)
				{
					splitToning.highlights.value = x;
				}, settings.splitToningHighlight, duration));
			}
			else
			{
				splitToning.shadows.value = settings.splitToningShadow;
				splitToning.highlights.value = settings.splitToningHighlight;
			}
		}
		if (volume.profile.TryGet<ColorAdjustments>(out var colorAdjustments))
		{
			if (shouldTransit)
			{
				ppmSequence.Join(DOTween.To(() => colorAdjustments.saturation.value, delegate(float x)
				{
					colorAdjustments.saturation.value = x;
				}, settings.saturation, duration));
			}
			else
			{
				colorAdjustments.saturation.value = settings.saturation;
			}
		}
		if (volume.profile.TryGet<Bloom>(out var bloom))
		{
			if (shouldTransit)
			{
				ppmSequence.Join(DOTween.To(() => bloom.threshold.value, delegate(float x)
				{
					bloom.threshold.value = x;
				}, settings.bloomThreshold, duration));
				ppmSequence.Join(DOTween.To(() => bloom.intensity.value, delegate(float x)
				{
					bloom.intensity.value = x;
				}, settings.bloomIntensity, duration));
			}
			else
			{
				bloom.threshold.value = settings.bloomThreshold;
				bloom.intensity.value = settings.bloomIntensity;
			}
		}
		ppmSequence?.OnComplete(delegate
		{
			ppmSequence = null;
		});
	}
}
