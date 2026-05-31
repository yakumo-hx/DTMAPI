using DolocTown.Config.Equipment;
using DolocTown.Config.Weather;

namespace DolocTown;

public abstract class WD_ScorchSun : WeatherDecorator
{
	public sealed override WeatherType WeatherType => WeatherType.SCORCH_SUN;

	protected WD_ScorchSun(Equipment.WeatherDecoratorManager parent, WeatherDecoratorProto proto)
		: base(parent, proto)
	{
	}

	protected WD_ScorchSun()
	{
	}
}
