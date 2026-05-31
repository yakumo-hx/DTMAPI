using Newtonsoft.Json;

namespace DolocTown.Weathers;

[JsonObject(MemberSerialization.OptIn)]
public abstract class WeatherController
{
	public virtual void OnStart()
	{
	}

	public virtual void OnStop()
	{
	}

	public virtual void UpdatePerTU(bool shouldRender)
	{
	}
}
