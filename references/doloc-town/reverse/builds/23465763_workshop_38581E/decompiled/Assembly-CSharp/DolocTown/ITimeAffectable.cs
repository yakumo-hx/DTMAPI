using DolocTown.Config.Weather;

namespace DolocTown;

public interface ITimeAffectable
{
	void InitTimeAffectable();

	void SetTimeInfo(float t, WeatherType weatherType, bool isInitial);

	void SetVisible(bool isVisible);
}
