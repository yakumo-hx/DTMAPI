using DG.Tweening;
using DolocTown.Config.Settings;
using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class WeatherRenderer_ScorchSun : WeatherRenderer
{
	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	[SerializeField]
	private Color shadowColor;

	[SerializeField]
	private AnimationCurve shadowIntensityCurve;

	private GlobalFogMateiralProperties globalFogProperty;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	public override WeatherType WeatherType => WeatherType.SCORCH_SUN;

	protected override void __Init()
	{
		base.__Init();
		globalFogProperty = GetComponent<GlobalFogMateiralProperties>();
	}

	public override void SetEnable(bool value, float dayProcess, bool transit)
	{
		if (value)
		{
			float num = shadowIntensityCurve.Evaluate(dayProcess);
			DolocAPI.ppm.SetEnabled(PPTypes.HEATWAVE, DolocAPI.userSettings.GetOrDefault<bool>(UserSettingType.GRAPHICS_SCORCH_SUN_FILTER));
			DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value: true);
			globalFogProperty.ApplyMaterialProperties(DolocAPI.ppm.MaterialGlobalFog);
			if (transit)
			{
				DolocAPI.ppm.HeatWave.Play(num, 5f);
				DolocAPI.ppm.GlobalFog.Play(globalFogProperty.Intensity * num, 5f);
			}
			else
			{
				DolocAPI.ppm.HeatWave.value = num;
				DolocAPI.ppm.GlobalFog.value = globalFogProperty.Intensity * num;
			}
		}
		else if (transit)
		{
			DolocAPI.ppm.HeatWave.Play(0f, 5f, Ease.Linear, delegate
			{
				DolocAPI.ppm.SetEnabled(PPTypes.HEATWAVE, value: false);
			});
			DolocAPI.ppm.GlobalFog.Play(0f, 5f, Ease.Linear, delegate
			{
				DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value: false);
			});
		}
		else
		{
			DolocAPI.ppm.SetEnabled(PPTypes.HEATWAVE, value: false);
			DolocAPI.ppm.SetEnabled(PPTypes.GLOBALFOG, value: false);
		}
		WeatherRenderer.ControlGlobalCloudShadow(value, dayProcess, transit, WeatherType, shadowColor);
	}

	public override void UpdateStatus(float process, bool transit)
	{
		float num = shadowIntensityCurve.Evaluate(process);
		DolocAPI.ppm.GlobalFog.Play(num * globalFogProperty.Intensity, 5f);
		DolocAPI.ppm.HeatWave.Play(num, 5f);
	}
}
