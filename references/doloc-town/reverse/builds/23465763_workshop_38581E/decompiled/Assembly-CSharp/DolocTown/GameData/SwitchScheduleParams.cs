using DolocTown.Config.Weather;

namespace DolocTown.GameData;

public struct SwitchScheduleParams
{
	public readonly DateInfo date;

	public readonly WeatherType weather;

	public SwitchScheduleParams(DateInfo date, WeatherType weather)
	{
		this.date = date;
		this.weather = weather;
	}
}
