using UnityEngine;

namespace DolocTown;

public class DayNightRenderSettings
{
	public float bloomThreshold;

	public float bloomIntensity;

	public float vignetteIntensity;

	public float vignetteSmoothness;

	public Color splitToningHighlight;

	public Color splitToningShadow;

	public float saturation;

	public DayNightRenderSettings(float bloomThreshold, float bloomIntensity, float vignetteIntensity, float vignetteSmoothness, Color splitToningHighlight, Color splitToningShadow, float saturation)
	{
		this.bloomThreshold = bloomThreshold;
		this.bloomIntensity = bloomIntensity;
		this.vignetteIntensity = vignetteIntensity;
		this.vignetteSmoothness = vignetteSmoothness;
		this.splitToningHighlight = splitToningHighlight;
		this.splitToningShadow = splitToningShadow;
		this.saturation = saturation;
	}
}
