using DolocTown.Config;
using DolocTown.Config.Weather;

namespace DolocTown.GameData;

public static class WeatherTypePatch
{
	public static bool IsMalignantWeather(this WeatherType weatherType)
	{
		return DolocConfig.Tables.TbWeather.IsMalignantWeather(weatherType);
	}

	public static bool IsRainyWeather(this WeatherType weatherType)
	{
		if (weatherType != WeatherType.RAIN)
		{
			return weatherType == WeatherType.THUNDERSTORM;
		}
		return true;
	}

	public static bool IsSunnyWeather(this WeatherType weatherType)
	{
		if (weatherType != WeatherType.SUNNY)
		{
			return weatherType == WeatherType.SCORCH_SUN;
		}
		return true;
	}
}
