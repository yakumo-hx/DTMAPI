using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;

namespace DolocTown;

public abstract class WD_AcidRain : WeatherDecorator
{
	public sealed override WeatherType WeatherType => WeatherType.ACID_RAIN;

	protected WD_AcidRain(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
	}

	protected WD_AcidRain()
	{
	}
}
