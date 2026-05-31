using System;
using DolocTown.Config.Weather;

namespace DolocTown.Weathers;

[AttributeUsage(AttributeTargets.Class)]
public class WeatherControllerAttribute : Attribute
{
	public readonly WeatherType type;

	public WeatherControllerAttribute(WeatherType type)
	{
		this.type = type;
	}
}
