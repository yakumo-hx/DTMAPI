using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class WeatherRenderer_Cloudy : WeatherRenderer
{
	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	public override WeatherType WeatherType => WeatherType.CLOUDY;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	public override void SetEnable(bool value, float process, bool transit)
	{
	}
}
