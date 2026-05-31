using DolocTown.Config.Weather;
using DolocTown.GameData;
using UnityEngine;

namespace DolocTown;

public class WeatherRenderer_None : WeatherRenderer
{
	[SerializeField]
	private WeatherRenderInfoSO renderInfo;

	public override WeatherType WeatherType => WeatherType.NONE;

	public override WeatherRenderInfoSO RenderInfo => renderInfo;

	public override void SetEnable(bool value, float p, bool transit)
	{
	}
}
