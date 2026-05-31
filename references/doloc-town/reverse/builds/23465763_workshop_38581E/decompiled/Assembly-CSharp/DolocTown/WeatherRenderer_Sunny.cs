using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class WeatherRenderer_Sunny : WeatherRenderer
{
	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	[SerializeField]
	private Color shadowColor;

	public override WeatherType WeatherType => WeatherType.SUNNY;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	public override void SetEnable(bool value, float process, bool transit)
	{
		WeatherRenderer.ControlGlobalCloudShadow(value, process, transit, WeatherType, shadowColor);
	}

	public override void UpdateStatus(float process, bool shouldTransit)
	{
		if (!shouldTransit)
		{
			DolocAPI.EnvCovariantController.CloudShadowController.UpdateCloudColorsIfNeed();
			DolocAPI.EnvCovariantController.CloudShadowController.RefreshPosition();
		}
	}
}
