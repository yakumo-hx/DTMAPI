using DolocTown.Config.Weather;
using DolocTown.GameData;
using ParadoxNotion.Design;
using UnityEngine;

namespace DolocTown.NodeCanvas;

[Name("天气过滤器", 0)]
[Color("c0edef")]
public class NpcScheduleNodeFilterWeather : NpcScheduleNodeFilter
{
	private enum WeatherCheckType
	{
		DIRECT,
		MALIGNANT,
		RAINY
	}

	[SerializeField]
	[ExposeField]
	private WeatherCheckType weatherCheckType;

	[SerializeField]
	[ExposeField]
	private string weatherTypeStr;

	[SerializeField]
	[ExposeField]
	private bool invertWeatherType;

	[SerializeField]
	[ExposeField]
	private bool isMalignantWeather;

	[SerializeField]
	[ExposeField]
	private bool isRainyWeather;

	private WeatherType WeatherType => weatherTypeStr.ConvertToEnumOrDefault<WeatherType>();

	public override bool IsMatch(NpcScheduleParams npcScheduleParams)
	{
		WeatherType weather = npcScheduleParams.weather;
		switch (weatherCheckType)
		{
		case WeatherCheckType.DIRECT:
			if (invertWeatherType)
			{
				return weather != WeatherType;
			}
			return weather == WeatherType;
		case WeatherCheckType.MALIGNANT:
			return weather.IsMalignantWeather() == isMalignantWeather;
		case WeatherCheckType.RAINY:
			return (weather.IsRainyWeather() || weather == WeatherType.ACID_RAIN) == isRainyWeather;
		default:
			return false;
		}
	}
}
